using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Helpers;
using Common.Helpers.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Calculators;
using Portal.Configuration;
using Portal.Models;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class HistoricalTasksModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDmEndDateCalculator _dmEndDateCalculator;
        private readonly IMapper _mapper;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<HistoricalTasksModel> _logger;

        [BindProperty(SupportsGet = true)]
        public HistoricalTasksSearchParameters SearchParameters { get; set; }

        public List<HistoricalTasksData> HistoricalTasks { get; set; }

        public List<string> ErrorMessages { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;
        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetailsAsync(this.User).Result;
                return _currentUser;
            }
        }

        public HistoricalTasksModel(
                                    WorkflowDbContext dbContext,
                                    IDmEndDateCalculator dmEndDateCalculator,
                                    IMapper mapper,
                                    IOptions<GeneralConfig> generalConfig,
                                    IAdDirectoryService adDirectoryService,
                                    ILogger<HistoricalTasksModel> logger)
        {
            _dbContext = dbContext;
            _dmEndDateCalculator = dmEndDateCalculator;
            _mapper = mapper;
            _generalConfig = generalConfig;
            _adDirectoryService = adDirectoryService;
            _logger = logger;

            ErrorMessages = new List<string>();
            HistoricalTasks = new List<HistoricalTasksData>();
        }

        public async Task OnGetAsync()
        {
            LogContext.PushProperty("ActivityName", "HistoricalTasks");
            LogContext.PushProperty("PortalResource", nameof(OnGetAsync));
            LogContext.PushProperty(" CurrentUser.DisplayName", CurrentUser.DisplayName);

            _logger.LogInformation("Entering Get initial Historical Tasks");

            List<WorkflowInstance> workflows = null;
            try
            {
                workflows = await _dbContext.WorkflowInstance
                    .Include(a => a.AssessmentData)
                    .Include(d => d.DbAssessmentReviewData)
                    .Include(vd => vd.DbAssessmentVerifyData)
                    .Where(wi =>
                        wi.Status == WorkflowStatus.Completed.ToString() ||
                        wi.Status == WorkflowStatus.Terminated.ToString())
                    .OrderByDescending(wi => wi.ActivityChangedAt)
                    .Take(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords)
                    .ToListAsync();


                _logger.LogInformation("Successfully returned initial data from database");
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"Error occurred while getting Historical Tasks from database: {e.Message}");
                _logger.LogError("Error occurred while getting Historical Tasks from database", e);
            }

            if (workflows != null && workflows.Count > 0)
            {
                try
                {
                    HistoricalTasks = PopulateHistoricalTasks(workflows);
                    _logger.LogInformation("Successfully populated Historical Tasks from initial data");

                }
                catch (Exception e)
                {
                    ErrorMessages.Add($"Error occurred while populating Historical Tasks from initial data: {e.Message}");
                    _logger.LogError("Error occurred while populating Historical Tasks from initial data", e);

                }
            }
        }

        public async Task OnPostAsync()
        {
            LogContext.PushProperty("ActivityName", "HistoricalTasks");
            LogContext.PushProperty("PortalResource", nameof(OnPostAsync));
            LogContext.PushProperty("HistoricalTasksSearchParameters", SearchParameters.ToJSONSerializedString());
            LogContext.PushProperty(" CurrentUser.DisplayName", CurrentUser.DisplayName);

            _logger.LogInformation("Entering Get filtered Historical Tasks with parameters {HistoricalTasksSearchParameters}");

            List<WorkflowInstance> workflows = null;

            try
            {
                workflows = await _dbContext.WorkflowInstance
                    .Include(a => a.AssessmentData)
                    .Include(d => d.DbAssessmentReviewData)
                    .Include(vd => vd.DbAssessmentVerifyData)
                    .Where(wi =>
                        (wi.Status == WorkflowStatus.Completed.ToString() || wi.Status == WorkflowStatus.Terminated.ToString())
                        && (
                            (!SearchParameters.ProcessId.HasValue || wi.ProcessId == SearchParameters.ProcessId.Value)
                            && (!SearchParameters.SourceDocumentId.HasValue || wi.AssessmentData.PrimarySdocId == SearchParameters.SourceDocumentId.Value)
                            && (string.IsNullOrWhiteSpace(SearchParameters.RsdraNumber) || wi.AssessmentData.RsdraNumber.ToUpper().Contains(SearchParameters.RsdraNumber.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.SourceDocumentName) || wi.AssessmentData.SourceDocumentName.ToUpper().Contains(SearchParameters.SourceDocumentName.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.Reviewer)
                                    || (wi.ActivityName == WorkflowStage.Review.ToString() ?
                                             wi.DbAssessmentReviewData.Reviewer.ToUpper().Contains(SearchParameters.Reviewer.ToUpper())
                                             : wi.DbAssessmentVerifyData.Reviewer.ToUpper().Contains(SearchParameters.Reviewer.ToUpper())))
                            && (string.IsNullOrWhiteSpace(SearchParameters.Assessor)
                                || (wi.ActivityName == WorkflowStage.Review.ToString() ?
                                    wi.DbAssessmentReviewData.Assessor.ToUpper().Contains(SearchParameters.Assessor.ToUpper())
                                    : wi.DbAssessmentVerifyData.Assessor.ToUpper().Contains(SearchParameters.Assessor.ToUpper())))
                            && (string.IsNullOrWhiteSpace(SearchParameters.Verifier)
                                || (wi.ActivityName == WorkflowStage.Review.ToString() ?
                                    wi.DbAssessmentReviewData.Verifier.ToUpper().Contains(SearchParameters.Verifier.ToUpper())
                                    : wi.DbAssessmentVerifyData.Verifier.ToUpper().Contains(SearchParameters.Verifier.ToUpper())))

                            ))
                    .OrderByDescending(wi => wi.ActivityChangedAt)
                    .Take(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords)
                    .ToListAsync();

                _logger.LogInformation("Successfully returned filtered data from database");

            }
            catch (Exception e)
            {
                ErrorMessages.Add($"Error occurred while getting filtered Historical Tasks from database: {e.Message}");
                _logger.LogError("Error occurred while getting filtered Historical Tasks from database with parameters {HistoricalTasksSearchParameters}", e);

            }

            if (workflows != null && workflows.Count > 0)
            {
                try
                {

                    HistoricalTasks = PopulateHistoricalTasks(workflows);
                    _logger.LogInformation("Successfully populated Historical Tasks from filtered data");

                }
                catch (Exception e)
                {
                    ErrorMessages.Add($"Error occurred while populating Historical Tasks from filtered data: {e.Message}");
                    _logger.LogError("Error occurred while populating Historical Tasks from filtered data with parameters {HistoricalTasksSearchParameters}", e);

                }
            }
        }

        private List<HistoricalTasksData> PopulateHistoricalTasks(List<WorkflowInstance> workflows)
        {
            var historicalTasks = _mapper.Map<List<WorkflowInstance>, List<HistoricalTasksData>>(workflows);

            foreach (var instance in workflows)
            {
                var task = historicalTasks.First(t => t.ProcessId == instance.ProcessId);
                SetUsersOnTask(instance, task);

                var taskType = GetTaskType(instance, task);

                if (instance.AssessmentData.EffectiveStartDate.HasValue)
                {
                    var result = _dmEndDateCalculator.CalculateDmEndDate(
                        instance.AssessmentData.EffectiveStartDate.Value,
                        taskType,
                        instance.ActivityName);

                    task.DmEndDate = result.dmEndDate;
                }
            }

            return historicalTasks;
        }

        private void SetUsersOnTask(WorkflowInstance instance, HistoricalTasksData task)
        {
            switch (task.TaskStage)
            {
                case WorkflowStage.Review:
                    task.Reviewer = instance.DbAssessmentReviewData.Reviewer;
                    task.Assessor = instance.DbAssessmentReviewData.Assessor;
                    task.Verifier = instance.DbAssessmentReviewData.Verifier;
                    break;
                case WorkflowStage.Verify:
                    task.Reviewer = instance.DbAssessmentVerifyData.Reviewer;
                    task.Assessor = instance.DbAssessmentVerifyData.Assessor;
                    task.Verifier = instance.DbAssessmentVerifyData.Verifier;
                    break;
                default:
                    throw new NotImplementedException($"{task.TaskStage} is not implemented.");
            }
        }

        private string GetTaskType(WorkflowInstance instance, HistoricalTasksData task)
        {
            switch (task.TaskStage)
            {
                case WorkflowStage.Review:
                    return instance.DbAssessmentReviewData.TaskType;
                case WorkflowStage.Verify:
                    return instance.DbAssessmentVerifyData.TaskType;
                default:
                    throw new NotImplementedException($"'{instance.ActivityName}' not implemented");
            }
        }

    }
}
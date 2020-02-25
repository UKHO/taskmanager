using AutoMapper;
using Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Auth;
using Portal.Configuration;
using Portal.Helpers;
using Portal.ViewModels;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;

        private readonly IMapper _mapper;
        private readonly IUserIdentityService _userIdentityService;
        private readonly IDirectoryService _directoryService;
        private readonly ILogger<IndexModel> _logger;
        private readonly IIndexFacade _indexFacade;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        [BindProperty(SupportsGet = true)]
        public IList<TaskViewModel> Tasks { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        public IndexModel(WorkflowDbContext dbContext,
            IMapper mapper,
            IUserIdentityService userIdentityService,
            IDirectoryService directoryService,
            ILogger<IndexModel> logger,
            IIndexFacade indexFacade,
            ICarisProjectHelper carisProjectHelper,
            IOptions<GeneralConfig> generalConfig)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _userIdentityService = userIdentityService;
            _directoryService = directoryService;
            _logger = logger;
            _indexFacade = indexFacade;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGetAsync()
        {
            var workflows = await _dbContext.WorkflowInstance
                .Include(c => c.Comments)
                .Include(a => a.AssessmentData)
                .Include(d => d.DbAssessmentReviewData)
                .Include(ad => ad.DbAssessmentAssessData)
                .Include(vd => vd.DbAssessmentVerifyData)
                .Include(t => t.TaskNote)
                .Include(o => o.OnHold)
                .Where(wi => wi.Status == WorkflowStatus.Started.ToString())
                .OrderBy(wi => wi.ProcessId)
                .ToListAsync();

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            Tasks = _mapper.Map<List<WorkflowInstance>, List<TaskViewModel>>(workflows);

            foreach (var instance in workflows)
            {
                var task = Tasks.First(t => t.ProcessId == instance.ProcessId);

                task.IsOnHold = instance.OnHold.Any(r => r.OffHoldTime == null);
                task.OnHoldDays = _indexFacade.CalculateOnHoldDays(instance.OnHold);

                var taskType = GetTaskType(instance);

                var result = _indexFacade.CalculateDmEndDate(
                                                                            instance.AssessmentData.EffectiveStartDate.Value,
                                                                            taskType,
                                                                            instance.ActivityName,
                                                                            instance.OnHold);
                task.DmEndDate = result.dmEndDate;
                task.DaysToDmEndDate = result.daysToDmEndDate;

                var alerts = _indexFacade.DetermineDaysToDmEndDateAlerts(task.DaysToDmEndDate);
                task.DaysToDmEndDateAmberAlert = alerts.amberAlert;
                task.DaysToDmEndDateRedAlert = alerts.redAlert;

                SetUsersOnTask(instance, task);
            }
        }

        private string GetTaskType(WorkflowInstance instance)
        {
            switch (instance.ActivityName)
            {
                case "Review":
                    return instance.DbAssessmentReviewData.TaskType;
                case "Assess":
                    return instance.DbAssessmentAssessData.TaskType;
                case "Verify":
                    return instance.DbAssessmentVerifyData.TaskType;
                default:
                    throw new NotImplementedException($"'{instance.ActivityName}' not implemented");
            }
        }

        public async Task<IActionResult> OnPostTaskNoteAsync(string taskNote, int processId)
        {
            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            taskNote = string.IsNullOrEmpty(taskNote) ? string.Empty : taskNote.Trim();

            var existingTaskNote = await _dbContext.TaskNote.FirstOrDefaultAsync(tn => tn.ProcessId == processId);

            if (existingTaskNote == null)
            {
                if (!string.IsNullOrEmpty(taskNote))
                {
                    var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.ProcessId == processId);

                    await _dbContext.TaskNote.AddAsync(new TaskNote()
                    {
                        WorkflowInstanceId = workflowInstance.WorkflowInstanceId,
                        ProcessId = processId,
                        Text = taskNote,
                        Created = DateTime.Now,
                        CreatedByUsername = UserFullName,
                        LastModified = DateTime.Now,
                        LastModifiedByUsername = UserFullName,
                    });
                    await _dbContext.SaveChangesAsync();
                }

                await OnGetAsync();
                return Page();
            }

            existingTaskNote.Text = taskNote;
            existingTaskNote.LastModified = DateTime.Now;
            existingTaskNote.LastModifiedByUsername = UserFullName;
            await _dbContext.SaveChangesAsync();

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAssignTaskToUserAsync(int processId, string userName, string taskStage)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("ActivityName", taskStage);
            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);
            LogContext.PushProperty("UserFullName", UserFullName);
            LogContext.PushProperty("AssignedUser", userName);

            ValidationErrorMessages.Clear();

            if (!await _userIdentityService.ValidateUser(userName))
            {
                _logger.LogInformation("Attempted to assign task to unknown user {AssignedUser}");
                ValidationErrorMessages.Add($"Unable to assign task to unknown user {userName}");
            }

            var instance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.ProcessId == processId);
            if (instance.ActivityName != taskStage)
            {
                _logger.LogInformation("Attempted to assign task with ProcessId: {ProcessId} to a user but the task being assigned is no longer at the expected step of {ActivityName}.");
                ValidationErrorMessages.Add($"Unable to assign task to {userName} because task with ProcessId: {processId} is not at expected step {taskStage}.");
            }

            if (ValidationErrorMessages.Any())
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            var isUpdateCarisProject = true;

            switch (taskStage)
            {
                case "Review":
                    var review = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);
                    review.Reviewer = userName;
                    isUpdateCarisProject = false;
                    break;
                case "Assess":
                    var assess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);
                    assess.Assessor = userName;
                    break;
                case "Verify":
                    throw new NotImplementedException($"'{taskStage}' not implemented");
                // TODO: implement Verify Data
                //var verify = await _dbContext.DbAssessmentVerifyData.FirstAsync(r => r.ProcessId == processId);
                //verify.Verifier = userName;
                default:
                    throw new NotImplementedException($"'{taskStage}' not implemented");
            }

            await _dbContext.SaveChangesAsync();

            if (isUpdateCarisProject)
            {
                await UpdateCarisProjectWithAdditionalUser(processId, userName);

                if (ValidationErrorMessages.Any())
                {
                    return new JsonResult(this.ValidationErrorMessages)
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }

            }

            return StatusCode(200);
        }


        public async Task<JsonResult> OnGetUsersAsync()
        {
            return new JsonResult(await _directoryService.GetGroupMembers());
        }

        private void SetUsersOnTask(WorkflowInstance instance, TaskViewModel task)
        {
            switch (task.TaskStage)
            {
                case "Review":
                    task.Reviewer = instance.DbAssessmentReviewData.Reviewer;
                    task.Assessor = instance.DbAssessmentReviewData.Assessor;
                    task.Verifier = instance.DbAssessmentReviewData.Verifier;
                    break;
                case "Assess":
                    task.Reviewer = instance.DbAssessmentAssessData.Reviewer;
                    task.Assessor = instance.DbAssessmentAssessData.Assessor;
                    task.Verifier = instance.DbAssessmentAssessData.Verifier;
                    break;
                case "Verify":
                    task.Reviewer = instance.DbAssessmentVerifyData.Reviewer;
                    task.Assessor = instance.DbAssessmentVerifyData.Assessor;
                    task.Verifier = instance.DbAssessmentVerifyData.Verifier;
                    break;
                default:
                    throw new NotImplementedException($"{task.TaskStage} is not implemented.");
            }
        }

        private async Task UpdateCarisProjectWithAdditionalUser(int processId, string userName)
        {
            var carisProjectDetails =
                await _dbContext.CarisProjectDetails.SingleOrDefaultAsync(cp => cp.ProcessId == processId);

            if (carisProjectDetails == null)
            {
                return;
            }

            HpdUser hpdUsername = null;
            try
            {
                 hpdUsername = await GetHpdUser(userName);  // which will also implicitly validate if the other user has been mapped to HPD account in our database

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to assign {userName} to Caris project: {carisProjectDetails.ProjectId}");
                ValidationErrorMessages.Add($"Failed to assign {userName} to Caris project: {carisProjectDetails.ProjectId}. {e.Message}");

                return;
            }
            LogContext.PushProperty("CarisProjectId", carisProjectDetails.ProjectId);
            LogContext.PushProperty("HpdUsername", hpdUsername.HpdUsername);

            try
            {
                await _carisProjectHelper.UpdateCarisProject(carisProjectDetails.ProjectId, hpdUsername.HpdUsername,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to assign {userName} ({hpdUsername.HpdUsername}) to Caris project: {carisProjectDetails.ProjectId}");
                ValidationErrorMessages.Add($"Failed to assign {userName} ({hpdUsername.HpdUsername}) to Caris project: {carisProjectDetails.ProjectId}. {e.Message}");
            }
        }


        private async Task<HpdUser> GetHpdUser(string username)
        {
            try
            {
                return await _dbContext.HpdUser.SingleAsync(u => u.AdUsername.Equals(username,
                    StringComparison.InvariantCultureIgnoreCase));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserFullName} in our system.");
                throw new InvalidOperationException($"Unable to find HPD username for {username}  in our system.",
                    ex.InnerException);
            }

        }

    }
}

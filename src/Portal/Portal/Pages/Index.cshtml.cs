using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Common.Helpers;
using Common.Helpers.Auth;
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
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;

        private readonly IMapper _mapper;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly IPortalUserDbService _portalUserDbService;
        private readonly ILogger<IndexModel> _logger;
        private readonly IIndexFacade _indexFacade;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;

        private (string DisplayName, string UserPrincipalName) _currentUser;
        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        [BindProperty(SupportsGet = true)]
        public IList<TaskViewModel> Tasks { get; set; }

        public List<string> TeamList { get; set; }
        public string TeamsUnassigned { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        public IndexModel(WorkflowDbContext dbContext,
            IMapper mapper,
            IAdDirectoryService adDirectoryService,
            IPortalUserDbService portalUserDbService,
            ILogger<IndexModel> logger,
            IIndexFacade indexFacade,
            ICarisProjectHelper carisProjectHelper,
            IOptions<GeneralConfig> generalConfig)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _adDirectoryService = adDirectoryService;
            _portalUserDbService = portalUserDbService;
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

            Tasks = _mapper.Map<List<WorkflowInstance>, List<TaskViewModel>>(workflows);

            foreach (var instance in workflows)
            {
                var task = Tasks.First(t => t.ProcessId == instance.ProcessId);

                task.IsOnHold = instance.OnHold.Any(r => r.OffHoldTime == null);
                task.OnHoldDays = _indexFacade.CalculateOnHoldDays(instance.OnHold);

                var (greenIcon, amberIcon, redIcon) = _indexFacade.DetermineOnHoldDaysIcons(task.OnHoldDays);
                task.OnHoldDaysGreen = greenIcon;
                task.OnHoldDaysAmber = amberIcon;
                task.OnHoldDaysRed = redIcon;

                var taskType = GetTaskType(instance);

                if (instance.AssessmentData?.EffectiveStartDate != null)
                {
                    var result = _indexFacade.CalculateDmEndDate(
                        instance.AssessmentData.EffectiveStartDate.Value,
                        taskType,
                        instance.ActivityName,
                        instance.OnHold);
                    task.DmEndDate = result.dmEndDate;
                    task.DaysToDmEndDate = result.daysToDmEndDate;

                    var alerts = _indexFacade.DetermineDaysToDmEndDateAlerts(task.DaysToDmEndDate.Value);
                    task.DaysToDmEndDateGreenAlert = alerts.greenAlert;
                    task.DaysToDmEndDateAmberAlert = alerts.amberAlert;
                    task.DaysToDmEndDateRedAlert = alerts.redAlert;
                }

                SetUsersOnTask(instance, task);
            }

            TeamList = _generalConfig.Value.GetTeams().ToList();
            TeamsUnassigned = _generalConfig.Value.TeamsUnassigned;
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
            taskNote = string.IsNullOrEmpty(taskNote) ? string.Empty : taskNote.Trim();

            var existingTaskNote = await _dbContext.TaskNote.FirstOrDefaultAsync(tn => tn.ProcessId == processId);

            var currentAdUser = await _portalUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

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
                        CreatedBy = currentAdUser,
                        LastModified = DateTime.Now,
                        LastModifiedBy = currentAdUser,
                    });
                    await _dbContext.SaveChangesAsync();
                }

                await OnGetAsync();
                return Page();
            }

            existingTaskNote.Text = taskNote;
            existingTaskNote.LastModified = DateTime.Now;
            existingTaskNote.LastModifiedBy = currentAdUser;
            await _dbContext.SaveChangesAsync();

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAssignTaskToUserAsync(int processId, string userPrincipalName, string taskStage)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("ActivityName", taskStage);
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);
            LogContext.PushProperty("AssignedUser", userPrincipalName);

            ValidationErrorMessages.Clear();

            if (!await _portalUserDbService.ValidateUserAsync(userPrincipalName))
            {
                _logger.LogInformation("Attempted to assign task to unknown user {AssignedUser}");
                ValidationErrorMessages.Add($"Unable to assign task to unknown user {userPrincipalName}");
            }

            var instance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.ProcessId == processId);
            if (instance.ActivityName != taskStage)
            {
                _logger.LogInformation("Attempted to assign task with ProcessId: {ProcessId} to a user but the task being assigned is no longer at the expected step of {ActivityName}.");
                ValidationErrorMessages.Add($"Unable to assign task to {userPrincipalName} because task with ProcessId: {processId} is not at expected step {taskStage}.");
            }

            if (ValidationErrorMessages.Any())
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            var isUpdateCarisProject = true;

            var adUser = await _portalUserDbService.GetAdUserAsync(userPrincipalName);

            switch (taskStage)
            {
                case "Review":
                    var review = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);
                    review.Reviewer = adUser;
                    isUpdateCarisProject = false;
                    break;
                case "Assess":
                    var assess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);
                    assess.Assessor = adUser;
                    break;
                case "Verify":
                    var verify = await _dbContext.DbAssessmentVerifyData.FirstAsync(r => r.ProcessId == processId);
                    verify.Verifier = adUser;
                    break;
                default:
                    throw new NotImplementedException($"'{taskStage}' not implemented");
            }

            await _dbContext.SaveChangesAsync();

            if (isUpdateCarisProject)
            {
                await UpdateCarisProjectWithAdditionalUser(processId, adUser);

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
            var users =
                (await _portalUserDbService.GetUsersFromDbAsync()).Select(u => new
                {
                    u.DisplayName,
                    u.UserPrincipalName
                });

            return new JsonResult(users);
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

        private async Task UpdateCarisProjectWithAdditionalUser(int processId, AdUser user)
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
                hpdUsername = await GetHpdUser(user.UserPrincipalName);  // which will also implicitly validate if the other user has been mapped to HPD account in our database

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to assign {user.DisplayName} - {user.UserPrincipalName} to Caris project: {carisProjectDetails.ProjectId}");
                ValidationErrorMessages.Add($"Failed to assign {user.DisplayName} to Caris project: {carisProjectDetails.ProjectId}. {e.Message}");

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
                _logger.LogError(e, $"Failed to assign {user.UserPrincipalName} ({hpdUsername.HpdUsername}) to Caris project: {carisProjectDetails.ProjectId}");
                ValidationErrorMessages.Add($"Failed to assign {user.DisplayName} ({hpdUsername.HpdUsername}) to Caris project: {carisProjectDetails.ProjectId}. {e.Message}");
            }
        }


        private async Task<HpdUser> GetHpdUser(string userPrincipalName)
        {
            try
            {
                return await _dbContext.HpdUser.SingleAsync(u => u.AdUser.UserPrincipalName.Equals(userPrincipalName,
                    StringComparison.InvariantCultureIgnoreCase));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserPrincipalName} in our system.");
                throw new InvalidOperationException($"Unable to find HPD username for {userPrincipalName}  in our system.",
                    ex.InnerException);
            }

        }

    }
}

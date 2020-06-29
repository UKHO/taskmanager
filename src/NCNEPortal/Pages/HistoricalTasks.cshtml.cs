using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Helpers.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using NCNEPortal.Models;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Serilog.Context;

namespace NCNEPortal.Pages
{
    public class HistoricalTasksModel : PageModel
    {
        private readonly NcneWorkflowDbContext _dbContext;

        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<HistoricalTasksModel> _logger;

        [BindProperty(SupportsGet = true)]
        public HistoricalTasksSearchParameters SearchParameters { get; set; }

        public List<string> ErrorMessages { get; set; }
        public List<TaskInfo> NcneTasks { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;

        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        public HistoricalTasksModel(NcneWorkflowDbContext dbContext,
            IAdDirectoryService adDirectoryService,
            IOptions<GeneralConfig> generalConfig,
            ILogger<HistoricalTasksModel> logger)
        {
            _dbContext = dbContext;
            _adDirectoryService = adDirectoryService;
            _generalConfig = generalConfig;
            _logger = logger;

            ErrorMessages = new List<string>();
        }

        public async Task OnGetAsync()
        {
            LogContext.PushProperty("ActivityName", "HistoricalTasks");
            LogContext.PushProperty("NCNEPortalResource", nameof(OnGetAsync));
            LogContext.PushProperty("UserFullName", CurrentUser.DisplayName);

            _logger.LogInformation("Entering Get initial Historical Tasks");

            try
            {
                NcneTasks = await _dbContext.TaskInfo
                    .Include(c => c.TaskRole)
                    .OrderByDescending(t => t.StatusChangeDate)
                    .Where(t => t.Status == NcneTaskStatus.Completed.ToString() ||
                                t.Status == NcneTaskStatus.Terminated.ToString())
                    .Take(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords)
                    .ToListAsync();
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"Error occurred while getting Historical Tasks from database: {e.Message}");
                _logger.LogError("Error occurred while getting Historical Tasks from database", e);
            }


        }


        public async Task OnPostAsync()
        {
            LogContext.PushProperty("ActivityName", "HistoricalTasks");
            LogContext.PushProperty("NCNEPortalResource", nameof(OnPostAsync));
            LogContext.PushProperty("HistoricalTasksSearchParameters", SearchParameters.ToJSONSerializedString());
            LogContext.PushProperty("UserFullName", CurrentUser.DisplayName);

            _logger.LogInformation("Entering Get filtered Historical Tasks with parameters {HistoricalTasksSearchParameters}");

            try
            {
                NcneTasks = await _dbContext.TaskInfo
                    .Include(t => t.TaskRole)
                    .Where(t =>
                        (t.Status == NcneTaskStatus.Completed.ToString() || t.Status == NcneTaskStatus.Terminated.ToString())
                        && (
                            (!SearchParameters.ProcessId.HasValue || t.ProcessId == SearchParameters.ProcessId.Value)
                            && (string.IsNullOrWhiteSpace(SearchParameters.Country) || t.Country.ToUpper().Contains(SearchParameters.Country.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.ChartType) || t.ChartType.ToUpper().Contains(SearchParameters.ChartType.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.WorkflowType) || t.WorkflowType.ToUpper().Contains(SearchParameters.WorkflowType.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.ChartNo) || t.ChartNumber.ToUpper().Contains(SearchParameters.ChartNo.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.Compiler) || t.TaskRole.Compiler.ToUpper().Contains(SearchParameters.Compiler.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.VerifierOne) || t.TaskRole.VerifierOne.ToUpper().Contains(SearchParameters.VerifierOne.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.VerifierTwo) || t.TaskRole.VerifierTwo.ToUpper().Contains(SearchParameters.VerifierTwo.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.HundredPercentCheck) || t.TaskRole.HundredPercentCheck.ToUpper().Contains(SearchParameters.HundredPercentCheck.ToUpper()))
                            ))
                    .OrderByDescending(t => t.StatusChangeDate)
                    .Take(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords)
                    .ToListAsync();

                _logger.LogInformation("Successfully returned filtered data from database");

            }
            catch (Exception e)
            {
                ErrorMessages.Add($"Error occurred while getting filtered Historical Tasks from database: {e.Message}");
                _logger.LogError("Error occurred while getting filtered Historical Tasks from database with parameters {HistoricalTasksSearchParameters}", e);

            }

        }

    }
}

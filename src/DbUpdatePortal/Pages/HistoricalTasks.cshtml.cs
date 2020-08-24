using Common.Helpers;
using Common.Helpers.Auth;
using DbUpdatePortal.Configuration;
using DbUpdatePortal.Enums;
using DbUpdatePortal.Models;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbUpdatePortal.Pages
{
    public class HistoricalTasksModel : PageModel
    {
        private readonly DbUpdateWorkflowDbContext _dbContext;

        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<HistoricalTasksModel> _logger;

        [BindProperty(SupportsGet = true)]
        public HistoricalTasksSearchParameters SearchParameters { get; set; }

        public List<string> ErrorMessages { get; set; }
        public List<TaskInfo> DbUpdateTasks { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;

        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        public HistoricalTasksModel(DbUpdateWorkflowDbContext dbContext,
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
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnGetAsync));
            LogContext.PushProperty("UserFullName", CurrentUser.DisplayName);

            _logger.LogInformation("Entering Get initial Historical Tasks");

            try
            {
                DbUpdateTasks = await _dbContext.TaskInfo
                    .Include(c => c.TaskRole)
                    .ThenInclude(c => c.Compiler)
                    .Include(c => c.TaskRole)
                    .ThenInclude(c => c.Verifier)
                    .OrderByDescending(t => t.StatusChangeDate)
                    .Where(t => t.Status == DbUpdateTaskStatus.Completed.ToString() ||
                                t.Status == DbUpdateTaskStatus.Terminated.ToString())
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
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostAsync));
            LogContext.PushProperty("HistoricalTasksSearchParameters", SearchParameters.ToJSONSerializedString());
            LogContext.PushProperty("UserFullName", CurrentUser.DisplayName);

            _logger.LogInformation("Entering Get filtered Historical Tasks with parameters {HistoricalTasksSearchParameters}");

            try
            {
                DbUpdateTasks = await _dbContext.TaskInfo
                    .Include(c => c.TaskRole)
                    .ThenInclude(c => c.Compiler)
                    .Include(c => c.TaskRole)
                    .ThenInclude(c => c.Verifier)
                    .Where(t =>
                        (t.Status == DbUpdateTaskStatus.Completed.ToString() || t.Status == DbUpdateTaskStatus.Terminated.ToString())
                        && (
                            (!SearchParameters.ProcessId.HasValue || t.ProcessId == SearchParameters.ProcessId.Value)
                            && (string.IsNullOrWhiteSpace(SearchParameters.ChartingArea) || t.ChartingArea.ToUpper().Contains(SearchParameters.ChartingArea.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.UpdateType) || t.UpdateType.ToUpper().Contains(SearchParameters.UpdateType.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.Name) || t.Name.ToUpper().Contains(SearchParameters.Name.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.Compiler) || t.TaskRole.Compiler.DisplayName.ToUpper().Contains(SearchParameters.Compiler.ToUpper()))
                            && (string.IsNullOrWhiteSpace(SearchParameters.Verifier) || t.TaskRole.Verifier.DisplayName.ToUpper().Contains(SearchParameters.Verifier.ToUpper()))
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

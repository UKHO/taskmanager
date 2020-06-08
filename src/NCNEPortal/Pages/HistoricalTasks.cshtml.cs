using Common.Helpers.Auth;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Serilog.Context;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCNEPortal.Pages
{
    public class HistoricalTasksModel : PageModel
    {
        private readonly NcneWorkflowDbContext _dbContext;

        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<HistoricalTasksModel> _logger;

        public List<string> ErrorMessages { get; set; }
        public List<TaskInfo> NcneTasks { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;

        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetailsAsync(this.User).Result;
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

            NcneTasks = await _dbContext.TaskInfo
                .Include(c => c.TaskRole)
                .OrderByDescending(t => t.AssignedDate)
                .Where(t => t.Status == NcneTaskStatus.Completed.ToString() ||
                                 t.Status == NcneTaskStatus.Terminated.ToString())
                .Take(20)
                .ToListAsync();

        }
    }
}

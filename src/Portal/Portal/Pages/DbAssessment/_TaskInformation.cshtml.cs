using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Auth;
using Portal.Calculators;
using Portal.Configuration;
using Portal.Helpers;
using Portal.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _TaskInformationModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOnHoldCalculator _onHoldCalculator;
        private readonly ICommentsHelper _commentsHelper;
        private readonly IUserIdentityService _userIdentityService;
        private readonly ITaskDataHelper _taskDataHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;

        [BindProperty(SupportsGet = true)]
        [DisplayName("Process ID:")]
        public int ProcessId { get; set; }

        [DisplayName("DM End Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime DmEndDate { get; set; }

        [DisplayName("DM Receipt Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime DmReceiptDate { get; set; }

        [DisplayName("Effective Receipt Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime EffectiveReceiptDate { get; set; }

        [DisplayName("External End Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime ExternalEndDate { get; set; }

        public bool IsOnHold { get; set; }
        public bool OnHoldDaysGreen { get; set; }
        public bool OnHoldDaysAmber { get; set; }
        public bool OnHoldDaysRed { get; set; }

        [DisplayName("On Hold:")]
        public int OnHoldDays { get; set; }

        [DisplayName("ION:")]
        public string Ion { get; set; }

        [DisplayName("Activity Code:")]
        public string ActivityCode { get; set; }

        [DisplayName("Source Category:")]
        public string SourceCategory { get; set; }
        public SelectList SourceCategories { get; set; }

        [DisplayName("Task Type:")]
        public string TaskType { get; set; }
        public SelectList TaskTypes { get; set; }

        [DisplayName("Team:")]
        public string Team { get; set; }
        public SelectList Teams { get; set; }

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public _TaskInformationModel(WorkflowDbContext DbContext,
            IOnHoldCalculator onHoldCalculator,
            ICommentsHelper commentsHelper, IUserIdentityService userIdentityService,
            ITaskDataHelper taskDataHelper,
            IOptions<GeneralConfig> generalConfig)
        {
            _dbContext = DbContext;
            _onHoldCalculator = onHoldCalculator;
            _commentsHelper = commentsHelper;
            _userIdentityService = userIdentityService;
            _taskDataHelper = taskDataHelper;
            _generalConfig = generalConfig;
        }

        public async Task OnGetAsync(int processId, string taskStage)
        {
            ProcessId = processId;

            await SetTaskInformationData();
        }

        private async Task SetTaskInformationData()
        {
            SetSourceCategories();

            var taskTypes = await _dbContext.AssignedTaskType.Select(st => st.Name).ToListAsync();
            TaskTypes = new SelectList(taskTypes);

            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == ProcessId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
            OnHoldDays = _onHoldCalculator.CalculateOnHoldDays(onHoldRows, DateTime.Now.Date);

            var (greenIcon, amberIcon, redIcon) = _onHoldCalculator.DetermineOnHoldDaysIcons(OnHoldDays);
            OnHoldDaysGreen = greenIcon;
            OnHoldDaysAmber = amberIcon;
            OnHoldDaysRed = redIcon;

            var activityName = _dbContext.WorkflowInstance.First(wi => wi.ProcessId == ProcessId).ActivityName;

            var taskData = await _taskDataHelper.GetTaskData(activityName, ProcessId);

            ActivityCode = taskData?.ActivityCode;
            Ion = taskData?.Ion;
            SourceCategory = taskData?.SourceCategory;
            TaskType = taskData?.TaskType;
            Teams = new SelectList(_generalConfig.Value.GetTeams());

            var assessmentData = await _dbContext.AssessmentData.SingleOrDefaultAsync(ad => ad.ProcessId == ProcessId);
            if (assessmentData != null)
            {
                EffectiveReceiptDate = assessmentData.ReceiptDate;
                Team = string.IsNullOrWhiteSpace(assessmentData.TeamDistributedTo) ? "" : assessmentData.TeamDistributedTo;
            }
        }

        private void SetSourceCategories()
        {
            if (!System.IO.File.Exists(@"Data\SourceCategories.json"))
                throw new FileNotFoundException(@"Data\SourceCategories.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\SourceCategories.json");
            var sourceCategories = JsonConvert.DeserializeObject<IEnumerable<SourceCategory>>(jsonString)
                .Select(sc => sc.Name);
            SourceCategories = new SelectList(
                sourceCategories);
        }
    }
}

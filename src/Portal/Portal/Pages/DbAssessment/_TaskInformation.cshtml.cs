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
using Portal.Calculators;
using Portal.Configuration;
using Portal.Helpers;
using Portal.Models;
using WorkflowDatabase.EF;

namespace Portal.Pages.DbAssessment
{
    public class _TaskInformationModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOnHoldCalculator _onHoldCalculator;
        private readonly ICommentsHelper _commentsHelper;
        private readonly ITaskDataHelper _taskDataHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IDmEndDateCalculator _dmEndDateCalculator;

        [BindProperty(SupportsGet = true)]
        [DisplayName("Process ID:")]
        public int ProcessId { get; set; }

        [DisplayName("DM End Date:")]
        public string DmEndDate { get; set; }

        [DisplayName("DM Receipt Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime DmReceiptDate { get; set; }

        [DisplayName("Effective Receipt Date:")]
        public string EffectiveReceiptDate { get; set; }

        [DisplayName("External End Date:")]
        public string ExternalEndDate { get; set; }

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

        public _TaskInformationModel(WorkflowDbContext DbContext,
            IOnHoldCalculator onHoldCalculator,
            ICommentsHelper commentsHelper,
            ITaskDataHelper taskDataHelper,
            IOptions<GeneralConfig> generalConfig,
            IDmEndDateCalculator dmEndDateCalculator)
        {
            _dbContext = DbContext;
            _onHoldCalculator = onHoldCalculator;
            _commentsHelper = commentsHelper;
            _taskDataHelper = taskDataHelper;
            _generalConfig = generalConfig;
            _dmEndDateCalculator = dmEndDateCalculator;
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

            var workflowInstanceRow = _dbContext.WorkflowInstance.First(wi => wi.ProcessId == ProcessId);

            var activityName = workflowInstanceRow.ActivityName;

            DmReceiptDate = workflowInstanceRow.StartedAt;
            
            var taskData = await _taskDataHelper.GetTaskData(activityName, ProcessId);

            ActivityCode = taskData?.ActivityCode;
            Ion = taskData?.Ion;
            SourceCategory = taskData?.SourceCategory;
            TaskType = taskData?.TaskType;
            Teams = new SelectList(_generalConfig.Value.GetTeams());
            
            var assessmentData = await _dbContext.AssessmentData.SingleOrDefaultAsync(ad => ad.ProcessId == ProcessId);
            if (assessmentData != null)
            {
                EffectiveReceiptDate = assessmentData.EffectiveStartDate != null ? assessmentData.EffectiveStartDate.Value.ToShortDateString() : "N/A" ;
                Team = string.IsNullOrWhiteSpace(assessmentData.TeamDistributedTo) ? "" : assessmentData.TeamDistributedTo;

                DmEndDate = taskData != null && assessmentData.EffectiveStartDate != null ? _dmEndDateCalculator.CalculateDmEndDate(assessmentData.EffectiveStartDate.Value,
                                taskData.TaskType, activityName).dmEndDate.ToShortDateString() : "N/A";

                ExternalEndDate = assessmentData.EffectiveStartDate != null ?
                                    assessmentData.EffectiveStartDate.Value.AddDays(_generalConfig.Value.ExternalEndDateDays).ToShortDateString() : "N/A";

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

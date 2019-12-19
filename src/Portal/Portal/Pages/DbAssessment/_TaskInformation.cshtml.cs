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
using Newtonsoft.Json;
using Portal.Auth;
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

        [DisplayName("On Hold:")]
        public int OnHoldDays { get; set; }

        [DisplayName("ION:")]
        public string Ion { get; set; }

        [DisplayName("Activity Code:")]
        public string ActivityCode { get; set; }

        [DisplayName("Source Category:")]
        public string SourceCategory { get; set; }

        public SelectList SourceCategories { get; set; }

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public _TaskInformationModel(WorkflowDbContext DbContext,
            IOnHoldCalculator onHoldCalculator,
            ICommentsHelper commentsHelper, IUserIdentityService userIdentityService,
            ITaskDataHelper taskDataHelper)
        {
            _dbContext = DbContext;
            _onHoldCalculator = onHoldCalculator;
            _commentsHelper = commentsHelper;
            _userIdentityService = userIdentityService;
            _taskDataHelper = taskDataHelper;
        }

        public async Task OnGetAsync()
        {
            await SetTaskInformationData();
        }

        public async Task<IActionResult> OnPostOnHoldAsync(int processId)
        {
            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(p => p.ProcessId == processId);
            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            var onHoldRecord = new OnHold
            {
                ProcessId = processId,
                OnHoldTime = DateTime.Now,
                OnHoldUser = UserFullName,
                WorkflowInstanceId = workflowInstance.WorkflowInstanceId
            };

            await _dbContext.OnHold.AddAsync(onHoldRecord);
            await _dbContext.SaveChangesAsync();

            IsOnHold = true;
            ProcessId = processId;

            await _commentsHelper.AddComment($"Task {processId} has been put on hold",
                processId,
                workflowInstance.WorkflowInstanceId,
                UserFullName);

            // As we're submitting, re-get task info for now
            await SetTaskInformationData();

            return Page();
        }

        public async Task<IActionResult> OnPostOffHoldAsync(int processId)
        {
            try
            {
                var onHoldRecord = await _dbContext.OnHold.FirstAsync(r => r.ProcessId == processId
                                                           && r.OffHoldTime == null);
                UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

                onHoldRecord.OffHoldTime = DateTime.Now;
                onHoldRecord.OffHoldUser = UserFullName;

                await _dbContext.SaveChangesAsync();

                IsOnHold = false;

                ProcessId = processId;

                await _commentsHelper.AddComment($"Task {processId} taken off hold",
                    processId,
                    _dbContext.WorkflowInstance.First(p => p.ProcessId == processId)
                        .WorkflowInstanceId,
                    UserFullName);

                // As we're submitting, re-get task info for now
                await SetTaskInformationData();
            }
            catch (InvalidOperationException e)
            {
                // Log error
                e.Data.Add("OurMessage", $"Cannot find an on hold row for ProcessId: {processId}");
                throw;
            }

            return Page();
        }

        private async Task SetTaskInformationData()
        {
            SetSourceCategories();

            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == ProcessId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
            OnHoldDays = _onHoldCalculator.CalculateOnHoldDays(onHoldRows, DateTime.Now.Date);

            var activityName = _dbContext.WorkflowInstance.First(wi => wi.ProcessId == ProcessId).ActivityName;

            var taskData = await _taskDataHelper.GetTaskData(activityName, ProcessId);

            ActivityCode = taskData.ActivityCode;
            Ion = taskData.Ion;
            SourceCategory = taskData.SourceCategory;

            var assessmentData = await _dbContext.AssessmentData.SingleOrDefaultAsync(ad => ad.ProcessId == ProcessId);
            if (assessmentData != null)
            {
                EffectiveReceiptDate = assessmentData.ReceiptDate;
                //assessmentData.EffectiveStartDate
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

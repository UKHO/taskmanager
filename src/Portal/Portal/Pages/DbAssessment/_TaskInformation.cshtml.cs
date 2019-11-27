using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Helpers;
using Portal.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _TaskInformationModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOnHoldCalculator _onHoldCalculator;
        private readonly ICommentsHelper _commentsHelper;

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
        public SourceCategory SourceCategory { get; set; }

        public SelectList SourceCategories { get; set; }

        public _TaskInformationModel(WorkflowDbContext DbContext,
            IHttpContextAccessor httpContextAccessor,
            IOnHoldCalculator onHoldCalculator,
            ICommentsHelper commentsHelper)
        {
            _dbContext = DbContext;
            _httpContextAccessor = httpContextAccessor;
            _onHoldCalculator = onHoldCalculator;
            _commentsHelper = commentsHelper;
        }

        public async Task OnGetAsync()
        {
            await SetTaskInformationDummyData();
        }

        public async Task<IActionResult> OnPostOnHoldAsync(int processId)
        {
            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(p => p.ProcessId == processId);

            var onHoldRecord = new OnHold
            {
                ProcessId = processId,
                OnHoldTime = DateTime.Now,
                OnHoldUser = "Ross",
                WorkflowInstanceId = workflowInstance.WorkflowInstanceId
            };

            await _dbContext.OnHold.AddAsync(onHoldRecord);
            await _dbContext.SaveChangesAsync();

            IsOnHold = true;
            ProcessId = processId;

            await _commentsHelper.AddComment($"Task {processId} has been put on hold", processId, workflowInstance.WorkflowInstanceId);

            // As we're submitting, re-get task info for now
            await SetTaskInformationDummyData();

            return Page();
        }

        public async Task<IActionResult> OnPostOffHoldAsync(int processId)
        {
            try
            {
                var onHoldRecord = await _dbContext.OnHold.FirstAsync(r => r.ProcessId == processId
                                                           && r.OffHoldTime == null);

                onHoldRecord.OffHoldTime = DateTime.Now;
                onHoldRecord.OffHoldUser = "Bon";

                await _dbContext.SaveChangesAsync();

                IsOnHold = false;

                ProcessId = processId;

                await _commentsHelper.AddComment($"Task {processId} taken off hold", processId, _dbContext.WorkflowInstance.First(p => p.ProcessId == processId).WorkflowInstanceId);

                // As we're submitting, re-get task info for now
                await SetTaskInformationDummyData();
            }
            catch (InvalidOperationException e)
            {
                // Log error
                e.Data.Add("OurMessage", $"Cannot find an on hold row for ProcessId: {processId}");
                throw;
            }

            return Page();
        }

        private async Task SetTaskInformationDummyData()
        {
            if (!System.IO.File.Exists(@"Data\SourceCategories.json")) throw new FileNotFoundException(@"Data\SourceCategories.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\SourceCategories.json");
            var sourceCategories = JsonConvert.DeserializeObject<IEnumerable<SourceCategory>>(jsonString);

            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == ProcessId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);

            DmEndDate = DateTime.Now;
            DmReceiptDate = DateTime.Now;
            EffectiveReceiptDate = DateTime.Now;
            ExternalEndDate = DateTime.Now;
            IsOnHold = IsOnHold;
            OnHoldDays = _onHoldCalculator.CalculateOnHoldDays(onHoldRows, DateTime.Now.Date);
            Ion = "2929";
            ActivityCode = "1272";
            SourceCategory = new SourceCategory { SourceCategoryId = 1, Name = "zzzzz" };
            SourceCategories = new SelectList(
                sourceCategories, "SourceCategoryId", "Name");
        }
    }
}

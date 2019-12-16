using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class VerifyModel : PageModel
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly ICommentsHelper _commentsHelper;
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;

        public bool IsOnHold { get; set; }
        public int ProcessId { get; set; }
        public _OperatorsModel OperatorsModel { get; set; }

        [BindProperty]
        public List<ProductAction> RecordProductAction { get; set; }

        public VerifyModel(WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            IOptions<UriConfig> uriConfig,
            ICommentsHelper commentsHelper)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _uriConfig = uriConfig;
            _commentsHelper = commentsHelper;
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;
            OperatorsModel = SetOperatorsDummyData();
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId)
        {
            return RedirectToPage("/Index");
        }

        private async Task UpdateSdraAssessmentAsCompleted(string comment, WorkflowInstance workflowInstance)
        {
            try
            {
                await _dataServiceApiClient.PutAssessmentCompleted(workflowInstance.AssessmentData.PrimarySdocId,
                    comment);
            }
            catch (Exception e)
            {
                //TODO: Log error!
            }
        }

        private _OperatorsModel SetOperatorsDummyData()
        {
            return new _OperatorsModel
            {
                WorkManager = "Greg Williams",
                Assessor = new Assessor { UserId = 1, Name = "Peter Bates" },
                Verifier = new Verifier { UserId = 1, Name = "Matt Stoodley" },
                Verifiers = new SelectList(
                    new List<Verifier>
                    {
                        new Verifier {UserId = 0, Name = "Brian Stenson"},
                        new Verifier {UserId = 1, Name = "Matt Stoodley"},
                        new Verifier {UserId = 2, Name = "Peter Bates"}
                    }, "UserId", "Name")
            };
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HpdDatabase.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portal.HttpClients;
using Portal.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class AssessModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly HpdDbContext _hpdDbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly ILogger<AssessModel> _logger;

        public bool IsOnHold { get; set; }
        public int ProcessId { get; set; }
        public _OperatorsModel OperatorsModel { get; set; }

        [BindProperty]
        public List<ProductAction> RecordProductAction { get; set; }
        [BindProperty]
        public List<DataImpact> DataImpacts { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        public AssessModel(WorkflowDbContext dbContext,
            HpdDbContext hpdDbContext,
            IDataServiceApiClient dataServiceApiClient,
            ILogger<AssessModel> logger)
        {
            _dbContext = dbContext;
            _hpdDbContext = hpdDbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _logger = logger;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;
            OperatorsModel = SetOperatorsDummyData();
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId)
        {
            bool validationSucceeded = true;
            ValidationErrorMessages.Clear();

            // Show error to user, that they've chosen the same usage more than once
            if (!await ValidateRecordProductAction())
            {
                validationSucceeded = false;
            }

            if (!ValidateDataImpact())
            {
                validationSucceeded = false;
            }

            // TODO: validate the other partials where required.
            if (validationSucceeded)
            {
                return RedirectToPage("/Index");
            }
            else
            {
                await OnGet(processId);
                return Page();
            }
        }

        private bool ValidateDataImpact()
        {
            if (DataImpacts.GroupBy(x => x.HpdUsageId)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key).Any())
            {
                ValidationErrorMessages.Add("Data Impact: More than one of the same Usage selected");
                return false;
            }

            return true;
        }

        private async Task<bool> ValidateRecordProductAction()
        {
            bool isValid = true;

            if (RecordProductAction != null && RecordProductAction.Count > 0)
            {
                foreach (var productAction in RecordProductAction)
                {
                    // Check for existing impacted products
                    var isExist = await _hpdDbContext.CarisProducts.AnyAsync(p =>
                        p.ProductStatus.Equals("Active", StringComparison.InvariantCultureIgnoreCase) &&
                        p.TypeKey.Equals("ENC", StringComparison.InvariantCultureIgnoreCase) &&
                        p.ProductName.Equals(productAction.ImpactedProduct, StringComparison.InvariantCultureIgnoreCase));

                    if (!isExist)
                    {
                        ValidationErrorMessages.Add($"Record Product Action: Impacted product {productAction.ImpactedProduct} does not exist");
                        isValid = false;
                    }
                }

                if (RecordProductAction.GroupBy(p => p.ImpactedProduct)
                    .Where(g => g.Count() > 1)
                    .Select(y => y.Key).Any())
                {
                    ValidationErrorMessages.Add("Record Product Action: More than one of the same Impacted Products selected");
                    isValid = false;
                }
            }

            return isValid;
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
                _logger.LogError(e, "Failed requesting DataService {DataServiceResource} with: PrimarySdocId: {PrimarySdocId}; Comment: {Comment};",
                    nameof(_dataServiceApiClient.PutAssessmentCompleted),
                    workflowInstance.AssessmentData.PrimarySdocId,
                    comment);
            }
        }

        private _OperatorsModel SetOperatorsDummyData()
        {
            return new _OperatorsModel
            {
                Reviewer = "Greg Williams",
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
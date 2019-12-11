using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HpdDatabase.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        public bool IsOnHold { get; set; }
        public int ProcessId { get; set; }
        public _OperatorsModel OperatorsModel { get; set; }

        [BindProperty]
        public _EditDatabaseModel EditDatabaseModel { get; set; }

        [BindProperty]
        public List<ProductAction> RecordProductAction { get; set; }
        [BindProperty]
        public List<DataImpact> DataImpacts { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        public AssessModel(WorkflowDbContext dbContext,
            HpdDbContext hpdDbContext,
            IDataServiceApiClient dataServiceApiClient)
        {
            _dbContext = dbContext;
            _hpdDbContext = hpdDbContext;
            _dataServiceApiClient = dataServiceApiClient;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;
            OperatorsModel = SetOperatorsDummyData();
            //EditDatabaseModel = SetEditDatabaseModel();
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
                //TODO: Log error!
            }
        }

        private _OperatorsModel SetOperatorsDummyData()
        {
            return new _OperatorsModel
            {
                WorkManager = "Greg Williams",
                Assessor = new Assessor { AssessorId = 1, Name = "Peter Bates" },
                Verifier = new Verifier { VerifierId = 1, Name = "Matt Stoodley" },
                Verifiers = new SelectList(
                    new List<Verifier>
                    {
                        new Verifier {VerifierId = 0, Name = "Brian Stenson"},
                        new Verifier {VerifierId = 1, Name = "Matt Stoodley"},
                        new Verifier {VerifierId = 2, Name = "Peter Bates"}
                    }, "VerifierId", "Name")
            };
        }


        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}
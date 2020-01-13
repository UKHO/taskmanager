using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HpdDatabase.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Portal.HttpClients;
using Portal.Models;
using Serilog.Context;
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

        [BindProperty]
        public string Ion { get; set; }

        [BindProperty]
        public string ActivityCode { get; set; }

        [BindProperty]
        public string SourceCategory { get; set; }

        [BindProperty]
        public string Verifier { get; set; }

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
            //TODO: Read operators from DB

            ProcessId = processId;
            OperatorsModel = SetOperatorsDummyData();
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            LogContext.PushProperty("ActivityName", "Assess");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("Action", action);

            _logger.LogInformation("Entering Done with: ProcessId: {ProcessId}; Action: {Action};");


            var isValid = true;
            ValidationErrorMessages.Clear();

            // Show error to user, that they've chosen the same usage more than once
            if (!ValidateTaskInformation())
            {
                isValid = false;
            }
            if (!ValidateOperators())
            {
                isValid = false;
            }
            if (!await ValidateRecordProductAction())
            {
                isValid = false;
            }

            if (!ValidateDataImpact())
            {
                isValid = false;
            }

            if (!isValid)
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            //TODO: Save Task information

            //TODO: Save operators

            //TODO: Save record product action

            //TODO: Save data impact

            // TODO: validate the other partials where required.

            return StatusCode((int)HttpStatusCode.OK);
        }

        private bool ValidateTaskInformation()
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(Ion))
            {
                ValidationErrorMessages.Add("Task Information: Ion cannot be empty");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(ActivityCode))
            {
                ValidationErrorMessages.Add("Task Information: Activity code cannot be empty");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(SourceCategory))
            {
                ValidationErrorMessages.Add("Task Information: Source category cannot be empty");
                isValid = false;
            }
            
            return isValid;
        }

        private bool ValidateOperators()
        {
            if (string.IsNullOrWhiteSpace(Verifier))
            {
                ValidationErrorMessages.Add("Operators: Verifier cannot be empty");
                return false;
            }
            return true;
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
            if (!System.IO.File.Exists(@"Data\Users.json")) throw new FileNotFoundException(@"Data\Users.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\Users.json");
            var users = JsonConvert.DeserializeObject<IEnumerable<Assessor>>(jsonString)
                .Select(u => u.Name)
                .ToList();

            return new _OperatorsModel
            {
                Reviewer = "Greg Williams",
                Assessor = "Peter Bates",
                Verifier = "Matt Stoodley",
                Verifiers = new SelectList(users)
            };
        }


        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}
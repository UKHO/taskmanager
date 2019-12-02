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
    public class AssessModel : PageModel
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
        public _EditDatabaseModel EditDatabaseModel { get; set; }
        public _RecordProductActionModel RecordProductActionModel { get; set; }
        [BindProperty]
        public List<DataImpact> DataImpacts { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        public AssessModel(WorkflowDbContext dbContext,
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

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;
            OperatorsModel = SetOperatorsDummyData();
            EditDatabaseModel = SetEditDatabaseModel();
            RecordProductActionModel = SetProductActionDummyData();
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId)
        {
            bool validationSucceeded = true;
            ValidationErrorMessages.Clear();

            // Show error to user, that they've chosen the same usage more than once
            if (!ValidateRecordProductAction())
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

        private bool ValidateRecordProductAction()
        {
            // TODO: this is set to always error to show the error popup
            ValidationErrorMessages.Add("Record Product Action: Failed to save Record Product Action");
            return false;

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

        private _EditDatabaseModel SetEditDatabaseModel()
        {
            return new _EditDatabaseModel
            {
                CarisWorkspace = new CarisWorkspace { Workspace = "Workspace1", WorkspaceId = 1 },
                CarisWorkspaces = new SelectList(
                    new List<CarisWorkspace>
                    {
                        new CarisWorkspace{Workspace = "Workspace1", WorkspaceId = 1},
                        new CarisWorkspace{Workspace = "Workspace2", WorkspaceId = 2},
                        new CarisWorkspace{Workspace = "Workspace3", WorkspaceId = 3}
                    }, "WorkspaceId", "Workspace"),
                ProjectName = "Testing Project"
            };
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

        private _RecordProductActionModel SetProductActionDummyData()
        {
            return new _RecordProductActionModel
            {
                Action = true,
                ProductActions = new List<ProductAction>
                {
                    new ProductAction
                    {
                        ActionType = "Please select a value...",
                        ImpactedProduct = "Unknown",
                        ProcessId = ProcessId,
                        ProductActionId = 1
                    }
                },
                ImpactedProducts = new SelectList(
                    new List<ImpactedProduct>
                    {
                        new ImpactedProduct {ProductId = 0, Product = "Select..."},
                        new ImpactedProduct {ProductId = 1, Product = "GB123456"},
                        new ImpactedProduct {ProductId = 2, Product = "GB111222"},
                        new ImpactedProduct {ProductId = 3, Product = "GB987651"}
                    }, "ProductId", "Product"),
                ProductActionTypes = new SelectList(
                    new List<ProductActionType>
                    {
                        new ProductActionType {ActionTypeId = 0, ActionType = "Select..."},
                        new ProductActionType {ActionTypeId = 1, ActionType = "CPTS/LTA"},
                        new ProductActionType {ActionTypeId = 2, ActionType = "CPTS/LTA MCOVER"},
                        new ProductActionType {ActionTypeId = 3, ActionType = "Product Only"},
                        new ProductActionType {ActionTypeId = 4, ActionType = "Scale too small"}
                    }, "ActionTypeId", "ActionType")
            };
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}
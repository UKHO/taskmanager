using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Configuration;
using Portal.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class AssessModel : PageModel
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;

        public int ProcessId { get; set; }
        public _TaskInformationModel TaskInformationModel { get; set; }
        public _OperatorsModel OperatorsModel { get; set; }
        public _EditDatabaseModel EditDatabaseModel { get; set; }
        public _RecordProductActionModel RecordProductActionModel { get; set; }
        public List<_DataImpactModel> DataImpactModel { get; set; }
        public _CommentsModel CommentsModel { get; set; }
        public WorkflowDbContext DbContext { get; set; }

        public AssessModel(WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            IHttpContextAccessor httpContextAccessor,
            IOptions<UriConfig> uriConfig)
        {
            DbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _httpContextAccessor = httpContextAccessor;
            _uriConfig = uriConfig;
        }

        public void OnGet(int processId)
        {
            ProcessId = processId;
            OperatorsModel = SetOperatorsDummyData();
            TaskInformationModel = SetTaskInformationData(processId);
            EditDatabaseModel = SetEditDatabaseModel();
            RecordProductActionModel = SetProductActionDummyData();
            DataImpactModel = SetDataImpactModelDummyData();
        }

        public IActionResult OnGetRetrieveComments(int processId)
        {
            var model = new _CommentsModel()
            {
                Comments = DbContext.Comment.Where(c => c.ProcessId == processId).ToList(),
                ProcessId = processId
            };

            // Repopulate models...
            OnGet(processId);

            return new PartialViewResult
            {
                ViewName = "_Comments",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                }
            };
        }

        public IActionResult OnGetCommentsPartialAsync(string comment, int processId)
        {
            // TODO: Test with Azure
            // TODO: This will not work in Azure; need alternative; but will work in local dev

            var workflowInstance = DbContext.WorkflowInstance.First(c => c.ProcessId == processId).WorkflowInstanceId;

            AddComment(comment, processId, workflowInstance);

            return OnGetRetrieveComments(processId);
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

        private void AddComment(string comment, int processId, int workflowInstanceId)
        {
            var userId = _httpContextAccessor.HttpContext.User.Identity.Name;

            DbContext.Comment.Add(new Comments
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                Created = DateTime.Now,
                Username = string.IsNullOrEmpty(userId) ? "Unknown" : userId,
                Text = comment
            });

            DbContext.SaveChanges();
        }

        // TODO: Update to match Review
        private _TaskInformationModel SetTaskInformationData(int processId)
        {
            if (!System.IO.File.Exists(@"Data\SourceCategories.json"))
                throw new FileNotFoundException(@"Data\SourceCategories.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\SourceCategories.json");
            var sourceCategories = JsonConvert.DeserializeObject<IEnumerable<SourceCategory>>(jsonString);

            return new _TaskInformationModel(DbContext,null, null)
            {
                ProcessId = processId,
                DmEndDate = DateTime.Now,
                DmReceiptDate = DateTime.Now,
                EffectiveReceiptDate = DateTime.Now,
                ExternalEndDate = DateTime.Now,
                OnHoldDays = 4,
                Ion = "2929",
                ActivityCode = "1272",
                SourceCategory = new SourceCategory { SourceCategoryId = 1, Name = "zzzzz" },
                SourceCategories = new SelectList(
                    sourceCategories, "SourceCategoryId", "Name")
            };
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

        private List<_DataImpactModel> SetDataImpactModelDummyData()
        {
            return new List<_DataImpactModel>
            {
                new _DataImpactModel{Usage = "Nav 1"},
                new _DataImpactModel{Usage = "Nav 2"},
                new _DataImpactModel{Usage = "Nav 3"},
                new _DataImpactModel{Usage = "Other"},
                new _DataImpactModel{Usage = "POLAR"}
            };
        }
    }
}
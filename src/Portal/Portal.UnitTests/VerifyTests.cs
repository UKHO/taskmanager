﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Portal.Auth;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class VerifyTests
    {

        private WorkflowDbContext _dbContext;
        private HpdDbContext _hpDbContext;
        private VerifyModel _verifyModel;
        private int ProcessId { get; set; }
        private ILogger<VerifyModel> _fakeLogger;
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private IEventServiceApiClient _fakeEventServiceApiClient;
        private IUserIdentityService _fakeUserIdentityService;
        private ICommentsHelper _fakeCommentsHelper;
        private IRecordProductActionHelper _recordProductActionHelper;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakeEventServiceApiClient = A.Fake<IEventServiceApiClient>();
            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();

            ProcessId = 123;

            _dbContext.DbAssessmentVerifyData.Add(new DbAssessmentVerifyData()
            {
                ProcessId = ProcessId
            });

            _dbContext.SaveChanges();

            var hpdDbContextOptions = new DbContextOptionsBuilder<HpdDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _hpDbContext = new HpdDbContext(hpdDbContextOptions);

            _fakeCommentsHelper = A.Fake<ICommentsHelper>();

            _fakeUserIdentityService = A.Fake<IUserIdentityService>();

            _fakeLogger = A.Dummy<ILogger<VerifyModel>>();

            _recordProductActionHelper = new RecordProductActionHelper(_hpDbContext);

            _verifyModel = new VerifyModel(_dbContext, _fakeDataServiceApiClient, _fakeWorkflowServiceApiClient, _fakeEventServiceApiClient,
                _fakeCommentsHelper, _fakeUserIdentityService, _fakeLogger, _hpDbContext, _recordProductActionHelper);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_entering_an_empty_ion_activityCode_sourceCategory_results_in_validation_error_message()
        {
            _verifyModel.Ion = "";
            _verifyModel.ActivityCode = "";
            _verifyModel.SourceCategory = "";

            _verifyModel.Verifier = "TestUser";
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(3, _verifyModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Task Information: Ion cannot be empty", _verifyModel.ValidationErrorMessages[0]);
            Assert.AreEqual($"Task Information: Activity code cannot be empty", _verifyModel.ValidationErrorMessages[1]);
            Assert.AreEqual($"Task Information: Source category cannot be empty", _verifyModel.ValidationErrorMessages[2]);
        }

        [Test]
        public async Task Test_entering_an_empty_Verifier_results_in_validation_error_message()
        {
            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCatagory";

            _verifyModel.Verifier = "";
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _verifyModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Operators: Verifier cannot be empty", _verifyModel.ValidationErrorMessages[0]);
        }
        
        [Test]
        public async Task Test_entering_duplicate_hpd_usages_in_dataImpact_results_in_validation_error_message()
        {
            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCatagory";

            _verifyModel.Verifier = "TestUser";
            _verifyModel.DataImpacts = new List<DataImpact>()
            {
                new DataImpact() { DataImpactId = 1, HpdUsageId = 1, ProcessId = 123},
                new DataImpact() {DataImpactId = 2, HpdUsageId = 1, ProcessId = 123}
            };

            await _verifyModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _verifyModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Data Impact: More than one of the same Usage selected", _verifyModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_entering_non_existing_impactedProduct_in_productAction_results_in_validation_error_message()
        {

            _hpDbContext.CarisProducts.Add(new CarisProducts()
                {ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC"});
            await _hpDbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCatagory";

            _verifyModel.Verifier = "TestUser";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB5678"}
            };

            await _verifyModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _verifyModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Record Product Action: Impacted product GB5678 does not exist", _verifyModel.ValidationErrorMessages[0]);
        }


        [Test]
        public async Task Test_entering_duplicate_impactedProducts_in_productAction_results_in_validation_error_message()
        {

            _hpDbContext.CarisProducts.Add(new CarisProducts()
                { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCatagory";

            _verifyModel.Verifier = "TestUser";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234"},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1234"}
            };

            await _verifyModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _verifyModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Record Product Action: More than one of the same Impacted Products selected", _verifyModel.ValidationErrorMessages[0]);
        }
    }
}
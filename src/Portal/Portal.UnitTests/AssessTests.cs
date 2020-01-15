﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Portal.Auth;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class AssessTests
    {

        private WorkflowDbContext _dbContext;
        private HpdDbContext _hpDbContext;
        private AssessModel _assessModel;
        private int ProcessId { get; set; }
        private ILogger<AssessModel> _fakeLogger;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            ProcessId = 123;

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                ProcessId = ProcessId
            });

            _dbContext.SaveChanges();

            var hpdDbContextOptions = new DbContextOptionsBuilder<HpdDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _hpDbContext = new HpdDbContext(hpdDbContextOptions);


            _fakeLogger = A.Dummy<ILogger<AssessModel>>();

            _assessModel = new AssessModel(_dbContext, _hpDbContext, null, _fakeLogger);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_entering_an_empty_ion_activityCode_sourceCategory_results_in_validation_error_message()
        {
            _assessModel.Ion = "";
            _assessModel.ActivityCode = "";
            _assessModel.SourceCategory = "";

            _assessModel.Verifier = "TestUser";
            _assessModel.DataImpacts = new List<DataImpact>();

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(3, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Task Information: Ion cannot be empty", _assessModel.ValidationErrorMessages[0]);
            Assert.AreEqual($"Task Information: Activity code cannot be empty", _assessModel.ValidationErrorMessages[1]);
            Assert.AreEqual($"Task Information: Source category cannot be empty", _assessModel.ValidationErrorMessages[2]);
        }

        [Test]
        public async Task Test_entering_an_empty_Verifier_results_in_validation_error_message()
        {
            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCatagory";

            _assessModel.Verifier = "";
            _assessModel.DataImpacts = new List<DataImpact>();

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Operators: Verifier cannot be empty", _assessModel.ValidationErrorMessages[0]);
        }
        
        [Test]
        public async Task Test_entering_duplicate_hpd_usages_in_dataImpact_results_in_validation_error_message()
        {
            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCatagory";

            _assessModel.Verifier = "TestUser";
            _assessModel.DataImpacts = new List<DataImpact>()
            {
                new DataImpact() { DataImpactId = 1, HpdUsageId = 1, ProcessId = 123},
                new DataImpact() {DataImpactId = 2, HpdUsageId = 1, ProcessId = 123}
            };

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Data Impact: More than one of the same Usage selected", _assessModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_entering_non_existing_impactedProduct_in_productAction_results_in_validation_error_message()
        {

            _hpDbContext.CarisProducts.Add(new CarisProducts()
                {ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC"});
            await _hpDbContext.SaveChangesAsync();

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCatagory";

            _assessModel.Verifier = "TestUser";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB5678"}
            };

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Record Product Action: Impacted product GB5678 does not exist", _assessModel.ValidationErrorMessages[0]);
        }


        [Test]
        public async Task Test_entering_duplicate_impactedProducts_in_productAction_results_in_validation_error_message()
        {

            _hpDbContext.CarisProducts.Add(new CarisProducts()
                { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCatagory";

            _assessModel.Verifier = "TestUser";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234"},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1234"}
            };

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Record Product Action: More than one of the same Impacted Products selected", _assessModel.ValidationErrorMessages[0]);
        }
    }
}
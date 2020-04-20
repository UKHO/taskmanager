using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Helpers.Auth;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Configuration;
using Portal.Helpers;
using Portal.Models;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class EditDatabaseTests
    {
        private WorkflowDbContext _dbContext;
        private _EditDatabaseModel _editDatabaseModel;
        private ILogger<_EditDatabaseModel> _fakeLogger;
        private IOptions<GeneralConfig> _generalConfig;
        private IAdDirectoryService _fakeAdDirectoryService;
        private ISessionFileGenerator _fakeSessionFileGenerator;
        private ICarisProjectHelper _fakeCarisProjectHelper;
        private ICarisProjectNameGenerator _fakeCarisProjectNameGenerator;

        public int ProcessId { get; set; }
        public string UserFullName { get; set; }

        [SetUp]
        public async Task Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeLogger = A.Dummy<ILogger<_EditDatabaseModel>>();
            _generalConfig = A.Fake<IOptions<GeneralConfig>>();
            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakeSessionFileGenerator = A.Fake<ISessionFileGenerator>();
            _fakeCarisProjectHelper = A.Fake<ICarisProjectHelper>();
            _fakeCarisProjectNameGenerator = A.Fake<ICarisProjectNameGenerator>();

            ProcessId = 123;
            UserFullName = "TestUser";

            A.CallTo(() => _fakeAdDirectoryService.GetFullNameForUserAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(UserFullName);

            _dbContext.CachedHpdWorkspace.Add(new CachedHpdWorkspace
            {
                Name = "TestWorkspace"
            });
            _dbContext.HpdUser.Add(new HpdUser
            {
                AdUsername = "TestUserAd",
                HpdUsername = "HpdUser"
            });
            await _dbContext.SaveChangesAsync();

            _editDatabaseModel = new _EditDatabaseModel(_dbContext, _fakeLogger, _generalConfig, _fakeAdDirectoryService,
                                                        _fakeSessionFileGenerator, _fakeCarisProjectHelper, _fakeCarisProjectNameGenerator);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_CreateCarisProject_Given_Valid_Data_Then_Updates_DbAssessmentAssessData_WorkspaceAffected()
        {
            //Arrange
            var userWithHpdUserRecord = "TestUserAd";

            var setupAssessData = new DbAssessmentAssessData()
            {
                ProcessId = ProcessId,
                Assessor = userWithHpdUserRecord,
                Verifier = userWithHpdUserRecord
            };
            await _dbContext.DbAssessmentAssessData.AddAsync(setupAssessData);
            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakeAdDirectoryService.GetFullNameForUserAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(userWithHpdUserRecord));

            A.CallTo(() => _fakeCarisProjectHelper.CreateCarisProject(
                    A<int>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<string>.Ignored,
                    A<int>.Ignored))
                .Returns(1);

            //Act
            await _editDatabaseModel.OnPostCreateCarisProjectAsync(ProcessId, "Assess", "TestProject", "TestWorkspace");

            //Assert
            var assessData = await _dbContext.DbAssessmentAssessData.FirstOrDefaultAsync();
            Assert.IsNotNull(assessData);
            Assert.AreEqual("TestWorkspace", assessData.WorkspaceAffected);
            Assert.IsFalse(_dbContext.ChangeTracker.HasChanges());
        }

        [Test]
        public void Test_CreateCarisProject_Throws_InvalidOperationException_When_Invalid_Workspace_Provided()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _editDatabaseModel.OnPostCreateCarisProjectAsync(ProcessId, "Assess", "TestProject", "InvalidWorkspace"));
        }

        [Test]
        public void Test_CreateCarisProject_Throws_InvalidOperationException_When_No_HpdUser_Found()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _editDatabaseModel.OnPostCreateCarisProjectAsync(ProcessId, "Assess", "TestProject", "TestWorkspace"));
        }

        [Test]
        public async Task Test_OnGet_Retrieves_All_Usages()
        {
            _dbContext.HpdUsage.Add(new HpdUsage()
            {
                HpdUsageId = 1,
                Name = "Nav1"
            });

            _dbContext.HpdUsage.Add(new HpdUsage()
            {
                HpdUsageId = 2,
                Name = "Nav2"
            });

            _dbContext.HpdUsage.Add(new HpdUsage()
            {
                HpdUsageId = 3,
                Name = "Nav3"
            });

            await SetupForOnGetAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual(3, _editDatabaseModel.HpdUsages.Count);
            Assert.That(_dbContext.HpdUsage.Select(h => h.Name).ToList(), Is.EqualTo(_editDatabaseModel.HpdUsages));
        }

        [Test]
        public async Task Test_OnGet_Retrieves_Primary_Source_Document()
        {
            await SetupForOnGetAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual(1, _editDatabaseModel.SourceDocuments.Count);
        }

        [Test]
        public async Task Test_OnGet_Retrieves_Linked_Documents()
        {
            await SetupForOnGetAsync();

            _dbContext.LinkedDocument.Add(new LinkedDocument()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            });

            _dbContext.LinkedDocument.Add(new LinkedDocument()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            });

            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            //Expected value is 1 higher than added LinkedDocuments as an item
            //is added to SourceDocuments in SetupForOnGetAsync()
            Assert.AreEqual(3, _editDatabaseModel.SourceDocuments.Count);
        }

        [Test]
        public async Task Test_OnGet_Retrieves_Database_Documents()
        {
            await SetupForOnGetAsync();

            _dbContext.DatabaseDocumentStatus.Add(new DatabaseDocumentStatus()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            });


            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            //Expected value is 1 higher than added DatabaseDocumentStatus as an item
            //is added to SourceDocuments in SetupForOnGetAsync()
            Assert.AreEqual(2, _editDatabaseModel.SourceDocuments.Count);
        }

        [Test]
        public async Task Test_OnGet_SourceViewModel_Has_Been_Populated_For_Primary_Document()
        {
            await SetupForOnGetAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual("Source", _editDatabaseModel.SourceDocuments[0].DocumentName);

        }

        [Test]
        public async Task Test_OnGet_SourceViewModel_Has_Been_Populated_For_Linked_Document()
        {
            await SetupForOnGetAsync();

            _dbContext.LinkedDocument.Add(new LinkedDocument()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString(),
                SourceDocumentName = "LinkedSource"
            });

            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual("LinkedSource", _editDatabaseModel.SourceDocuments[1].DocumentName);

        }

        [Test]
        public async Task Test_OnGet_SourceViewModel_Has_Been_Populated_For_Database_Document()
        {
            await SetupForOnGetAsync();

            _dbContext.DatabaseDocumentStatus.Add(new DatabaseDocumentStatus()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString(),
                SourceDocumentName = "DatabaseSource"

            });

            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual("DatabaseSource", _editDatabaseModel.SourceDocuments[1].DocumentName);

        }

        [Test]
        public async Task Test_OnGet_Retrieves_Linked_Documents_And_Database_Documents()
        {
            await SetupForOnGetAsync();

            _dbContext.LinkedDocument.Add(new LinkedDocument()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            });

            _dbContext.DatabaseDocumentStatus.Add(new DatabaseDocumentStatus()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            });

            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            //Expected value is 1 higher than added DatabaseDocumentStatus as an item
            //is added to SourceDocuments in SetupForOnGetAsync()
            Assert.AreEqual(3, _editDatabaseModel.SourceDocuments.Count);
        }

        [Test]
        public async Task Test_OnGet_Only_Documents_With_Matching_ProcessId_Are_Added()
        {
            await SetupForOnGetAsync();

            _dbContext.AssessmentData.Add(new AssessmentData()
            {
                ProcessId = 456,
                SourceDocumentName = "Source2",
                RsdraNumber = "RSDRA456"
            });

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                ProcessId = 456,
                WorkspaceAffected = "AWorkspace2"
            });

            _dbContext.LinkedDocument.Add(new LinkedDocument()
            {
                ProcessId = 456,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            });

            _dbContext.DatabaseDocumentStatus.Add(new DatabaseDocumentStatus()
            {
                ProcessId = 456,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            });

            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual(1, _editDatabaseModel.SourceDocuments.Count);
            Assert.AreEqual("Source", _editDatabaseModel.SourceDocuments[0].DocumentName);

        }

        [Test]
        public async Task Test_OnGet_Given_Document_Status_Is_Not_FileGenerated_Then_Sources_List_Is_Empty()
        {
            await SetupForOnGetAsync();

            var primaryDocumentStatus = _dbContext.PrimaryDocumentStatus.First(pds => pds.ProcessId == ProcessId);
            primaryDocumentStatus.Status = SourceDocumentRetrievalStatus.Started.ToString();

            _dbContext.LinkedDocument.Add(new LinkedDocument()
            {
                ProcessId = ProcessId,
                Status = LinkedDocumentRetrievalStatus.NotAttached.ToString()
            });

            _dbContext.DatabaseDocumentStatus.Add(new DatabaseDocumentStatus()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.Started.ToString()
            });

            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual(0, _editDatabaseModel.SourceDocuments.Count);
        }

        [Test]
        public async Task Test_OnGet_Sets_Project_Name_To_Default_Value_When_At_Assess()
        {
            // Default being: {processId}_{parsedRsdraNumber}_{sourceDocumentName}

            _generalConfig.Value.CarisProjectNameCharacterLimit = 50;

            await SetupForOnGetAsync();

            var thisAssessmentData = _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId);
            var parsedRsdraNumber = thisAssessmentData.ParsedRsdraNumber;
            var sourceDocumentName = thisAssessmentData.SourceDocumentName;

            A.CallTo(() => _fakeCarisProjectNameGenerator.Generate(ProcessId, parsedRsdraNumber, sourceDocumentName))
                .Returns($"{ProcessId}_{parsedRsdraNumber}_{sourceDocumentName}");

            await _editDatabaseModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual("123_123_Source", _editDatabaseModel.ProjectName);
        }

        [Test]
        public async Task Test_OnGet_Sets_Project_Name_To_Warning_When_At_Verify_And_No_Project_Has_Been_Created_At_Assess()
        {
            await SetupForOnGetAsync();

            _dbContext.DbAssessmentVerifyData.Add(new DbAssessmentVerifyData()
            {
                ProcessId = ProcessId,
                WorkspaceAffected = "AWorkspace"
            });

            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Verify");

            Assert.AreEqual("NO PROJECT WAS CREATED AT ASSESS", _editDatabaseModel.ProjectName);
        }

        [Test]
        public async Task Test_OnGet_Sets_Project_Name_To_Name_Given_At_Assess_When_At_Verify()
        {
            await SetupForOnGetAsync();

            _dbContext.DbAssessmentVerifyData.Add(new DbAssessmentVerifyData()
            {
                ProcessId = ProcessId,
                WorkspaceAffected = "AWorkspace"
            });

            _dbContext.CarisProjectDetails.Add(new CarisProjectDetails()
            {
                ProcessId = ProcessId,
                ProjectName = "Name given at Assess"
            });

            await _dbContext.SaveChangesAsync();

            await _editDatabaseModel.OnGetAsync(ProcessId, "Verify");

            Assert.AreEqual("Name given at Assess", _editDatabaseModel.ProjectName);
        }

        private async Task SetupForOnGetAsync()
        {
            _dbContext.AssessmentData.Add(new AssessmentData()
            {
                ProcessId = ProcessId,
                SourceDocumentName = "Source",
                RsdraNumber = "RSDRA123"
            });

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = ProcessId,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            });

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                ProcessId = ProcessId,
                WorkspaceAffected = "AWorkspace"
            });

            await _dbContext.SaveChangesAsync();
        }

        [Test]
        public void Test_OnGetLaunchSourceEditor_Throws_ArgumentException_When_No_Usages_Supplied()
        {
            var taskStage = "Assess";

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _editDatabaseModel.OnGetLaunchSourceEditorAsync(
                                                                            ProcessId,
                                                                            taskStage,
                                                                            "SomeFilename",
                                                                            null,
                                                                            null));
            Assert.AreEqual("Failed to generate session file. No Hpd Usages were selected.", ex.Message);
            A.CallTo(() => _fakeSessionFileGenerator.PopulateSessionFile(
                                                                            ProcessId,
                                                                            UserFullName,
                                                                            taskStage,
                                                                            A<CarisProjectDetails>.Ignored,
                                                                            A<List<string>>.Ignored,
                                                                            A<List<string>>.Ignored)).MustNotHaveHappened();

        }

        [Test]
        public void Test_OnGetLaunchSourceEditor_Throws_ArgumentException_When_No_Caris_Project_Was_Created()
        {
            var taskStage = "Assess";

            var selectedUsages = new List<string>()
            {
                "Usage1",
                "Usage2"
            };

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => _editDatabaseModel.OnGetLaunchSourceEditorAsync(
                    ProcessId,
                    taskStage,
                    "SomeFilename",
                    selectedUsages,
                    null));
            Assert.AreEqual("Failed to generate session file. Caris project was never created.", ex.Message);
            A.CallTo(() => _fakeSessionFileGenerator.PopulateSessionFile(
                ProcessId,
                UserFullName,
                taskStage,
                A<CarisProjectDetails>.Ignored,
                A<List<string>>.Ignored,
                A<List<string>>.Ignored)).MustNotHaveHappened();

        }

        [Test]
        public async Task Test_OnGetLaunchSourceEditor_When_Supplied_With_Correct_Data_Then_Generates_Session_File()
        {
            var taskStage = "Assess";
            var sessionFilename = "SomeFilename";

            var selectedUsages = new List<string>()
            {
                "Usage1",
                "Usage2"
            };

            var carisproject = new CarisProjectDetails()
            {
                ProcessId = ProcessId,
                ProjectId = 123456,
                ProjectName = "SomeName",
                Created = DateTime.Today,
                CreatedBy = UserFullName
            };

            await _dbContext.CarisProjectDetails.AddAsync(carisproject);

            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakeSessionFileGenerator.PopulateSessionFile(
                ProcessId,
                UserFullName,
                taskStage,
                carisproject,
                selectedUsages,
                A<List<string>>.Ignored)).Returns(new SessionFile()
                                                                            {
                                                                                DataSources = new SessionFile.DataSourcesNode(),
                                                                                Properties = new SessionFile.PropertiesNode(),
                                                                                Version = "1.1",
                                                                                Views = new SessionFile.ViewsNode()
                                                                            });

            var response = await _editDatabaseModel.OnGetLaunchSourceEditorAsync(
                                                                                            ProcessId,
                                                                                            taskStage,
                                                                                            sessionFilename,
                                                                                            selectedUsages,
                                                                                            null);

            A.CallTo(() => _fakeSessionFileGenerator.PopulateSessionFile(
                ProcessId,
                UserFullName,
                taskStage,
                carisproject,
                selectedUsages,
                A<List<string>>.Ignored)).MustHaveHappened();

            var sessionFileStream = response as FileStreamResult;
            Assert.IsNotNull(sessionFileStream);
            Assert.AreEqual(sessionFilename, sessionFileStream.FileDownloadName);


        }
    }
}

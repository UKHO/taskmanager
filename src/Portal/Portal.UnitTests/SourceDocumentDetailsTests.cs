using System;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Portal.Configuration;
using Portal.HttpClients;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class SourceDocumentDetailsTests
    {
        private WorkflowDbContext _dbContext;
        private OptionsSnapshotWrapper<GeneralConfig> _generalConfigWrapper;
        private OptionsSnapshotWrapper<UriConfig> _uriConfigWrapper;
        private int ProcessId { get; set; }

        public IEventServiceApiClient _HttpClient { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            var httpClient = new HttpClient();

            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var generalConfig = GetGeneralConfigs(appConfigurationConfigRoot);
            var uriConfig = GetUriConfigs(appConfigurationConfigRoot);
            var generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(generalConfig);
            _uriConfigWrapper = new OptionsSnapshotWrapper<UriConfig>(uriConfig);

            _HttpClient = new EventServiceApiClient(httpClient, generalConfigOptions, _uriConfigWrapper);

            ProcessId = 123;
        }

        [Test]
        public void Test_InvalidOperationException_thrown_when_no_assessmentdata_exists()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = ProcessId,
                ActivityName = "GregTest",
                AssessmentData = null,
                SerialNumber = "123_sn",
                Status = "Started",
                WorkflowType = "DbAssessment"
            });

            _dbContext.SaveChanges();

            var sourceDocumentDetailsModel = new _SourceDocumentDetailsModel(_dbContext, null, null);
            var ex = Assert.Throws<InvalidOperationException>(() =>
                sourceDocumentDetailsModel.OnGet());
            Assert.AreEqual("Unable to retrieve AssessmentData", ex.Data["OurMessage"]);
        }

        [Test]
        public void Test_no_exception_thrown_when_no_sourcedocumentstatus_row_exists()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = ProcessId,
                ActivityName = "GregTest",
                AssessmentData = null,
                SerialNumber = "123_sn",
                Status = "Started",
                WorkflowType = "DbAssessment"
            });
            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = ProcessId,
                SdocId = 123456,
                SourceDocumentName = "MyName",
                RsdraNumber = "12345",
                ReceiptDate = DateTime.Now,
                EffectiveStartDate = DateTime.Now,
                SourceNature = "Au naturale",
                Datum = "What",
                SourceDocumentType = "This",
                TeamDistributedTo = "HW"
            });
            _dbContext.SaveChanges();

            var sourceDocumentDetailsModel = new _SourceDocumentDetailsModel(_dbContext, null, null) { ProcessId = ProcessId };
            Assert.DoesNotThrow(() => sourceDocumentDetailsModel.OnGet());
        }

        [Test]
        public async Task Test_InitiateSourceDocumentRetrievalEvent_publishes_when_OnPostAttachLinkedDocumentAsync_invoked()
        {
            var sourceDocumentDetailsModel = new _SourceDocumentDetailsModel(_dbContext, _uriConfigWrapper, _HttpClient)
            {
                SourceDocumentStatus = new SourceDocumentStatus
                {
                    ProcessId = ProcessId,
                    SdocId = 12345,
                    CorrelationId = null,
                    Status = "Ready",
                    ContentServiceId = null,
                    StartedAt = DateTime.Now
                }
            };

            await sourceDocumentDetailsModel.OnPostAttachLinkedDocumentAsync(1235);
        }

        private GeneralConfig GetGeneralConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var generalConfig = new GeneralConfig();

            appConfigurationConfigRoot.GetSection("apis").Bind(generalConfig);

            return generalConfig;
        }

        private UriConfig GetUriConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var uriConfig = new UriConfig();

            appConfigurationConfigRoot.GetSection("urls").Bind(uriConfig);

            return uriConfig;
        }

    }
}

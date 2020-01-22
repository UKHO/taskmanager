using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using NUnit.Framework;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Models;

namespace WorkflowCoordinator.IntegrationTests.EndToEndK2WorkflowTests
{
    public class WorkflowEndToEndTests
    {
        private WorkflowServiceApiClient _workflowServiceApiClient;
        

        [SetUp]
        public void Setup()
        {
            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultConfigRoot = AzureKeyVaultConfigConfigurationRoot.Instance;
            var uriConfig = GetUriConfigs(appConfigurationConfigRoot);
            var generalConfig = GetGeneralConfigs(appConfigurationConfigRoot);
            var startupSecretsConfig = GetSecretsConfigs(keyVaultConfigRoot);
            IOptionsSnapshot<GeneralConfig> generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(generalConfig);
            IOptionsSnapshot<UriConfig> uriConfigOptions = new OptionsSnapshotWrapper<UriConfig>(uriConfig);

            _workflowServiceApiClient = SetupWorkflowServiceApiClient(startupSecretsConfig, generalConfigOptions, uriConfigOptions);
        }

        [Test]
        public async Task Test()
        {
            var workflowId = await _workflowServiceApiClient.GetDBAssessmentWorkflowId();
            var processId = await _workflowServiceApiClient.CreateWorkflowInstance(workflowId);
            var serialNumber = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(processId);

            serialNumber = await ProgressAndValidate(processId, serialNumber, "Review", "Assess");
            serialNumber = await ProgressAndValidate(processId, serialNumber, "Assess", "Verify");
            await ProgressAndValidate(processId, serialNumber, "Verify", "Complete");
            
        }
        private async Task<string> ProgressAndValidate(int processId, string serialNumber, string fromTask, string toTask)
        {
            var success = await _workflowServiceApiClient.ProgressWorkflowInstance(processId, serialNumber);
            Assert.IsTrue(success, $"Task has not moved from {fromTask} to {toTask}.");
            
            var k2Task = await _workflowServiceApiClient.GetWorkflowInstanceData(processId);
            Assert.IsTrue(k2Task.ActivityName.Equals(toTask, StringComparison.OrdinalIgnoreCase));

            return k2Task.SerialNumber;
        }

        private WorkflowServiceApiClient SetupWorkflowServiceApiClient(StartupSecretsConfig startupSecretsConfig,
            IOptions<GeneralConfig> generalConfigOptions, IOptions<UriConfig> uriConfig)
        {
            return new WorkflowServiceApiClient(
                new HttpClient(
                    new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                        Credentials = new NetworkCredential(startupSecretsConfig.K2RestApiUsername, startupSecretsConfig.K2RestApiPassword)
                    }
                ), generalConfigOptions, uriConfig);
        }

        private StartupSecretsConfig GetSecretsConfigs(IConfigurationRoot keyVaultConfigRoot)
        {
            var startupSecretsConfig = new StartupSecretsConfig();

            keyVaultConfigRoot.GetSection("K2RestApi").Bind(startupSecretsConfig);

            return startupSecretsConfig;
        }

        private GeneralConfig GetGeneralConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var generalConfig = new GeneralConfig();

            appConfigurationConfigRoot.GetSection("k2").Bind(generalConfig);
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

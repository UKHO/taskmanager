using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;

namespace WorkflowCoordinator.IntegrationTests.EndToEndK2WorkflowTests
{
    public class WorkflowEndToEndTests
    {
        private WorkflowServiceApiClient _workflowServiceApiClient;
        private int _processId;

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

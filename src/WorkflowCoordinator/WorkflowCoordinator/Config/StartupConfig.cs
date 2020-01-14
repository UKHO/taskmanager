using System;

namespace WorkflowCoordinator.Config
{
    public class StartupConfig
    {
        public string WorkflowCoordinatorName { get; set; }
        public string WorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public Guid AssessmentPollingSagaCorrelationGuid { get; set; }

    }
}

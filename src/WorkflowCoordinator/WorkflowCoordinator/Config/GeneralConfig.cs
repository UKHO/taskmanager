using System;

namespace WorkflowCoordinator.Config
{
    public class GeneralConfig
    {
        public Uri DataAccessLocalhostBaseUri { get; set; }
        public Uri DataServicesWebServiceBaseUri { get; set; }
        public Uri DataServicesWebServiceDocumentsForAssessmentUri { get; set; }
        public string SourceDocumentCoordinatorName { get; set; }
        public string WorkflowCoordinatorName { get; set; }
        public string CallerCode { get; set; }
        public int WorkflowCoordinatorAssessmentPollingIntervalSeconds { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public Guid AssessmentPollingSagaCorrelationGuid { get; set; }
        public Uri K2WebServiceBaseUri { get; set; }
        public Uri K2WebServiceGetWorkflowsUri { get; set; }
        public string DBAssessmentWorkflowName { get; set; }
    }
}

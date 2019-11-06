using System;

namespace WorkflowCoordinator.Config
{
    public class GeneralConfig
    {
        public string SourceDocumentCoordinatorName { get; set; }
        public string WorkflowCoordinatorName { get; set; }
        public string EventServiceName { get; set; }
        public string CallerCode { get; set; }
        public int WorkflowCoordinatorAssessmentPollingIntervalSeconds { get; set; }
        public string LocalDbServer { get; set; }
       
        public Guid AssessmentPollingSagaCorrelationGuid { get; set; }
        public string K2DBAssessmentWorkflowName { get; set; }
    }
}

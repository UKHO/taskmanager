using System;
using Common.Helpers;

namespace WorkflowCoordinator.Config
{
    public class UriConfig
    {
        public Uri DataAccessLocalhostBaseUri { get; set; }
        public Uri DataServicesWebServiceBaseUri { get; set; }
        public Uri DataServicesWebServiceDocumentsForAssessmentUri { get; set; }
        public Uri DataServicesDocumentAssessmentDataUri { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public Uri K2WebServiceBaseUri { get; set; }
        public Uri K2WebServiceGetWorkflowsUri { get; set; }
        public Uri K2WebServiceStartWorkflowInstanceUri { get; set; }
        public Uri K2WebServiceTerminateWorkflowInstanceUri { get; set; }
        public Uri K2WebServiceGetTasksUri { get; set; }
        public Uri DataServicesLocalhostHealthcheckUrl { get; set; }
        public Uri DataServicesHealthcheckUrl { get; set; }

        public Uri BuildDataServicesUri(string callerCode, int sdocId)
        {
            return sdocId == 0 ? new Uri(ConfigHelpers.IsLocalDevelopment ? DataAccessLocalhostBaseUri : DataServicesWebServiceBaseUri, $@"{DataServicesWebServiceDocumentsForAssessmentUri}{callerCode}") : 
                new Uri(ConfigHelpers.IsLocalDevelopment ? DataAccessLocalhostBaseUri : DataServicesWebServiceBaseUri, $@"{DataServicesDocumentAssessmentDataUri}{sdocId}");
        }
    }
}

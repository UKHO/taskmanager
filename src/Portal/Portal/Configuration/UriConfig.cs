using System;
using Common.Helpers;

namespace Portal.Configuration
{
    public class UriConfig
    {
        public Uri ContentServiceBaseUrl { get; set; }
        public Uri DataAccessLocalhostBaseUri { get; set; }
        public Uri DataServicesWebServiceBaseUri { get; set; }
        public string DataServicesWebServiceAssessmentCompletedUri { get; set; }
        public Uri K2WebServiceBaseUri { get; set; }
        public Uri K2WebServiceGetTasksUri { get; set; }
        public Uri K2WebServiceTerminateWorkflowInstanceUri { get; set; }
        public Uri K2WebServiceGetWorkflowsUri { get; set; }

        public Uri BuildDataServicesUri(string callerCode, int sdocId, string comment)
        {
            return new Uri(
                ConfigHelpers.IsLocalDevelopment ? DataAccessLocalhostBaseUri : DataServicesWebServiceBaseUri,
                $@"{DataServicesWebServiceAssessmentCompletedUri}{callerCode}/{sdocId}?comment={Uri.EscapeDataString(comment)}");
        }
    }
}

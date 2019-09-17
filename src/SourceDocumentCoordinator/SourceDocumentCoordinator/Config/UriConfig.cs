using System;
using Common.Helpers;

namespace SourceDocumentCoordinator.Config
{
    public class UriConfig
    {
        public Uri DataAccessLocalhostBaseUri { get; set; }
        public Uri DataServicesLocalhostHealthcheckUrl { get; set; }
        public Uri DataServicesHealthcheckUrl { get; set; }
        public Uri DataServicesWebServiceBaseUri { get; set; }
        public Uri DataServicesWebServiceGetDocumentForViewingUri { get; set; }
        public Uri DataServicesWebServiceDocumentRequestQueueStatusUri { get; set; }
        public Uri DataServicesWebServiceDeleteDocumentRequestJobFromQueueUri { get; set; }
        public Uri DataServicesWebServiceHealthcheckUri { get; set; }
        public Uri AzureDbTokenUrl { get; set; }

        public Uri BuildDataServicesBaseUri()
        {
            return ConfigHelpers.IsLocalDevelopment ? DataAccessLocalhostBaseUri : DataServicesWebServiceBaseUri;

        }
    }
}

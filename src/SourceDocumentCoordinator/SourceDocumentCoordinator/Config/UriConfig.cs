using System;
using Common.Helpers;

namespace SourceDocumentCoordinator.Config
{
    public class UriConfig
    {
        public Uri DataAccessLocalhostBaseUri { get; set; }
        public Uri DataServicesLocalhostHealthcheckUrl { get; set; }
        public Uri DataServicesHealthcheckUrl { get; set; }
        public Uri DataServicesDocumentAssessmentDataUri { get; set; }
        public Uri DataServicesWebServiceBaseUri { get; set; }
        public Uri DataServicesWebServiceGetDocumentForViewingUri { get; set; }
        public Uri DataServicesWebServiceDocumentRequestQueueStatusUri { get; set; }
        public Uri DataServicesWebServiceDeleteDocumentRequestJobFromQueueUri { get; set; }
        public Uri DataServicesWebServiceSepLinksUri { get; set; }
        public Uri DataServicesWebServiceForwardLinksUri { get; set; }
        public Uri DataServicesWebServiceBackwardLinksUri { get; set; }
        public Uri DataServicesWebServiceDocumentsFromListUri { get; set; }
        public string DataServicesWebServiceDocumentsFromListUriSdocIdQuery { get; set; }  
        public Uri DataServicesWebServiceHealthcheckUri { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public Uri SourceDocumentServiceLocalhostBaseUrl { get; set; }
        public Uri SourceDocumentServiceBaseUrl { get; set; }
        public Uri SourceDocumentServicePostDocumentUrl { get; set; }

        public Uri BuildDataServicesBaseUri()
        {
            return ConfigHelpers.IsLocalDevelopment ? DataAccessLocalhostBaseUri : DataServicesWebServiceBaseUri;
        }

        public Uri BuildDataServicesUri(int sdocId)
        {
            return new Uri(ConfigHelpers.IsLocalDevelopment ? DataAccessLocalhostBaseUri : DataServicesWebServiceBaseUri, $@"{DataServicesDocumentAssessmentDataUri}{sdocId}");
        }

        public Uri BuildSourceDocumentServicePostDocumentUri(int processId, int sdocId, string filename)
        {
            return new Uri(
                ConfigHelpers.IsLocalDevelopment ? SourceDocumentServiceLocalhostBaseUrl : SourceDocumentServiceBaseUrl,
                $@"{SourceDocumentServicePostDocumentUrl}{processId}/{sdocId}/{filename}");
        }
    }
}

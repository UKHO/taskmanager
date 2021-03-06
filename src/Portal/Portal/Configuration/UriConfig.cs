﻿using System;
using Common.Helpers;

namespace Portal.Configuration
{
    public class UriConfig
    {
        public Uri ContentServiceBaseUrl { get; set; }
        public Uri DataAccessLocalhostBaseUri { get; set; }
        public Uri DataServicesWebServiceBaseUri { get; set; }
        public Uri EventServiceLocalhostBaseUri { get; set; }
        public Uri EventServiceWebServiceBaseUri { get; set; }
        public Uri EventServiceWebServicePostEventUrl { get; set; }
        public Uri DataServicesDocumentAssessmentDataUri { get; set; }
        public Uri LocalDevLandingPageHttpsUrl { get; set; }
        public Uri LandingPageUrl { get; set; }

        public Uri BuildEventServiceUri(string eventName)
        {
            return new Uri(
                ConfigHelpers.IsLocalDevelopment ? EventServiceLocalhostBaseUri : EventServiceWebServiceBaseUri,
                $@"{EventServiceWebServicePostEventUrl}{Uri.EscapeDataString(eventName)}");
        }

        public Uri BuildContentServiceUri(Guid fileGuid)
        {
            return new Uri(ContentServiceBaseUrl, fileGuid + "/data");
        }

        public Uri BuildDocumentAssessmentDataDataServicesUri(int sdocId)
        {
            return new Uri(ConfigHelpers.IsLocalDevelopment ? DataAccessLocalhostBaseUri : DataServicesWebServiceBaseUri, $@"{DataServicesDocumentAssessmentDataUri}{sdocId}");
        }
    }
}

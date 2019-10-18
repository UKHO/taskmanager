using Common.Helpers;

using System;

namespace Portal.Configuration
{
    public class UriConfig
    {
        public Uri DataAccessLocalhostBaseUri { get; set; }
        public Uri DataServicesWebServiceBaseUri { get; set; }
        public string DataServicesWebServiceAssessmentCompletedUri { get; set; }


        public Uri BuildDataServicesUri(string callerCode, int sdocId, string comment)
        {
            return new Uri(
                ConfigHelpers.IsLocalDevelopment ? DataAccessLocalhostBaseUri : DataServicesWebServiceBaseUri,
                $@"{DataServicesWebServiceAssessmentCompletedUri}{callerCode}/{sdocId}/{comment}");
        }
    }
}

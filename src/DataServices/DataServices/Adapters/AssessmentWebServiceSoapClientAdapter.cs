using System.ServiceModel;
using DataServices.Config;
using DataServices.Connected_Services.SDRAAssessmentWebService;
using Microsoft.Extensions.Options;

namespace DataServices.Adapters
{
    public class AssessmentWebServiceSoapClientAdapter : IAssessmentWebServiceSoapClientAdapter
    {
        private readonly IOptionsSnapshot<Settings> _settings;
        private readonly IOptions<SecretsConfig> _secretsConfig;

        public AssessmentWebServiceSoapClientAdapter(IOptionsSnapshot<Settings> settings, IOptions<SecretsConfig> secretsConfig)
        {
            _settings = settings;
            _secretsConfig = secretsConfig;
        }

        public SDRAExternalInterfaceAssessmentWebServiceSoap SoapClient
        {
            get
            {
                var result = new BasicHttpBinding
                {
                    MaxBufferSize = int.MaxValue,
                    MaxReceivedMessageSize = int.MaxValue,
                    Security =
                    {
                        Mode = BasicHttpSecurityMode.Transport,
                        Transport = {ClientCredentialType = HttpClientCredentialType.Basic}
                    }
                };
                
                var client = new SDRAExternalInterfaceAssessmentWebServiceSoapClient(result,
                    new EndpointAddress(_settings.Value.AssessmentWebServiceUri));

                client.ClientCredentials.UserName.UserName = _secretsConfig.Value.SdraWebserviceUsername;
                client.ClientCredentials.UserName.Password = _secretsConfig.Value.SdraWebservicePassword;

                return client;
            }
        }
    }

    public interface IAssessmentWebServiceSoapClientAdapter
    {
        SDRAExternalInterfaceAssessmentWebServiceSoap SoapClient { get; }
    }
}


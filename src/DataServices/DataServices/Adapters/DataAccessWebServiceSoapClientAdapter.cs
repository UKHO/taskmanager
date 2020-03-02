using System.ServiceModel;
using DataServices.Config;
using DataServices.Connected_Services.SDRADataAccessWebService;
using Microsoft.Extensions.Options;

namespace DataServices.Adapters
{
    public class DataAccessWebServiceSoapClientAdapter : IDataAccessWebServiceSoapClientAdapter
    {
        private readonly IOptionsSnapshot<Settings> _settings;
        private readonly IOptions<SecretsConfig> _secretsConfig;

        public DataAccessWebServiceSoapClientAdapter(IOptionsSnapshot<Settings> settings, IOptions<SecretsConfig> secretsConfig)
        {
            _settings = settings;
            _secretsConfig = secretsConfig;
        }

        public SDRAExternalInterfaceDataAccessWebServiceSoap SoapClient
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

                var client = new SDRAExternalInterfaceDataAccessWebServiceSoapClient(result,
                    new EndpointAddress(_settings.Value.DataAccessWebServiceUri)
                );

                client.ClientCredentials.UserName.UserName = _secretsConfig.Value.SdraWebserviceUsername;
                client.ClientCredentials.UserName.Password = _secretsConfig.Value.SdraWebservicePassword;

                return client;
            }
        }
    }

    public interface IDataAccessWebServiceSoapClientAdapter
    {
        SDRAExternalInterfaceDataAccessWebServiceSoap SoapClient { get; }
    }
}


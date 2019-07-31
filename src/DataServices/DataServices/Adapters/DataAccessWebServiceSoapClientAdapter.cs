using System.ServiceModel;
using DataServices.Config;
using DataServices.Connected_Services.SDRADataAccessWebService;
using Microsoft.Extensions.Options;

namespace DataServices.Adapters
{
    public class DataAccessWebServiceSoapClientAdapter : IDataAccessWebServiceSoapClientAdapter
    {
        private readonly IOptionsSnapshot<Settings> _settings;

        public DataAccessWebServiceSoapClientAdapter(IOptionsSnapshot<Settings> settings)
        {
            _settings = settings;
        }

        public SDRAExternalInterfaceDataAccessWebServiceSoap SoapClient
        {
            get
            {
                var result = new BasicHttpBinding
                {
                    MaxBufferSize = int.MaxValue,
                    MaxReceivedMessageSize = int.MaxValue
                };

                return new SDRAExternalInterfaceDataAccessWebServiceSoapClient(result,
                    new EndpointAddress(_settings.Value.DataAccessWebServiceUri)
                );
            }
        }
    }

    public interface IDataAccessWebServiceSoapClientAdapter
    {
        SDRAExternalInterfaceDataAccessWebServiceSoap SoapClient { get; }
    }
}


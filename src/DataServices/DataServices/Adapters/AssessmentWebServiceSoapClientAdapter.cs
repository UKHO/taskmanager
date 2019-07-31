using System.ServiceModel;
using DataServices.Config;
using DataServices.Connected_Services.SDRAAssessmentWebService;
using Microsoft.Extensions.Options;

namespace DataServices.Adapters
{
    public class AssessmentWebServiceSoapClientAdapter : IAssessmentWebServiceSoapClientAdapter
    {
        private readonly IOptionsSnapshot<Settings> _settings;

        public AssessmentWebServiceSoapClientAdapter(IOptionsSnapshot<Settings> settings)
        {
            _settings = settings;
        }

        public SDRAExternalInterfaceAssessmentWebServiceSoap SoapClient
        {
            get
            {
                var result = new BasicHttpBinding
                {
                    MaxBufferSize = int.MaxValue,
                    MaxReceivedMessageSize = int.MaxValue
                };

                return new SDRAExternalInterfaceAssessmentWebServiceSoapClient(result,
                    new EndpointAddress(_settings.Value.AssessmentWebServiceUri)
                );
            }
        }
    }

    public interface IAssessmentWebServiceSoapClientAdapter
    {
        SDRAExternalInterfaceAssessmentWebServiceSoap SoapClient { get; }
    }
}


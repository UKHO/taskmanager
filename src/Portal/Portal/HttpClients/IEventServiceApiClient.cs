using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Portal.HttpClients
{
    public interface IEventServiceApiClient
    {
        Task PostEvent(string eventName, JObject eventBody);
    }
}
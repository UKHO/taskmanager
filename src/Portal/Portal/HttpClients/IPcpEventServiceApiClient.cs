using System.Threading.Tasks;

namespace Portal.HttpClients
{
    public interface IPcpEventServiceApiClient
    {
        Task PostEvent<T>(string eventName, T eventBody) where T : class, UKHO.Events.IUkhoEvent, new();
    }
}
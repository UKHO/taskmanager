using System.Threading.Tasks;
using Common.Messages;

namespace Portal.HttpClients
{
    public interface IEventServiceApiClient
    {
        Task PostEvent<T>(string eventName, T eventBody) where T : class, ICorrelate, new();
    }
}
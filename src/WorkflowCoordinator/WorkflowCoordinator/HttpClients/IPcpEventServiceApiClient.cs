using System.Threading.Tasks;

namespace WorkflowCoordinator.HttpClients
{
    public interface IPcpEventServiceApiClient
    {
        Task PostEvent<T>(string eventName, T eventBody) where T : class, UKHO.Events.IUkhoEvent, new();
    }
}
using System.Threading.Tasks;

namespace Portal.Helpers
{
    public interface ICommentsHelper
    {
        Task AddComment(string comment, int processId, int workflowInstanceId, string userFullName);
    }
}
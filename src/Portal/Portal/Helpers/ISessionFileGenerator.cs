using System.Threading.Tasks;
using Portal.Models;

namespace Portal.Helpers
{
    public interface ISessionFileGenerator
    {
        Task<SessionFile> PopulateSessionFile(int processId, string userFullName);
    }
}
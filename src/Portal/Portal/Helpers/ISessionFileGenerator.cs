using System.Collections.Generic;
using System.Threading.Tasks;
using Portal.Models;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public interface ISessionFileGenerator
    {
        Task<SessionFile> PopulateSessionFile(int processId, string userPrincipalName, string taskStage, CarisProjectDetails carisProjectDetails, List<string> selectedHpdUsages, List<string> selectedSources);
    }
}
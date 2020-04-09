using System.Collections.Generic;
using System.Threading.Tasks;
using Portal.Models;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public interface ISessionFileGenerator
    {
        Task<SessionFile> PopulateSessionFile(int processId, string userFullName, string taskStage, CarisProjectDetails carisProjectDetails, List<string> selectedHpdUsages, List<string> selectedSources);
    }
}
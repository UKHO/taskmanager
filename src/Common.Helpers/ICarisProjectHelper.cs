using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public interface ICarisProjectHelper
    {
        Task<int> CreateCarisProject(int k2ProcessId, string projectName, string creatorHpdUsername,
            List<string> assignedHpdUsernames, string projectType, string projectStatus, string projectPriority,
            int carisTimeout, string workspace);
    }
}
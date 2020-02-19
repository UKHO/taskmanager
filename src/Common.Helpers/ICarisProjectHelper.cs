using System.Threading.Tasks;

namespace Common.Helpers
{
    public interface ICarisProjectHelper
    {
        Task<int> CreateCarisProject(int k2ProcessId, string projectName, string creatorHpdUsername,
            string projectType, string projectStatus, string projectPriority,
            int carisTimeout, string workspace);

        Task UpdateCarisProject(int projectId, string assignedUsername, int carisTimeout);
    }
}
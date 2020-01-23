using System.Collections.Generic;
using System.Threading.Tasks;

namespace NCNEPortal.Auth
{
    public interface IDirectoryService
    {
        Task<IEnumerable<string>> GetGroupMembers();
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Auth
{
    public interface IDirectoryService
    {
        Task<IEnumerable<string>> GetGroupMembers();
    }
}
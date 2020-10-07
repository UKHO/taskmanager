using System.Threading.Tasks;

namespace SourceDocumentService.Helpers
{
    public interface ICuiaDatabaseHelper
    {
        Task<int> GetNextWreckIdAsync();
    }
}
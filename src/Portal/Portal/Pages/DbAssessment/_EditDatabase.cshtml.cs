using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Portal.Models;
using WorkflowDatabase.EF;

namespace Portal.Pages.DbAssessment
{
    public class _EditDatabaseModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;

        [DisplayName("Select CARIS Workspace:")]
        public string SelectedCarisWorkspace { get; set; }

        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        public _EditDatabaseModel(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(int processId)
        {
        }

        public async Task<JsonResult> OnGetWorkspacesAsync()
        {
            var cachedHpdWorkspaces = await _dbContext.CachedHpdWorkspace.Select(c => c.Name).ToListAsync();
            return new JsonResult(cachedHpdWorkspaces);
        }

        public async Task<IActionResult> OnGetLaunchSourceEditorAsync(int processId)
        {
            var something = processId;

            var sessionFile = new SessionFile
            {
                CarisWorkspace =
                {
                    DataSources = new SessionFile.DataSources(),
                    Properties = new SessionFile.Properties(),
                    Version = "My first version"
                },
                DataSourceProp =
                {
                    SourceParam = new SessionFile.SourceParam(),
                    SourceString = "My source string",
                    UserLayers = "Layers prop"
                },
                SelectedProjectUsages =
                {
                    Value = "Project usage!"
                }
            };

            var serializer = new XmlSerializer(typeof(SessionFile));

            byte[] a = new byte[1000000];

            var fs = new MemoryStream(a, true);

            serializer.Serialize(fs, sessionFile);

            fs.Position = 0;

            return File(fs, MediaTypeNames.Application.Xml, "blibble.wrk");
        }
    }
}
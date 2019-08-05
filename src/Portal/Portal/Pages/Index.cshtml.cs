using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        public IList<Task> Tasks { get; set; }

        public IndexModel(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void OnGet()
        {
            Tasks = _dbContext.Tasks.ToList();
        }
    }
}

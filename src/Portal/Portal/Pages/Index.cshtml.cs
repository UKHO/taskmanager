using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.ViewModels;
using WorkflowDatabase.EF;

namespace Portal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        public IList<TaskViewModel> Tasks { get; set; }

        public IndexModel(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void OnGet()
        {
            Tasks = _dbContext.WorkflowInstance.ToList();
        }
    }
}

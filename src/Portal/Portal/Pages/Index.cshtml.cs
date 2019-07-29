using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.DataContext;
using System.Collections.Generic;
using System.Linq;

namespace Portal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TasksDbContext _dbContext;
        public IList<Models.Task> Tasks { get; set; }

        public IndexModel(TasksDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void OnGet()
        {
            Tasks = _dbContext.Tasks.ToList();
        }
    }
}

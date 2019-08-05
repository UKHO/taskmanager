using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using Database.SQL.EF;
using Database.SQL.EF.Models;

namespace Portal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TasksDbContext _dbContext;
        public IList<Task> Tasks { get; set; }

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

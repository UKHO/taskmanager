using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Portal.ViewModels;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;

        private readonly IMapper _mapper;

        public IList<TaskViewModel> Tasks { get; set; }

        public IndexModel(WorkflowDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public void OnGet()
        {
            var workflows = _dbContext.WorkflowInstance
                .Include(c => c.Comment)
                .Include(a => a.AssessmentData)
                .Include(d => d.DbAssessmentReviewData)
                .Where(wi => wi.Status == WorkflowStatus.Started.ToString())
                .ToList();

            this.Tasks = _mapper.Map<List<WorkflowInstance>, List<TaskViewModel>>(workflows);
        }

        public async Task<IActionResult> OnPostTaskNoteAsync(string taskNote, int processId)
        {
            return null;
        }
    }
}

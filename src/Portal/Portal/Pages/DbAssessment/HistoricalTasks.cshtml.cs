using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Portal.Models;
using Portal.ViewModels;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class HistoricalTasksModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IMapper _mapper;

        [BindProperty(SupportsGet = true)]
        public HistoricalTasksSearchParameters SearchParameters { get; set; }

        public List<HistoricalTasksData> HistoricalTasks { get; set; }

        public List<string> ErrorMessages { get; set; }

        public HistoricalTasksModel(WorkflowDbContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            ErrorMessages = new List<string>();
            HistoricalTasks = new List<HistoricalTasksData>();
        }

        public async Task OnGet()
        {
            // TODO: Get the latest 20 of finished tasks
            var workflows = await _dbContext.WorkflowInstance
                .Include(a => a.AssessmentData)
                .Include(d => d.DbAssessmentReviewData)
                .Include(vd => vd.DbAssessmentVerifyData)
                .Where(wi => wi.Status == WorkflowStatus.Completed.ToString() || wi.Status == WorkflowStatus.Terminated.ToString())
                .OrderByDescending(wi => wi.ActivityChangedAt)
                .Take(20)
                .ToListAsync();


            HistoricalTasks = _mapper.Map<List<WorkflowInstance>, List<HistoricalTasksData>>(workflows);

            foreach (var instance in workflows)
            {
                var task = HistoricalTasks.First(t => t.ProcessId == instance.ProcessId);
                SetUsersOnTask(instance, task);
            }

        }

        public void OnPost()
        {
            // TODO: Validate search parameters
            // TODO: Get results
            // TODO: Check results count. if zero or too large then warn user

            ErrorMessages = new List<string>()
            {
                "Error1",
                "Error2"
            };

        }

        private void SetUsersOnTask(WorkflowInstance instance, HistoricalTasksData task)
        {
            switch (task.TaskStage)
            {
                case WorkflowStage.Review:
                    task.Reviewer = instance.DbAssessmentReviewData.Reviewer;
                    task.Assessor = instance.DbAssessmentReviewData.Assessor;
                    task.Verifier = instance.DbAssessmentReviewData.Verifier;
                    break;
                case WorkflowStage.Assess:
                    throw new NotImplementedException($"{task.TaskStage} is not implemented.");
                case WorkflowStage.Completed:
                    task.Reviewer = instance.DbAssessmentVerifyData.Reviewer;
                    task.Assessor = instance.DbAssessmentVerifyData.Assessor;
                    task.Verifier = instance.DbAssessmentVerifyData.Verifier;
                    break;
                default:
                    throw new NotImplementedException($"{task.TaskStage} is not implemented.");
            }
        }
    }
}
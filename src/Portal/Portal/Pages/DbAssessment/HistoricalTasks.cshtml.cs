using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.Models;
using WorkflowDatabase.EF;

namespace Portal.Pages.DbAssessment
{
    public class HistoricalTasksModel : PageModel
    {

        [BindProperty(SupportsGet = true)]
        public HistoricalTasksSearchParameters SearchParameters { get; set; }

        public List<HistoricalTasksData> HistoricalTasks { get; set; }

        public List<string> ErrorMessages { get; set; }

        public HistoricalTasksModel()
        {
            ErrorMessages = new List<string>();
            HistoricalTasks = new List<HistoricalTasksData>();
        }

        public void OnGet()
        {
            // TODO: Get the latest 20 of finished tasks
            HistoricalTasks.Add(new HistoricalTasksData()
            {
                WorkflowInstanceId = 1,
                ProcessId = 1234,
                DmEndDate = new DateTime(2020, 05, 01),
                AssessmentDataRsdraNumber= "RSDRA20200501",
                AssessmentDataSourceDocumentName = "From galaxy far far away",
                TaskStage = WorkflowStage.Completed,
                Status = WorkflowStatus.Completed,
                Reviewer= "Rossal Sandford",
                Assessor = "Ben Hall",
                Verifier= "Greg Williams",
                Team = "Testing Team",
                ActivityChangedAt = new DateTime(2020,02,01)
            });

            HistoricalTasks.Add(new HistoricalTasksData()
            {
                WorkflowInstanceId = 2,
                ProcessId = 2345,
                DmEndDate = new DateTime(2020, 07, 01),
                AssessmentDataRsdraNumber = "RSDRA20200701",
                AssessmentDataSourceDocumentName = "From galaxy far far away",
                TaskStage = WorkflowStage.Completed,
                Status = WorkflowStatus.Completed,
                Reviewer = "Rossal Sandford",
                Assessor = "Ben Hall",
                Verifier = "Greg Williams",
                Team = "Testing Team",
                ActivityChangedAt = new DateTime(2020, 02, 01)
            });

        }

        public void OnPost()
        {
            // TODO: Validate search parameters
            // TODO: Get results
            // TODO: Check results count. if zero or too large then warn user

            if (ModelState.IsValid) return;

            ErrorMessages = new List<string>()
            {
                "Error1",
                "Error2"
            };

        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _AssignTaskModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        public int AssignTaskId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        public DbAssessmentReviewData PrimaryAssignedTask { get; set; }
        public List<DbAssessmentAssignTask> AdditionalAssignedTasks { get; set; }

        public SelectList Assessors { get; set; }
        public SelectList Verifiers { get; set; }
        public SelectList AssignedTaskTypes { get; set; }
        
        public int Ordinal { get; set; }

        public _AssignTaskModel(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync()
        {
            await PopulateDropDowns();
            await GetPrimaryAssignedTask();
            await GetAdditionalAssignTasks();
        }

        private async Task GetPrimaryAssignedTask()
        {
            try
            {
                PrimaryAssignedTask = await _dbContext
                    .DbAssessmentReviewData
                    .FirstAsync(c => c.ProcessId == ProcessId);
            }
            catch (InvalidOperationException e)
            {
                // Log and throw, as we're unable to get primary assigned task data
                e.Data.Add("OurMessage", "Unable to retrieve DbAssessmentReviewData");
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task GetAdditionalAssignTasks()
        {
            try
            {
                AdditionalAssignedTasks = await _dbContext
                    .DbAssessmentAssignTask
                    .Where(a => a.ProcessId == ProcessId)
                    .ToListAsync();
            }
            catch (ArgumentNullException e)
            {
                // Log and throw, as we're unable to get additional assigned tasks
                e.Data.Add("OurMessage", "Unable to retrieve additional assign tasks");
                Console.WriteLine(e);
                throw;
            }
        }


        private async Task<List<_AssignTaskModel>> SetAssignTaskDummyData(int processId)
        {
            return null;

        }

        /// <summary>
        /// TODO - Remove assessor and verifier code retrieval from JSON once we are reading users from AD
        /// </summary>
        private async Task PopulateDropDowns()
        {
            if (!System.IO.File.Exists(@"Data\Users.json")) throw new FileNotFoundException(@"Data\Users.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\Users.json");
            var users = JsonConvert.DeserializeObject<IEnumerable<Assessor>>(jsonString)
                .Select(u => u.Name)
                .ToList();

            Assessors = new SelectList(users);

            Verifiers = new SelectList(users);

            var assignedTaskTypes = await _dbContext.AssignedTaskType.Select(st => st.Name).ToListAsync();

            AssignedTaskTypes = new SelectList(assignedTaskTypes);
        }
    }
}

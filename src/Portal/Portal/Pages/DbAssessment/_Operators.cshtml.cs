using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _OperatorsModel : PageModel
    {

        [DisplayName("Reviewer:")]
        public string Reviewer { get; set; }

        [DisplayName("Assessor:")]
        public string Assessor { get; set; }

        [DisplayName("Verifier:")]
        public string Verifier { get; set; }

        public SelectList Verifiers { get; set; }

        public SelectList Reviewers { get; set; }

        public WorkflowStage ParentPage { get; set; }

        public void OnGet()
        {

        }

        internal static async Task<_OperatorsModel> GetOperatorsDataAsync(IOperatorData currentData, WorkflowDbContext workflowDbContext)
        {
            var users = await workflowDbContext.AdUser.ToListAsync().ConfigureAwait(false);

            return new _OperatorsModel
            {
                Reviewer = currentData.Reviewer ?? "",
                Assessor = currentData.Assessor ?? "Unknown",
                Verifier = currentData.Verifier ?? "",
                Verifiers = new SelectList(users),
                Reviewers = new SelectList(users)
            };
        }
    }
}


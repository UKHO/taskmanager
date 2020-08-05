using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    [Authorize]
    public class _OperatorsModel : PageModel
    {

        [DisplayName("Reviewer:")]
        public virtual AdUser Reviewer { get; set; }

        [DisplayName("Assessor:")]
        public virtual AdUser Assessor { get; set; }

        [DisplayName("Verifier:")]
        public virtual AdUser Verifier { get; set; }

        public List<AdUser> Verifiers { get; set; }

        public List<AdUser> Reviewers { get; set; }

        public WorkflowStage ParentPage { get; set; }

        public void OnGet()
        {

        }

        internal static async Task<_OperatorsModel> GetOperatorsDataAsync(IOperatorData currentData, WorkflowDbContext workflowDbContext)
        {
            var users = await workflowDbContext.AdUsers.Select(u => new AdUser
            {
                DisplayName = u.DisplayName,
                UserPrincipalName = u.UserPrincipalName
            }).ToListAsync();

            return new _OperatorsModel
            {
                Reviewer = currentData.Reviewer ?? AdUser.Empty,
                Assessor = currentData.Assessor ?? AdUser.Empty,
                Verifier = currentData.Verifier ?? AdUser.Empty,
                Verifiers = users,
                Reviewers = users
            };
        }
    }
}


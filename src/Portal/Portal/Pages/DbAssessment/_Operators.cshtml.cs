using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Portal.Models;
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

        public void OnGet()
        {

        }

        internal static _OperatorsModel GetOperatorsData(IOperatorData currentData)
        {
            if (!System.IO.File.Exists(@"Data\Users.json")) throw new FileNotFoundException(@"Data\Users.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\Users.json");
            var users = JsonConvert.DeserializeObject<IEnumerable<Assessor>>(jsonString)
                .Select(u => u.Name)
                .ToList();

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


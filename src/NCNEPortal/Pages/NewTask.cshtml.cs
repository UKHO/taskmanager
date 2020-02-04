using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NCNEPortal.Calculators;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChartType = NCNEPortal.Models.ChartType;
using User = NCNEPortal.Models.User;
using WorkflowType = NCNEPortal.Models.WorkflowType;


namespace NCNEPortal
{
    public class NewTaskModel : PageModel
    {
        private readonly NcneWorkflowDbContext _ncneWorkflowDbContext;
        private readonly IMileStoneCalculator _milestoneCalculator;

        [BindProperty]
        [DisplayName("ION")] public string Ion { get; set; }

        [BindProperty]
        [DisplayName("Chart number")] public string ChartNo { get; set; }

        [BindProperty]
        [DisplayName("Country:")] public string Country { get; set; }

        [BindProperty]
        [DisplayName("Chart type")] public string ChartType { get; set; }

        public SelectList ChartTypes { get; set; }

        [BindProperty]
        [DisplayName("Workflow type")] public string WorkflowType { get; set; }

        public SelectList WorkflowTypes { get; set; }

        [BindProperty]
        [DisplayName("Duration")] public string Dating { get; set; }

        public SelectList DatingList { get; set; }

        [BindProperty]
        [DisplayName("Publication date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? PublicationDate { get; set; }

        [DisplayName("H Forms/Announce:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? AnnounceDate { get; set; }

        [DisplayName("Commit to Print:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? CommitToPrintDate { get; set; }

        [DisplayName("CIS:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? CISDate { get; set; }

        [BindProperty]
        [DisplayName("Compiler")]
        public string Compiler { get; set; }

        public SelectList CompilerList { get; set; }

        [BindProperty]
        [DisplayName("Verifier V1")]
        public string Verifier1 { get; set; }

        public SelectList VerifierList1 { get; set; }

        [BindProperty]
        [DisplayName("Verifier V2")]
        public string Verifier2 { get; set; }

        public SelectList VerifierList2 { get; set; }

        [BindProperty]
        [DisplayName("Publication")]
        public string Publisher { get; set; }

        public SelectList PublisherList { get; set; }

        public NewTaskModel(NcneWorkflowDbContext ncneWorkflowDbContext, IMileStoneCalculator milestoneCalculator)
        {
            _ncneWorkflowDbContext = ncneWorkflowDbContext;
            _milestoneCalculator = milestoneCalculator;


            Ion = "";
            ChartNo = "";
            Country = "United Kingdom";

            SetChartTypes();
            SetWorkflowTypes();

            SetUsers();

            PublicationDate = null;

        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPost()
        {

            if ((PublicationDate != null) && (this.Dating == "Two weeks" || this.Dating == "Three weeks"))
            {
                var (formsDate, cisDate, commitDate) = _milestoneCalculator.CalculateMilestones(Dating, (DateTime)this.PublicationDate);

                this.CommitToPrintDate = commitDate;
                this.CISDate = cisDate;
                this.AnnounceDate = formsDate;


            }

            var taskInfo = _ncneWorkflowDbContext.TaskInfo.Add(new TaskInfo()
            {
                Ion = this.Ion,
                ChartNumber = this.ChartNo,
                ChartType = this.ChartType,
                WorkflowType = this.WorkflowType,
                Duration = this.Dating,
                PublicationDate = this.PublicationDate,
                AnnounceDate = this.AnnounceDate,
                CommitDate = this.CommitToPrintDate,
                CisDate = this.CISDate,
                Country = this.Country,
                AssignedUser = this.Compiler,
                AssignedDate = DateTime.Now,
                TaskRole = new TaskRole()
                {
                    Compiler = this.Compiler,
                    VerifierOne = this.Verifier1,
                    VerifierTwo = this.Verifier2,
                    Publisher = this.Publisher
                }


            });

            await _ncneWorkflowDbContext.SaveChangesAsync();
            return RedirectToPage("./Index");

        }
        private void SetChartTypes()
        {
            if (!System.IO.File.Exists(@"Data\ChartTypes.json"))
                throw new FileNotFoundException(@"Data\ChartTypes.json");


            var jsonString = System.IO.File.ReadAllText(@"Data\ChartTypes.json");

            var chartTypes = JsonConvert.DeserializeObject<IEnumerable<ChartType>>(jsonString).Select(sc => sc.Name);

            ChartTypes = new SelectList(chartTypes);
        }

        private void SetWorkflowTypes()
        {
            if (!System.IO.File.Exists(@"Data\WorkflowTypes.json"))
                throw new FileNotFoundException(@"Data\WorkflowTypes.json");


            var jsonString = System.IO.File.ReadAllText(@"Data\WorkflowTypes.json");

            var workflowTypes = JsonConvert.DeserializeObject<IEnumerable<WorkflowType>>(jsonString)
                .Select(sc => sc.Name);

            WorkflowTypes = new SelectList(workflowTypes);
        }

        private void SetUsers()
        {
            if (!System.IO.File.Exists(@"Data\Users.json"))
                throw new FileNotFoundException(@"Data\Users.json");


            var jsonString = System.IO.File.ReadAllText(@"Data\Users.json");

            var users = JsonConvert.DeserializeObject<IEnumerable<User>>(jsonString)
                .Select(sc => sc.Name);

            CompilerList = new SelectList(users);
            VerifierList1 = new SelectList(users);
            VerifierList2 = new SelectList(users);
            PublisherList = new SelectList(users);
        }
    }
}
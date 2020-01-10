using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NCNEPortal.Models;
using NCNEWorkflowDatabase.EF;


namespace NCNEPortal
{
    public class NewTaskModel : PageModel
    {
        private readonly NcneWorkflowDbContext _ncneWorkflowDbContext;
        [DisplayName("ION:")] public string Ion { get; set; }

        [DisplayName("Chart No.:")] public string ChartNo { get; set; }

        [DisplayName("Country:")] public string Country { get; set; }

        [DisplayName("Chart Type:")] public string ChartType { get; set; }

        public SelectList ChartTypes { get; set; }

        [DisplayName("Workflow Type:")] public string WorkflowType { get; set; }

        public SelectList WorkflowTypes { get; set; }

        [DisplayName("Dating:")] public string Dating { get; set; }

        public SelectList DatingList { get; set; }

        [DisplayName("Publication Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime PublicationDate { get; set; }

        [DisplayName("H Forms/Announce:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime AnnounceDate { get; set; }

        [DisplayName("Commit to Print:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime CommitToPrintDate { get; set; }

        [DisplayName("CIS:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime CISDate { get; set; }

        [DisplayName("Compiler:")]
        public string Compiler { get; set; }

        public SelectList CompilerList { get; set; }

        [DisplayName("Verifier 1:")]
        public  string Verifier1 { get; set; }

        public  SelectList VerifierList1 { get; set; }

        [DisplayName("Verifier 2:")]
        public  string Verifier2 { get; set; }

        public SelectList VerifierList2 { get; set; }


        [DisplayName("Publication:")]
        public string Publisher { get; set; }

        public SelectList PublisherList { get; set; }


        public NewTaskModel(NcneWorkflowDbContext ncneWorkflowDbContext)
        {
            _ncneWorkflowDbContext = ncneWorkflowDbContext;

            Ion = "DC0892322";
            ChartNo = "192";
            Country = "United Kingdom";

            SetChartTypes();
            SetWorkflowTypes();

            SetUsers();

            PublicationDate = DateTime.Today;
            AnnounceDate = DateTime.Today.AddDays(7);
            CommitToPrintDate = AnnounceDate.AddDays(7);
            CISDate = CommitToPrintDate.AddDays(7);


        }

        public void OnGet()
        {

        }

        private void SetChartTypes( ){

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
                .Select(sc=>sc.Name);

            CompilerList = new SelectList(users);
            VerifierList1 = new SelectList(users);
            VerifierList2 = new SelectList(users);
            PublisherList = new SelectList(users);


        }
    }
}
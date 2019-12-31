using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using NCNEPortal.Models;


namespace NCNEPortal
{
    public class NewTaskModel : PageModel
    {
        [DisplayName ("ION")]
        public string Ion { get; set; }

        [DisplayName("Chart No.")]
        public  string ChartNo { get; set; }

        [DisplayName("Country")]
        public string Country { get; set; }

        [DisplayName("Chart Type")]
        public string ChartType { get; set; }

        public SelectList ChartTypes { get; set; }

        [DisplayName("Workflow Type")]
        public string WorkflowType { get; set; }

        public SelectList WorkflowTypes { get; set; }

        public NewTaskModel()
        {
            //ChartTypes= new SelectList("NC,CME, UNE, Refresh");

            Ion = "DC0892322";
            ChartNo = "192";
            Country = "United Kingdom";

            SetChartTypes();
            SetWorkflowTypes();

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
    }
}
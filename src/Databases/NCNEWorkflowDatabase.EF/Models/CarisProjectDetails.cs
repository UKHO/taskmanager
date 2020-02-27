using System;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class CarisProjectDetails
    {

        public int CarisProjectDetailsId { get; set; }
        public int ProcessId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
    }
}

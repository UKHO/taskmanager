using System;

namespace WorkflowDatabase.EF.Models
{
    public class CarisProjectDetails
    {
        public int CarisProjectDetailsId { get; set; }
        public int ProcessId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime Created { get; set; }

        public virtual AdUser CreatedBy { get; set; }
        public int CreatedByAdUserId { get; set; }
    }
}

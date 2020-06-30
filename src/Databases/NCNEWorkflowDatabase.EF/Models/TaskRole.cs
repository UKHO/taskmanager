using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class TaskRole
    {
        [Key]
        [DatabaseGenerated(databaseGeneratedOption: DatabaseGeneratedOption.Identity)]
        public int TaskRoleId { get; set; }
        public int ProcessId { get; set; }
        public string Compiler { get; set; }
        public string VerifierOne { get; set; }
        public string VerifierTwo { get; set; }
        public string HundredPercentCheck { get; set; }
    }
}

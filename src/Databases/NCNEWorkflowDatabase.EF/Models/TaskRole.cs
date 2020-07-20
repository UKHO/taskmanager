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

        public virtual AdUser Compiler { get; set; }
        public int? CompilerAdUserId { get; set; }
        public virtual AdUser VerifierOne { get; set; }
        public int? VerifierOneAdUserId { get; set; }
        public virtual AdUser VerifierTwo { get; set; }
        public int? VerifierTwoAdUserId { get; set; }
        public virtual AdUser HundredPercentCheck { get; set; }
        public int? HundredPercentCheckAdUserId { get; set; }
    }
}

﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class TaskRole
    {
        [Key]
        [DatabaseGenerated(databaseGeneratedOption: DatabaseGeneratedOption.Identity)]
        public int TaskRoleId { get; set; }
        public int ProcessId { get; set; }
        public virtual AdUser Compiler { get; set; }
        public int CompilerAdUserId { get; set; }
        public virtual AdUser Verifier { get; set; }
        public int? VerifierAdUserId { get; set; }
    }
}

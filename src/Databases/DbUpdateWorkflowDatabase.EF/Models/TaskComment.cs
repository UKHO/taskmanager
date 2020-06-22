using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class TaskComment
    {
        [Key]
        [DatabaseGenerated(databaseGeneratedOption: DatabaseGeneratedOption.Identity)]
        public int TaskCommentId { get; set; }
        public int ProcessId { get; set; }
        public string Comment { get; set; }
        public string Username { get; set; }
        public DateTime Created { get; set; }
        public bool ActionIndicator { get; set; }
        public string ActionRole { get; set; }
    }
}

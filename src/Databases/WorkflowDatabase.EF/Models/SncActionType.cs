using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class SncActionType
    {
        public int SncActionTypeId { get; set; }

        [DisplayName("Action Type:")]
        public string Name { get; set; }
    }
}

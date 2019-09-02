using System.Collections.Generic;

namespace WorkflowCoordinator.Models
{
    internal class K2Workflows
    {

        public int ItemCount { get; set; }
        public IEnumerable<K2WorkflowData> Workflows { get; set; }
    }
}

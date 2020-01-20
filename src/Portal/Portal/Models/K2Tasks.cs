using System.Collections.Generic;

namespace Portal.Models
{
    internal class K2Tasks
    {
        public int ItemCount { get; set; }
        public IEnumerable<K2Task> Tasks { get; set; }
    }
}

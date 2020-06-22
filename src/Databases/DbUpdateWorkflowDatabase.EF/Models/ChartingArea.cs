using System.ComponentModel;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class ChartingArea
    {
        public int ChartingAreaId { get; set; }

        [DisplayName("Charting Area")]
        public string Name { get; set; }
    }
}

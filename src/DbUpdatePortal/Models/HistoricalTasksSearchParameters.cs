using System.ComponentModel;

namespace DbUpdatePortal.Models
{
    public class HistoricalTasksSearchParameters
    {
        [DisplayName("Process Id:")]
        public int? ProcessId { get; set; }

        [DisplayName("Task Name")]
        public string Name { get; set; }

        [DisplayName("Charting Area")] public string ChartingArea { get; set; }

        [DisplayName("Update Type")] public string UpdateType { get; set; }
        [DisplayName("Compiler")] public string Compiler { get; set; }
        [DisplayName("Verifier")] public string Verifier { get; set; }

    }
}

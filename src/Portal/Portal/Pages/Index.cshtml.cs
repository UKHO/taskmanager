using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using Portal.Models;

namespace Portal.Pages
{
    public class IndexModel : PageModel
    {
        public IList<Models.Task> Tasks { get; set; }

        public IndexModel()
        {
            Tasks = new List<Task>();
        }
        public void OnGet()
        {
            var rows = System.IO.File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "ExampleDataCSV.csv"));

            Tasks = new List<Models.Task>();

            foreach (var row in rows.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    Tasks.Add(new Models.Task
                    {
                        ProcessId = row.Split(',')[0],
                        DaysToDmEndDate = Convert.ToInt32(row.Split(',')[1]),
                        DmEndDate = Convert.ToDateTime(row.Split(',')[2]),
                        DaysOnHold = Convert.ToInt32(row.Split(',')[3]),
                        RsdraNo = row.Split(',')[4],
                        SourceName = row.Split(',')[5],
                        Workspace = row.Split(',')[6],
                        TaskType = row.Split(',')[7],
                        TaskStage = row.Split(',')[8],
                        Assessor = row.Split(',')[9],
                        Verifier = row.Split(',')[10],
                        Team = row.Split(',')[11]
                    });
                }
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;

namespace Portal.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            var rows = System.IO.File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "ExampleDataCSV.csv"));

            var lst = new List<Models.LandingPage>();

            foreach (var row in rows.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    lst.Add(new Models.LandingPage
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

            var no = lst.Count;
        }
    }
}

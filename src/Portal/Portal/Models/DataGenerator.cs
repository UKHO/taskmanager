using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portal.DataContext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Portal.Models
{
    public class DataGenerator
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {

            using (var context = new TasksDbContext(serviceProvider.GetService<DbContextOptions<TasksDbContext>>()))
            {
                // Database already seeded
                if (context.Tasks.Any())
                {
                    return;
                }



                if (File.Exists("TasksSeedData.json"))
                {
                    var jsonString = File.ReadAllText("TasksSeedData.json");
                    var tasks = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<Task>>(jsonString);

                    context.Tasks.AddRange(tasks);
                    context.SaveChanges();
                }
                //else
                //{

                //}

                //// Serialize from JSON?
                //context.Tasks.AddRange(
                //    new Task
                //    {
                //        Id = 1,
                //        Assessor = "Ben",
                //        DaysOnHold = 34,
                //        DaysToDmEndDate = 2,
                //        DmEndDate = DateTime.Now,
                //        ProcessId = "123123",
                //        RsdraNo = "sdfsdf",
                //        SourceName = "sdfdsf",
                //        TaskStage = "sdfdsf",
                //        TaskType = "sdfdsf",
                //        Team = "sdfdsf",
                //        Verifier = "sdfdsf",
                //        Workspace = "sdfdsf"
                //    },
                //    new Task
                //    {
                //        Id = 2,
                //        Assessor = "Greg",
                //        DaysOnHold = 2,
                //        DaysToDmEndDate = 2,
                //        DmEndDate = DateTime.Now,
                //        ProcessId = "93123",
                //        RsdraNo = "sdfsdf",
                //        SourceName = "sdfdsf",
                //        TaskStage = "sdfdsf",
                //        TaskType = "sdfdsf",
                //        Team = "sdfdsf",
                //        Verifier = "sdfdsf",
                //        Workspace = "sdfdsf"
                //    });

                //context.SaveChanges();
            }

        }

    }
}

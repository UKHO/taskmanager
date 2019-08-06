using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.EF
{
    public class TasksDbBuilder : ICanPopulateTables, ICanSaveChanges
    {
        private readonly WorkflowDbContext _context;

        private TasksDbBuilder(WorkflowDbContext context)
        {
            _context = context;
        }

        public static ICanPopulateTables UsingDbContext(WorkflowDbContext context)
        {
            return new TasksDbBuilder(context);
        }

        private void RunSql(RawSqlString sqlString)
        {
            // Not ideal mixing SQL with EF
            _context.Database.ExecuteSqlCommand(sqlString);
            _context.SaveChanges();
        }

        public ICanSaveChanges PopulateTables()
        {
            if (!File.Exists(@"Data\ProcessesSeedData.json")) return this;
            if (!File.Exists(@"Data\TasksSeedData.json")) return this;

            var jsonString = File.ReadAllText(@"Data\ProcessesSeedData.json");
            var processes = JsonConvert.DeserializeObject<IEnumerable<Process>>(jsonString);
            _context.Processes.AddRange(processes);

            jsonString = File.ReadAllText(@"Data\TasksSeedData.json");
            var tasks = JsonConvert.DeserializeObject<IEnumerable<Task>>(jsonString);

            _context.Tasks.AddRange(tasks);

            return this;
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }

    public interface ICanSaveChanges
    {
        void SaveChanges();
    }

    public interface ICanPopulateTables
    {
        ICanSaveChanges PopulateTables();
    }
}

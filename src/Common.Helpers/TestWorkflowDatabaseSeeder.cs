using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Common.Helpers
{
    public class TestWorkflowDatabaseSeeder : ICanPopulateTables, ICanSaveChanges
    {
        private readonly WorkflowDbContext _context;

        private TestWorkflowDatabaseSeeder(WorkflowDbContext context)
        {
            _context = context;
        }

        public static ICanPopulateTables UsingDbContext(WorkflowDbContext context)
        {
            return new TestWorkflowDatabaseSeeder(context);
        }

        public ICanSaveChanges PopulateTables()
        {
            if (!File.Exists(@"Data\TasksSeedData.json")) throw new FileNotFoundException(@"Data\TasksSeedData.json");

            var jsonString = File.ReadAllText(@"Data\TasksSeedData.json");
            var tasks = JsonConvert.DeserializeObject<IEnumerable<WorkflowInstance>>(jsonString);

            DatabasesHelpers.ClearWorkflowDbTables(_context);

            _context.WorkflowInstance.AddRange(tasks);

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

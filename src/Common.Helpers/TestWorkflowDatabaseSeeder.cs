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

            DatabasesHelpers.ClearWorkflowDbTables(_context);
            PopulateHpdUsage();
            PopulateWorkflowInstance();

            return this;
        }

        private void PopulateHpdUsage()
        {
            if (!File.Exists(@"Data\HpdUsages.json")) throw new FileNotFoundException(@"Data\HpdUsages.json");

            var jsonString = File.ReadAllText(@"Data\HpdUsages.json");
            var hpdUsages = JsonConvert.DeserializeObject<IEnumerable<HpdUsage>>(jsonString);

            _context.HpdUsage.AddRange(hpdUsages);
        }

        private void PopulateWorkflowInstance()
        {
            if (!File.Exists(@"Data\TasksSeedData.json")) throw new FileNotFoundException(@"Data\TasksSeedData.json");

            var jsonString = File.ReadAllText(@"Data\TasksSeedData.json");
            var tasks = JsonConvert.DeserializeObject<IEnumerable<WorkflowInstance>>(jsonString);

            _context.WorkflowInstance.AddRange(tasks);
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

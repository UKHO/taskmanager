using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Common.Helpers
{
    public class NcneTestWorkflowDatabaseSeeder : ICanPopulateTables, ICanSaveChanges
    {
        private readonly NcneWorkflowDbContext _context;

        private NcneTestWorkflowDatabaseSeeder(NcneWorkflowDbContext context)
        {
            _context = context;
        }

        public static ICanPopulateTables UsingDbContext(NcneWorkflowDbContext context)
        {
            return new NcneTestWorkflowDatabaseSeeder(context);
        }

        public ICanSaveChanges PopulateTables()
        {
            DatabasesHelpers.ClearNcneWorkflowDbTables(_context);
            PopulateTaskInfo();

            return this;
        }

        private void PopulateTaskInfo()
        {
            if (!File.Exists(@"Data\TaskInfo.json")) throw new FileNotFoundException(@"Data\TaskInfo.json");

            var jsonString = File.ReadAllText(@"Data\TaskInfo.json");
            var taskInfo = JsonConvert.DeserializeObject<IEnumerable<NcneTaskInfo>>(jsonString);

            _context.NcneTaskInfo.AddRange(taskInfo);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}

using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.Helpers
{
    public class DbUpdateWorkflowDatabaseSeeder : ICanPopulateTables, ICanSaveChanges
    {
        private readonly DbUpdateWorkflowDbContext _context;

        public DbUpdateWorkflowDatabaseSeeder(DbUpdateWorkflowDbContext context)
        {
            _context = context;
        }

        public static ICanPopulateTables UsingDbContext(DbUpdateWorkflowDbContext context)
        {
            return new DbUpdateWorkflowDatabaseSeeder(context);

        }

        public ICanSaveChanges PopulateTables()
        {
            DatabasesHelpers.ClearDbUpdateWorkflowDbTables(_context, true);

            //AddAdditionalAdUsers();

            PopulateTaskStageType();
            PopulateChartingArea();
            PopulateUpdateType();
            PopulateTaskInfo();
            //PopulateHpdUser();

            return this;
        }

        private void AddAdditionalAdUsers()
        {
            if (!File.Exists(@"Data\Users.json")) throw new FileNotFoundException(@"Data\Users.json");

            var jsonString = File.ReadAllText(@"Data\Users.json");
            var users = JsonConvert.DeserializeObject<IEnumerable<AdUser>>(jsonString);

            if (users?.Any() ?? false) _context.AdUser.AddRange(users);
        }

        private void PopulateTaskInfo()
        {
            if (!File.Exists(@"Data\TaskInfo.json")) throw new FileNotFoundException(@"Data\TaskInfo.json");

            var jsonString = File.ReadAllText(@"Data\TaskInfo.json");
            var taskInfo = JsonConvert.DeserializeObject<IEnumerable<TaskInfo>>(jsonString);

            _context.TaskInfo.AddRange(taskInfo);
        }

        private void PopulateTaskStageType()
        {
            if (!File.Exists(@"Data\TaskStageType.json")) throw new FileNotFoundException(@"Data\TaskStageType.json");

            var jsonString = File.ReadAllText(@"Data\TaskStageType.json");
            var stageType = JsonConvert.DeserializeObject<IEnumerable<TaskStageType>>(jsonString);

            _context.TaskStageType.AddRange(stageType);
        }

        private void PopulateChartingArea()
        {
            if (!File.Exists(@"Data\ChartingAreas.json")) throw new FileNotFoundException(@"Data\ChartingAreas.json");

            var jsonString = File.ReadAllText(@"Data\ChartingAreas.json");
            var chartType = JsonConvert.DeserializeObject<IEnumerable<ChartingArea>>(jsonString);

            _context.ChartingArea.AddRange(chartType);

        }

        private void PopulateUpdateType()
        {
            if (!File.Exists(@"Data\UpdateTypes.json")) throw new FileNotFoundException(@"Data\UpdateTypes.json");

            var jsonString = File.ReadAllText(@"Data\UpdateTypes.json");
            var workflowType = JsonConvert.DeserializeObject<IEnumerable<UpdateType>>(jsonString);

            _context.UpdateType.AddRange(workflowType);
        }

        private void PopulateHpdUser()
        {
            if (!File.Exists(@"Data\HpdUsers.json")) throw new FileNotFoundException(@"Data\HpdUsers.json");

            var jsonString = File.ReadAllText(@"Data\HpdUsers.json");
            var hpdUsers = JsonConvert.DeserializeObject<IEnumerable<HpdUser>>(jsonString);

            _context.HpdUser.AddRange(hpdUsers);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}

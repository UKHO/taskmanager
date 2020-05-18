using System.Collections.Generic;
using System.IO;
using System.Linq;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Newtonsoft.Json;

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
            DatabasesHelpers.ClearNcneWorkflowDbTables(_context, true);

            AddAdditionalAdUsers();

            PopulateTaskStageType();
            PopulateChartType();
            PopulateWorkflowType();
            PopulateTaskInfo();
            PopulateHpdUser();

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

        private void PopulateChartType()
        {
            if (!File.Exists(@"Data\ChartTypes.json")) throw new FileNotFoundException(@"Data\ChartTypes.json");

            var jsonString = File.ReadAllText(@"Data\ChartTypes.json");
            var chartType = JsonConvert.DeserializeObject<IEnumerable<ChartType>>(jsonString);

            _context.ChartType.AddRange(chartType);

        }

        private void PopulateWorkflowType()
        {
            if (!File.Exists(@"Data\WorkflowTypes.json")) throw new FileNotFoundException(@"Data\WorkflowTypes.json");

            var jsonString = File.ReadAllText(@"Data\WorkflowTypes.json");
            var workflowType = JsonConvert.DeserializeObject<IEnumerable<WorkflowType>>(jsonString);

            _context.WorkflowType.AddRange(workflowType);
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

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            PopulateAdUser();
            PopulateHpdUser();
            PopulateHpdUsage();
            PopulateProductActionType();
            PopulateAssignedTaskSourceType();
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

        private void PopulateHpdUser()
        {
            if (!File.Exists(@"Data\HpdUsers.json")) throw new FileNotFoundException(@"Data\HpdUsers.json");

            var jsonString = File.ReadAllText(@"Data\HpdUsers.json");
            var hpdUsers = JsonConvert.DeserializeObject<IEnumerable<HpdUser>>(jsonString);

            _context.HpdUser.AddRange(hpdUsers);
        }

        private void PopulateAdUser()
        {
            if (!File.Exists(@"Data\AdUsers.json")) throw new FileNotFoundException(@"Data\AdUsers.json");

            var jsonString = File.ReadAllText(@"Data\AdUsers.json");
            var adUsers = JsonConvert.DeserializeObject<IEnumerable<AdUser>>(jsonString).ToList();
            adUsers.ForEach(a => a.LastCheckedDate = DateTime.Today.AddDays(1));

            _context.AdUser.AddRange(adUsers);
        }

        private void PopulateProductActionType()
        {
            if (!File.Exists(@"Data\ProductActionType.json")) throw new FileNotFoundException(@"Data\ProductActionType.json");

            var jsonString = File.ReadAllText(@"Data\ProductActionType.json");
            var productActionType = JsonConvert.DeserializeObject<IEnumerable<ProductActionType>>(jsonString);

            _context.ProductActionType.AddRange(productActionType);
        }

        private void PopulateAssignedTaskSourceType()
        {
            if (!File.Exists(@"Data\AssignedTaskType.json")) throw new FileNotFoundException(@"Data\AssignedTaskType.json");

            var jsonString = File.ReadAllText(@"Data\AssignedTaskType.json");
            var assignedTaskSourceType = JsonConvert.DeserializeObject<IEnumerable<AssignedTaskType>>(jsonString);

            _context.AssignedTaskType.AddRange(assignedTaskSourceType);
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

}

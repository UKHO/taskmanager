using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Common.Helpers
{
    public class TestWorkflowDatabaseSeeder : ICanPopulateTables, ICanSaveChanges
    {
        protected readonly DbContextOptions<WorkflowDbContext> _dbContextOptions;

        protected TestWorkflowDatabaseSeeder(string workflowDbConnectionString)
        {
            _dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(workflowDbConnectionString)
                .Options;
        }

        public static ICanPopulateTables UsingDbConnectionString(string workflowDbConnectionString)
        {
            return new TestWorkflowDatabaseSeeder(workflowDbConnectionString);
        }

        public ICanSaveChanges PopulateTables()
        {
            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);
            DatabasesHelpers.ClearWorkflowDbTables(workflowDbContext);

            AddAdditionalAdUsers();

            PopulateHpdUser();
            PopulateHpdUsage();
            PopulateProductActionType();
            PopulateAssignedTaskSourceType();
            PopulateWorkflowInstance();

            return this;
        }

        private void AddAdditionalAdUsers()
        {
            if (!File.Exists(@"Data\Users.json")) throw new FileNotFoundException(@"Data\Users.json");

            var jsonString = File.ReadAllText(@"Data\Users.json");
            var users = JsonConvert.DeserializeObject<IEnumerable<AdUser>>(jsonString);
            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);

            if (users?.Any() ?? false) workflowDbContext.AdUsers.AddRange(users);

            workflowDbContext.SaveChanges();
        }

        private void PopulateHpdUsage()
        {
            if (!File.Exists(@"Data\HpdUsages.json")) throw new FileNotFoundException(@"Data\HpdUsages.json");

            var jsonString = File.ReadAllText(@"Data\HpdUsages.json");
            var hpdUsages = JsonConvert.DeserializeObject<IEnumerable<HpdUsage>>(jsonString);

            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);
            workflowDbContext.HpdUsage.AddRange(hpdUsages);
            workflowDbContext.SaveChanges();
        }

        private void PopulateHpdUser()
        {
            if (!File.Exists(@"Data\HpdUsers.json")) throw new FileNotFoundException(@"Data\HpdUsers.json");

            var jsonString = File.ReadAllText(@"Data\HpdUsers.json");
            var hpdUsers = JsonConvert.DeserializeObject<IEnumerable<HpdUser>>(jsonString);
            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);

            foreach (var hpdUser in hpdUsers)
            {
                workflowDbContext.Entry(hpdUser.AdUser).State = EntityState.Unchanged;
                workflowDbContext.HpdUser.Add(hpdUser);
            }

            workflowDbContext.SaveChanges();
        }

        private void PopulateProductActionType()
        {
            if (!File.Exists(@"Data\ProductActionType.json")) throw new FileNotFoundException(@"Data\ProductActionType.json");

            var jsonString = File.ReadAllText(@"Data\ProductActionType.json");
            var productActionType = JsonConvert.DeserializeObject<IEnumerable<ProductActionType>>(jsonString);
            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);
            workflowDbContext.ProductActionType.AddRange(productActionType);
            workflowDbContext.SaveChanges();
        }

        private void PopulateAssignedTaskSourceType()
        {
            if (!File.Exists(@"Data\AssignedTaskType.json")) throw new FileNotFoundException(@"Data\AssignedTaskType.json");

            var jsonString = File.ReadAllText(@"Data\AssignedTaskType.json");
            var assignedTaskSourceType = JsonConvert.DeserializeObject<IEnumerable<AssignedTaskType>>(jsonString);
            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);
            workflowDbContext.AssignedTaskType.AddRange(assignedTaskSourceType);
            workflowDbContext.SaveChanges();
        }


        private void PopulateWorkflowInstance()
        {
            if (!File.Exists(@"Data\TasksSeedData.json")) throw new FileNotFoundException(@"Data\TasksSeedData.json");

            var jsonString = File.ReadAllText(@"Data\TasksSeedData.json");
            var tasks = JsonConvert.DeserializeObject<IEnumerable<WorkflowInstance>>(jsonString);

            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);

            foreach (var task in tasks)
            {
                workflowDbContext.WorkflowInstance.Add(task);
                workflowDbContext.SaveChanges();
            }

            workflowDbContext.SaveChanges();
        }

        public void SaveChanges()
        {
            //_context.SaveChanges();
        }
    }

}

using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Linq;

namespace NCNEWorkflowDatabase.Tests.Helpers
{
    internal static class AdUserHelper
    {

        internal static AdUser CreateTestUser(NcneWorkflowDbContext dbContext, string name = "Test user", int uniqueNumber = 0)
        {
            var email = "test@email.com";

            if (uniqueNumber > 0)
            {
                email = $"test{uniqueNumber}@email.com";
                name = $"{name}{uniqueNumber}";
            }

            var users = dbContext.AdUser.ToList();
            var user = users.SingleOrDefault(u =>
                u.UserPrincipalName == email);
            if (user != null) return user;

            user = new AdUser
            {
                DisplayName = name,
                UserPrincipalName = email,
                LastCheckedDate = DateTime.Now
            };
            dbContext.AdUser.Add(user);
            dbContext.SaveChanges();

            return user;
        }

        internal static TaskInfo CreateSkeletonTaskInfoInstance(NcneWorkflowDbContext dbContext, AdUser user, int processId = 1)
        {
            var taskInfoInstanceList = dbContext.TaskInfo.ToList();
            var taskInfoInstance = taskInfoInstanceList.SingleOrDefault(u =>
                u.ProcessId == processId);
            if (taskInfoInstance != null) return taskInfoInstance;

            taskInfoInstance = new TaskInfo()
            {
                Ion = "232",
                Country = "UK",
                ChartNumber = "35",
                ChartTitle = "Hamoaze",
                ChartType = "NE",
                WorkflowType = "Derived",
                Duration = "3 Weeks",
                PublicationDate = DateTime.Now,
                AnnounceDate = DateTime.Now,
                CommitDate = DateTime.Now,
                CisDate = DateTime.Now,
                Assigned = user,
                AssignedDate = DateTime.Now,
                Status = "Inprogress",
            };
            dbContext.TaskInfo.Add(taskInfoInstance);
            dbContext.SaveChanges();

            return taskInfoInstance;
        }
    }
}

using System;
using System.Linq;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.Tests.Helpers
{
    internal static class AdUserHelper
    {
        internal static AdUser CreateTestUser(WorkflowDbContext dbContext, int uniqueNumber = 0)
        {
            var email = "test@email.com";
            var name = "Test User";

            if (uniqueNumber > 0)
            {
                email = $"test{uniqueNumber}@email.com";
                name = $"Test User {uniqueNumber}";
            }

            var users = dbContext.AdUsers.ToList();
            var user = users.SingleOrDefault(u =>
               u.UserPrincipalName == email);
            if (user != null) return user;

            user = new AdUser
            {
                DisplayName = name,
                UserPrincipalName = email,
                LastCheckedDate = DateTime.Now
            };
            dbContext.AdUsers.Add(user);
            dbContext.SaveChanges();

            return user;
        }

        internal static WorkflowInstance CreateSkeletonWorkflowInstance(WorkflowDbContext dbContext, int processId = 1)
        {
            var workflowInstanceList = dbContext.WorkflowInstance.ToList();
            var workflowInstance = workflowInstanceList.SingleOrDefault(u =>
                u.ProcessId == processId);
            if (workflowInstance != null) return workflowInstance;

            workflowInstance = new WorkflowInstance
            {
                ProcessId = processId,
                PrimarySdocId = 1,
                SerialNumber = string.Empty,
                ActivityName = string.Empty,
                ActivityChangedAt = DateTime.Now,
                StartedAt = DateTime.Now,
                Status = string.Empty
            };
            dbContext.WorkflowInstance.Add(workflowInstance);
            dbContext.SaveChanges();

            return workflowInstance;
        }
    }
}

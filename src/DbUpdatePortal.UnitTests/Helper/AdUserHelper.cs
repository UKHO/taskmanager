using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using System;
using System.Linq;


namespace DbUpdatePortal.UnitTests.Helper
{
    internal static class AdUserHelper
    {

        internal static AdUser CreateTestUser(DbUpdateWorkflowDbContext dbContext, string name = "Test user", int uniqueNumber = 0)
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

        internal static TaskInfo CreateSkeletonTaskInfoInstance(DbUpdateWorkflowDbContext dbContext, AdUser user, int processId = 1)
        {
            var taskInfoList = dbContext.TaskInfo.ToList();
            var taskInfo = taskInfoList.SingleOrDefault(u =>
                u.ProcessId == processId);
            if (taskInfo != null) return taskInfo;

            taskInfo = new TaskInfo()
            {
                Name = "New Task" + processId.ToString(),
                ChartingArea = "Home waters",
                UpdateType = "Steady State",
                TargetDate = DateTime.Now,
                Assigned = user,
                AssignedDate = DateTime.Now,
                Status = "Inprogress",
            };
            dbContext.TaskInfo.Add(taskInfo);
            dbContext.SaveChanges();

            return taskInfo;
        }
    }
}

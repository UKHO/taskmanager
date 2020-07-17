using System;
using System.Linq;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests.Helpers
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
    }
}

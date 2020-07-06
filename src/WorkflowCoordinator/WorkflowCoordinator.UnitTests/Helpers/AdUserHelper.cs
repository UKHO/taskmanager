using System;
using System.Linq;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.UnitTests.Helpers
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


            var user = dbContext.AdUsers.SingleOrDefault(u =>
                u.UserPrincipalName.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user != null) return user;

            user = new AdUser
            {
                DisplayName = name,
                UserPrincipalName = email
            };
            dbContext.AdUsers.Add(user);
            dbContext.SaveChanges();

            return user;
        }
    }
}

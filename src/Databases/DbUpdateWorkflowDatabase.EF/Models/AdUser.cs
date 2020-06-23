using System;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class AdUser
    {
        public int AdUserId { get; set; }
        public string DisplayName { get; set; }
        public DateTime LastCheckedDate { get; set; }

        // A UPN maps to a user's email by convention
        // https://docs.microsoft.com/en-us/windows/win32/ad/naming-properties#userprincipalname
        public string UserPrincipalName { get; set; }
    }
}

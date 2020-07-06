using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    public class AdUser
    {
        public int AdUserId { get; set; }
        public string DisplayName { get; set; }
        public string UserPrincipalName { get; set; }
        public DateTime LastCheckedDate { get; set; }

        [NotMapped]
        public static AdUser Unknown => new AdUser
        {
            DisplayName = "Unknown",
            UserPrincipalName = ""
        };

        [NotMapped]
        public static AdUser Empty => new AdUser
        {
            DisplayName = "",
            UserPrincipalName = ""
        };

        /// <summary>
        ///  Returns true where UserPrincipalName is null, empty or whitespace.
        /// </summary>
        [NotMapped]
        public bool HasNoUserPrincipalName => string.IsNullOrWhiteSpace(UserPrincipalName);
    }
}
namespace Portal.Configuration
{
    public class AdUserUpdateServiceSecrets
    {

        // Expected to come from KV as comma separated string of GUIDs
        public string AdUserGroups { get; set; }
    }
}

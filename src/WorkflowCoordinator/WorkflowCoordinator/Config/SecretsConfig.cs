using NServiceBus;

namespace WorkflowCoordinator.Config
{
    public class SecretsConfig
    {
        public string NsbDataSource { get; set; }

        public string NsbInitialCatalog { get; set; }

        public string NsbToK2ApiUsername { get; set; }


        public string NsbToK2ApiPassword { get; set; }

    }
}

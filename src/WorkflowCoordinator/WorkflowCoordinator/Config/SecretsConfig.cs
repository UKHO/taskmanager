namespace WorkflowCoordinator.Config
{
    public class SecretsConfig
    { 
        public string NsbDataSource { get; set; }

        public string NsbInitialCatalog { get; set; }

        // e.g.
        //public IEnumerable<string> Validate()
        //{
        //     if ....
        //     yield return $"Valid ... not ... {nameof(MySecret)}";
        //}
    }
}

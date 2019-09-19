namespace SourceDocumentCoordinator.Config
{
    public class SecretsConfig
    {
        public string NsbDataSource { get; set; }
        public string NsbInitialCatalog { get; set; }
        public string ContentServiceUsername { get; set; }
        public string ContentServicePassword { get; set; }
        public string ContentServiceDomain { get; set; }
        // e.g.
        //public IEnumerable<string> Validate()
        //{
        //     if ....
        //     yield return $"Valid ... not ... {nameof(MySecret)}";
        //}
    }
}

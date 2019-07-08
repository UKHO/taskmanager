namespace WorkflowCoordinator.Config
{
    public class SecretsConfig
    {
        public string MySecret { get; set; }

        // e.g.
        //public IEnumerable<string> Validate()
        //{
        //     if ....
        //     yield return $"Valid ... not ... {nameof(MySecret)}";
        //}
    }
}

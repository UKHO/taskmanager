using Newtonsoft.Json.Linq;

namespace NCNEPortal.AccessibilityTests.AxeModel
{
    public class AxeResultItem
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Help { get; set; }
        public string HelpUrl { get; set; }
        public string Impact { get; set; }
        public string[] Tags { get; set; }
        public AxeResultNode[] Nodes { get; set; }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }
    }
}

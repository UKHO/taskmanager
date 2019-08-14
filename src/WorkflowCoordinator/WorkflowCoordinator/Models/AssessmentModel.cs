using Newtonsoft.Json;

namespace WorkflowCoordinator.Models
{
    public class AssessmentModel
    {
        [JsonProperty("id")]
        public int SdocId { get; set; }
        public string Name { get; set; }
        [JsonProperty("sourceName")]
        public string RsdraNumber { get; set; }
    }
}

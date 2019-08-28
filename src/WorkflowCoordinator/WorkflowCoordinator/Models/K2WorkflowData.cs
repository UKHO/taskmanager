using Newtonsoft.Json;

namespace WorkflowCoordinator.Models
{
    internal class K2WorkflowData
    {

        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("defaultVersionId")]
        public int DefaultVersionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("folder")]
        public string Folder { get; set; }

        [JsonProperty("systemName")]
        public string SystemName { get; set; }
    }
}

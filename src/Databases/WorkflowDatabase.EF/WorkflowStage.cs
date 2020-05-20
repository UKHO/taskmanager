using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WorkflowDatabase.EF
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WorkflowStage
    {
        Review,
        Assess,
        Verify,
        Completed,
        Terminated
    }
}

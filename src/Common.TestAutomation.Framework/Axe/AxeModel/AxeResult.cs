using System;
using Newtonsoft.Json.Linq;

namespace Common.TestAutomation.Framework.Axe.AxeModel
{
    public class AxeResult
    {
        private readonly JObject _jObject;

        public AxeResult(JObject jObject)
        {
            _jObject = jObject;
            Violations = jObject.SelectToken("violations")?.ToObject<AxeResultItem[]>();
            Passes = jObject.SelectToken("passes")?.ToObject<AxeResultItem[]>();
            Inapplicable = jObject.SelectToken("inapplicable")?.ToObject<AxeResultItem[]>();
            Incomplete = jObject.SelectToken("incomplete")?.ToObject<AxeResultItem[]>();
            Timestamp = jObject.SelectToken("timestamp")?.ToObject<DateTimeOffset>();
            Url = jObject.SelectToken("url")?.ToObject<string>();
        }

        public AxeResultItem[] Violations { get; }
        public AxeResultItem[] Passes { get; }
        public AxeResultItem[] Inapplicable { get; }
        public AxeResultItem[] Incomplete { get; }
        public DateTimeOffset? Timestamp { get; }
        public string Url { get; }

        public override string ToString()
        {
            return _jObject.ToString();
        }
    }
}
using Newtonsoft.Json;

namespace Common.Helpers
{
    public static class JsonExtensionMethods
    {
        public static string ToJSONSerializedString<T>(this T t) where T : class, new()
        {
            return JsonConvert.SerializeObject((object)t);
        }
    }
}

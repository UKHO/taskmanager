using System;
using System.Linq;
using Newtonsoft.Json;

namespace Portal.Extensions
{
    public static class EnumHandlers
    {
        public static string EnumToString<T>() where T: Enum
        {
            var values = Enum.GetValues(typeof(T)).Cast<int>();
            var enumDictionary = values.ToDictionary(value => Enum.GetName(typeof(T), value));

            return JsonConvert.SerializeObject(enumDictionary);
        }
    }
}

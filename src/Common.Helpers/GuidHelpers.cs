using System;
using System.Text.RegularExpressions;

namespace Common.Helpers
{
    public static class GuidHelpers
    {
        public static Guid ExtractGuidFromString(string stringContainingGuid)
        {
            if (string.IsNullOrEmpty(stringContainingGuid))
                throw new ArgumentNullException($"{nameof(stringContainingGuid)} must have a value");

            // Regex string modified from: http://stackoverflow.com/questions/13190436/find-matching-guid-in-string
            var regexGuid = new Regex(
                @"(\{){0,1}[0-9a-z]{8}\-[0-9a-z]{4}\-[0-9a-z]{4}\-[0-9a-z]{4}\-[0-9a-z]{12}(\}){0,1}",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

            var matchGuid = regexGuid.Match(stringContainingGuid);

            if (!matchGuid.Success) throw new InvalidOperationException($"Parameter {nameof(stringContainingGuid)} did not contain GUID: {stringContainingGuid}");

            return Guid.Parse(matchGuid.Value);
        }
    }
}

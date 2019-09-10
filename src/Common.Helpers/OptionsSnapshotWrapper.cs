using System;
using Microsoft.Extensions.Options;

namespace Common.Helpers
{
    public class OptionsSnapshotWrapper<TOptions> : IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        public TOptions Value { get; }

        public OptionsSnapshotWrapper(TOptions options)
        {
            Value = options;
        }

        public TOptions Get(string name)
        {
            throw new NotImplementedException();
        }
    }
}
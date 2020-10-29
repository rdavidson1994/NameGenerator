using System;

namespace NameGenerator
{
    public class GenerationFailedException<T> : Exception where T : class, IStringSymbol
    {
        public GenerationFailedException(string? message, ContinuationMap<T> map) : base(message)
        {
            Map = map;
        }
        public ContinuationMap<T> Map { get; }
    }
}

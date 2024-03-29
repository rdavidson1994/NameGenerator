﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NameGenerator
{
    public class ContinuationMap<T> where T : class, IStringSymbol
    {
        public Dictionary<string, Counter<T>> Data { get; }
        [JsonConstructor]
        public ContinuationMap(Dictionary<string, Counter<T>> data)
        {
            this.Data = data;
        }
        public ContinuationMap()
        {
            this.Data = new Dictionary<string, Counter<T>>();
        }
        public int Count()
        {
            return Data.Count;
        }
        public void Add(T start, T continuation, int count = 1)
        {
            Data.TryAdd(start.Symbol(), new Counter<T>());
            Data[start.Symbol()].Add(continuation, count);
        }
        public T? GenerateContinuation(T start)
        {
            if (Data.TryGetValue(start.Symbol(), out Counter<T>? counter))
            {
                return counter.GetRandom();
            }
            throw new GenerationFailedException<T>(
                $"No continuation found for {start.Symbol()}",
                this
                );
        }
    }
}

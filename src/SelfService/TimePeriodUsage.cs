using System;
using Newtonsoft.Json;

namespace SelfService
{
    public class TimePeriodUsage
    {
        [JsonConstructor]
        public TimePeriodUsage(long start, long end, decimal value, bool complete)
        {
            Start = new DateTime(1970, 1, 1).AddMilliseconds(start);
            Duration = TimeSpan.FromMilliseconds(end - start);
            Value = value;
            IsComplete = complete;
        }

        public DateTime Start { get; }

        public TimeSpan Duration { get; }

        public decimal Value { get; }

        public bool IsComplete { get; }

        public DateTime End => Start.Add(Duration);
    }
}
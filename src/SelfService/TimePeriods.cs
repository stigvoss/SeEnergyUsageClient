using System.Collections.Generic;

namespace SelfService
{
    public enum TimePeriod
    {
        Default = 0,
        Hours = 0,
        Days = 1,
        Months = 2,
        Quarters = 3
    }

    public static class TimePeriods
    {

        private static readonly IReadOnlyDictionary<TimePeriod, string> _mapping = new Dictionary<TimePeriod, string>
        { { TimePeriod.Hours, "day_by_hours" },
            // { PeriodType.WeekOfDays, "week_by_days" },
            { TimePeriod.Days, "month_by_days" },
            { TimePeriod.Months, "year_by_months" },
            { TimePeriod.Quarters, "year_by_quarters" },
        };

        public static string GetPeriodStringFrom(TimePeriod periodType)
        {
            return _mapping[periodType];
        }
    }
}
using System.Collections.Generic;

namespace SelfService
{
    public static class TimePeriods
    {
        public enum PeriodType
        {
            Default = 0,
            Hours = 0,
            Days = 1,
            Months = 2,
            Quarters = 3
        }

        private static readonly IReadOnlyDictionary<PeriodType, string> _mapping = new Dictionary<PeriodType, string>
        { { PeriodType.Hours, "day_by_hours" },
            // { PeriodType.WeekOfDays, "week_by_days" },
            { PeriodType.Days, "month_by_days" },
            { PeriodType.Months, "year_by_months" },
            { PeriodType.Quarters, "year_by_quarters" },
        };

        public static string GetStringFrom(PeriodType periodType)
        {
            return _mapping[periodType];
        }
    }
}
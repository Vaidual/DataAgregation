using DataAgregation.ClusterModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Tools
{
    public static class LastDayIncomeService
    {
        public static DateTime LastDate { get; set; }
        public static Interval[] Intervals { get; set; }

        public static async Task<List<DateIntervalEnters<int>>> GetIncomeStatisticByEventTypeAsync(int type)
        {
            var events = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Events
                    .Where(e => e.EventType == type)
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { UserId = e.UserId, Age = u.Age, Date = e.DateTime }
                    )
                    .GroupBy(e => e.Date)
                    .Select(g => new DateIntervalEnters<int>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters1 = g.Where(e => e.Age >= intervals.ElementAt(0).MinValue && e.Age <= intervals.ElementAt(0).MaxValue).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters2 = g.Where(e => e.Age >= intervals.ElementAt(1).MinValue && e.Age <= intervals.ElementAt(1).MaxValue).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters3 = g.Where(e => e.Age >= intervals.ElementAt(2).MinValue && e.Age <= intervals.ElementAt(2).MaxValue).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters4 = g.Where(e => e.Age >= intervals.ElementAt(3).MinValue && e.Age <= intervals.ElementAt(3).MaxValue).Select(e => e.UserId).Distinct().Count(),
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new DateIntervalEnters<int>
                {
                    Date = g.Key,
                    IntervalEnters1 = g.Sum(e => e.IntervalEnters1),
                    IntervalEnters2 = g.Sum(e => e.IntervalEnters2),
                    IntervalEnters3 = g.Sum(e => e.IntervalEnters3),
                    IntervalEnters4 = g.Sum(e => e.IntervalEnters4),
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedEvents;
        }

        public static async Task<List<DateListIntervalEnters<decimal>>> GetRevenuebyIncomeAsync()
        {
            var events = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.CurrencyPurchases
                    .Join(
                        context.Events,
                        cp => cp.EventId,
                        e => e.EventId,
                        (cp, e) => new { Date = e.DateTime, Income = cp.Price, UserId = e.UserId }
                    )
                    .GroupBy(e => e.Date)
                    .AsEnumerable()
                    .Select(g => new DateListIntervalEnters<decimal>() 
                    { 
                        Date = DateOnly.FromDateTime(g.Key), 
                        IntervalEnters = Intervals
                            .Select(i => g
                                .Where(x => x.Income >= i.MinValue && x.Income <= i.MaxValue)
                                .Sum(x => x.Income))
                            .ToList()
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g =>
                {
                    var result = new DateListIntervalEnters<decimal> { Date = g.Key, IntervalEnters = new List<decimal>() };
                    for (int i = 0; i < Intervals.Count(); i++)
                    {
                        result.IntervalEnters.Add(g.Sum(e => e.IntervalEnters[i]));
                    }
                    return result;
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedEvents;
        }

        public static async Task<IEnumerable<ClusterInput>> GetIncomClusterInputs()
        {
            var lastDate = await DBHandler.GetDateOfLastCurrencyPurchaseAsync();
            var result = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.CurrencyPurchases
                    .Join(
                        context.Events.Where(e => e.EventType == 6  && e.DateTime == lastDate),
                        cp => cp.EventId,
                        e => e.EventId,
                        (cp, e) => new { e.UserId, cp.Price})
                    .GroupBy(x => x.UserId)
                    .Select(g => new ClusterInput((float)g.Sum(x => x.Price)));
            });
            return result;
        }
    }
}

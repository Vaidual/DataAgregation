using DataAgregation.ClusterModels;
using DataAgregation.DataManipulationModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Tools
{
    static public class AgeService
    {
        public static async Task<List<DateIntervalEnters<int>>> GetAgeStatisticByEventTypeAsync(int type, IEnumerable<Interval> intervals)
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

        public static async Task<List<StageStatisticWithIntervals>> GetStageStatisticByAgeAsync(IEnumerable<Interval> intervals)
        {
            var events = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                var Ends = context.StageEnds
                    .Join(
                        context.Events,
                        se => se.EventId,
                        e => e.EventId,
                        (ss, e) => new { ss.Stage, ss.IsWon, ss.Income, UserId = e.UserId }
                    )
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (ss, u) => new { ss.Stage, ss.IsWon, ss.Income, u.Age }
                    )
                    .GroupBy(se => se.Stage)
                    .AsEnumerable()
                    .Select(g =>
                    {
                        return new
                        {
                            Stage = g.Key,
                            Wins = intervals
                            .Select(i => g
                                .Where(e => e.Age >= i.MinValue && e.Age <= i.MaxValue)
                                .Count(se => se.IsWon))
                            .ToList(),
                            Income = intervals
                            .Select(i => g
                                .Where(e => e.Age >= i.MinValue && e.Age <= i.MaxValue)
                                .Sum(se => (int)se.Income))
                            .ToList(),
                            Ends = intervals
                            .Select(i => g
                                .Where(e => e.Age >= i.MinValue && e.Age <= i.MaxValue)
                                .Count())
                            .ToList()
                        };
                    });
                var Starts = context.StageStarts
                    .Join(
                        context.Events,
                        ss => ss.EventId,
                        e => e.EventId,
                        (ss, e) => new { ss.Stage, UserId = e.UserId }
                    )
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { e.Stage, u.Age }
                    )
                    .GroupBy(ss => ss.Stage)
                    .AsEnumerable()
                    .Select(g => new
                    {
                        Stage = g.Key,
                        Starts = intervals
                            .Select(i => g
                                .Where(e => e.Age >= i.MinValue && e.Age <= i.MaxValue)
                                .Count())
                        .ToList()
                    });
                return Starts.Join(
                    Ends,
                    ss => ss.Stage,
                    se => se.Stage,
                    (ss, se) => new StageStatisticWithIntervals
                    {
                        Stage = ss.Stage,
                        Starts = ss.Starts,
                        Ends = se.Ends,
                        Wins = se.Wins,
                        Income = se.Income,
                    });
            });
            var mergedStageStats = events
                .GroupBy(e => e.Stage)
                .Select(g => new StageStatisticWithIntervals
                {
                    Stage = g.Key,
                    Starts = Enumerable
                        .Range(0, intervals.Count())
                        .Select(i => g.Select(e => e.Starts).Sum(list => list[i]))
                        .ToList(),
                    Ends = Enumerable
                        .Range(0, intervals.Count())
                        .Select(i => g.Select(e => e.Ends).Sum(list => list[i]))
                        .ToList(),
                    Wins = Enumerable
                        .Range(0, intervals.Count())
                        .Select(i => g.Select(e => e.Wins).Sum(list => list[i]))
                        .ToList(),
                    Income = Enumerable
                        .Range(0, intervals.Count())
                        .Select(i => g.Select(e => e.Income).Sum(list => list[i]))
                        .ToList(),
                })
                .OrderBy(e => e.Stage)
                .ToList();
            List<StageDateCount> stageDateCounts = await DBHandler.GetStageDateCountAsync();
            List<CurrencyRate> rate = await DBHandler.GetCurrencyRateAsync();
            foreach (var stageStat in mergedStageStats)
            {
                var dates = stageDateCounts.Where(e => e.Stage == stageStat.Stage);
                decimal coef = dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
                stageStat.USD = stageStat.Income.Select(x => x * coef).ToList();
            }
            return mergedStageStats;
        }

        public static async Task<List<ItemStatisticWithValues<int>>> GetItemsStatisticWithAges()
        {
            var events = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.IngamePurchases
                    .Join(
                        context.Events,
                        ip => ip.EventId,
                        e => e.EventId,
                        (ip, e) => new { ip.ItemName, ip.Price, UserId = e.UserId }
                    )
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { e.ItemName, e.Price, u.Age }
                    )
                    .GroupBy(ss => ss.ItemName)
                    .Select(g => new
                    {
                        Item = g.Key,
                        Amount = g.Count(),
                        Income = g.Sum(i => i.Price),
                        Values = g.Select(e => e.Age)
                    })
                    .OrderBy(stat => stat.Item);
            });
            var mergedEvents = events
                .GroupBy(e => e.Item)
                .Select(g => new ItemStatisticWithValues<int>
                {
                    Item = g.Key,
                    Amount = g.Sum(i => i.Amount),
                    Income = g.Sum(i => i.Income),
                    Values = g.SelectMany(e => e.Values).ToList()
                })
                .OrderBy(e => e.Item)
                .ToList();
            List<ItemDateCount> itemDateCounts = await DBHandler.GetItemDateCountAsync();
            List<CurrencyRate> rate = await DBHandler.GetCurrencyRateAsync();
            foreach (var stat in mergedEvents)
            {
                var dates = itemDateCounts.Where(e => e.Item == stat.Item);
                stat.USD = stat.Income * dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
            }
            return mergedEvents;
        }

        public static async Task<List<ItemStatisticWithIntervals>> GetItemsStatisticByAge(IEnumerable<Interval> intervals)
        {
            var events = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.IngamePurchases
                    .Join(
                        context.Events,
                        ip => ip.EventId,
                        e => e.EventId,
                        (ip, e) => new { ip.ItemName, ip.Price, UserId = e.UserId }
                    )
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { e.ItemName, e.Price, u.Age }
                    )
                    .GroupBy(e => e.ItemName)
                    .AsEnumerable()
                    .Select(g => new ItemStatisticWithIntervals()
                    {
                        ItemName = g.Key,
                        Amount = intervals
                            .Select(i => g
                                .Where(e => e.Age >= i.MinValue && e.Age <= i.MaxValue)
                                .Count())
                            .ToList(),
                        Income = intervals
                            .Select(i => g
                                .Where(e => e.Age >= i.MinValue && e.Age <= i.MaxValue)
                                .Sum(e => e.Price))
                            .ToList()
                    });
            });
            var sw = new Stopwatch();
            var mergedItemStats = events
                .GroupBy(e => e.ItemName)
                .Select(g =>
                {
                    sw.Start();
                    var result = new ItemStatisticWithIntervals()
                    {
                        ItemName = g.Key,
                        Amount = Enumerable
                            .Range(0, intervals.Count())
                            .Select(i => g.Select(e => e.Amount).Sum(list => list[i]))
                            .ToList(),
                        Income = Enumerable
                            .Range(0, intervals.Count())
                            .Select(i => g.Select(e => e.Income).Sum(list => list[i]))
                            .ToList(),
                    };
                    return result;
                })
                .OrderBy(e => e.ItemName)
                .ToList();
            Console.WriteLine(sw.Elapsed.Microseconds);
            List<ItemDateCount> itemDateCounts = await DBHandler.GetItemDateCountAsync();
            List<CurrencyRate> rate = await DBHandler.GetCurrencyRateAsync();
            foreach (var itemStat in mergedItemStats)
            {
                var dates = itemDateCounts.Where(e => e.Item == itemStat.ItemName);
                decimal coef = dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
                itemStat.USD = itemStat.Income.Select(x => x * coef).ToList();
            }
            return mergedItemStats;
        }

        public static async Task<List<DateListIntervalEnters<decimal>>> GetRevenuebyAgeAsync(IEnumerable<Interval> intervals)
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
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { e.Date, e.Income, u.Age }
                    )
                    .GroupBy(e => e.Date)
                    .AsEnumerable()
                    .Select(g =>
                    {
                        var result = new DateListIntervalEnters<decimal>() { Date = DateOnly.FromDateTime(g.Key), IntervalEnters = new List<decimal>() };
                        for (int i = 0; i < intervals.Count(); i++)
                        {
                            int minAge = intervals.ElementAt(i).MinValue;
                            int maxAge = intervals.ElementAt(i).MaxValue;

                            result.IntervalEnters.Add(g.Where(e => e.Age >= minAge && e.Age <= maxAge).Sum(e => e.Income));
                        }
                        return result;
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new DateListIntervalEnters<decimal>
                {
                    Date = g.Key,
                    IntervalEnters = Enumerable
                        .Range(0, g.Max(e => e.IntervalEnters.Count))
                        .Select(i => g.Select(e => e.IntervalEnters.ElementAtOrDefault(i, 0)).Sum())
                        .ToList(),
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedEvents;
        }

        public static async Task<List<int>> GetMauByAgeAsync(IEnumerable<Interval> intervals)
        {
            DateTime lastDate = await DBHandler.GetDateOfLastEventAsync();
            var stats = await DBHandler.ExecuteInMultiThreadUsingSingleValue((context) =>
            {
                return context.Events
                    .Where(e => e.EventType == 1 && EF.Functions.DateDiffDay(e.DateTime, lastDate) <= 30)
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { UserId = e.UserId, Gender = u.Gender }
                    )
                    .GroupBy(e => e.Gender)
                    .OrderBy(g => g.Key)
                    .Select(g => g
                        .Select(e => e.UserId)
                        .Distinct()
                        .Count());
            });

            var mergedStats = Enumerable
                .Range(0, stats.First().Count())
                .Select(i => stats.Sum(list => list.ElementAt(i)))
                .ToList();

            return mergedStats;
        }

        public static async Task<IEnumerable<ClusterInput>> GetUserAges()
        {
            var result = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Users.Select(u => new ClusterInput(u.Age));
            });
            return result;
        }

        public static async Task<List<DateAges>> GetDateAgesByEventTypeAsync(int type)
        {
            var events = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Events
                    .Where(e => e.EventType == type)
                    .Include(e => e.User)
                    .GroupBy(e => e.DateTime)
                    .Select(g => new DateAges
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        Ages = g.Select(g => g.User.Age)
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new DateAges
                {
                    Date = g.Key,
                    Ages = g.SelectMany(g => g.Ages)
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedEvents;
        }
    }
}

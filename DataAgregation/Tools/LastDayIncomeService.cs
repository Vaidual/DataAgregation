using DataAgregation.ClusterModels;
using DataAgregation.DataManipulationModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataAgregation.Tools
{
    public static class LastDayIncomeService
    {
        public static DateTime LastDate { get; set; }
        public static Interval[] Intervals { get; set; }

        public static async Task<List<StageStatisticWithIntervals>> GetStageStatisticByIncomeAsync()
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
                    .Select(x => new
                    {
                        x.Stage,
                        x.IsWon,
                        x.Income,
                        IncomeFromUser = context.CurrencyPurchases
                            .Join(
                                context.Events.Where(e => e.UserId == x.UserId && e.DateTime == LastDate),
                                cp => cp.EventId,
                                e => e.EventId,
                                (cp, e) => cp.Price
                            )
                            .Sum()
                    })
                    .GroupBy(se => se.Stage)
                    .AsEnumerable()
                    .Select(g =>
                    {
                        return new
                        {
                            Stage = g.Key,
                            Wins = Intervals
                            .Select(i => g
                                .Where(e => e.IncomeFromUser >= i.MinValue && e.IncomeFromUser <= i.MaxValue)
                                .Count(se => se.IsWon))
                            .ToList(),
                            Income = Intervals
                            .Select(i => g
                                .Where(e => e.IncomeFromUser >= i.MinValue && e.IncomeFromUser <= i.MaxValue)
                                .Sum(se => (int)se.Income))
                            .ToList(),
                            Ends = Intervals
                            .Select(i => g
                                .Where(e => e.IncomeFromUser >= i.MinValue && e.IncomeFromUser <= i.MaxValue)
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
                    .Select(x => new
                    {
                        x.Stage,
                        IncomeFromUser = context.CurrencyPurchases
                            .Join(
                                context.Events.Where(e => e.UserId == x.UserId && e.DateTime == LastDate),
                                cp => cp.EventId,
                                e => e.EventId,
                                (cp, e) => cp.Price
                            )
                            .Sum()
                    })
                    .GroupBy(ss => ss.Stage)
                    .AsEnumerable()
                    .Select(g => new
                    {
                        Stage = g.Key,
                        Starts = Intervals
                            .Select(i => g
                                .Where(e => e.IncomeFromUser >= i.MinValue && e.IncomeFromUser <= i.MaxValue)
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
                        .Range(0, Intervals.Count())
                        .Select(i => g.Select(e => e.Starts).Sum(list => list[i]))
                        .ToList(),
                    Ends = Enumerable
                        .Range(0, Intervals.Count())
                        .Select(i => g.Select(e => e.Ends).Sum(list => list[i]))
                        .ToList(),
                    Wins = Enumerable
                        .Range(0, Intervals.Count())
                        .Select(i => g.Select(e => e.Wins).Sum(list => list[i]))
                        .ToList(),
                    Income = Enumerable
                        .Range(0, Intervals.Count())
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

        public static async Task<List<ItemStatisticWithIntervals>> GetItemsStatisticByIncome()
        {
            var events = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.IngamePurchases
                    .Join(
                        context.Events,
                        ip => ip.EventId,
                        e => e.EventId,
                        (ip, e) => new { ip.ItemName, ip.Price, Date = e.DateTime, e.UserId }
                    )
                    .Select(x => new
                    {
                        x.Date,
                        x.ItemName,
                        x.Price,
                        IncomeFromUser = context.CurrencyPurchases
                            .Join(
                                context.Events.Where(e => e.UserId == x.UserId && e.DateTime == LastDate),
                                cp => cp.EventId,
                                e => e.EventId,
                                (cp, e) => cp.Price
                            )
                            .Sum()
                    })
                    .GroupBy(e => e.ItemName)
                    .AsEnumerable()
                    .Select(g => new ItemStatisticWithIntervals()
                    {
                        ItemName = g.Key,
                        Amount = Intervals
                            .Select(i => g
                                .Where(e => e.IncomeFromUser >= i.MinValue && e.IncomeFromUser <= i.MaxValue)
                                .Count())
                            .ToList(),
                        Income = Intervals
                            .Select(i => g
                                .Where(e => e.IncomeFromUser >= i.MinValue && e.IncomeFromUser <= i.MaxValue)
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
                            .Range(0, Intervals.Count())
                            .Select(i => g.Select(e => e.Amount).Sum(list => list[i]))
                            .ToList(),
                        Income = Enumerable
                            .Range(0, Intervals.Count())
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

        public static async Task<List<int>> GetMauByIncomeAsync()
        {
            DateTime lastDate = await DBHandler.GetDateOfLastEventAsync();
            var stats = await DBHandler.ExecuteInMultiThreadUsingSingleValue((context) =>
            {
                var groups = context.Events
                    .Where(e => e.EventType == 1 && EF.Functions.DateDiffDay(e.DateTime, lastDate) <= 30)
                    .Select(x => new
                    {
                        x.UserId,
                        IncomeFromUser = context.CurrencyPurchases
                            .Join(
                                context.Events.Where(e => e.UserId == x.UserId && e.DateTime == LastDate),
                                cp => cp.EventId,
                                e => e.EventId,
                                (cp, e) => cp.Price
                            )
                            .Sum()
                    });
                var intervals = Intervals;
                return intervals
                    .Select(i => groups
                        .Where(g => g.IncomeFromUser >= i.MinValue && g.IncomeFromUser <= i.MaxValue)
                        .Select(e => e.UserId)
                        .Distinct()
                        .Count())
                    .ToList();
            });

            var mergedStats = Enumerable
                .Range(0, Intervals.Count())
                .Select(i => stats.Select(e => e.ElementAtOrDefault(i, 0)).Sum())
                .ToList();

            return mergedStats;
        }

        public static async Task<List<DateListIntervalEnters<int>>> GetIncomeStatisticByEventTypeAsync(int type)
        {
            var events = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Events
                    .Where(e => e.EventType == type)
                    .Select(x => new
                    {
                        x.UserId,
                        Date = x.DateTime,
                        IncomeFromUser = context.CurrencyPurchases
                            .Join(
                                context.Events.Where(e => e.UserId == x.UserId && e.DateTime == LastDate),
                                cp => cp.EventId,
                                e => e.EventId,
                                (cp, e) => cp.Price
                            )
                            .Sum()
                    })
                    .GroupBy(e => e.Date)
                    .AsEnumerable()
                    .Select(g => new DateListIntervalEnters<int>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters = Intervals
                            .Select(i => g
                                .Where(x => x.IncomeFromUser >= i.MinValue && x.IncomeFromUser <= i.MaxValue)
                                .Select(x => x.UserId).Distinct().Count())
                            .ToList()
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new DateListIntervalEnters<int>
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
                    .Select(x => new
                    {
                        x.UserId,
                        x.Date,
                        x.Income,
                        IncomeFromUser = context.CurrencyPurchases
                            .Join(
                                context.Events.Where(e => e.UserId == x.UserId && e.DateTime == LastDate),
                                cp => cp.EventId,
                                e => e.EventId,
                                (cp, e) => cp.Price
                            )
                            .Sum()
                    })
                    .GroupBy(e => e.Date)
                    .AsEnumerable()
                    .Select(g => new DateListIntervalEnters<decimal>() 
                    { 
                        Date = DateOnly.FromDateTime(g.Key), 
                        IntervalEnters = Intervals
                            .Select(i => g
                                .Where(x => x.IncomeFromUser >= i.MinValue && x.IncomeFromUser <= i.MaxValue)
                                .Sum(x => x.Income))
                            .ToList()
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

using DataAgregation.ClusterModels;
using DataAgregation.DataManipulationModels;
using DataAgregation.Models;
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
    internal class UserIsCheaterByDate
    {
        public int EventId { get; set; }
        public DateTime Date { get; set; }
        public string UserId { get; set; }
        public bool IsCheater { get; set; }
    }

    static public class CheaterService
    {
        public static async Task<List<DateListIntervalEnters<int>>> GetCheatersStatisticByEventTypeAsync(int type)
        {
            var stats = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return GetUserIsCheaterByDates(
                        context, 
                        context.Events.Where(e => e.EventType == type)
                    )
                    .GroupBy(e => e.Date)
                    .AsEnumerable()
                    .Select(g => new DateListIntervalEnters<int>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters = new List<int> 
                        { 
                            g.Where(e => !e.IsCheater).Select(e => e.UserId).Distinct().Count(),
                            g.Where(e => e.IsCheater).Select(e => e.UserId).Distinct().Count()
                        }
                    });
            });

            var mergedStats = stats
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

            return mergedStats;
        }

        private static IQueryable<UserIsCheaterByDate> GetUserIsCheaterByDates(DataContext context, IQueryable<Event> events)
        {
            return events
                //.Join(
                //    context.Users,
                //    e => e.UserId,
                //    u => u.UserId,
                //    (e, u) => new
                //    {
                //        UserId = e.UserId,
                //        Date = e.DateTime,
                //        EventId = e.EventId,
                //    }
                //)
                .Select(x => new
                {
                    Date = x.DateTime,
                    x.UserId,
                    x.EventId,
                    Income = context.Events.Where(e => e.UserId == x.UserId && e.DateTime == x.DateTime)
                        .Join(
                            context.StageEnds,
                            e => e.EventId,
                            se => se.EventId,
                            (e, se) => new
                            {
                                Income = se.Income,
                            })
                        .Sum(se => (int)se.Income)
                })
                .Select(x => new
                {
                    x.Date,
                    x.UserId,
                    x.EventId,
                    Income = x.Income + context.Events.Where(e => e.UserId == x.UserId && e.DateTime == x.Date && e.EventType == 6)
                        .Join(
                            context.CurrencyPurchases,
                            e => e.EventId,
                            cp => cp.EventId,
                            (e, cp) => new
                            {
                                Income = cp.Income,
                            })
                        .Sum(se => se.Income)
                })
                .Select(x => new
                {
                    x.Date,
                    x.UserId,
                    x.Income,
                    x.EventId,
                    Expenses = context.Events.Where(e => e.UserId == x.UserId && e.DateTime == x.Date && e.EventType == 5)
                        .Join(
                            context.IngamePurchases,
                            e => e.EventId,
                            ip => ip.EventId,
                            (e, ip) => new
                            {
                                Expenses = ip.Price,
                            })
                        .Sum(se => se.Expenses)
                })
                .Select(x => new UserIsCheaterByDate
                {
                    EventId = x.EventId,
                    Date = x.Date,
                    UserId = x.UserId,
                    IsCheater = x.Income - x.Expenses < 0
                });
        }

        public static async Task<List<DateListIntervalEnters<decimal>>> GetRevenuebyCheatingAsync()
        {
            var stats = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.CurrencyPurchases
                    .Join(
                        GetUserIsCheaterByDates(
                            context,
                            context.Events.Where(e => e.EventType == 6)),
                        cp => cp.EventId,
                        e => e.EventId,
                        (cp, e) => new { Date = e.Date, Income = cp.Price, UserId = e.UserId, e.IsCheater }
                    )
                    .GroupBy(e => e.Date)
                    .AsEnumerable()
                    .Select(g => new DateListIntervalEnters<decimal>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters = new List<decimal>
                        {
                            g.Where(e => !e.IsCheater).Sum(e => e.Income),
                            g.Where(e => e.IsCheater).Sum(e => e.Income),
                        }
                    });
            });

            var mergedStats = stats
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
            return mergedStats;
        }

        public static async Task<List<int>> GetMauByCheatingAsync()
        {
            DateTime lastDate = await DBHandler.GetDateOfLastEventAsync();
            var stats = await DBHandler.ExecuteInMultiThreadUsingSingleValue((context) =>
            {
                var group = GetUserIsCheaterByDates(
                        context,
                        context.Events
                            .Where(e => e.EventType == 1 && EF.Functions.DateDiffDay(e.DateTime, lastDate) <= 30)
                );
                return new List<int>
                {
                    group.Where(e => !e.IsCheater).Select(x => x.UserId).Distinct().Count(),
                    group.Where(e => e.IsCheater).Select(x => x.UserId).Distinct().Count(),
                };
            });
            var mergedStats = Enumerable
                .Range(0, stats.Max(e => e.Count))
                .Select(i => stats.Sum(list => list.ElementAtOrDefault(i, 0)))
                .ToList();

            return mergedStats;
        }

        public static async Task<List<ItemStatisticWithIntervals>> GetItemsStatisticByCheating()
        {
            var stats = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.IngamePurchases
                    .Join(
                        GetUserIsCheaterByDates(
                            context,
                            context.Events.Where(e => e.EventType == 5)),
                        ip => ip.EventId,
                        e => e.EventId,
                        (ip, e) => new { ip.ItemName, ip.Price, UserId = e.UserId, e.IsCheater }
                    )
                    .GroupBy(e => e.ItemName)
                    .AsEnumerable()
                    .Select(g => new ItemStatisticWithIntervals
                    {
                        ItemName = g.Key,
                        Income = new List<int>
                        {
                            g.Where(e => !e.IsCheater).Sum(e => e.Price),
                            g.Where(e => e.IsCheater).Sum(e => e.Price),
                        },
                        Amount = new List<int>
                        {
                            g.Where(e => !e.IsCheater).Count(),
                            g.Where(e => e.IsCheater).Count(),
                        }
                    });
            });

            var mergedStats = stats
                .GroupBy(e => e.ItemName)
                .Select(g => new ItemStatisticWithIntervals
                {
                    ItemName = g.Key,
                    Amount = Enumerable
                        .Range(0, g.Max(e => e.Amount.Count))
                        .Select(i => g.Select(e => e.Amount.ElementAtOrDefault(i, 0)).Sum())
                        .ToList(),
                    Income = Enumerable
                        .Range(0, g.Max(e => e.Income.Count))
                        .Select(i => g.Select(e => e.Income.ElementAtOrDefault(i, 0)).Sum())
                        .ToList(),
                })
                .OrderBy(e => e.ItemName)
                .ToList();

            List<ItemDateCount> itemDateCounts = await DBHandler.GetItemDateCountAsync();
            List<CurrencyRate> rate = await DBHandler.GetCurrencyRateAsync();
            foreach (var itemStat in mergedStats)
            {
                var dates = itemDateCounts.Where(e => e.Item == itemStat.ItemName);
                decimal coef = dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
                itemStat.USD = itemStat.Income.Select(x => x * coef).ToList();
            }

            return mergedStats;
        }

        public static async Task<List<StageStatisticWithIntervals>> GetStageStatisticByCheatingAsync()
        {
            var stats = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.StageStarts
                    .Join(
                        GetUserIsCheaterByDates(
                            context,
                            context.Events.Where(e => e.EventType == 3)),
                        ss => ss.EventId,
                        e => e.EventId,
                        (ss, e) => new { ss.Stage, UserId = e.UserId, e.IsCheater }
                    )
                    .GroupBy(ss => ss.Stage)
                    .AsEnumerable()
                    .Select(g => new
                    {
                        Stage = g.Key,
                        Starts = new List<int>
                        {
                            g.Where(e => !e.IsCheater).Count(),
                            g.Where(e => e.IsCheater).Count(),
                        }
                    }).Join(
                    context.StageEnds
                    .Join(
                        GetUserIsCheaterByDates(
                            context,
                            context.Events.Where(e => e.EventType == 4)),
                        se => se.EventId,
                        e => e.EventId,
                        (ss, e) => new { ss.Stage, ss.IsWon, ss.Income, UserId = e.UserId, e.IsCheater }
                    )
                    .GroupBy(se => se.Stage)
                    .AsEnumerable()
                    .Select(g => new
                    {
                        Stage = g.Key,
                        Wins = new List<int>
                        {
                            g.Where(e => !e.IsCheater).Count(e => e.IsWon),
                            g.Where(e => e.IsCheater).Count(e => e.IsWon),
                        },
                        Income = new List<int>
                        {
                            g.Where(e => !e.IsCheater).Sum(e => (int)e.Income),
                            g.Where(e => e.IsCheater).Sum(e => (int)e.Income),
                        },
                        Ends = new List<int>
                        {
                            g.Where(e => !e.IsCheater).Count(),
                            g.Where(e => e.IsCheater).Count(),
                        },
                    }),
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

            var mergedStats = stats
                .GroupBy(e => e.Stage)
                .Select(g => new StageStatisticWithIntervals
                {
                    Stage = g.Key,
                    Starts = Enumerable
                        .Range(0, g.Max(e => e.Starts.Count))
                        .Select(i => g.Select(e => e.Starts.ElementAtOrDefault(i, 0)).Sum())
                        .ToList(),
                    Ends = Enumerable
                        .Range(0, g.Max(e => e.Ends.Count))
                        .Select(i => g.Select(e => e.Ends.ElementAtOrDefault(i, 0)).Sum())
                        .ToList(),
                    Wins = Enumerable
                        .Range(0, g.Max(e => e.Wins.Count))
                        .Select(i => g.Select(e => e.Wins.ElementAtOrDefault(i, 0)).Sum())
                        .ToList(),
                    Income = Enumerable
                        .Range(0, g.Max(e => e.Income.Count))
                        .Select(i => g.Select(e => e.Income.ElementAtOrDefault(i, 0)).Sum())
                        .ToList(),
                })
                .OrderBy(e => e.Stage)
                .ToList();

            List<StageDateCount> stageDateCounts = await DBHandler.GetStageDateCountAsync();
            List<CurrencyRate> rate = await DBHandler.GetCurrencyRateAsync();
            foreach (var stageStat in mergedStats)
            {
                var dates = stageDateCounts.Where(e => e.Stage == stageStat.Stage);
                decimal coef = dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
                stageStat.USD = stageStat.Income.Select(x => x * coef).ToList();
            }

            return mergedStats;
        }
    }
}

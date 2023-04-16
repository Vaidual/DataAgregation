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
    static public class CountryService
    {
        public static string?[] SortedDistinctCountries = Array.Empty<string?>();

        public static async Task<string?[]> GetCountryTypes()
        {
            var Countries = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Users.Select(u => u.Country);
            });
            var mergedCountries = Countries.Distinct().Order().ToArray();

            return mergedCountries;
        }

        public static async Task<List<DateListIntervalEnters<int>>> GetCountryStatisticByEventTypeAsync(int type)
        {
            var stats = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Events
                    .Where(e => e.EventType == type)
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new 
                        {
                            UserId = e.UserId, 
                            Country = u.Country, 
                            Date = e.DateTime 
                        }
                    )
                    .GroupBy(e => e.Date)
                    .AsEnumerable()
                    .Select(g =>
                    {
                        var Countries = SortedDistinctCountries;
                        return new DateListIntervalEnters<int>
                        {
                            Date = DateOnly.FromDateTime(g.Key),
                            IntervalEnters = Countries
                                .Select(Country => g
                                    .Where(e => e.Country == Country)
                                    .Select(e => e.UserId)
                                    .Distinct()
                                    .Count())
                                .ToList()
                        };
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

        public static async Task<List<DateListIntervalEnters<decimal>>> GetRevenuebyCountryAsync()
        {
            var stats = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
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
                        (e, u) => new { e.Date, e.Income, u.Country }
                    )
                    .GroupBy(e => e.Date)
                    .AsEnumerable()
                    .Select(g =>
                    {
                        var Countries = SortedDistinctCountries;
                        return new DateListIntervalEnters<decimal>
                        {
                            Date = DateOnly.FromDateTime(g.Key),
                            IntervalEnters = Countries
                            .Select(Country => g
                                .Where(g => g.Country == Country)
                                .Sum(e => e.Income))
                            .ToList(),
                        };
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

        public static async Task<List<int>> GetMauByCountryAsync()
        {
            DateTime lastDate = await DBHandler.GetDateOfLastEventAsync();
            var stats = await DBHandler.ExecuteInMultiThreadUsingSingleValue((context) =>
            {
                var groups = context.Events
                    .Where(e => e.EventType == 1 && EF.Functions.DateDiffDay(e.DateTime, lastDate) <= 30)
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { UserId = e.UserId, Country = u.Country }
                    );
                var Countries = SortedDistinctCountries;
                return Countries
                    .Select(Country => groups
                        .Where(g => g.Country == Country)
                        .Select(e => e.UserId)
                        .Distinct()
                        .Count())
                    .ToList();
            });
            var mergedStats = Enumerable
                .Range(0, stats.Max(e => e.Count))
                .Select(i => stats.Sum(list => list.ElementAtOrDefault(i)))
                .ToList();

            return mergedStats;
        }

        public static async Task<List<ItemStatisticWithIntervals>> GetItemsStatisticByCountry()
        {
            var stats = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
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
                        (e, u) => new { e.ItemName, e.Price, u.Country }
                    )
                    .GroupBy(e => e.ItemName)
                    .AsEnumerable()
                    .Select(g =>
                    {
                        var Countries = SortedDistinctCountries;
                        return new ItemStatisticWithIntervals
                        {
                            ItemName = g.Key,
                            Income = Countries
                                .Select(Country => g
                                    .Where(g => g.Country == Country)
                                    .Sum(e => e.Price))
                                .ToList(),
                            Amount = Countries
                                .Select(Country => g
                                    .Where(g => g.Country == Country)
                                    .Count())
                                .ToList(),
                        };
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

        public static async Task<List<StageStatisticWithIntervals>> GetStageStatisticByCountryAsync()
        {
            var stats = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.StageStarts
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
                        (e, u) => new { e.Stage, u.Country }
                    )
                    .GroupBy(ss => ss.Stage)
                    .AsEnumerable()
                    .Select(g =>
                    {
                        var Countries = SortedDistinctCountries;
                        return new
                        {
                            Stage = g.Key,
                            Starts = Countries
                            .Select(Country => g.Where(g => g.Country == Country).Count())
                            .ToList(),
                        };
                    }).Join(
                    context.StageEnds
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
                        (ss, u) => new { ss.Stage, ss.IsWon, ss.Income, u.Country }
                    )
                    .GroupBy(se => se.Stage)
                    .AsEnumerable()
                    .Select(g =>
                    {
                        var Countries = SortedDistinctCountries;
                        return new
                        {
                            Stage = g.Key,
                            Wins = Countries
                            .Select(Country => g
                                .Where(g => g.Country == Country)
                                .Count(e => e.IsWon))
                            .ToList(),
                            Income = Countries
                            .Select(Country => g
                                .Where(g => g.Country == Country)
                                .Sum(e => (int)e.Income))
                            .ToList(),
                            Ends = Countries
                            .Select(Country => g
                                .Where(g => g.Country == Country)
                                .Count())
                            .ToList(),
                        };
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

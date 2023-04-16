using DataAgregation.ClusterModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation.Tools
{
    static public class GenderService
    {
        static public async Task<string[]> GetGenderTypes()
        {
            var genders = await DBHandler.ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Users.Select(u => u.Gender);
            });
            var mergedGenders = genders.Distinct().Order().ToArray();

            return mergedGenders;
        }

        static public async Task<List<DateListIntervalEnters<int>>> GetGenderStatisticByEventTypeAsync(int type)
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
                            Gender = u.Gender, 
                            Date = e.DateTime 
                        }
                    )
                    .GroupBy(e => e.Date)
                    .Select(g => new DateListIntervalEnters<int>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters = g
                            .GroupBy(e => e.Gender)
                            .OrderBy(g => g.Key)
                            .Select(g => g
                                .Select(e => e.UserId)
                                .Distinct()
                                .Count())
                            .ToList()
                    });
            });
            var mergedStats = stats
                .GroupBy(e => e.Date)
                .Select(g => new DateListIntervalEnters<int>
                {
                    Date = g.Key,
                    IntervalEnters = Enumerable
                        .Range(0, g.First().IntervalEnters.Count)
                        .Select(i => g.Select(e => e.IntervalEnters).Sum(list => list[i]))
                        .ToList(),
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedStats;
        }

        public async static Task<List<DateListIntervalEnters<decimal>>> GetRevenuebyGenderAsync()
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
                        (e, u) => new { e.Date, e.Income, u.Gender }
                    )
                    .GroupBy(e => e.Date)
                    .Select(g => new DateListIntervalEnters<decimal>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters = g
                            .GroupBy(e => e.Gender)
                            .OrderBy(g => g.Key)
                            .Select(g => g
                                .Sum(e => e.Income))
                            .ToList()
                    });
            });

            var mergedStats = stats
                .GroupBy(e => e.Date)
                .Select(g => new DateListIntervalEnters<decimal>
                {
                    Date = g.Key,
                    IntervalEnters = Enumerable
                        .Range(0, g.First().IntervalEnters.Count)
                        .Select(i => g.Select(e => e.IntervalEnters).Sum(list => list[i]))
                        .ToList(),
                })
                .OrderBy(e => e.Date)
                .ToList();
            return mergedStats;
        }

        static public async Task<List<int>> GetMauByGenderAsync()
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
                        .Count())
                    .ToList();
            });
            var mergedStats = Enumerable
                .Range(0, stats.First().Count())
                .Select(i => stats.Sum(list => list.ElementAt(i)))
                .ToList();

            return mergedStats;
        }
    }
}

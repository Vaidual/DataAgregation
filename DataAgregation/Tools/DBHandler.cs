using DataAgregation.ClusterModels;
using DataAgregation.DataManipulationModels;
using DataAgregation.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataAgregation.Tools
{
    public class DBHandler
    {
        const int maxDb = 8;

        public async Task<List<ItemStatisticWithValues<int>>> GetItemsStatisticWithAges()
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
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
            List<ItemDateCount> itemDateCounts = await GetItemDateCountAsync();
            List<CurrencyRate> rate = await GetCurrencyRateAsync();
            foreach (var stat in mergedEvents)
            {
                var dates = itemDateCounts.Where(e => e.Item == stat.Item);
                stat.USD = stat.Income * dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
            }
            return mergedEvents;
        }

        public async Task<List<ItemStatisticWithIntervals>> GetItemsStatisticByAge(IEnumerable<Interval> intervals)
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
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
                            .Range(0, g.Select(e => e.Amount).First().Count)
                            .Select(i => g.Select(e => e.Amount).Sum(list => list[i]))
                            .ToList(),
                        Income = Enumerable
                            .Range(0, g.Select(e => e.Income).First().Count)
                            .Select(i => g.Select(e => e.Income).Sum(list => list[i]))
                            .ToList(),
                        //Income = new List<int>(),
                        //Amount = new List<int>(),
                    };
                    //for (int i = 0; i < intervals.Count(); i++)
                    //{
                    //    result.Amount.Add(g.Sum(e => e.Amount[i]));
                    //    result.Income.Add(g.Sum(e => e.Income[i]));
                    //}
                    return result;
                })
                .OrderBy(e => e.ItemName)
                .ToList();
            Console.WriteLine(sw.Elapsed.Microseconds);
            List<ItemDateCount> itemDateCounts = await GetItemDateCountAsync();
            List<CurrencyRate> rate = await GetCurrencyRateAsync();
            foreach (var itemStat in mergedItemStats)
            {
                var dates = itemDateCounts.Where(e => e.Item == itemStat.ItemName);
                decimal coef = dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
                itemStat.USD = itemStat.Income.Select(x => x * coef).ToList();
            }
            return mergedItemStats;
        }

        public async Task<List<DateListIntervalEnters<decimal>>> GetRevenuebyAgeAsync2(IEnumerable<Interval> intervals)
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
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
                        var result = new DateListIntervalEnters<decimal>() { Date = DateOnly.FromDateTime(g.Key), IntervalEnters = new List<decimal>()};
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
                .Select(g =>
                {
                    var result = new DateListIntervalEnters<decimal> { Date = g.Key, IntervalEnters = new List<decimal>()};
                    for (int i = 0; i < intervals.Count(); i++)
                    {
                        result.IntervalEnters.Add(g.Sum(e => e.IntervalEnters[i]));
                    }
                    return result;
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedEvents;
        }

        public async Task<List<DateIntervalEnters<decimal>>> GetRevenuebyAgeAsync(IEnumerable<Interval> intervals)
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.CurrencyPurchases
                    .Join(
                        context.Events,
                        cp => cp.EventId,
                        e => e.EventId,
                        (cp, e) => new { Date = e.DateTime, Income = cp.Price, UserId = e.UserId}
                    )
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { e.Date, e.Income, u.Age }
                    )
                    .GroupBy(e => e.Date)
                    .Select(g => new DateIntervalEnters<decimal>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters1 = g.Where(e => e.Age >= intervals.ElementAt(0).MinValue && e.Age <= intervals.ElementAt(0).MaxValue).Sum(e => e.Income),
                        IntervalEnters2 = g.Where(e => e.Age >= intervals.ElementAt(1).MinValue && e.Age <= intervals.ElementAt(1).MaxValue).Sum(e => e.Income),
                        IntervalEnters3 = g.Where(e => e.Age >= intervals.ElementAt(2).MinValue && e.Age <= intervals.ElementAt(2).MaxValue).Sum(e => e.Income),
                        IntervalEnters4 = g.Where(e => e.Age >= intervals.ElementAt(3).MinValue && e.Age <= intervals.ElementAt(3).MaxValue).Sum(e => e.Income),
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new DateIntervalEnters<decimal>
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

        public async Task<MauIntervalEnters> GetMauByAgeAsync(IEnumerable<Interval> intervals)
        {
            DateTime lastDate = await GetDateOfLastEventAsync();
            var events = await ExecuteInMultiThreadUsingSingleValue((context) =>
            {
                return context.Events
                    .Where(e => e.EventType == 1 && EF.Functions.DateDiffDay(e.DateTime, lastDate) <= 30)
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.UserId,
                        (e, u) => new { UserId = e.UserId, Age = u.Age }
                    )
                    .GroupBy(u => 1)
                    .Select(g => new MauIntervalEnters
                    {
                        IntervalEnters1 = g.Where(e => e.Age >= intervals.ElementAt(0).MinValue && e.Age <= intervals.ElementAt(0).MaxValue).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters2 = g.Where(e => e.Age >= intervals.ElementAt(1).MinValue && e.Age <= intervals.ElementAt(1).MaxValue).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters3 = g.Where(e => e.Age >= intervals.ElementAt(2).MinValue && e.Age <= intervals.ElementAt(2).MaxValue).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters4 = g.Where(e => e.Age >= intervals.ElementAt(3).MinValue && e.Age <= intervals.ElementAt(3).MaxValue).Select(e => e.UserId).Distinct().Count(),
                    })
                    .Single();
            });

            return new MauIntervalEnters
            {
                IntervalEnters1 = events.Sum(e => e.IntervalEnters1),
                IntervalEnters2 = events.Sum(e => e.IntervalEnters2),
                IntervalEnters3 = events.Sum(e => e.IntervalEnters3),
                IntervalEnters4 = events.Sum(e => e.IntervalEnters4),
            };
        }

        public async Task<IEnumerable<ClusterInput>> GetUserAges()
        {
            var result = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Users.Select(u => new ClusterInput(u.Age));
            });
            return result;
        }

        private async Task<ConcurrentBag<T>> ExecuteInMultiThreadUsingSingleValue<T>(Func<DataContext, T> func)
        {
            var result = new ConcurrentBag<T>();
            Task[] tasks = new Task[maxDb];
            try
            {
                for (int i = 0; i < maxDb; i++)
                {
                    tasks[i] = await Task.Factory.StartNew(async () =>
                    {
                        using (var context = new DataContext(i))
                        {
                            T data = func(context);
                            Console.WriteLine($"Data from database {i}: {data}");
                            result.Add(data);
                        }
                    });
                }
                Task.WaitAll(tasks);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }

        private async Task<ConcurrentBag<T>> ExecuteInMultiThreadUsingList<T>(Func<DataContext, IEnumerable<T>> func)
        {
            var result = new ConcurrentBag<T>();
            Task[] tasks = new Task[maxDb];
            for (int i = 0; i < maxDb; i++)
            {
                tasks[i] = await Task.Factory.StartNew( async() =>
                {
                    using (var context = new DataContext(i))
                    {
                        IEnumerable<T> data = func(context);
                        foreach (var e in data) result.Add(e);
                    }
                });
            }
            Task.WaitAll(tasks);

            return result;
        }

        internal async Task<List<ItemsStatiscticByDate>> GetItemsByDateStatistic()
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.IngamePurchases
                    .Include(ip => ip.Event)
                    .GroupBy(ip => ip.Event.DateTime)
                    .Select(g => new ItemsStatiscticByDate
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        SoldAmount = g.Count(),
                        Income = g.Sum(e => e.Price)
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new ItemsStatiscticByDate
                {
                    Date = g.Key,
                    SoldAmount = g.Sum(e => e.SoldAmount),
                    Income = g.Sum(e => e.Income)
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedEvents;
        }

        public async Task<List<DateIntervalEnters<int>>> GetAgeStatisticByEventTypeAsync(int type, IEnumerable<Interval> intervals)
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
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

        public async Task<List<DateAges>> GetDateAgesByEventTypeAsync(int type)
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
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

        public async Task<List<DateUsersCount>> GetDateUsersCountByEventTypeAsync(int type)
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Events
                    .Where(e => e.EventType == type)
                    .GroupBy(e => e.DateTime)
                    .Select(g => new DateUsersCount
                    {
                        Date = g.Key.ToShortDateString(),
                        Count = g.Select(e => e.UserId).Distinct().Count()
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new DateUsersCount
                {
                    Date = g.Key,
                    Count = g.Sum(e => e.Count)
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedEvents;
        }

        public async Task<List<CurrencyRate>> GetCurrencyRateAsync()
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.CurrencyPurchases
                    .Join(
                        context.Events,
                        cp => cp.EventId,
                        e => e.EventId,
                        (cp, e) => new { Date = e.DateTime, USD = cp.Price, Income = cp.Income }
                    )
                    .GroupBy(e => e.Date)
                    .Select(g => new CurrencyRateData
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        Income = g.Sum(e => e.USD),
                        BoughtCurrency = g.Sum(e => e.Income),
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new CurrencyRate
                {
                    Date = g.Key,
                    Rate = g.Sum(e => e.Income) / g.Sum(e => e.BoughtCurrency)
                })
                .OrderBy(e => e.Date)
                .ToList();

            return mergedEvents;
        }

        public async Task<List<StageStatistic>> GetStageStatisticAsync()
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.StageStarts
                    .GroupBy(ss => ss.Stage)
                    .Select(ssGroup => new
                    {
                        Stage = ssGroup.Key,
                        Starts = ssGroup.Count()
                    })
                    .Join(context.StageEnds
                        .GroupBy(se => se.Stage)
                        .Select(seGroup => new
                        {
                            Stage = seGroup.Key,
                            Ends = seGroup.Count(),
                            Wins = seGroup.Count(se => se.IsWon),
                            Income = seGroup.Sum(se => se.Income)
                        }),
                        ss => ss.Stage,
                        se => se.Stage,
                        (ss, se) => new StageStatistic
                        {
                            Stage = ss.Stage,
                            Starts = ss.Starts,
                            Ends = se.Ends,
                            Wins = se.Wins,
                            Income = (int)se.Income!,
                            USD = 0
                        })
                    .OrderBy(stat => stat.Stage);
            });
            var mergedEvents = events
                .GroupBy(e => e.Stage)
                .Select(g => new StageStatistic
                {
                    Stage = g.Key,
                    Starts = g.Sum(e => e.Starts),
                    Ends = g.Sum(e => e.Ends),
                    Wins = g.Sum(e => e.Wins),
                    Income = g.Sum(e => e.Income)
                })
                .OrderBy(e => e.Stage)
                .ToList();
            List<StageDateCount> stageDateCounts = await GetStageDateCountAsync();
            List<CurrencyRate> rate = await GetCurrencyRateAsync();
            foreach (var stat in mergedEvents)
            {
                var dates = stageDateCounts.Where(e => e.Stage == stat.Stage);
                stat.USD = stat.Income * dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
            }
            return mergedEvents;
        }

        private async Task<List<ItemDateCount>> GetItemDateCountAsync()
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.IngamePurchases
                    .Join(context.Events,
                        ip => ip.EventId,
                        e => e.EventId,
                        (ip, e) => new
                        {
                            ip.ItemName,
                            Date = e.DateTime
                        })
                    .GroupBy(e => new { e.ItemName, e.Date })
                    .Select(g => new ItemDateCount
                    {
                        Item = g.Key.ItemName,
                        Date = g.Key.Date,
                        Count = g.Count()
                    })
                    .OrderBy(stat => stat.Item)
                    .ThenBy(stat => stat.Date);
            });
            var mergedEvents = events
                .GroupBy(stat => new { stat.Item, stat.Date })
                .Select(g => new ItemDateCount
                {
                    Item = g.Key.Item,
                    Date = g.Key.Date,
                    Count = g.Sum(stat => stat.Count)
                })
                .OrderBy(stat => stat.Item)
                .ThenBy(stat => stat.Date)
                .ToList();
            return mergedEvents;
        }

        public async Task<List<ItemStatistic>> GetItemsStatistic()
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.IngamePurchases
                    .GroupBy(ss => ss.ItemName)
                    .Select(g => new ItemStatistic
                    {
                        Item = g.Key,
                        Amount = g.Count(),
                        Income = g.Sum(i => i.Price),
                        USD = 0
                    })
                    .OrderBy(stat => stat.Item);
            });
            var mergedEvents = events
                .GroupBy(e => e.Item)
                .Select(g => new ItemStatistic
                {
                    Item = g.Key,
                    Amount = g.Sum(i => i.Amount),
                    Income = g.Sum(i => i.Income)
                })
                .OrderBy(e => e.Item)
                .ToList();
            List<ItemDateCount> itemDateCounts = await GetItemDateCountAsync();
            List<CurrencyRate> rate = await GetCurrencyRateAsync();
            foreach (var stat in mergedEvents)
            {
                var dates = itemDateCounts.Where(e => e.Item == stat.Item);
                stat.USD = stat.Income * dates.Sum(d => d.Count * rate.First(r => r.Date == DateOnly.FromDateTime(d.Date)).Rate / dates.Sum(d => d.Count));
            }
            return mergedEvents;
        }

        public async Task<List<StageDateCount>> GetStageDateCountAsync()
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.StageEnds
                    .Where(se => se.Income != null)
                        .Join(context.Events,
                            se => se.EventId,
                            e => e.EventId,
                            (se, e) => new
                            {
                                se.Stage,
                                Date = e.DateTime
                            })
                        .GroupBy(ss => new { ss.Stage, ss.Date })
                        .Select(g => new StageDateCount
                        {
                            Stage = g.Key.Stage,
                            Date = g.Key.Date,
                            Count = g.Count()
                        })
                        .OrderBy(stat => stat.Stage)
                        .ThenBy(stat => stat.Date);
            });
            var mergedEvents = events
                .GroupBy(ss => new { ss.Stage, ss.Date })
                .Select(g => new StageDateCount
                {
                    Stage = g.Key.Stage,
                    Date = g.Key.Date,
                    Count = g.Sum(stat => stat.Count)
                })
                .OrderBy(stat => stat.Stage)
                .ThenBy(stat => stat.Date)
                .ToList();
            return mergedEvents;
        }

        public async Task<List<Revenue>> GetRevenueAsync()
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Events
                    .Where(e => e.EventType == 6)
                    .Include(e => e.CurrencyPurchases)
                    .GroupBy(e => e.DateTime)
                    .Select(g => new Revenue
                    {
                        Date = g.Key.ToShortDateString(),
                        Income = g.SelectMany(e => e.CurrencyPurchases).Sum(cp => cp.Price)
                    });
            });
            var mergedEvents = events
                .GroupBy(e => e.Date)
                .Select(g => new Revenue
                {
                    Date = g.Key,
                    Income = g.Sum(e => e.Income)
                })
                .OrderBy(e => e.Date)
                .ToList();
            return mergedEvents;
        }

        public async Task<int> GetLastMonthUsersCountAsync()
        {
            Task[] tasks = new Task[maxDb];
            DateTime lastDate = await GetDateOfLastEventAsync();
            int lastMonthUsersCount = 0;
            for (int i = 0; i < maxDb; i++)
            {
                tasks[i] = await Task.Factory.StartNew(async () =>
                {
                    using (var context = new DataContext(i))
                    {
                        var result = context.Events
                            .Where(e => e.EventType == 1 && EF.Functions.DateDiffDay(e.DateTime, lastDate) <= 30)
                            .Select(e => e.UserId)
                            .Distinct()
                            .Count();
                        Interlocked.Add(ref lastMonthUsersCount, result);
                    }
                });
            }
            Task.WaitAll(tasks);
            return lastMonthUsersCount;
        }

        public async Task<DateTime> GetDateOfLastEventAsync()
        {
            ConcurrentBag<DateTime> dates = await ExecuteInMultiThreadUsingSingleValue((context) =>
            {
                return context.Events.Max(e => e.DateTime);
            });
            return dates.Max();
        }

        public async Task AddEventsAsync(FileInfo file)
        {
            Console.WriteLine(file.Name + " -- started");
            Stopwatch stopwatch = Stopwatch.StartNew();
            string json = File.ReadAllText(file.FullName);
            List<JObject> data = JsonConvert.DeserializeObject<JObject[]>(json)!.ToList();
            int count = data.Count;
            var distinctUsers = data.Select(o => o.Value<string>("udid")).Distinct().ToList();
            int distinctUsersCount = distinctUsers.Count();
            Task[] tasks = new Task[maxDb];
            for (int i = 0; i < maxDb; i++)
            {
                List<JObject> eventsToIterate;

                if (i == maxDb - 1)
                {
                    eventsToIterate = data;
                }
                else
                {
                    int userCountToTake = (int)Math.Ceiling((float)distinctUsersCount / (maxDb - i));
                    var indexToStop = data.FindIndex(0, o => o.Value<string>("udid") == distinctUsers.ElementAt(userCountToTake + 1));
                    eventsToIterate = data.Take(indexToStop).ToList();

                    distinctUsers = distinctUsers.Skip(userCountToTake).ToList();
                    distinctUsersCount -= userCountToTake;
                    data = data.Skip(indexToStop).ToList();
                }
                tasks[i] = await Task.Factory.StartNew(async () =>
                {
                    using (var context = new DataContext(i))
                    {
                        await context.Database.EnsureCreatedAsync();
                        foreach (JObject o in eventsToIterate)
                        {
                            await AddEventAsync(o, context);
                        }
                        await context.SaveChangesAsync();
                        await context.Database.CloseConnectionAsync();
                    }
                });
            }
            Task.WaitAll(tasks);
            Console.WriteLine(file.Name + " : " + count + " : " + stopwatch.Elapsed.Minutes + ":" + stopwatch.Elapsed.Seconds);
        }

        private async Task AddEventAsync(JObject o, DataContext context)
        {
            int eventTypeIdentifier = o.Value<int>("event_id");
            if (eventTypeIdentifier == 2 && await context.Users.FirstOrDefaultAsync(u => u.UserId == o.Value<string>("udid")) != null) return;
            var parameters = o.Value<JObject>("parameters");
            Event newEvent = new()
            {
                EventType = eventTypeIdentifier,
                DateTime = o.Value<DateTime>("date"),
                UserId = o.Value<string>("udid"),
            };
            await context.AddAsync(newEvent);

            if (eventTypeIdentifier == 2)
            {
                User newUser = new()
                {
                    UserId = o.Value<string>("udid"),
                    Gender = parameters.Value<string>("gender"),
                    Country = parameters.ContainsKey("country") ? parameters.Value<string>("country") : null,
                    Age = parameters.Value<int>("age"),
                };
                await context.AddAsync(newUser);
                return;
            }

            switch (eventTypeIdentifier)
            {
                case 3:
                    StageStart stageStart = new()
                    {
                        Stage = parameters.Value<int>("stage"),
                        Event = newEvent
                    };
                    await context.AddAsync(stageStart);
                    break;
                case 4:
                    StageEnd stageEnd = new()
                    {
                        Event = newEvent,
                        Stage = parameters.Value<int>("stage"),
                        IsWon = parameters.Value<bool>("win"),
                        Time = parameters.Value<int>("time"),
                        Income = parameters.ContainsKey("income") ? parameters.Value<int>("income") : null
                    };
                    await context.AddAsync(stageEnd);
                    break;
                case 5:
                    IngamePurchase ingamePurchase = new()
                    {
                        Event = newEvent,
                        ItemName = parameters.Value<string>("item"),
                        Price = parameters.Value<int>("price")
                    };
                    await context.AddAsync(ingamePurchase);
                    break;
                case 6:
                    CurrencyPurchase purchase = new()
                    {
                        Event = newEvent,
                        Income = parameters.Value<int>("income"),
                        PackName = parameters.Value<string>("name"),
                        Price = parameters.Value<decimal>("price")
                    };
                    await context.AddAsync(purchase);
                    break;
            }
        }
    }
}

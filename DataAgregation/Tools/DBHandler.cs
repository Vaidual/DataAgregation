﻿using DataAgregation.ClusterModels;
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

        public async Task<List<DateIntervalEnters<decimal>>> GetRevenuebyAgeAsync(IEnumerable<AgeInterval> intervals)
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.CurrencyPurchases
                    .Include(cp => cp.Event)
                    .ThenInclude(e => e.User)
                    .Select(e => new { Date = e.Event.Time, Age = e.Event.User.Age, Income = e.Price })
                    .GroupBy(e => e.Date)
                    .Select(g => new DateIntervalEnters<decimal>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters1 = g.Where(e => e.Age >= intervals.ElementAt(0).MinAge && e.Age <= intervals.ElementAt(0).MaxAge).Sum(e => e.Income),
                        IntervalEnters2 = g.Where(e => e.Age >= intervals.ElementAt(1).MinAge && e.Age <= intervals.ElementAt(1).MaxAge).Sum(e => e.Income),
                        IntervalEnters3 = g.Where(e => e.Age >= intervals.ElementAt(2).MinAge && e.Age <= intervals.ElementAt(2).MaxAge).Sum(e => e.Income),
                        IntervalEnters4 = g.Where(e => e.Age >= intervals.ElementAt(3).MinAge && e.Age <= intervals.ElementAt(3).MaxAge).Sum(e => e.Income),
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

        public async Task<MauIntervalEnters> GetMauByAgeAsync(IEnumerable<AgeInterval> intervals)
        {
            DateTime lastDate = await GetDateOfLastEventAsync();
            var events = await ExecuteInMultiThreadUsingSingleValue((context) =>
            {
                return context.Events
                    .Where(e => e.EventIdentifier == 1 && EF.Functions.DateDiffDay(e.Time, lastDate) <= 30)
                    .Join(
                        context.Users,
                        e => e.UserId,
                        u => u.Id,
                        (e, u) => new { UserId = e.UserId, Age = u.Age }
                    )
                    .GroupBy(u => 1)
                    .Select(g => new MauIntervalEnters
                    {
                        IntervalEnters1 = g.Where(e => e.Age >= intervals.ElementAt(0).MinAge && e.Age <= intervals.ElementAt(0).MaxAge).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters2 = g.Where(e => e.Age >= intervals.ElementAt(1).MinAge && e.Age <= intervals.ElementAt(1).MaxAge).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters3 = g.Where(e => e.Age >= intervals.ElementAt(2).MinAge && e.Age <= intervals.ElementAt(2).MaxAge).Select(e => e.UserId).Distinct().Count(),
                        IntervalEnters4 = g.Where(e => e.Age >= intervals.ElementAt(3).MinAge && e.Age <= intervals.ElementAt(3).MaxAge).Select(e => e.UserId).Distinct().Count(),
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
                    .GroupBy(ip => ip.Event.Time)
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

        public async Task<List<DateIntervalEnters<int>>> GetAgeStatisticByEventTypeAsync(int type, IEnumerable<AgeInterval> intervals)
        {
            var events = await ExecuteInMultiThreadUsingList((context) =>
            {
                return context.Events
                    .Where(e => e.EventIdentifier == type)
                    .Include(e => e.User)
                    .GroupBy(e => e.Time)
                    .Select(g => new DateIntervalEnters<int>
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        IntervalEnters1 = g.Count(e => e.User.Age >= intervals.ElementAt(0).MinAge && e.User.Age <= intervals.ElementAt(0).MaxAge),
                        IntervalEnters2 = g.Count(e => e.User.Age >= intervals.ElementAt(1).MinAge && e.User.Age <= intervals.ElementAt(1).MaxAge),
                        IntervalEnters3 = g.Count(e => e.User.Age >= intervals.ElementAt(2).MinAge && e.User.Age <= intervals.ElementAt(2).MaxAge),
                        IntervalEnters4 = g.Count(e => e.User.Age >= intervals.ElementAt(3).MinAge && e.User.Age <= intervals.ElementAt(3).MaxAge),
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
                    .Where(e => e.EventIdentifier == type)
                    .Include(e => e.User)
                    .GroupBy(e => e.Time)
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
                    .Where(e => e.EventIdentifier == type)
                    .GroupBy(e => e.Time)
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
                return context.Events
                    .Where(e => e.EventIdentifier == 6)
                    .Include(e => e.CurrencyPurchases)
                    .GroupBy(e => e.Time)
                    .Select(g => new CurrencyRateData
                    {
                        Date = DateOnly.FromDateTime(g.Key),
                        Income = g.SelectMany(e => e.CurrencyPurchases).Sum(cp => cp.Price),
                        BoughtCurrency = g.SelectMany(e => e.CurrencyPurchases).Sum(cp => cp.Income),
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
                        e => e.Id,
                        (ip, e) => new
                        {
                            ip.ItemName,
                            Date = e.Time
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
                            e => e.Id,
                            (se, e) => new
                            {
                                se.Stage,
                                Date = e.Time
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
                    .Where(e => e.EventIdentifier == 6)
                    .Include(e => e.CurrencyPurchases)
                    .GroupBy(e => e.Time)
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
                            .Where(e => e.EventIdentifier == 1 && EF.Functions.DateDiffDay(e.Time, lastDate) <= 30)
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
                return context.Events.Max(e => e.Time);
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
            if (eventTypeIdentifier == 2 && await context.Users.FirstOrDefaultAsync(u => u.Id == o.Value<string>("udid")) != null) return;
            var parameters = o.Value<JObject>("parameters");
            Event newEvent = new()
            {
                EventIdentifier = eventTypeIdentifier,
                Time = o.Value<DateTime>("date"),
                UserId = o.Value<string>("udid"),
            };
            await context.AddAsync(newEvent);

            if (eventTypeIdentifier == 2)
            {
                User newUser = new()
                {
                    Id = o.Value<string>("udid"),
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

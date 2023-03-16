using DataAgregation.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAgregation
{
    public class DBHandler
    {
        public DataContext context;
        public DBHandler(int dbIndex) 
        {
            context = new DataContext(dbIndex);
        }

        public async Task AddEventAsync(JObject o)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();
                int eventTypeIdentifier = o.Value<int>("event_id");
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
                    await context.SaveChangesAsync();
                    return;
                }

                await context.SaveChangesAsync();
                int eventId = newEvent.Id;

                switch (eventTypeIdentifier)
                {
                    case 3:
                        StageStart stageStart = new()
                        {
                            Stage = parameters.Value<int>("stage"),
                            EventId = eventId
                        };
                        await context.AddAsync(stageStart);
                        break;
                    case 4:
                        StageEnd stageEnd = new()
                        {
                            EventId = eventId,
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
                            EventId = eventId,
                            ItemName = parameters.Value<string>("item"),
                            Price = parameters.Value<int>("price")
                        };
                        await context.AddAsync(ingamePurchase);
                        break;
                    case 6:
                        CurrencyPurchase purchase = new()
                        {
                            EventId = eventId,
                            Income = parameters.Value<int>("income"),
                            PackName = parameters.Value<string>("name"),
                            Price = parameters.Value<decimal>("price")
                        };
                        await context.AddAsync(purchase);
                        break;
                }
                await context.SaveChangesAsync();
            }
            catch { }
            return;
        }
    }
}

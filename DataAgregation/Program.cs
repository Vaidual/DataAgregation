// See https://aka.ms/new-console-template for more information

using DataAgregation;
using DataAgregation.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;

const int maxDb = 8;
const string dirPath = "../../../data";

var dir = new DirectoryInfo(dirPath).GetFiles();

foreach (var file in dir.Skip(1))
{
    Console.WriteLine(file.Name + " -- started");
    Stopwatch stopwatch = Stopwatch.StartNew();
    string json = file.OpenText().ReadToEnd();
    var data = JsonConvert.DeserializeObject<JObject[]>(json)!.ToList();
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
            DBHandler dBHandler = new DBHandler(i);
            foreach (JObject o in eventsToIterate)
            {
                await dBHandler.AddEventAsync(o);
            }
            await dBHandler.context.Database.CloseConnectionAsync();
        });
    }
    Task.WaitAll(tasks);
    Console.WriteLine(file.Name + " : " + count + " : " + stopwatch.Elapsed.Minutes + ":" + stopwatch.Elapsed.Seconds);
}

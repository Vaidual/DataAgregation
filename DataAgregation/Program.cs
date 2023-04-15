// See https://aka.ms/new-console-template for more information

using DataAgregation.ClusterModels;
using DataAgregation.DataManipulationModels;
using DataAgregation.Models;
using DataAgregation.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using static System.Runtime.InteropServices.JavaScript.JSType;

const string dataDirPath = "../../../data";
const string excelFilePath = "../../../events_data.xlsx";
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

DBHandler dBHandler = new DBHandler();
//var dataDirInfo = new DirectoryInfo(dataDirPath).GetFiles();
//foreach (FileInfo file in dataDirInfo)
//{
//    await dBHandler.AddEventsAsync(file);
//}


using (ExcelHandler excelHandler = new ExcelHandler(excelFilePath))
{
    //await excelHandler.WriteDAUAsync();
    //await excelHandler.WriteNewUsersAsync();
    //await excelHandler.WriteMAUAsync();
    //await excelHandler.WriteRevenueAsync();
    //await excelHandler.WriteCurrencyRate();
    //await excelHandler.WriteStageStatistic();
    //await excelHandler.WriteItemsStatistic();
    //await excelHandler.WriteItemsByDateStatistic();
}

using (ExcelHandler excelHandler = new ExcelHandler("../../../age-clusters.xlsx"))
{
    var ages = await dBHandler.GetUserAges();
    ClasterMaker clasterMaker = new ClasterMaker();
    var intervals = clasterMaker.FindAgeIntervals(ages);

    string[] intervalHeaders = new string[4];
    for (int i = 0; i < intervals.Count(); i++)
    {
        intervalHeaders[i] = $"{intervals.ElementAt(i).MinValue}-{intervals.ElementAt(i).MaxValue}";
    }
    //excelHandler.WriteInExcel(
    //    "DAU",
    //    new string[] { "ItemName" + intervalHeaders },
    //    await dBHandler.GetAgeStatisticByEventTypeAsync(1, intervals));
    //excelHandler.WriteInExcel(
    //    "New Users",
    //    new string[] { "ItemName" + intervalHeaders },
    //    await dBHandler.GetAgeStatisticByEventTypeAsync(2, intervals));
    //excelHandler.WriteInExcel(
    //    "Revenue",
    //    new string[] { "ItemName" + intervalHeaders },
    //    await dBHandler.GetRevenuebyAgeAsync2(intervals));
    //excelHandler.WriteInExcel(
    //    "MAU",
    //    intervalHeaders,
    //    await dBHandler.GetMauByAgeAsync(intervals));
    excelHandler.WriteInExcel(
        "Items Statistic",
        new string[][]
        {
            new string[] { "Item"},
            new string[] { "Amount" }.Concat(intervalHeaders).ToArray(),
            new string[] { "Income" }.Concat(intervalHeaders).ToArray(),
            new string[] { "USD" }.Concat(intervalHeaders).ToArray(),

        },
        await dBHandler.GetItemsStatisticByAge(intervals));

    //var items = await dBHandler.GetItemsStatisticWithAges();
    //var enterances = new List<EntrancesInTheInterval>();
    //var result = new List<ItemStatisticWithIntervals>();
    //foreach (var item in items) 
    //{
    //    decimal[] prop = new decimal[intervals.Count()];
    //    for (int i = 0; i < intervals.Count(); i++)
    //    {
    //        var hoeMany = (decimal)item.Values.Count(e => e >= intervals.ElementAt(i).MinValue && e <= intervals.ElementAt(i).MaxValue);
    //        prop[i] = hoeMany / item.Values.Count();
    //    }
    //    result.Add(new ItemStatisticWithIntervals
    //    {
    //        ItemName = item.Item,
    //        USD = item.USD,
    //        Amount = prop.Select(x => (int)(x * item.Amount)).ToList(),
    //        Income = prop.Select(x => (int)(x * item.Income)).ToList()
    //    });
    //}
    //excelHandler.WriteInExcel(
    //"Items Statistic",
    //new string[][]
    //{
    //        new string[] { "Item"},
    //        new string[] { "Amount" }.Concat(intervalHeaders).ToArray(),
    //        new string[] { "Income" }.Concat(intervalHeaders).ToArray(),
    //        new string[] { "USD"},

    //},
    //result);
}

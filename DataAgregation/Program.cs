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
const string excelFilePath = "../../../output/events_data.xlsx";
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

DBHandler dBHandler = new DBHandler();
//var dataDirInfo = new DirectoryInfo(dataDirPath).GetFiles();
//foreach (FileInfo file in dataDirInfo)
//{
//    await dBHandler.AddEventsAsync(file);
//}


//using (ExcelHandler excelHandler = new ExcelHandler(excelFilePath))
//{
//    await excelHandler.WriteDAUAsync();
//    await excelHandler.WriteNewUsersAsync();
//    await excelHandler.WriteMAUAsync();
//    await excelHandler.WriteRevenueAsync();
//    await excelHandler.WriteCurrencyRate();
//    await excelHandler.WriteStageStatistic();
//    await excelHandler.WriteItemsStatistic();
//    await excelHandler.WriteItemsByDateStatistic();
//}

//using (ExcelHandler excelHandler = new ExcelHandler("../../../output/clusters/age-clusters.xlsx"))
//{
//    var ages = await AgeService.GetUserAges();
//    ClasterMaker clasterMaker = new ClasterMaker();
//    var intervals = clasterMaker.FindIntervals(ages, 4);

//    string[] intervalHeaders = new string[4];
//    for (int i = 0; i < intervals.Count(); i++)
//    {
//        intervalHeaders[i] = $"{intervals.ElementAt(i).MinValue}-{intervals.ElementAt(i).MaxValue}";
//    }
//    excelHandler.WriteInExcel(
//        "DAU",
//        intervalHeaders.Prepend("ItemName").ToArray(),
//        await AgeService.GetAgeStatisticByEventTypeAsync(1, intervals));
//    excelHandler.WriteInExcel(
//        "New Users",
//        intervalHeaders.Prepend("ItemName").ToArray(),
//        await AgeService.GetAgeStatisticByEventTypeAsync(2, intervals));
//    excelHandler.WriteInExcel(
//        "Revenue",
//        intervalHeaders.Prepend("ItemName").ToArray(),
//        await AgeService.GetRevenuebyAgeAsync(intervals));
//    excelHandler.WriteInExcel(
//        "MAU",
//        intervalHeaders,
//        await AgeService.GetMauByAgeAsync(intervals));
//    excelHandler.WriteInExcel(
//        "Items Statistic",
//        new string[][]
//        {
//            new string[] { "Item"},
//            intervalHeaders.Prepend("Amount").ToArray(),
//            intervalHeaders.Prepend("Income").ToArray(),
//            intervalHeaders.Prepend("USD").ToArray(),
//        },
//        await AgeService.GetItemsStatisticByAge(intervals));
//    excelHandler.WriteInExcel(
//        "Stages Statistic",
//        new string[][]
//        {
//            new string[] { "Stage"},
//            intervalHeaders.Prepend("Starts").ToArray(),
//            intervalHeaders.Prepend("Ends").ToArray(),
//            intervalHeaders.Prepend("Wins").ToArray(),
//            intervalHeaders.Prepend("Income").ToArray(),
//            intervalHeaders.Prepend("USD").ToArray(),

//        },
//        await AgeService.GetStageStatisticByAgeAsync(intervals));
//}

//using (ExcelHandler excelHandler = new ExcelHandler("../../../output/clusters/gender-clusters.xlsx"))
//{
//    var genderTypes = await GenderService.GetGenderTypes();
//    GenderService.SortedDistinctGenders = genderTypes;
//    excelHandler.WriteInExcel(
//        "DAU",
//        genderTypes.Prepend("Date").ToArray(),
//        await GenderService.GetGenderStatisticByEventTypeAsync(1));

//    excelHandler.WriteInExcel(
//        "New Users",
//        genderTypes.Prepend("Date").ToArray(),
//        await GenderService.GetGenderStatisticByEventTypeAsync(2));

//    excelHandler.WriteInExcel(
//        "Revenue",
//        genderTypes.Prepend("ItemName").ToArray(),
//        await GenderService.GetRevenuebyGenderAsync());

//    excelHandler.WriteInExcel(
//        "MAU",
//        genderTypes,
//        await GenderService.GetMauByGenderAsync());
//    excelHandler.WriteInExcel(
//        "Items Statistic",
//        new string[][]
//        {
//                new string[] { "Item"},
//                genderTypes.Prepend("Amount").ToArray(),
//                genderTypes.Prepend("Income").ToArray(),
//                genderTypes.Prepend("USD").ToArray(),
//        },
//        await GenderService.GetItemsStatisticByGender());
//    excelHandler.WriteInExcel(
//        "Stages Statistic",
//        new string[][]
//        {
//                new string[] { "Stage"},
//                genderTypes.Prepend("Starts").ToArray(),
//                genderTypes.Prepend("Ends").ToArray(),
//                genderTypes.Prepend("Wins").ToArray(),
//                genderTypes.Prepend("Income").ToArray(),
//                genderTypes.Prepend("USD").ToArray(),

//        },
//        await GenderService.GetStageStatisticByGenderAsync());
//}

//using (ExcelHandler excelHandler = new ExcelHandler("../../../output/clusters/country-clusters.xlsx"))
//{
//    var countryTypes = await CountryService.GetCountryTypes();
//    CountryService.SortedDistinctCountries = countryTypes;
//    excelHandler.WriteInExcel(
//        "DAU",
//        countryTypes.Prepend("Date").ToArray(),
//        await CountryService.GetCountryStatisticByEventTypeAsync(1));

//    excelHandler.WriteInExcel(
//        "New Users",
//        countryTypes.Prepend("Date").ToArray(),
//        await CountryService.GetCountryStatisticByEventTypeAsync(2));

//    excelHandler.WriteInExcel(
//        "Revenue",
//        countryTypes.Prepend("ItemName").ToArray(),
//        await CountryService.GetRevenuebyCountryAsync());

//    excelHandler.WriteInExcel(
//        "MAU",
//        countryTypes,
//        await CountryService.GetMauByCountryAsync());
//    excelHandler.WriteInExcel(
//        "Items Statistic",
//        new string[][]
//        {
//                new string[] { "Item"},
//                countryTypes.Prepend("Amount").ToArray(),
//                countryTypes.Prepend("Income").ToArray(),
//                countryTypes.Prepend("USD").ToArray(),
//        },
//        await CountryService.GetItemsStatisticByCountry());
//    excelHandler.WriteInExcel(
//        "Stages Statistic",
//        new string[][]
//        {
//                new string[] { "Stage"},
//                countryTypes.Prepend("Starts").ToArray(),
//                countryTypes.Prepend("Ends").ToArray(),
//                countryTypes.Prepend("Wins").ToArray(),
//                countryTypes.Prepend("Income").ToArray(),
//                countryTypes.Prepend("USD").ToArray(),

//        },
//        await CountryService.GetStageStatisticByCountryAsync());

//}

//using (ExcelHandler excelHandler = new ExcelHandler("../../../output/clusters/cheaters.xlsx"))
//{
//    var headers = new string[] { "Noncheaters", "Cheaters" };
//    excelHandler.WriteInExcel(
//        "DAU",
//        headers.Prepend("Date").ToArray(),
//        await CheaterService.GetCheatersStatisticByEventTypeAsync(1));

//    excelHandler.WriteInExcel(
//        "New Users",
//        headers.Prepend("Date").ToArray(),
//        await CheaterService.GetCheatersStatisticByEventTypeAsync(2));

//    excelHandler.WriteInExcel(
//        "Revenue",
//        headers.Prepend("ItemName").ToArray(),
//        await CheaterService.GetRevenuebyCheatingAsync());

//    excelHandler.WriteInExcel(
//        "MAU",
//        headers,
//        await CheaterService.GetMauByCheatingAsync());
//    excelHandler.WriteInExcel(
//        "Items Statistic",
//        new string[][]
//        {
//                new string[] { "Item"},
//                headers.Prepend("Amount").ToArray(),
//                headers.Prepend("Income").ToArray(),
//                headers.Prepend("USD").ToArray(),
//        },
//        await CheaterService.GetItemsStatisticByCheating());
//    excelHandler.WriteInExcel(
//        "Stages Statistic",
//        new string[][]
//        {
//                new string[] { "Stage"},
//                headers.Prepend("Starts").ToArray(),
//                headers.Prepend("Ends").ToArray(),
//                headers.Prepend("Wins").ToArray(),
//                headers.Prepend("Income").ToArray(),
//                headers.Prepend("USD").ToArray(),

//        },
//        await CheaterService.GetStageStatisticByCheatingAsync());
//}

using (ExcelHandler excelHandler = new ExcelHandler("../../../output/clusters/lastDayIncome.xlsx"))
{
    LastDayIncomeService.LastDate = await DBHandler.GetDateOfLastCurrencyPurchaseAsync();
    var income = await LastDayIncomeService.GetIncomClusterInputs();
    ClasterMaker clasterMaker = new ClasterMaker();
    var intervals = clasterMaker.FindIntervals(income, 3);

    string[] intervalHeaders = new string[4];
    intervalHeaders[0] = "Tier0";
    for (int i = 1; i < intervalHeaders.Count(); i++)
    {
        if (intervals.ElementAt(i - 1).MinValue == intervals.ElementAt(i - 1).MaxValue)
        {
            intervalHeaders[i] = $"Tier{i}({intervals.ElementAt(i - 1).MinValue})";
        }
        else
        {
            intervalHeaders[i] = $"Tier{i}({intervals.ElementAt(i - 1).MinValue}-{intervals.ElementAt(i - 1).MaxValue})";
        }
    }

    LastDayIncomeService.Intervals = intervals.Prepend(new Interval { MinValue = 0, MaxValue = 0 }).ToArray();

    //excelHandler.WriteInExcel(
    //    "Revenue",
    //    intervalHeaders.Prepend("ItemName").ToArray(),
    //    await LastDayIncomeService.GetRevenuebyIncomeAsync());
    //excelHandler.WriteInExcel(
    //    "DAU",
    //    intervalHeaders.Prepend("ItemName").ToArray(),
    //    await LastDayIncomeService.GetIncomeStatisticByEventTypeAsync(1));
    //excelHandler.WriteInExcel(
    //    "New Users",
    //    intervalHeaders.Prepend("ItemName").ToArray(),
    //    await LastDayIncomeService.GetIncomeStatisticByEventTypeAsync(2));
    //excelHandler.WriteInExcel(
    //    "MAU",
    //    intervalHeaders,
    //    await LastDayIncomeService.GetMauByIncomeAsync());
    //excelHandler.WriteInExcel(
    //    "Items Statistic",
    //    new string[][]
    //    {
    //        new string[] { "Item"},
    //        intervalHeaders.Prepend("Amount").ToArray(),
    //        intervalHeaders.Prepend("Income").ToArray(),
    //        intervalHeaders.Prepend("USD").ToArray(),
    //    },
    //    await LastDayIncomeService.GetItemsStatisticByIncome());
    excelHandler.WriteInExcel(
        "Stages Statistic",
        new string[][]
        {
            new string[] { "Stage"},
            intervalHeaders.Prepend("Starts").ToArray(),
            intervalHeaders.Prepend("Ends").ToArray(),
            intervalHeaders.Prepend("Wins").ToArray(),
            intervalHeaders.Prepend("Income").ToArray(),
            intervalHeaders.Prepend("USD").ToArray(),

        },
        await LastDayIncomeService.GetStageStatisticByIncomeAsync());
}
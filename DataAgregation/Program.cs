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
//    var ages = await dBHandler.GetUserAges();
//    ClasterMaker clasterMaker = new ClasterMaker();
//    var intervals = clasterMaker.FindAgeIntervals(ages);

//    string[] intervalHeaders = new string[4];
//    for (int i = 0; i < intervals.Count(); i++)
//    {
//        intervalHeaders[i] = $"{intervals.ElementAt(i).MinValue}-{intervals.ElementAt(i).MaxValue}";
//    }
//    excelHandler.WriteInExcel(
//        "DAU",
//        intervalHeaders.Prepend("ItemName").ToArray(),
//        await dBHandler.GetAgeStatisticByEventTypeAsync(1, intervals));
//    excelHandler.WriteInExcel(
//        "New Users",
//        intervalHeaders.Prepend("ItemName").ToArray(),
//        await dBHandler.GetAgeStatisticByEventTypeAsync(2, intervals));
//    excelHandler.WriteInExcel(
//        "Revenue",
//        intervalHeaders.Prepend("ItemName").ToArray(),
//        await dBHandler.GetRevenuebyAgeAsync2(intervals));
//    excelHandler.WriteInExcel(
//        "MAU",
//        intervalHeaders,
//        await dBHandler.GetMauByAgeAsync(intervals));
//    excelHandler.WriteInExcel(
//        "Items Statistic",
//        new string[][]
//        {
//            new string[] { "Item"},
//            intervalHeaders.Prepend("Amount").ToArray(),
//            intervalHeaders.Prepend("Income").ToArray(),
//            intervalHeaders.Prepend("USD").ToArray(),
//        },
//        await dBHandler.GetItemsStatisticByAge(intervals));
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
//        await dBHandler.GetStageStatisticByAgeAsync(intervals));
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

using (ExcelHandler excelHandler = new ExcelHandler("../../../output/clusters/country-clusters.xlsx"))
{
    var countryTypes = await CountryService.GetCountryTypes();
    CountryService.SortedDistinctCountries = countryTypes;
    excelHandler.WriteInExcel(
        "DAU",
        countryTypes.Prepend("Date").ToArray(),
        await CountryService.GetCountryStatisticByEventTypeAsync(1));

    excelHandler.WriteInExcel(
        "New Users",
        countryTypes.Prepend("Date").ToArray(),
        await CountryService.GetCountryStatisticByEventTypeAsync(2));

    excelHandler.WriteInExcel(
        "Revenue",
        countryTypes.Prepend("ItemName").ToArray(),
        await CountryService.GetRevenuebyCountryAsync());

    excelHandler.WriteInExcel(
        "MAU",
        countryTypes,
        await CountryService.GetMauByCountryAsync());
    excelHandler.WriteInExcel(
        "Items Statistic",
        new string[][]
        {
                new string[] { "Item"},
                countryTypes.Prepend("Amount").ToArray(),
                countryTypes.Prepend("Income").ToArray(),
                countryTypes.Prepend("USD").ToArray(),
        },
        await CountryService.GetItemsStatisticByCountry());
    excelHandler.WriteInExcel(
        "Stages Statistic",
        new string[][]
        {
                new string[] { "Stage"},
                countryTypes.Prepend("Starts").ToArray(),
                countryTypes.Prepend("Ends").ToArray(),
                countryTypes.Prepend("Wins").ToArray(),
                countryTypes.Prepend("Income").ToArray(),
                countryTypes.Prepend("USD").ToArray(),

        },
        await CountryService.GetStageStatisticByCountryAsync());

}
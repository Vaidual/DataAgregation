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
const string clustersFilePath = "../../../clusters_data.xlsx";
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


using (ExcelHandler excelHandler = new ExcelHandler(clustersFilePath))
{
    var ages = await dBHandler.GetUserAges();
    ClasterMaker clasterMaker = new ClasterMaker();
    var model = clasterMaker.CreateModel(ages, 4);
    var intervals = clasterMaker.FindIntervals(ages, model);

    string[] colunsHeaders = new string[5];
    colunsHeaders[0] = "Date";
    for (int i = 0; i < intervals.Count(); i++)
    {
        colunsHeaders[i + 1] = $"{intervals.ElementAt(i).MinAge}-{intervals.ElementAt(i).MaxAge}";
    }

    excelHandler.WriteInExcel(
        "DAU",
        colunsHeaders,
        await dBHandler.GetDateIntervalEntersByEventTypeAsync(1, intervals));
    excelHandler.WriteInExcel(
        "New Users",
        colunsHeaders,
        await dBHandler.GetDateIntervalEntersByEventTypeAsync(2, intervals));
    excelHandler.WriteInExcel(
        "MAU",
        colunsHeaders.Skip(1),
        await dBHandler.GetDateIntervalMauAsync(intervals));
}

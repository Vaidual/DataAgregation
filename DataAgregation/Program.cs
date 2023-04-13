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

//var dataDirInfo = new DirectoryInfo(dataDirPath).GetFiles();

//DBHandler dBHandler = new DBHandler();
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

DBHandler dBHandler = new DBHandler();
using (ExcelHandler excelHandler = new ExcelHandler(clustersFilePath))
{
    await excelHandler.WriteDAUWithClusters2();
}

// See https://aka.ms/new-console-template for more information

using DataAgregation;
using DataAgregation.DataManipulationModels;
using DataAgregation.Models;
using Microsoft.EntityFrameworkCore;
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

const string dataDirPath = "../../../data";
const string excelFilePath = "../../../events_data.xlsx";

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

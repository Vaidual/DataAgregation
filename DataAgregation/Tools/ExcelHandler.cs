using DataAgregation.ClusterModels;
using DataAgregation.DataManipulationModels;
using Microsoft.ML.Data;
using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataAgregation.Tools
{
    public class ExcelHandler : IDisposable
    {
        ExcelPackage package;
        DBHandler dBHandler = new DBHandler();
        public ExcelHandler(string path)
        {
            package = new ExcelPackage(path);
        }

        internal async Task WriteDAUWithClusters()
        {
            var usersData = await dBHandler.GetUserAgeData();
            ClasterMaker clasterMaker = new ClasterMaker();
            var intervals = clasterMaker.CreateClastersByAge2(usersData);
            string[] colunsHeaders = new string[5];
            colunsHeaders[0] = "Date";
            for (int i = 0; i < intervals.Length; i++)
            {
                colunsHeaders[i + 1] = $"{intervals[i].MinAge}-{intervals[i].MaxAge}";
            }
            var data = await dBHandler.GetDateUsersClustersByEventTypeAsync(1);
            WriteInExcel(
                "DAU",
                colunsHeaders,
                data);
        }

        internal async Task WriteDAUWithClusters2()
        {
            var dateAges = await dBHandler.GetDateAgesByEventTypeAsync(1);
            ClasterMaker clasterMaker = new ClasterMaker();
            var clusterInput = dateAges.SelectMany(da => da.Ages).Select(a => new ClusterInput(a));
            var model = clasterMaker.CreateModel(clusterInput, 4);

            var intervals = clasterMaker.FindIntervals(clusterInput, model);
            string[] colunsHeaders = new string[5];
            colunsHeaders[0] = "Date";
            for (int i = 0; i < intervals.Length; i++)
            {
                colunsHeaders[i + 1] = $"{intervals[0].MinAge}-{intervals[0].MaxAge}";
            }

            List<DateAgeEnters> result = new();
            foreach (var date in dateAges)
            {
                result.Add(new DateAgeEnters 
                { 
                    Date = date.Date, 
                    Ages = clasterMaker.GetIntervalsEnters(clusterInput, model) 
                });
            }
            var data = await dBHandler.GetDateUsersClustersByEventTypeAsync(1);
            WriteInExcelWithNestedLists(
                "DAU",
                colunsHeaders,
                result);
        }

        internal async Task WriteDAUAsync()
        {
            var DAU = await dBHandler.GetDateUsersCountByEventTypeAsync(1);
            WriteInExcel(
                "DAU",
                new string[] { "Date", "Active Users" },
                DAU);
        }

        internal async Task WriteNewUsersAsync()
        {
            var newUsers = await dBHandler.GetDateUsersCountByEventTypeAsync(2);
            WriteInExcel(
                "New Users",
                new string[] { "Date", "New Users" },
                newUsers);
        }

        internal async Task WriteMAUAsync()
        {
            int MAU = await dBHandler.GetLastMonthUsersCountAsync();
            WriteInExcel(
                "MAU",
                "Active Users",
                MAU.ToString());
        }

        internal async Task WriteRevenueAsync()
        {
            var revenues = await dBHandler.GetRevenueAsync();
            WriteInExcel(
                "Revenue",
                new string[] { "Date", "Revenue" },
                revenues);
        }

        internal async Task WriteCurrencyRate()
        {
            var currencyRate = await dBHandler.GetCurrencyRateAsync();
            WriteInExcel(
                "Currency Rate",
                new string[] { "Date", "Rate" },
                currencyRate);
        }

        internal async Task WriteStageStatistic()
        {
            var stageStatistic = await dBHandler.GetStageStatisticAsync();
            WriteInExcel(
                "Stage Info",
                new string[] { "Stage", "Starts", "Ends", "Wins", "Income", "USD" },
                stageStatistic);
        }

        internal async Task WriteItemsStatistic()
        {
            var itemsStatistic = await dBHandler.GetItemsStatistic();
            WriteInExcel(
                "Item Info",
                new string[] { "Item", "Amount", "Income", "USD" },
                itemsStatistic);
        }
        internal async Task WriteItemsByDateStatistic()
        {
            var itemsStatistic = await dBHandler.GetItemsByDateStatistic();
            WriteInExcel(
                "Items by date info",
                new string[] { "Date", "Items sold", "Income" },
                itemsStatistic);
        }

        private void WriteInExcel<T>(string sheetName, string[] columnNames, List<T> data)
        {
            var sheet = CreateUniqueSheet(sheetName);

            for (int i = 0; i < columnNames.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = columnNames[i];
            }
            sheet.Cells[2, 1].LoadFromCollection(data);
            package.Save();
        }

        private void WriteInExcelWithNestedLists<T>(string sheetName, string[] columnNames, List<T> data)
        {
            var sheet = CreateUniqueSheet(sheetName);

            for (int i = 0; i < columnNames.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = columnNames[i];
            }

            for (int i = 0; i < data.Count; i++)
            {
                T obj = data[i];
                int cell = 1;
                for (int j = 0; j < obj.GetType().GetProperties().Length; j++)
                {
                    var prop = obj.GetType().GetProperties()[j];
                    var type = prop.GetType();
                    if (!typeof(IEnumerable<>).IsAssignableFrom(type))
                    {
                        sheet.Cells[i + 2, cell].Value = prop.GetValue(obj);
                        cell++;
                    }
                    else
                    {
                        var value = prop.GetValue(obj) as IEnumerable<object>;
                        sheet.Cells[i + 2, cell].LoadFromCollection(value);
                        cell += value.Count();
                    }
                }
            }
            package.Save();
        }

        private void WriteInExcel(string sheetName, string columnName, string value)
        {
            var sheet = CreateUniqueSheet(sheetName);
            sheet.Cells[1, 1].Value = columnName;
            sheet.Cells[2, 1].Value = value;
            package.Save();
        }

        private ExcelWorksheet CreateUniqueSheet(string sheetName)
        {
            if (package.Workbook.Worksheets.Any(s => s.Name == sheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[sheetName];
                package.Workbook.Worksheets.Delete(sheetName);
            }
            return package.Workbook.Worksheets.Add(sheetName);
        }

        public void Dispose()
        {
            package.Dispose();
        }
    }
}

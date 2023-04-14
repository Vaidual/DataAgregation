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

        internal async Task WriteDAUWithAgeClusters(IEnumerable<AgeInterval> intervals)
        {
            string[] colunsHeaders = new string[5];
            colunsHeaders[0] = "Date";
            for (int i = 0; i < intervals.Count(); i++)
            {
                colunsHeaders[i + 1] = $"{intervals.ElementAt(i).MinAge}-{intervals.ElementAt(i).MaxAge}";
            }
            var data = await dBHandler.GetAgeStatisticByEventTypeAsync(1, intervals);
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
            for (int i = 0; i < intervals.Count(); i++)
            {
                colunsHeaders[i + 1] = $"{intervals.ElementAt(i).MinAge}-{intervals.ElementAt(i).MaxAge}";
            }

            Dictionary<DateOnly, IEnumerable<int>> result = new();
            foreach (var date in dateAges)
            {
                result.Add(
                    date.Date,
                    clasterMaker.GetIntervalsEnters(date.Ages.Select(a => new ClusterInput(a)), model));
            }
            WriteInExcel(
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

        //public void WriteInExcel<T>(string sheetName, IEnumerable<string> columnNames, List<T> data)
        //{
        //    var sheet = CreateUniqueSheet(sheetName);

        //    for (int i = 0; i < columnNames.Count(); i++)
        //    {
        //        sheet.Cells[1, i + 1].Value = columnNames.ElementAt(i);
        //    }
        //    sheet.Cells[2, 1].LoadFromCollection(data);
        //    package.Save();
        //}

        private void WriteInExcel<KeyT, ListT>(string sheetName, string[] columnNames, Dictionary<KeyT, IEnumerable<ListT>> data)
        {
            var sheet = CreateUniqueSheet(sheetName);

            for (int i = 0; i < columnNames.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = columnNames[i];
            }

            for (int i = 0; i < data.Count; i++)
            {
                sheet.Cells[i + 2, 1].Value = data.Keys.ElementAt(i);
                for (int j = 0; j < data.First().Value.Count(); j++)
                {
                    sheet.Cells[i + 2, j + 2].Value = data.Values.ElementAt(i).ElementAt(j);
                }
            }
            package.Save();
        }

        public void WriteInExcel(string sheetName, string columnName, string value)
        {
            var sheet = CreateUniqueSheet(sheetName);
            sheet.Cells[1, 1].Value = columnName;
            sheet.Cells[2, 1].Value = value;
            package.Save();
        }

        public void WriteInExcel(string sheetName, IEnumerable<string> columnNames, string value)
        {
            var sheet = CreateUniqueSheet(sheetName);
            for (int i = 0; i < columnNames.Count(); i++)
            {
                sheet.Cells[1, i + 1].Value = columnNames.ElementAt(i);
            }
            sheet.Cells[2, 1].Value = value;
            package.Save();
        }

        public void WriteInExcel<T>(string sheetName, IEnumerable<string> columnNames, T value) where T : class
        {
            var sheet = CreateUniqueSheet(sheetName);
            for (int i = 0; i < columnNames.Count(); i++)
            {
                sheet.Cells[1, i + 1].Value = columnNames.ElementAt(i);
            }
            sheet.Cells[2, 1].LoadFromCollection(new List<T>{ value });
            package.Save();
        }

        public void WriteInExcel<T>(string sheetName, IEnumerable<string> columnNames, List<T> data)
        {
            var sheet = CreateUniqueSheet(sheetName);
            for (int i = 0; i < columnNames.Count(); i++)
            {
                sheet.Cells[1, i + 1].Value = columnNames.ElementAt(i);
            }
            for (int i = 0; i < data.Count(); i++)
            {
                var obj = data[i];
                int col = 1;
                for (int j = 0; j < obj.GetType().GetProperties().Count(); j++)
                {
                    var prop = obj.GetType().GetProperties()[j];
                    var value = prop.GetValue(obj);
                    var type = prop.PropertyType;
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        var list = value as IEnumerable;
                        foreach (var item in list)
                        {
                            if (!item.GetType().IsValueType)
                            {
                                throw new Exception($"Type '{item.GetType().Name}' is not a valid type for printing");
                            }
                            sheet.Cells[i + 2, col].Value = item;
                            col++;
                        }
                    }
                    else if (type.IsValueType)
                    {
                        sheet.Cells[i + 2, col].Value = value;
                        col++;
                    }
                    else if (type.IsClass)
                    {
                        sheet.Cells[i + 2, col].LoadFromCollection(new List<object> { value });
                        col += type.GetProperties().Count();
                    }
                    else
                    {
                        throw new Exception($"Type '{type.Name}' is not a valid type for printing");
                    }
                }
            }
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

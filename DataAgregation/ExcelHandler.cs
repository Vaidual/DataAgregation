using DataAgregation.DataManipulationModels;
using Microsoft.ML.Data;
using OfficeOpenXml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace DataAgregation
{
    public class ExcelHandler : IDisposable
    {
        ExcelPackage package;
        DBHandler dBHandler = new DBHandler();
        public ExcelHandler(string path)
        { 
            package = new ExcelPackage(path);
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
                new string[] { "Active Users" },
                new List<int>(MAU));
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
                new string[] { "Date", "Rate"},
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
            if (package.Workbook.Worksheets.Any(s => s.Name == sheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[sheetName];
                package.Workbook.Worksheets.Delete(sheetName);
            }
            var sheet = package.Workbook.Worksheets.Add(sheetName);

            for (int i = 0; i < columnNames.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = columnNames[i];
            }
            sheet.Cells[2, 1].LoadFromCollection(data);
            package.Save();
        }

        public void Dispose()
        {
            this.package.Dispose();
        }
    }
}

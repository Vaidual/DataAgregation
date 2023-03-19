using DataAgregation.DataManipulationModels;
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
    public class ExcelHandler
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
            string worksheetName = "DAU";
            ExcelWorksheet sheet;
            if (package.Workbook.Worksheets.Any(s => s.Name == worksheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[worksheetName];
                package.Workbook.Worksheets.Delete(worksheetName);
            }
            sheet = package.Workbook.Worksheets.Add(worksheetName);

            sheet.Cells[1, 1].Value = "Date";
            sheet.Cells[1, 2].Value = "Active Users";
            sheet.Cells[2, 1].LoadFromCollection(DAU);
            package.Save();
        }

        internal async Task WriteNewUsersAsync()
        {
            var newUsers = await dBHandler.GetDateUsersCountByEventTypeAsync(2);
            string worksheetName = "New Users";
            if (package.Workbook.Worksheets.Any(s => s.Name == worksheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[worksheetName];
                package.Workbook.Worksheets.Delete(worksheetName);
            }
            var sheet = package.Workbook.Worksheets.Add(worksheetName);

            sheet.Cells[1, 1].Value = "Date";
            sheet.Cells[1, 2].Value = "New Users";
            sheet.Cells[2, 1].LoadFromCollection(newUsers);
            package.Save();
        }

        internal async Task WriteMAUAsync()
        {
            int MAU = await dBHandler.GetLastMonthUsersCountAsync();
            string worksheetName = "MAU";
            if (package.Workbook.Worksheets.Any(s => s.Name == worksheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[worksheetName];
                package.Workbook.Worksheets.Delete(worksheetName);
            }
            var sheet = package.Workbook.Worksheets.Add(worksheetName);

            sheet.Cells[1, 1].Value = "Active Users";
            sheet.Cells[2, 1].Value = MAU;
            package.Save();
        }

        internal async Task WriteRevenueAsync()
        {
            var revenues = await dBHandler.GetRevenueAsync();
            string worksheetName = "Revenue";
            if (package.Workbook.Worksheets.Any(s => s.Name == worksheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[worksheetName];
                package.Workbook.Worksheets.Delete(worksheetName);
            }
            var sheet = package.Workbook.Worksheets.Add(worksheetName);

            sheet.Cells[1, 1].Value = "Date";
            sheet.Cells[1, 2].Value = "Revenue";
            sheet.Cells[2, 1].LoadFromCollection(revenues);
            package.Save();
        }

        internal async Task WriteCurrencyRate()
        {
            var currencyRate = await dBHandler.GetCurrencyRateAsync();
            string worksheetName = "Currency Rate";
            if (package.Workbook.Worksheets.Any(s => s.Name == worksheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[worksheetName];
                package.Workbook.Worksheets.Delete(worksheetName);
            }
            var sheet = package.Workbook.Worksheets.Add(worksheetName);

            sheet.Cells[1, 1].Value = "Date";
            sheet.Cells[1, 2].Value = "Rate";
            sheet.Cells[2, 1].LoadFromCollection(currencyRate);
            package.Save();
            return;
        }

        internal async Task WriteStageStatistic()
        {
            var stageStatistic = await dBHandler.GetStageStatisticAsync();
            string worksheetName = "Stage Info";
            if (package.Workbook.Worksheets.Any(s => s.Name == worksheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[worksheetName];
                package.Workbook.Worksheets.Delete(worksheetName);
            }
            var sheet = package.Workbook.Worksheets.Add(worksheetName);

            sheet.Cells[1, 1].Value = "Stage";
            sheet.Cells[1, 2].Value = "Starts";
            sheet.Cells[1, 3].Value = "Ends";
            sheet.Cells[1, 4].Value = "Wins";
            sheet.Cells[1, 5].Value = "Income";
            sheet.Cells[1, 6].Value = "USD";
            sheet.Cells[2, 1].LoadFromCollection(stageStatistic);
            package.Save();
        }

        internal async Task WriteItemsStatistic()
        {
            var itemsStatistic = await dBHandler.GetItemsStatistic();
            string worksheetName = "Item Info";
            if (package.Workbook.Worksheets.Any(s => s.Name == worksheetName))
            {
                var existingWorksheet = package.Workbook.Worksheets[worksheetName];
                package.Workbook.Worksheets.Delete(worksheetName);
            }
            var sheet = package.Workbook.Worksheets.Add(worksheetName);

            sheet.Cells[1, 1].Value = "Item";
            sheet.Cells[1, 2].Value = "Amount";
            sheet.Cells[1, 3].Value = "Income";
            sheet.Cells[1, 4].Value = "USD";
            sheet.Cells[2, 1].LoadFromCollection(itemsStatistic);
            package.Save();
        }
    }
}

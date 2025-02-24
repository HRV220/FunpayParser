using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
using System.IO;
using System.IO.Compression;
using word = Microsoft.Office.Interop.Word;
using excel = Microsoft.Office.Interop.Excel;

namespace WebApplication2.Controllers
{
    public class ReportsController : Controller
    {
        private readonly Entities _context;
        public ReportsController(Entities context) 
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult DownloadReportsArchive()
        {
            var wordReportPath = Path.Combine("C:\\Users\\Роман\\source\\repos\\WebApplication2\\WebApplication2\\Reports\\ExcelReports\\", "ExcelReport.xlsx");
            var excelReportPath = Path.Combine("C:\\Users\\Роман\\source\\repos\\WebApplication2\\WebApplication2\\Reports\\WordReports\\", "WordReport.docx");

            // Проверка существования файлов
            if (!System.IO.File.Exists(wordReportPath) || !System.IO.File.Exists(excelReportPath))
            {
                return NotFound(); // Если хотя бы один файл не найден, возвращаем ошибку 404
            }

            // Создание имени архива
            var archiveName = "ReportsArchive.zip";
            var archivePath = Path.Combine("C:\\Users\\Роман\\source\\repos\\WebApplication2\\WebApplication2\\Reports\\", archiveName);

            // Создание архива
            using (var zipArchive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                // Добавляем Word отчет в архив
                zipArchive.CreateEntryFromFile(wordReportPath, "WordReport.docx");

                // Добавляем Excel отчет в архив
                zipArchive.CreateEntryFromFile(excelReportPath, "ExcelReport.xlsx");
            }

            // Чтение архива в байты
            var fileBytes = System.IO.File.ReadAllBytes(archivePath);

            // Удаляем архив после того, как он был отправлен
            System.IO.File.Delete(archivePath);

            // Возвращаем архив в качестве ответа
            return File(fileBytes, "application/zip", archiveName);
        }

        public IActionResult GenerateReport()
        {
            // Создаем приложение Word
            word.Application wordApp = new word.Application();
            var document = wordApp.Documents.Add();

            // Добавляем заголовок
            var range = document.Range();
            range.Text = "Отчет о лотах и продавцах\n";
            range.InsertParagraphAfter();

            range = document.Content; // Обновляем текущий диапазон
            range.InsertAfter("Дата создания отчета: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "\n");
            range.InsertParagraphAfter();

            var mostActiveSeller = _context.Sellers
                .OrderByDescending(s => s.LotDetails.Count)
                .FirstOrDefault();

            var totalLots = _context.LotDetails.Count();
            var totalAmount = _context.LotDetails.Sum(l => l.Amount ?? 0);

            // Получаем данные из базы
            var mostExpensiveLot = _context.LotDetails
                .OrderByDescending(l => l.Price)
                .FirstOrDefault();

            var categoryCounts = _context.LotDetails
                .GroupBy(l => new { l.GamesCategory.GameName, l.GamesCategory.Category })
                .Select(g => new
                {
                    GameName = g.Key.GameName,
                    Category = g.Key.Category,
                    Count = g.Count() // Подсчет количества лотов в каждой группе
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            var sellersWithLotCounts = _context.Sellers
                .Select(s => new
                {
                    SellerName = s.Sellername,
                    LotCount = s.LotDetails.Count()
                })
                .OrderByDescending(s => s.LotCount)
                .Take(10) // Для графика возьмем только топ-10 продавцов
                .ToList();

            // Заполнение Word документа
            // Самый дорогой лот
            range = document.Content;
            range.InsertAfter("1. Самый дорогой лот:\n");
            range.InsertAfter($"Название лота: {mostExpensiveLot?.DescriptionLot ?? "Не найдено"}\n");
            range.InsertAfter($"Цена: {mostExpensiveLot?.Price ?? "Не указана"}\n\n");
            range.InsertParagraphAfter();

            // Заполнение информации о самом дорогом лоте
            range = document.Range();
            range.InsertAfter("2. Самый дорогой лот:");
            range.InsertParagraphAfter();
            range = document.Range();
            range.InsertAfter($"Название лота: {mostExpensiveLot?.DescriptionLot ?? "Не найдено"}\n" +
                         $"Цена: {mostExpensiveLot?.Price ?? "Не указана"}");
            range.InsertParagraphAfter();


            // Общая сумма всех лотов
            range = document.Range();
            range.InsertAfter("3. Общая сумма всех лотов:");
            range.InsertParagraphAfter();
            range = document.Range();
            range.InsertAfter( $"Общая сумма лотов: {totalAmount:C}");
            range.InsertParagraphAfter();

            // Количество лотов по категориям
            range.InsertAfter("4. Количество лотов по категориям:\n");
            foreach (var category in categoryCounts)
            {
                range.InsertAfter($"Категория: {category.GameName} {category.Category} — Количество лотов: {category.Count}\n");
            }
            range.InsertParagraphAfter();

            // Количество лотов у продавцов
            range.InsertAfter("5. Топ-10 продавцов по количеству лотов:\n");
            foreach (var seller in sellersWithLotCounts)
            {
                range.InsertAfter($"Продавец: {seller.SellerName} — Количество лотов: {seller.LotCount}\n");
            }
            range.InsertParagraphAfter();

            // Заключение
            range.InsertAfter("Это базовый отчет, который можно использовать для анализа лотов и продавцов на платформе.\n");
            range.InsertParagraphAfter();

            // Сохраняем Word документ
            var wordFilePath = Path.Combine("C:\\Users\\Роман\\source\\repos\\WebApplication2\\WebApplication2\\Reports\\WordReports\\", "WordReport.docx");
            document.SaveAs(wordFilePath);
            document.Close();
            wordApp.Quit();

            // Создание Excel документа
            var excelApp = new excel.Application();
            var workbook = excelApp.Workbooks.Add();
            var worksheet = (excel.Worksheet)workbook.Worksheets[1];

            // Таблица количества лотов по категориям
            worksheet.Cells[1, 1] = "Категория";
            worksheet.Cells[1, 2] = "Количество лотов";

            int row = 2;
            foreach (var category in categoryCounts)
            {
                worksheet.Cells[row, 1] = $"{category.GameName} {category.Category}";
                worksheet.Cells[row, 2] = category.Count;
                row++;
            }

            // Добавление графика для количества лотов по категориям
            var categoryChart = workbook.Charts.Add();
            categoryChart.ChartType = excel.XlChartType.xlColumnClustered;
            var categoryRange = worksheet.Range["A1", $"B{row - 1}"];
            categoryChart.SetSourceData(categoryRange);
            categoryChart.HasTitle = true;
            categoryChart.ChartTitle.Text = "Количество лотов по категориям";
            categoryChart.Location(excel.XlChartLocation.xlLocationAsNewSheet);

            // Таблица количества лотов у продавцов
            worksheet = (excel.Worksheet)workbook.Worksheets.Add();
            worksheet.Name = "Продавцы";

            worksheet.Cells[1, 1] = "Продавец";
            worksheet.Cells[1, 2] = "Количество лотов";

            row = 2;
            foreach (var seller in sellersWithLotCounts)
            {
                worksheet.Cells[row, 1] = seller.SellerName;
                worksheet.Cells[row, 2] = seller.LotCount;
                row++;
            }

            // Добавление графика для количества лотов у продавцов
            var sellerChart = workbook.Charts.Add();
            sellerChart.ChartType = excel.XlChartType.xlBarClustered;
            var sellerRange = worksheet.Range["A1", $"B{row - 1}"];
            sellerChart.SetSourceData(sellerRange);
            sellerChart.HasTitle = true;
            sellerChart.ChartTitle.Text = "Топ-10 продавцов по количеству лотов";
            sellerChart.Location(excel.XlChartLocation.xlLocationAsNewSheet);

            // Сохранение Excel документа
            var excelFilePath = Path.Combine("C:\\Users\\Роман\\source\\repos\\WebApplication2\\WebApplication2\\Reports\\ExcelReports\\", "ExcelReport.xlsx");
            workbook.SaveAs(excelFilePath);
            workbook.Close();
            excelApp.Quit();

            return RedirectToAction("Index", "FunpayParser");
        }
    }
}

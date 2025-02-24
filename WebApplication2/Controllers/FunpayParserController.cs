using AngleSharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using WebApplication2.Models;
using WebApplication2.Models.VM;

namespace WebApplication2.Controllers
{
    public class FunpayParserController : Controller
    {
        private readonly ILogger<FunpayParserController> _logger;
        private readonly Entities _context;

        private readonly IMemoryCache _cache;

        public FunpayParserController(ILogger<FunpayParserController> logger, Entities context, IMemoryCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
            _context.Database.SetCommandTimeout(180);  // Время ожидания в секундах (например, 3 минуты)

        }

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            var batchSize = 1000;
            while (_context.LotDetails.Any())
            {
                var batch = _context.LotDetails.Take(batchSize).ToList();
                _context.LotDetails.RemoveRange(batch);
                await _context.SaveChangesAsync();
            }
            while (_context.Sellers.Any())
            {
                var batch = _context.Sellers.Take(batchSize).ToList();
                _context.Sellers.RemoveRange(batch);
                await _context.SaveChangesAsync();
            }
            while (_context.GamesCategories.Any())
            {
                var batch = _context.GamesCategories.Take(batchSize).ToList();
                _context.GamesCategories.RemoveRange(batch);
                await _context.SaveChangesAsync();
            }


            // Сохраняем изменения
            await _context.SaveChangesAsync();

            await FetchLotsAndSellerData();
            await FetchGamesData();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 100, string tab = "lots")
        {
            // Получаем общее количество продавцов и лотов
            var totalSellers = await _context.Sellers.CountAsync();
            var totalLots = await _context.LotDetails.CountAsync();
            var totalGames = _context.GamesCategories.Count();

            // Рассчитываем количество страниц
            var totalPagesSellers = (int)Math.Ceiling((double)totalSellers / pageSize);
            var totalPagesLots = (int)Math.Ceiling((double)totalLots / pageSize);
            var totalPagesGames = (int)Math.Ceiling((double)totalGames / pageSize);

            // Получаем список продавцов для текущей страницы
            var sellers = _context.Sellers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderByDescending(s => s.ReviewCount)
                .Select(s => new SellerViewModel
                {
                    Id = s.SellerId,
                    SellerName = s.Sellername,
                    ReviewCount = s.ReviewCount ?? 0,
                    SellerInfo = s.SellerInfo,
                    RatingStar = s.RatingStar,
                    LotsCount = s.LotDetails.Count()
                }).ToList();

            // Получаем список лотов для текущей страницы
            var lots = _context.LotDetails
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(l => l.GamesCategory.GameName)
                .Select(l => new LotViewModel
                {
                    Id = l.LotId,
                    GameName = l.GamesCategory.GameName,
                    Category = l.GamesCategory.Category,
                    ServerName = l.ServerName,
                    SellerName = l.Seller.Sellername,
                    Amount = l.Amount ?? 0,
                    Description = l.DescriptionLot,
                    Price = l.Price
                }).ToList();

            var games = _context.GamesCategories
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(g => g.GameName)
                .Select(g => new GamesCategory
                {
                    GamesCategoryId = g.GamesCategoryId,
                    GameName = g.GameName,
                    Category = g.Category,
                }).ToList();

            // Формируем модель для передачи в представление
            var viewModel = new DataViewModel
            {
                Sellers = sellers,
                Lots = lots,
                Games = games,
                CurrentPage = page,
                TotalPagesSellers = totalPagesSellers,
                TotalPagesLots = totalPagesLots,
                TotalPagesGames = totalPagesGames,
                ActiveTab = tab // Передаем активную вкладку
            };

            // Передаем модель в представление
            return View(viewModel);
        }
        [HttpPost]
        public IActionResult DeleteGame(int id)
        {
            // Находим игру по Id
            var game = _context.GamesCategories
                .FirstOrDefault(g => g.GamesCategoryId == id);

            if (game == null)
            {
                return NotFound();  // Если игра не найдена
            }

            // Удаляем все связанные лоты, которые используют эту игру
            var relatedLots = _context.LotDetails
                .Where(l => l.GamesCategoryId == game.GamesCategoryId)
                .ToList();
            _context.LotDetails.RemoveRange(relatedLots);  // Удаляем все связанные лоты

            // Удаляем игру
            _context.GamesCategories.Remove(game);

            // Сохраняем изменения в базе данных
            _context.SaveChanges();

            // Перенаправляем на список всех игр
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteLot(int id)
        {
            // Находим игру по Id
            var lot = _context.LotDetails
                .FirstOrDefault(g => g.LotId == id);

            if (lot == null)
            {
                return NotFound();  // Если игра не найдена
            }

            // Удаляем игру
            _context.LotDetails.Remove(lot);

            // Сохраняем изменения в базе данных
            _context.SaveChanges();

            // Перенаправляем на список всех игр
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteSeller(int id)
        {
            // Находим игру по Id
            var seller = _context.Sellers
                .FirstOrDefault(g => g.SellerId == id);

            if (seller == null)
            {
                return NotFound();  // Если игра не найдена
            }

            // Удаляем все связанные лоты, которые используют эту игру
            var relatedLots = _context.LotDetails
                .Where(l => l.SellerId == seller.SellerId)
                .ToList();
            _context.LotDetails.RemoveRange(relatedLots);  // Удаляем все связанные лоты

            // Удаляем игру
            _context.Sellers.Remove(seller);

            // Сохраняем изменения в базе данных
            _context.SaveChanges();

            // Перенаправляем на список всех игр
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult EditGame(int id)
        {
            GamesCategory model;

            GamesCategory gamesCategory = _context.GamesCategories.Find(id);
            model = new GamesCategory
            {
                GamesCategoryId = gamesCategory.GamesCategoryId,
                GameName = gamesCategory.GameName,
                Category = gamesCategory.Category
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult EditGame(GamesCategory model)
        {
            if (ModelState.IsValid)
            {
                // Вызов Update вместо Attach и EntityState.Modified
                _context.GamesCategories.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult EditLot(int id)
        {
            var lotDetails = _context.LotDetails
                .Include(l => l.GamesCategory)
                .Include(l => l.Seller)
                .FirstOrDefault(l => l.LotId == id);

            if (lotDetails == null)
            {
                return NotFound(); // Если запись не найдена
            }

            var model = new LotViewModel
            {
                Id = lotDetails.LotId,
                GameName = lotDetails.GamesCategory?.GameName,
                Category = lotDetails.GamesCategory?.Category,
                ServerName = lotDetails.ServerName,
                SellerName = lotDetails.Seller?.Sellername,
                Amount = lotDetails.Amount,
                Description = lotDetails.DescriptionLot,
                Price = lotDetails.Price,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditLot(LotViewModel model)
        {
            if (ModelState.IsValid)
            {
                var lotDetails = new LotDetail
                {
                    LotId = model.Id,
                    ServerName = model.ServerName,
                    Amount = model.Amount,
                    DescriptionLot = model.Description,
                    Price = model.Price
                };

                _context.LotDetails.Attach(lotDetails);
                _context.Entry(lotDetails).Property(l => l.ServerName).IsModified = true;
                _context.Entry(lotDetails).Property(l => l.Amount).IsModified = true;
                _context.Entry(lotDetails).Property(l => l.DescriptionLot).IsModified = true;
                _context.Entry(lotDetails).Property(l => l.Price).IsModified = true;

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult EditSeller(int id)
        {
            // Находим продавца по ID
            var seller = _context.Sellers.FirstOrDefault(s => s.SellerId == id);

            if (seller == null)
            {
                return NotFound(); // Если продавец не найден
            }

            // Преобразуем данные в ViewModel
            var model = new SellerViewModel
            {
                Id = seller.SellerId,
                SellerName = seller.Sellername,
                ReviewCount = seller.ReviewCount,
                SellerInfo = seller.SellerInfo,
                RatingStar = seller.RatingStar,
                LotsCount = seller.LotDetails.Count // Количество лотов у продавца
            };

            return View(model); // Передаем модель в представление
        }


        [HttpPost]
        public IActionResult EditSeller(SellerViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Находим продавца по ID
                var seller = _context.Sellers.FirstOrDefault(s => s.SellerId == model.Id);

                if (seller == null)
                {
                    return NotFound(); // Если продавец не найден
                }

                // Обновляем данные
                seller.Sellername = model.SellerName;
                seller.ReviewCount = model.ReviewCount;
                seller.SellerInfo = model.SellerInfo;
                seller.RatingStar = model.RatingStar;

                // Сохраняем изменения в базе данных
                _context.SaveChanges();

                return RedirectToAction("Index"); // Перенаправляем на список продавцов
            }

            // Если модель некорректна, возвращаем ее в представление
            return View(model);
        }

        private async Task FetchGamesData()
        {
            string url = "https://funpay.com";
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);

            var document = await context.OpenAsync(url);
            var gameItems = document.QuerySelectorAll(".promo-game-item").Take(30);

            foreach (var item in gameItems)
            {
                var gameTitleElement = item.QuerySelector(".game-title a");
                var gameTitle = gameTitleElement?.TextContent.Trim() ?? "Unknown";

                var categoriesElements = item.QuerySelectorAll(".list-inline li a");
                var categories = categoriesElements.Select(e => e.TextContent.Trim()).ToList();
                var categoryUrls = categoriesElements.Select(e => e.GetAttribute("href")).Where(url => url != null).ToList();


                foreach (var category in categories)
                {
                    var game = new GamesCategory
                    {
                        GameName = gameTitle,
                        Category = category
                    };
                    try
                    {
                        _context.GamesCategories.Add(game);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }

                _context.SaveChanges();

            }
        }

        public async Task FetchLotsAndSellerData()
        {
            try
            {
                string url = "https://funpay.com";
                var config = Configuration.Default.WithDefaultLoader();
                var browsingContext = BrowsingContext.New(config);

                // Загружаем главную страницу
                var document = await browsingContext.OpenAsync(url);
                var gameItems = document.QuerySelectorAll(".promo-game-item").Take(30);

                foreach (var item in gameItems)
                {
                    // Получаем название игры
                    var gameTitleElement = item.QuerySelector(".game-title a");
                    var gameTitle = gameTitleElement?.TextContent.Trim() ?? "Unknown";

                    // Получаем категории игры
                    var categories = item.QuerySelectorAll(".list-inline li a");

                    foreach (var categoryElement in categories)
                    {
                        var categoryName = categoryElement.TextContent.Trim();
                        var categoryUrl = categoryElement.GetAttribute("href");

                        if (string.IsNullOrEmpty(categoryUrl)) continue;

                        // Переход на страницу категории
                        var fullCategoryUrl = new Uri(new Uri(url), categoryUrl).ToString();
                        var categoryDocument = await browsingContext.OpenAsync(fullCategoryUrl);

                        // Парсим лоты
                        var offerElements = categoryDocument.QuerySelectorAll(".tc-item").Take(10);

                        foreach (var offerElement in offerElements)
                        {
                            try
                            {
                                // Парсим информацию о лоте
                                var server = offerElement.GetAttribute("data-server");
                                var userElement = offerElement.QuerySelector(".tc-user .media-user-name");
                                var userName = userElement?.TextContent.Trim();
                                var reviewCountElement = offerElement.QuerySelector(".rating-mini-count");
                                var reviewCount = reviewCountElement != null ? int.Parse(reviewCountElement.TextContent.Trim()) : 0;
                                var sellerInfo = offerElement.QuerySelector(".media-user-info")?.TextContent.Trim();
                                var ratingStars = offerElement.QuerySelector(".rating-stars")?.GetAttribute("class")?.Replace("rating-stars ", "");

                                var amountElement = offerElement.QuerySelector(".tc-amount");
                                var amount = amountElement != null ? int.Parse(amountElement.TextContent.Trim()) : (int?)null;
                                var description = offerElement.QuerySelector(".tc-desc .tc-desc-text")?.TextContent.Trim();
                                var price = offerElement.QuerySelector(".tc-price")?.TextContent.Trim();

                                // Проверяем и добавляем продавца в базу данных
                                var seller = _context.Sellers.FirstOrDefault(s => s.Sellername == userName);
                                if (seller == null)
                                {
                                    seller = new Seller
                                    {
                                        Sellername = userName,
                                        ReviewCount = reviewCount,
                                        SellerInfo = sellerInfo,
                                        RatingStar = ratingStars
                                    };
                                    _context.Sellers.Add(seller);
                                    await _context.SaveChangesAsync();
                                }

                                // Проверяем и добавляем категорию игры
                                var gameCategory = _context.GamesCategories.FirstOrDefault(g =>
                                    g.GameName == gameTitle && g.Category == categoryName);
                                if (gameCategory == null)
                                {
                                    gameCategory = new GamesCategory
                                    {
                                        GameName = gameTitle,
                                        Category = categoryName
                                    };
                                    _context.GamesCategories.Add(gameCategory);
                                    await _context.SaveChangesAsync();
                                }

                                // Добавляем лот в базу данных
                                var lot = new LotDetail
                                {
                                    ServerName = server,
                                    SellerId = seller.SellerId,
                                    Amount = amount,
                                    DescriptionLot = description,
                                    Price = price,
                                    GamesCategoryId = gameCategory.GamesCategoryId
                                };

                                _context.LotDetails.Add(lot);
                            }
                            catch (Exception ex)
                            {
                                // Логируем ошибки для каждого лота
                                _logger.LogError(ex, "Error while processing a lot");
                            }
                        }

                        // Сохраняем лоты в базе данных
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем общие ошибки
                _logger.LogError(ex, "Error while fetching and saving lots and sellers data");
            }
        }
    }
}
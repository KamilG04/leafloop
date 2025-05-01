using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(IUnitOfWork unitOfWork, ILogger<HomeController> logger)
            : base(unitOfWork)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Sprawdź, czy możemy pobrać kategorie
                var categories = await _unitOfWork.Categories.GetAllAsync();
                
                // Sprawdź, czy możemy pobrać najnowsze przedmioty
                var recentItems = await _unitOfWork.Items.GetAvailableItemsAsync(5);
                
                ViewBag.Categories = categories;
                return View(recentItems);
            }
            catch (Exception ex)
            {
                // Błąd oznacza, że coś nie działa z repozytoriami
                _logger.LogError(ex, "Error occurred while testing repositories");
                return View("Error");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
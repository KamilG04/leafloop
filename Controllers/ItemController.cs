// Ścieżka: Controllers/ItemsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
// Jeśli potrzebujesz pobrać kategorie dla formularza Create, dodaj using dla serwisu kategorii
using LeafLoop.Services.Interfaces;
using System.Threading.Tasks;

namespace LeafLoop.Controllers
{
    [Authorize] // Dostęp tylko dla zalogowanych użytkowników (można dostosować)
    public class ItemsController : Controller // Dziedziczy z Controller
    {
        // Opcjonalnie: Wstrzyknij serwis kategorii, jeśli chcesz przekazać kategorie do widoku Create
        private readonly ICategoryService _categoryService;
        public ItemsController(ICategoryService categoryService) { _categoryService = categoryService; }

        // Zwraca widok listy przedmiotów (będzie hostował Reacta)
        // GET: /Items lub /Items/Index
        public IActionResult Index()
        {
            return View(); // Szuka Views/Items/Index.cshtml
        }

        // Zwraca widok szczegółów przedmiotu (będzie hostował Reacta)
        // GET: /Items/Details/5
        public IActionResult Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Nieprawidłowe ID przedmiotu.");
            }
            // Przekazujemy ID, React użyje go do pobrania danych z API
            ViewBag.ItemId = id;
            return View(); // Szuka Views/Items/Details.cshtml
        }

        // Zwraca widok formularza tworzenia przedmiotu (będzie hostował Reacta)
        // GET: /Items/Create
        public async Task<IActionResult> Create() // Zmienione na async jeśli pobierasz kategorie
        {
            // Przykład pobrania kategorii - odkomentuj jeśli potrzebne i masz ICategoryService
            try
            {
                 var categories = await _categoryService.GetAllCategoriesAsync(); // Zakładając metodę GetAllCategoriesAsync w serwisie
                 ViewBag.Categories = categories;
             }
             catch (System.Exception ex)
             {
                 // Logowanie błędu
                 ViewBag.Categories = new List<LeafLoop.Services.DTOs.CategoryDto>(); // Pusta lista w razie błędu
                ModelState.AddModelError(string.Empty, "Nie udało się załadować kategorii.");
            }
            return View(); // Szuka Views/Items/Create.cshtml
        }

        // Można dodać akcję Edit(int id) analogicznie do Details
        // GET: /Items/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                  return BadRequest("Nieprawidłowe ID przedmiotu.");
             }
             ViewBag.ItemId = id;
             return View(); // Szuka Views/Items/Edit.cshtml
         }

// GET: /Items/MyItems
        public IActionResult MyItems()
        {
            // Ta akcja tylko zwraca widok. React pobierze dane z API.
            return View(); // Będzie szukać widoku Views/Items/MyItems.cshtml
        }
    }
}
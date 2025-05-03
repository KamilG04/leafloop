// Controllers/TransactionsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeafLoop.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
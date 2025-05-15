using LeafLoop.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LeafLoop.Controllers; //ale baza

public abstract class BaseController : Controller
{
    protected readonly IUnitOfWork _unitOfWork;

    protected BaseController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
}
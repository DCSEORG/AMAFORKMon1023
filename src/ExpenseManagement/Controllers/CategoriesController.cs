using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(DatabaseService db, ILogger<CategoriesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get all active expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetCategories()
    {
        var categories = await _db.GetCategoriesAsync();
        return Ok(categories);
    }
}

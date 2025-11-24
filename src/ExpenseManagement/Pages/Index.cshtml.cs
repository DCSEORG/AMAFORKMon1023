using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DatabaseService db, ILogger<IndexModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public string? Filter { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? filter = null)
    {
        Filter = filter;
        Expenses = await _db.GetExpensesAsync(filter);
        
        if (_db.IsUsingDummyData && !string.IsNullOrEmpty(_db.LastError))
        {
            ErrorMessage = _db.LastError;
        }
    }
}

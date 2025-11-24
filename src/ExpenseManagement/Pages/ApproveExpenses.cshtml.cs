using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly ILogger<ApproveExpensesModel> _logger;

    public ApproveExpensesModel(DatabaseService db, ILogger<ApproveExpensesModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public string? Filter { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? filter = null)
    {
        Filter = filter;
        
        if (string.IsNullOrEmpty(filter))
        {
            PendingExpenses = await _db.GetPendingExpensesAsync();
        }
        else
        {
            var allExpenses = await _db.GetExpensesAsync(filter, "Submitted");
            PendingExpenses = allExpenses;
        }
        
        if (_db.IsUsingDummyData && !string.IsNullOrEmpty(_db.LastError))
        {
            ErrorMessage = _db.LastError;
        }
    }

    public async Task<IActionResult> OnPostAsync(int expenseId)
    {
        await _db.UpdateExpenseStatusAsync(expenseId, "Approved", 1);
        return RedirectToPage();
    }
}

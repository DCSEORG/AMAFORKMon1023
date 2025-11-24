using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly DatabaseService _db;
    private readonly ILogger<AddExpenseModel> _logger;

    public AddExpenseModel(DatabaseService db, ILogger<AddExpenseModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _db.GetCategoriesAsync();
        
        if (_db.IsUsingDummyData && !string.IsNullOrEmpty(_db.LastError))
        {
            ErrorMessage = _db.LastError;
        }
    }

    public async Task<IActionResult> OnPostAsync(decimal amount, DateTime date, int categoryId, string? description)
    {
        Categories = await _db.GetCategoriesAsync();
        
        if (amount <= 0)
        {
            ErrorMessage = "Amount must be greater than zero";
            return Page();
        }

        var expenseId = await _db.CreateExpenseAsync(1, categoryId, amount, date, description);
        
        if (expenseId > 0)
        {
            SuccessMessage = $"Expense created successfully! (ID: {expenseId})";
        }
        else if (_db.IsUsingDummyData && !string.IsNullOrEmpty(_db.LastError))
        {
            ErrorMessage = _db.LastError;
        }
        else
        {
            ErrorMessage = "Failed to create expense";
        }
        
        return Page();
    }
}

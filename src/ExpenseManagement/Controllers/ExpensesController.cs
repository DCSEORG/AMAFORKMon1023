using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(DatabaseService db, ILogger<ExpensesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses([FromQuery] string? filter = null, [FromQuery] string? status = null)
    {
        var expenses = await _db.GetExpensesAsync(filter, status);
        return Ok(expenses);
    }

    /// <summary>
    /// Get pending expenses (status = Submitted)
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<Expense>>> GetPendingExpenses()
    {
        var expenses = await _db.GetPendingExpensesAsync();
        return Ok(expenses);
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var expense = await _db.GetExpenseByIdAsync(id);
        if (expense == null)
        {
            return NotFound();
        }
        return Ok(expense);
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var expenseId = await _db.CreateExpenseAsync(
            request.UserId,
            request.CategoryId,
            request.Amount,
            request.ExpenseDate,
            request.Description
        );
        
        if (expenseId == 0)
        {
            return StatusCode(500, "Failed to create expense");
        }
        
        return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, new { id = expenseId });
    }

    /// <summary>
    /// Update expense status (Submit, Approve, Reject)
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateExpenseStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var success = await _db.UpdateExpenseStatusAsync(id, request.Status, request.ReviewedBy);
        if (!success)
        {
            return StatusCode(500, "Failed to update expense status");
        }
        return NoContent();
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveExpense(int id, [FromBody] ApproveRequest? request = null)
    {
        var reviewedBy = request?.ReviewedBy ?? 1; // Default to user 1 if not specified
        var success = await _db.UpdateExpenseStatusAsync(id, "Approved", reviewedBy);
        if (!success)
        {
            return StatusCode(500, "Failed to approve expense");
        }
        return NoContent();
    }

    /// <summary>
    /// Reject an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectExpense(int id, [FromBody] ApproveRequest? request = null)
    {
        var reviewedBy = request?.ReviewedBy ?? 1; // Default to user 1 if not specified
        var success = await _db.UpdateExpenseStatusAsync(id, "Rejected", reviewedBy);
        if (!success)
        {
            return StatusCode(500, "Failed to reject expense");
        }
        return NoContent();
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        var success = await _db.UpdateExpenseStatusAsync(id, "Submitted");
        if (!success)
        {
            return StatusCode(500, "Failed to submit expense");
        }
        return NoContent();
    }
}

public class CreateExpenseRequest
{
    public int UserId { get; set; } = 1; // Default user
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public int? ReviewedBy { get; set; }
}

public class ApproveRequest
{
    public int ReviewedBy { get; set; } = 1;
}

using Microsoft.Data.SqlClient;
using Azure.Identity;
using Azure.Core;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;
    private bool _useDummyData = false;
    private string? _lastError = null;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        this._configuration = configuration;
        this._logger = logger;
        
        var sqlServer = configuration["SQL_SERVER"];
        var sqlDatabase = configuration["SQL_DATABASE"];
        var managedIdentityClientId = configuration["MANAGED_IDENTITY_CLIENT_ID"];
        
        if (string.IsNullOrEmpty(sqlServer) || string.IsNullOrEmpty(sqlDatabase))
        {
            // Try connection string
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }
        else
        {
            _connectionString = $"Server=tcp:{sqlServer},1433;Database={sqlDatabase};Authentication=Active Directory Managed Identity;User Id={managedIdentityClientId};";
        }
    }

    public string? LastError => _lastError;
    public bool IsUsingDummyData => _useDummyData;

    private SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<List<Expense>> GetExpensesAsync(string? filter = null, string? status = null)
    {
        try
        {
            _useDummyData = false;
            _lastError = null;
            
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            var query = @"
                SELECT e.ExpenseId, e.UserId, e.CategoryId, e.StatusId, e.AmountMinor, 
                       e.Currency, e.ExpenseDate, e.Description, e.ReceiptFile,
                       e.SubmittedAt, e.ReviewedBy, e.ReviewedAt, e.CreatedAt,
                       c.CategoryName, s.StatusName, u.UserName
                FROM dbo.Expenses e
                INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                INNER JOIN dbo.Users u ON e.UserId = u.UserId
                WHERE (@filter IS NULL OR c.CategoryName LIKE '%' + @filter + '%' OR u.UserName LIKE '%' + @filter + '%')
                  AND (@status IS NULL OR s.StatusName = @status)
                ORDER BY e.ExpenseDate DESC";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@filter", (object?)filter ?? DBNull.Value);
            command.Parameters.AddWithValue("@status", (object?)status ?? DBNull.Value);
            
            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error in GetExpensesAsync");
            _useDummyData = true;
            _lastError = $"Database connection failed in GetExpensesAsync at DatabaseService.cs:line 72. {GetManagedIdentityErrorDetails(ex)}";
            return GetDummyExpenses();
        }
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        return await GetExpensesAsync(null, "Submitted");
    }

    public async Task<Expense?> GetExpenseByIdAsync(int id)
    {
        try
        {
            _useDummyData = false;
            _lastError = null;
            
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            var query = @"
                SELECT e.ExpenseId, e.UserId, e.CategoryId, e.StatusId, e.AmountMinor, 
                       e.Currency, e.ExpenseDate, e.Description, e.ReceiptFile,
                       e.SubmittedAt, e.ReviewedBy, e.ReviewedAt, e.CreatedAt,
                       c.CategoryName, s.StatusName, u.UserName
                FROM dbo.Expenses e
                INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                INNER JOIN dbo.Users u ON e.UserId = u.UserId
                WHERE e.ExpenseId = @id";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error in GetExpenseByIdAsync");
            _useDummyData = true;
            _lastError = $"Database connection failed in GetExpenseByIdAsync at DatabaseService.cs:line 117. {GetManagedIdentityErrorDetails(ex)}";
            return null;
        }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        try
        {
            _useDummyData = false;
            _lastError = null;
            
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            var query = "SELECT CategoryId, CategoryName, IsActive FROM dbo.ExpenseCategories WHERE IsActive = 1";
            using var command = new SqlCommand(query, connection);
            
            var categories = new List<ExpenseCategory>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
            
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error in GetCategoriesAsync");
            _useDummyData = true;
            _lastError = $"Database connection failed in GetCategoriesAsync at DatabaseService.cs:line 155. {GetManagedIdentityErrorDetails(ex)}";
            return GetDummyCategories();
        }
    }

    public async Task<int> CreateExpenseAsync(int userId, int categoryId, decimal amount, DateTime date, string? description)
    {
        try
        {
            _useDummyData = false;
            _lastError = null;
            
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            var query = @"
                INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, CreatedAt)
                VALUES (@userId, @categoryId, (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft'), @amountMinor, 'GBP', @date, @description, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@categoryId", categoryId);
            command.Parameters.AddWithValue("@amountMinor", (int)(amount * 100));
            command.Parameters.AddWithValue("@date", date);
            command.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);
            
            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error in CreateExpenseAsync");
            _useDummyData = true;
            _lastError = $"Database connection failed in CreateExpenseAsync at DatabaseService.cs:line 193. {GetManagedIdentityErrorDetails(ex)}";
            return 0;
        }
    }

    public async Task<bool> UpdateExpenseStatusAsync(int expenseId, string status, int? reviewedBy = null)
    {
        try
        {
            _useDummyData = false;
            _lastError = null;
            
            using var connection = GetConnection();
            await connection.OpenAsync();
            
            var query = @"
                UPDATE dbo.Expenses 
                SET StatusId = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = @status),
                    ReviewedBy = @reviewedBy,
                    ReviewedAt = CASE WHEN @status IN ('Approved', 'Rejected') THEN SYSUTCDATETIME() ELSE NULL END,
                    SubmittedAt = CASE WHEN @status = 'Submitted' THEN SYSUTCDATETIME() ELSE SubmittedAt END
                WHERE ExpenseId = @expenseId";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@expenseId", expenseId);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@reviewedBy", (object?)reviewedBy ?? DBNull.Value);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error in UpdateExpenseStatusAsync");
            _useDummyData = true;
            _lastError = $"Database connection failed in UpdateExpenseStatusAsync at DatabaseService.cs:line 226. {GetManagedIdentityErrorDetails(ex)}";
            return false;
        }
    }

    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            CategoryId = reader.GetInt32(2),
            StatusId = reader.GetInt32(3),
            AmountMinor = reader.GetInt32(4),
            Currency = reader.GetString(5),
            ExpenseDate = reader.GetDateTime(6),
            Description = reader.IsDBNull(7) ? null : reader.GetString(7),
            ReceiptFile = reader.IsDBNull(8) ? null : reader.GetString(8),
            SubmittedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            ReviewedBy = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            ReviewedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
            CreatedAt = reader.GetDateTime(12),
            CategoryName = reader.GetString(13),
            StatusName = reader.GetString(14),
            UserName = reader.GetString(15)
        };
    }

    private string GetManagedIdentityErrorDetails(Exception ex)
    {
        if (ex.Message.Contains("Login failed") || ex.Message.Contains("authentication"))
        {
            return "MANAGED IDENTITY FIX: Ensure the managed identity has been granted database access. Run the database role configuration script (run-sql-dbrole.py) to assign db_datareader and db_datawriter roles to the managed identity. The managed identity user must be created in the database using 'CREATE USER [managed-identity-name] FROM EXTERNAL PROVIDER'.";
        }
        if (ex.Message.Contains("Cannot open server"))
        {
            return "MANAGED IDENTITY FIX: Check that the SQL server firewall allows Azure services. Verify the managed identity is assigned to the App Service in Azure Portal.";
        }
        return "MANAGED IDENTITY FIX: Verify managed identity configuration, database firewall rules, and that the identity has appropriate database roles assigned.";
    }

    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                CategoryId = 1,
                StatusId = 2,
                AmountMinor = 12000,
                ExpenseDate = new DateTime(2024, 1, 20),
                CategoryName = "Travel",
                StatusName = "Submitted",
                UserName = "Demo User",
                Description = "Dummy data - database unavailable"
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                CategoryId = 3,
                StatusId = 2,
                AmountMinor = 9950,
                ExpenseDate = new DateTime(2023, 12, 14),
                CategoryName = "Office Supplies",
                StatusName = "Submitted",
                UserName = "Demo User",
                Description = "Dummy data - database unavailable"
            }
        };
    }

    private List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }
}

using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using ExpenseManagement.Services;
using System.ClientModel;
using System.Text.Json;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly DatabaseService _dbService;
    private ChatClient? _chatClient;
    private bool _isConfigured = false;
    private string? _errorMessage = null;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, DatabaseService dbService)
    {
        _configuration = configuration;
        _logger = logger;
        _dbService = dbService;
        
        try
        {
            var endpoint = _configuration["OpenAI__Endpoint"];
            var deploymentName = _configuration["OpenAI__DeploymentName"];
            
            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(deploymentName))
            {
                var credential = new DefaultAzureCredential();
                var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                _chatClient = azureClient.GetChatClient(deploymentName);
                _isConfigured = true;
            }
            else
            {
                _errorMessage = "OpenAI configuration not found. Deploy with GenAI resources using deploy-with-chat.sh";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ChatService");
            _errorMessage = $"Failed to initialize OpenAI: {ex.Message}";
        }
    }

    public bool IsConfigured => _isConfigured;
    public string? ErrorMessage => _errorMessage;

    public async Task<string> ChatAsync(string userMessage, List<ChatMessage> conversationHistory)
    {
        if (!_isConfigured || _chatClient == null)
        {
            return "⚠️ GenAI services are not deployed. Run deploy-with-chat.sh to enable AI chat functionality.";
        }

        try
        {
            // Build chat messages with function calling tools
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"You are an AI assistant for an expense management system. You can help users:
- View expenses
- Add new expenses
- Approve expenses
- Get expense statistics

Use the available functions to interact with the database. Always be helpful and provide clear information.
When showing amounts, always include the £ symbol for GBP currency.")
            };

            // Add conversation history
            messages.AddRange(conversationHistory);
            
            // Add the new user message
            messages.Add(new UserChatMessage(userMessage));

            // Define function tools
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_expenses",
                    "Get all expenses with optional filtering",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            filter = new { type = "string", description = "Optional filter by category or user name" },
                            status = new { type = "string", description = "Optional filter by status (Submitted, Approved, Rejected, Draft)" }
                        }
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                ),
                ChatTool.CreateFunctionTool(
                    "create_expense",
                    "Create a new expense",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            amount = new { type = "number", description = "Amount in GBP" },
                            category = new { type = "string", description = "Category name (Travel, Meals, Supplies, Accommodation, Other)" },
                            date = new { type = "string", description = "Expense date in YYYY-MM-DD format" },
                            description = new { type = "string", description = "Optional description" }
                        },
                        required = new[] { "amount", "category", "date" }
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                ),
                ChatTool.CreateFunctionTool(
                    "get_pending_expenses",
                    "Get all expenses pending approval",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new { }
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                ),
                ChatTool.CreateFunctionTool(
                    "approve_expense",
                    "Approve an expense",
                    BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            expenseId = new { type = "integer", description = "The ID of the expense to approve" }
                        },
                        required = new[] { "expenseId" }
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                )
            };

            var chatOptions = new ChatCompletionOptions
            {
                Tools = { tools[0], tools[1], tools[2], tools[3] }
            };

            // Get completion with function calling
            var completion = await _chatClient.CompleteChatAsync(messages, chatOptions);

            // Check if we need to call any functions
            if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                var toolCalls = completion.Value.ToolCalls;
                foreach (var toolCall in toolCalls)
                {
                    if (toolCall is ChatToolCall functionToolCall)
                    {
                        var functionResult = await ExecuteFunctionAsync(functionToolCall.FunctionName, functionToolCall.FunctionArguments.ToString());
                        
                        // Add function call and result to messages
                        messages.Add(new AssistantChatMessage(completion.Value));
                        messages.Add(new ToolChatMessage(functionToolCall.Id, functionResult));
                    }
                }

                // Get final response
                var finalCompletion = await _chatClient.CompleteChatAsync(messages);
                return finalCompletion.Value.Content[0].Text;
            }

            return completion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChatAsync");
            return $"I encountered an error: {ex.Message}. Please try again.";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string argumentsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);

            switch (functionName)
            {
                case "get_expenses":
                    {
                        var filter = args?.ContainsKey("filter") == true ? args["filter"].GetString() : null;
                        var status = args?.ContainsKey("status") == true ? args["status"].GetString() : null;
                        var expenses = await _dbService.GetExpensesAsync(filter, status);
                        return JsonSerializer.Serialize(expenses.Select(e => new
                        {
                            e.ExpenseId,
                            e.ExpenseDate,
                            e.CategoryName,
                            Amount = e.Amount,
                            e.StatusName,
                            e.UserName,
                            e.Description
                        }));
                    }

                case "create_expense":
                    {
                        var amount = args?["amount"].GetDecimal() ?? 0;
                        var category = args?["category"].GetString() ?? "";
                        var dateStr = args?["date"].GetString() ?? DateTime.Now.ToString("yyyy-MM-dd");
                        var description = args?.ContainsKey("description") == true ? args["description"].GetString() : null;

                        var date = DateTime.Parse(dateStr);
                        var categories = await _dbService.GetCategoriesAsync();
                        var categoryObj = categories.FirstOrDefault(c => c.CategoryName.Equals(category, StringComparison.OrdinalIgnoreCase));

                        if (categoryObj == null)
                        {
                            return JsonSerializer.Serialize(new { success = false, error = "Invalid category" });
                        }

                        var expenseId = await _dbService.CreateExpenseAsync(1, categoryObj.CategoryId, amount, date, description);
                        return JsonSerializer.Serialize(new { success = expenseId > 0, expenseId });
                    }

                case "get_pending_expenses":
                    {
                        var expenses = await _dbService.GetPendingExpensesAsync();
                        return JsonSerializer.Serialize(expenses.Select(e => new
                        {
                            e.ExpenseId,
                            e.ExpenseDate,
                            e.CategoryName,
                            Amount = e.Amount,
                            e.UserName,
                            e.Description
                        }));
                    }

                case "approve_expense":
                    {
                        var expenseId = args?["expenseId"].GetInt32() ?? 0;
                        var success = await _dbService.UpdateExpenseStatusAsync(expenseId, "Approved", 1);
                        return JsonSerializer.Serialize(new { success });
                    }

                default:
                    return JsonSerializer.Serialize(new { error = "Unknown function" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}

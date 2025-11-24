using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using OpenAI.Chat;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatModel> _logger;

    public ChatModel(ChatService chatService, ILogger<ChatModel> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public List<ChatMessageItem> Messages { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }

    public void OnGet()
    {
        LoadMessagesFromSession();
        
        if (!_chatService.IsConfigured)
        {
            WarningMessage = _chatService.ErrorMessage;
            Messages.Add(new ChatMessageItem
            {
                IsUser = false,
                Content = "👋 Hello! I'm your AI assistant for the Expense Management System. However, GenAI services are not currently deployed. To enable full AI functionality, please run the deploy-with-chat.sh script. I can still provide basic assistance!"
            });
        }
        else if (Messages.Count == 0)
        {
            Messages.Add(new ChatMessageItem
            {
                IsUser = false,
                Content = "👋 Hello! I'm your AI assistant for the Expense Management System. I can help you view expenses, create new expenses, approve expenses, and provide insights. How can I help you today?"
            });
        }
    }

    public async Task<IActionResult> OnPostAsync(string userMessage)
    {
        LoadMessagesFromSession();
        
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            ErrorMessage = "Please enter a message";
            return Page();
        }

        // Add user message
        Messages.Add(new ChatMessageItem
        {
            IsUser = true,
            Content = userMessage
        });

        try
        {
            // Convert to OpenAI chat messages
            var conversationHistory = Messages.Take(Messages.Count - 1).Select(m => 
                m.IsUser 
                    ? (ChatMessage)new UserChatMessage(m.Content)
                    : (ChatMessage)new AssistantChatMessage(m.Content)
            ).ToList();

            // Get AI response
            var response = await _chatService.ChatAsync(userMessage, conversationHistory);
            
            // Add assistant message
            Messages.Add(new ChatMessageItem
            {
                IsUser = false,
                Content = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat");
            Messages.Add(new ChatMessageItem
            {
                IsUser = false,
                Content = $"I encountered an error: {ex.Message}. Please try again."
            });
        }

        SaveMessagesToSession();
        
        if (!_chatService.IsConfigured)
        {
            WarningMessage = _chatService.ErrorMessage;
        }

        return Page();
    }

    private void LoadMessagesFromSession()
    {
        var sessionMessages = HttpContext.Session.GetString("ChatMessages");
        if (!string.IsNullOrEmpty(sessionMessages))
        {
            Messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessageItem>>(sessionMessages) ?? new();
        }
    }

    private void SaveMessagesToSession()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(Messages);
        HttpContext.Session.SetString("ChatMessages", json);
    }
}

public class ChatMessageItem
{
    public bool IsUser { get; set; }
    public string Content { get; set; } = string.Empty;
}

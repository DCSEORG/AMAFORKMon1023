# Expense Management System - Modernized Azure Solution

A cloud-native expense management application built with ASP.NET Core, Azure SQL Database, and Azure OpenAI, featuring AI-powered natural language interactions.

## Features

### Core Functionality
- ✅ **View Expenses**: Filter and view all expense records
- ✅ **Add Expenses**: Create new expense entries with categories
- ✅ **Approve Expenses**: Manager workflow for expense approval
- ✅ **REST APIs**: Complete API set with Swagger documentation
- ✅ **Modern UI**: Clean, responsive interface with error handling

### AI-Powered Features (Optional)
- 🤖 **AI Chat Assistant**: Natural language interface for expense management
- 🔧 **Function Calling**: AI can execute database operations
- 💬 **Contextual Responses**: Intelligent assistance based on your data

## Architecture

The solution uses modern Azure services with secure, credential-free authentication:

- **Azure App Service**: Hosts the web application (Linux, Basic SKU)
- **Azure SQL Database**: Stores expense data with Entra ID authentication
- **Managed Identity**: Secure access without passwords or keys
- **Azure OpenAI** (Optional): GPT-4o model for AI chat functionality
- **Azure AI Search** (Optional): RAG capabilities for enhanced responses

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture diagram.

## Prerequisites

Before deploying, ensure you have:

1. **Azure CLI** installed and configured
2. **Azure Subscription** with permissions to create resources
3. **Python 3** with pip (for database schema import)
4. **.NET SDK** (if building locally)
5. **ODBC Driver 18 for SQL Server** (for schema import)

## Quick Start

### 1. Fork and Clone

Fork this repository and clone it locally:

```bash
git clone https://github.com/YOUR-USERNAME/AMAFORKMon1023.git
cd AMAFORKMon1023
```

### 2. Login to Azure

```bash
az login
az account set --subscription "YOUR-SUBSCRIPTION-ID"
```

### 3. Deploy (Standard - No AI)

Deploy the application without GenAI services:

```bash
chmod +x deploy.sh
./deploy.sh
```

This deploys:
- Azure App Service with Managed Identity
- Azure SQL Database with Northwind schema
- REST APIs with Swagger

**Application URL**: Check output for `Application URL: https://[app-name].azurewebsites.net/Index`

### 4. Deploy with AI Chat (Optional)

To enable AI-powered chat functionality:

```bash
chmod +x deploy-with-chat.sh
./deploy-with-chat.sh
```

This additionally deploys:
- Azure OpenAI (GPT-4o in Sweden)
- Azure AI Search
- Configures AI chat with function calling

**Chat URL**: `https://[app-name].azurewebsites.net/Chat`

## Usage

### Web Interface

1. **View Expenses**: Navigate to `/Index` to see all expenses
   - Use the filter box to search by category or user
   - View status: Draft, Submitted, Approved, Rejected

2. **Add Expense**: Go to `/AddExpense`
   - Enter amount, date, category, and description
   - Click Submit to create the expense

3. **Approve Expenses**: Visit `/ApproveExpenses`
   - View all pending (submitted) expenses
   - Click Approve to approve an expense

4. **AI Chat** (if deployed): Access `/Chat`
   - Ask questions in natural language
   - Examples:
     - "Show me all pending expenses"
     - "Add a £50 travel expense for today"
     - "What are my total expenses?"
     - "Approve expense ID 1"

### REST API

Access Swagger documentation at `/swagger` for:

- **GET /api/expenses** - List all expenses (with optional filtering)
- **GET /api/expenses/pending** - Get pending expenses
- **GET /api/expenses/{id}** - Get specific expense
- **POST /api/expenses** - Create new expense
- **PUT /api/expenses/{id}/status** - Update expense status
- **POST /api/expenses/{id}/approve** - Approve expense
- **POST /api/expenses/{id}/submit** - Submit expense
- **GET /api/categories** - List all categories

## Configuration

### Environment Variables (App Service Settings)

Set via Azure Portal or Azure CLI:

```bash
# Required for all deployments
MANAGED_IDENTITY_CLIENT_ID=[managed-identity-client-id]
SQL_SERVER=[sql-server].database.windows.net
SQL_DATABASE=Northwind

# Required only for AI chat deployments
OpenAI__Endpoint=https://[openai-name].openai.azure.com/
OpenAI__DeploymentName=gpt-4o
AzureSearch__Endpoint=https://[search-name].search.windows.net
```

## Security Features

✅ **No Credentials in Code**: All authentication uses Managed Identity  
✅ **MCAPS Compliant**: Azure AD-only authentication on SQL Database  
✅ **HTTPS Only**: All traffic encrypted  
✅ **TLS 1.2+**: Minimum TLS version enforced  
✅ **Error Handling**: Detailed managed identity troubleshooting  

## Troubleshooting

### Database Connection Issues

If you see database errors with dummy data:

1. **Check Managed Identity**: Verify it's assigned to the App Service
2. **Database Roles**: Run `python3 run-sql-dbrole.py` to grant permissions
3. **Firewall**: Ensure "Allow Azure services" is enabled on SQL Server

The error message will include specific managed identity troubleshooting steps.

### AI Chat Not Working

If AI chat shows a warning message:

1. **Deploy GenAI Services**: Run `./deploy-with-chat.sh`
2. **Check Configuration**: Verify OpenAI settings in App Service configuration
3. **Managed Identity**: Ensure identity has "Cognitive Services OpenAI User" role

## Development

### Build Locally

```bash
cd src/ExpenseManagement
dotnet build
dotnet run
```

Navigate to `https://localhost:5001/Index`

### Run Tests

```bash
cd src/ExpenseManagement
dotnet test
```

### Publish

```bash
dotnet publish -c Release -o /tmp/publish
cd /tmp/publish && zip -r ../../app.zip .
```

## Cost Estimation

**Standard Deployment** (without AI):
- App Service (Basic B1): ~£40/month
- SQL Database (Basic): ~£4/month
- **Total**: ~£44/month

**Full Deployment** (with AI):
- App Service (Basic B1): ~£40/month
- SQL Database (Basic): ~£4/month
- Azure OpenAI (S0): ~£0.002/1K tokens
- Azure AI Search (Basic): ~£60/month
- **Total**: ~£104/month + usage

*Prices are estimates and may vary by region*

## Clean Up

To delete all resources:

```bash
az group delete --name rg-ExpenseManagement --yes --no-wait
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Support

For issues or questions:
1. Check [ARCHITECTURE.md](ARCHITECTURE.md) for architecture details
2. Review error messages (they include troubleshooting steps)
3. Check Azure Portal for resource status
4. Review deployment script output for errors

## Acknowledgments

- Built following Azure best practices from [Microsoft Docs](https://www.microsoft.com)
- Uses MCAPS compliant security patterns
- Implements Azure AD-only authentication for SQL Database
- Leverages Managed Identity for credential-free authentication

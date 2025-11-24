![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

# App-Mod-Assist

A project demonstrating how GitHub Copilot coding agent can transform legacy applications into modern, cloud-native Azure solutions. This repository takes screenshots and a database schema as input and generates a complete, production-ready Azure application.

## 🚀 Quick Start

### For Template Users

1. **Fork this repo** - Start with a clean template each time you run the coding agent
2. **Replace the inputs**:
   - Add your legacy app screenshots to `Legacy-Screenshots/`
   - Add your database schema SQL to `Database-Schema/`
3. **Run the coding agent** with the prompt: "modernise my app"
4. **Deploy to Azure** using the generated scripts

### For Deployment

Once the agent has generated the code:

```bash
git clone https://github.com/YOUR-USERNAME/YOUR-FORK
cd YOUR-FORK

# Login to Azure
az login
az account set --subscription "YOUR-SUBSCRIPTION-ID"

# Deploy without AI chat
./deploy.sh

# OR deploy with AI chat functionality
./deploy-with-chat.sh
```

## ✨ What Gets Generated

The coding agent creates:

- **Bicep Infrastructure**: App Service, SQL Database, Managed Identity, Azure OpenAI
- **ASP.NET Application**: Razor Pages with REST APIs and Swagger docs
- **AI Chat Interface**: Natural language expense management with GPT-4o
- **Deployment Scripts**: Automated, one-command deployments
- **Documentation**: Architecture diagrams and deployment guides

## 📁 Repository Structure

```
.
├── Legacy-Screenshots/     # Input: Your legacy app screenshots
├── Database-Schema/        # Input: Your database schema SQL
├── infrastructure/         # Generated: Bicep templates
├── src/ExpenseManagement/ # Generated: ASP.NET application
├── deploy.sh              # Generated: Standard deployment
├── deploy-with-chat.sh    # Generated: AI-enabled deployment
└── ARCHITECTURE.md        # Generated: Architecture documentation
```

## 🎯 Features

### Core Application
- ✅ View, filter, and search expenses
- ✅ Add new expense entries
- ✅ Manager approval workflow
- ✅ REST APIs with Swagger documentation
- ✅ Modern, responsive UI

### AI-Powered (Optional)
- 🤖 Natural language chat interface
- 🔧 Function calling for database operations
- 💬 Context-aware responses
- 📊 Intelligent expense insights

## 🔒 Security & Compliance

- **Credential-Free**: Managed Identity authentication throughout
- **MCAPS Compliant**: Azure AD-only authentication
- **Encrypted**: HTTPS/TLS 1.2+ for all traffic
- **Best Practices**: Following Microsoft Azure security guidelines

## 📚 Documentation

- [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) - Complete deployment instructions
- [ARCHITECTURE.md](ARCHITECTURE.md) - Detailed architecture diagram
- [Guiding-Principles](Guiding-Principles/) - Development guidelines
- [Style-Guide](Style-Guide/) - Code style standards

## 💡 Example Use Case

This template demonstrates expense management modernization:

**Input**:
- 3 screenshots of a legacy expense tracking system
- SQL schema for Northwind-style expense database

**Output**:
- Full-stack Azure application
- AI chat assistant
- REST APIs
- Automated deployment
- Complete documentation

## 🛠️ Technology Stack

- **Frontend**: ASP.NET Razor Pages, Bootstrap, jQuery
- **Backend**: ASP.NET Core Web API, C#
- **Database**: Azure SQL Database (Basic tier)
- **AI**: Azure OpenAI (GPT-4o in Sweden)
- **Infrastructure**: Azure App Service (Linux), Bicep IaC
- **Authentication**: Azure Managed Identity, Entra ID

## 📊 Cost Estimate

**Standard** (no AI): ~£44/month  
**With AI**: ~£104/month + usage

See [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for detailed cost breakdown.

## 🤝 Contributing

This is a template repository. To contribute:

1. Test the template with different inputs
2. Suggest improvements to the agent instructions
3. Report issues with the generated code
4. Share your modernization results

## ⚠️ Important Notes

- **Fork each time**: Start with a fresh fork for each modernization to keep the template clean
- **ODBC Driver**: Install ODBC Driver 18 for SQL Server before running deployment scripts
- **Azure Permissions**: Requires subscription contributor access for deployments
- **Regional**: Configured for UK South by default, OpenAI in Sweden Central

## 📝 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- Powered by GitHub Copilot Coding Agent
- Built with Azure best practices from [Microsoft Docs](https://www.microsoft.com)
- Template maintained by the App Modernization team

---

**Need Help?** Check the [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for troubleshooting and detailed instructions.

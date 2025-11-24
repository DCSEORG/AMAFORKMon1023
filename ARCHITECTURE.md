# Azure Services Architecture Diagram

## Expense Management System - Cloud Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                              USER / BROWSER                                 │
│                                                                             │
└────────────────────────┬────────────────────────────────────────────────────┘
                         │ HTTPS
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                         Azure App Service (Web App)                         │
│                         - ASP.NET Razor Pages                               │
│                         - REST APIs with Swagger                            │
│                         - Chat UI with AI Integration                       │
│                         - Linux, Basic (B1) SKU                             │
│                                                                             │
└──────────┬────────────────────────┬─────────────────────┬──────────────────┘
           │                        │                     │
           │ Managed Identity       │ Managed Identity    │ Managed Identity
           │ Authentication         │ Authentication      │ Authentication
           │                        │                     │
           ▼                        ▼                     ▼
┌──────────────────────┐  ┌───────────────────┐  ┌─────────────────────────┐
│                      │  │                   │  │                         │
│  Azure SQL Database  │  │  Azure OpenAI     │  │  Azure AI Search        │
│  - Northwind Schema  │  │  - GPT-4o Model   │  │  - RAG for Context      │
│  - Basic Tier        │  │  - Sweden Region  │  │  - Basic Tier           │
│  - Entra ID Auth     │  │  - S0 SKU         │  │  - Optional             │
│  - MCAPS Compliant   │  │                   │  │                         │
│                      │  │                   │  │                         │
└──────────────────────┘  └───────────────────┘  └─────────────────────────┘
           ▲
           │
           │
┌──────────┴───────────┐
│                      │
│  User Assigned       │
│  Managed Identity    │
│  mid-AppModAssist-   │
│  [Day-Hour-Minute]   │
│                      │
└──────────────────────┘
```

## Component Descriptions

### 1. **Azure App Service**
   - Hosts the ASP.NET Razor Pages web application
   - Provides REST APIs for expense management operations
   - Includes Swagger documentation for API testing
   - Hosts AI Chat UI for natural language interactions
   - Deployed on Linux with Basic (B1) SKU for cost optimization
   - Located in UK South region

### 2. **User Assigned Managed Identity**
   - Provides secure, credential-free authentication
   - Assigned to App Service at deployment time
   - Grants access to:
     - Azure SQL Database (db_datareader, db_datawriter roles)
     - Azure OpenAI (Cognitive Services OpenAI User role)
     - Azure AI Search (Search Index Data Reader role)
   - Named: `mid-AppModAssist-[Day-Hour-Minute]`

### 3. **Azure SQL Database**
   - Stores expense management data (Northwind schema)
   - Configured with Entra ID (Azure AD) authentication only
   - SQL authentication disabled (MCAPS compliance)
   - Basic tier for development
   - Firewall allows Azure services
   - Schema imported via Python script using Azure CLI credentials

### 4. **Azure OpenAI Service** (Optional - with deploy-with-chat.sh)
   - Provides GPT-4o model for AI chat functionality
   - Deployed in Sweden Central region (even though app is in UK South)
   - S0 SKU for cost optimization
   - Enables natural language database interactions
   - Function calling for executing database operations

### 5. **Azure AI Search** (Optional - with deploy-with-chat.sh)
   - Supports Retrieval-Augmented Generation (RAG)
   - Provides contextual information for AI responses
   - Basic tier for development
   - Enables semantic search capabilities

## Security Features

1. **No Passwords/Keys**: All authentication uses Managed Identity
2. **MCAPS Compliance**: Azure AD-only authentication on SQL Database
3. **HTTPS Only**: All traffic encrypted in transit
4. **TLS 1.2+**: Minimum TLS version enforced
5. **FTPS Disabled**: Only HTTPS deployment supported

## Deployment Options

### Standard Deployment (`deploy.sh`)
- Deploys App Service, Managed Identity, and Azure SQL Database
- No GenAI services
- Chat UI shows dummy responses with instructions to deploy GenAI

### Full Deployment with AI (`deploy-with-chat.sh`)
- Deploys all services including Azure OpenAI and AI Search
- Enables full AI chat functionality with function calling
- Configures App Service with OpenAI settings post-deployment

## Data Flow

1. User accesses web application via HTTPS
2. App Service authenticates to Azure SQL using Managed Identity
3. Database operations performed through REST APIs
4. (Optional) User interacts with AI Chat
5. AI Chat calls OpenAI via Managed Identity
6. OpenAI uses function calling to execute database operations
7. Results returned to user through natural language response

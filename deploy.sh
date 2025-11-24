#!/bin/bash
set -e

echo "=================================="
echo "Expense Management System Deployment"
echo "=================================="
echo ""

# Configuration
RESOURCE_GROUP_NAME="rg-ExpenseManagement"
LOCATION="uksouth"
BASE_NAME="expensemgmt"

# Get current user info for SQL admin
echo "Getting current user information..."
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Deploying infrastructure (without GenAI)..."
echo ""

# Deploy main Bicep template
DEPLOYMENT_OUTPUT=$(az deployment sub create \
  --location $LOCATION \
  --template-file infrastructure/main.bicep \
  --parameters \
    resourceGroupName=$RESOURCE_GROUP_NAME \
    location=$LOCATION \
    baseName=$BASE_NAME \
    deployGenAI=false \
  --parameters infrastructure/azure-sql.bicep \
    adminObjectId=$ADMIN_OBJECT_ID \
    adminLogin=$ADMIN_LOGIN \
  --query 'properties.outputs' \
  -o json)

echo "✓ Infrastructure deployed successfully"
echo ""

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
SQL_DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlDatabaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')

echo "Deployment Details:"
echo "  Resource Group: $RESOURCE_GROUP_NAME"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $SQL_DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo ""

# Configure App Service settings
echo "Configuring App Service settings..."
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP_NAME \
  --name $APP_SERVICE_NAME \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:$SQL_SERVER_FQDN,1433;Database=$SQL_DATABASE_NAME;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;" \
    "MANAGED_IDENTITY_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
    "SQL_SERVER=$SQL_SERVER_FQDN" \
    "SQL_DATABASE=$SQL_DATABASE_NAME" \
  --output none

echo "✓ App Service configured"
echo ""

# Install required Python packages
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Import database schema
echo "Importing database schema..."
python3 run-sql.py

# Configure database roles for managed identity
echo "Configuring database roles for managed identity..."
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak
python3 run-sql-dbrole.py

echo "✓ Database configured"
echo ""

# Deploy application code
if [ -f "app.zip" ]; then
  echo "Deploying application code..."
  az webapp deploy \
    --resource-group $RESOURCE_GROUP_NAME \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip
  
  echo "✓ Application deployed"
  echo ""
fi

echo "=================================="
echo "Deployment Complete!"
echo "=================================="
echo ""
echo "Application URL: ${APP_SERVICE_URL}/Index"
echo ""
echo "Note: Navigate to ${APP_SERVICE_URL}/Index to view the application"
echo "      (not just the root URL)"
echo ""
echo "To deploy with GenAI chat functionality, run: ./deploy-with-chat.sh"

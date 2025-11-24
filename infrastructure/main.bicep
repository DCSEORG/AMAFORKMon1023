// Main Bicep file for deploying the Expense Management System
targetScope = 'subscription'

@description('Name of the resource group')
param resourceGroupName string = 'rg-ExpenseManagement'

@description('Location for all resources')
param location string = 'uksouth'

@description('Base name for all resources')
param baseName string = 'expensemgmt'

@description('Deploy GenAI resources (set to true when using deploy-with-chat.sh)')
param deployGenAI bool = false

// Create resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
}

// Deploy App Service and Managed Identity
module appService 'app-service.bicep' = {
  scope: rg
  name: 'appServiceDeployment'
  params: {
    location: location
    baseName: baseName
  }
}

// Deploy Azure SQL Database
module database 'azure-sql.bicep' = {
  scope: rg
  name: 'databaseDeployment'
  params: {
    location: location
    baseName: baseName
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityName: appService.outputs.managedIdentityName
  }
}

// Conditionally deploy GenAI resources
module genai 'genai.bicep' = if (deployGenAI) {
  scope: rg
  name: 'genaiDeployment'
  params: {
    location: location
    baseName: baseName
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output resourceGroupName string = rg.name
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityName string = appService.outputs.managedIdentityName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output sqlServerName string = database.outputs.sqlServerName
output sqlDatabaseName string = database.outputs.databaseName
output sqlServerFqdn string = database.outputs.sqlServerFqdn

// GenAI outputs (only when deployed)
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genai.outputs.searchName : ''

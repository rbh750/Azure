# Service.Azure Overview

This repository provides a set of services for interacting with Azure resources. The `Service.Azure` project contains reusable service classes for common Azure operations. The project also includes integration tests to validate all these services; however, each corresponding Azure resource must be created in your Azure subscription before running the tests.


## Getting Started
1. Clone the repository.
2. Update `appsettings.json` with your Azure credentials.
3. Build and run the solution.

## Services in Service.Azure

### 1. AppInsightsTelemetryService
**Location:** `Service.Azure/AppInsights/AppInsightsTelemetryService.cs`

Provides integration with Azure Application Insights:
- Tracks custom events, metrics, and exceptions
- Enables distributed tracing and telemetry
- Useful for monitoring and diagnostics

### 2. AppInsightsQueryService
**Location:** `Service.Azure/AppInsights/AppInsightsQueryService.cs`

Provides querying capabilities for Azure Application Insights:
- Execute KQL (Kusto Query Language) queries against Application Insights logs
- Returns query results as JSON arrays
- Supports retry policies for robust operations

### 3. BlobStorageService
**Location:** `Service.Azure/Storage/Blob/BlobStorageService.cs`

Handles general operations for Azure Blob Storage:
- Upload, download, and delete blobs
- List containers and blobs
- Manage access policies and metadata

### 4. BlobStoragePushService
**Location:** `Service.Azure/Storage/Blob/BlobStoragePushService.cs`

Specialized for pushing batches of data to Blob Storage:
- Event-driven blob creation
- Batch operations
- Customizable blob naming

### 5. ContainerInstanceService
**Location:** `Service.Azure/Docker/ContainerInstanceService.cs`

Manages Azure Container Instances:
- Create, start, stop, and delete containers
- Configure container groups and networking
- Integrates with Azure Container Registry

### 6. CosmosDbService
**Location:** `Service.Azure/CosmosDb/CosmosDbService.cs`

Handles operations for Azure Cosmos DB:
- CRUD operations for documents and containers
- Querying with SQL-like syntax
- Partition key and throughput management

### 7. KeyVaultService
**Location:** `Service.Azure/KeyVault/KeyVaultService.cs`

Manages secrets and certificates in Azure Key Vault:
- Retrieve and set secrets
- Certificate management

### 8. ServiceBusService
**Location:** `Service.Azure/ServiceBus/ServiceBusService.cs`

Handles Azure Service Bus messaging:
- Send and receive messages to queues and topics
- Dead-letter handling
- Integrated retry policies

### 9. TableStorageService
**Location:** `Service.Azure/Storage/Table/TableStorageService.cs`

Handles CRUD operations for Azure Table Storage:
- Add, update, delete, and query table entities
- Optimistic concurrency with ETag
- Retry policies for robust operations

---

## Configuration
All services require Azure resource connection strings and settings, typically provided via `appsettings.json`.

### Example `appsettings.json`
```json
{
  "Azure": {
    "AppInsights": {
      "ConnectionString": "<ApplicationInsightsConnectionString>",
      "CallerId": "<ApplicationInsightsCallerId>",
      "CallerEnvironment": "<ApplicationInsightsCallerEnvironment>",
      "DeveloperMode": false,
      "Resources": {
        "ResourceGroupName": "<ApplicationInsightsResourceGroupName>",
        "ResourceNameApi": "<ApplicationInsightsApiResourceName>",
        "ResourceNameWebJobs": "<ApplicationInsightsWebJobsResourceName>",
        "SubscriptionId": "<ApplicationInsightsSubscriptionId>"
      }
    },
    "CosmosDb": {
      "ConnectionString": "<CosmosDbConnectionString>",
      "DatabaseName": "<CosmosDbDatabaseName>"
    },
    "Docker": {
      "Registry": {
        "Server": "<ContainerRegistryServer>",
        "UserName": "<ContainerRegistryUserName>",
        "Password": "<ContainerRegistryPassword>"
      }
    },
    "Entra": {
      "PublicAuth": {
        "ClientId": "not used at the moment"
      },
      "KeyVaultAuth": {
        "ClientId": "<KeyVaultClientId>",
        "Secret": "<KeyVaultSecret>",
        "TenantId": "<KeyVaultTenantId>",
        "KeyVaultUrl": "<KeyVaultUrl>"
      },
      "AppInsightsAuth": {
        "ClientId": "<AppInsightsAppClientId>",
        "Secret": "<AppInsightsAppSecret>",
        "TenantId": "<AppInsightsAppTenantId>"
      }
    },
    "ServiceBus": {
      "ConnectionString": "<ServiceBusConnectionString>",
      "TranscriptQueue": "<ServiceBusQueueName>"
    },
    "Storage": {
      "ConnectionString": "<StorageAccountConnectionString>"
    }
  },
  "RetryPolicy": {
    "DelayMilliseconds": 100,
    "MaxDelayMilliseconds": 1000,
    "MaxRetries": 3
  }
}
```

### How to Get Each Value from Azure Portal

- **Application Insights Connection String** (for AppInsightsTelemetryService): Go to your Application Insights resource > Overview > Copy the Connection String.
- **Application Insights Resource Settings** (for AppInsightsQueryService): 
  - **CallerId**: A unique identifier for your application (custom value)
  - **CallerEnvironment**: The environment name (e.g., "dev", "staging", "prod")
  - **ResourceGroupName**: Go to your Application Insights resource > Overview > Copy the Resource group name
  - **ResourceNameApi/ResourceNameWebJobs**: The names of your specific Application Insights resources for API and WebJobs
  - **SubscriptionId**: Go to your Application Insights resource > Overview > Copy the Subscription ID
- **Blob Storage Connection String** (for BlobStorageService): Go to your Storage Account > Access keys > Copy the Connection string.
- **Cosmos DB Connection String & Database Name** (for CosmosDbService): Go to your Cosmos DB account > Keys > Copy the PRIMARY CONNECTION STRING. Database name is under Data Explorer.
- **Container Registry Server/UserName/Password** (for ContainerInstanceService): Go to your Container Registry > Access Keys. 
  - **Server**: Copy the Login server value.
  - **UserName**: Copy the Username value (usually 'admin' if admin user is enabled).
  - **Password**: Copy one of the available passwords (enable 'Admin user' if not already enabled to see these values).
- **Azure Container Instance Resource Group, Subscription, and Location** (for ContainerInstanceService): Go to your Container Instance > Overview > Copy Resource Group, Subscription ID, and Location.
- **Entra PublicAuth ClientId**: Go to Azure Active Directory > App registrations > Select your app > Copy the Application (client) ID.
- **KeyVaultAuth ClientId/Secret/TenantId/KeyVaultUrl**: 
  - Create an App Registration in Azure Active Directory and generate a client secret under Certificates & secrets.
  - Grant the RBAC role **Key Vault Secrets User** to the service principal (the app registration) for your Key Vault.
  - Copy the Application (client) ID and Tenant ID from the app registration.
  - KeyVaultUrl is the DNS name of your Key Vault (found in the Key Vault Overview).
- **AppInsightsAuth ClientId/Secret/TenantId** (for AppInsightsQueryService):
  - Create an App Registration in Azure Active Directory and generate a client secret under Certificates & secrets.
  - **Configure API Permissions:**
    1. In the App Registration ? API permissions blade, add the Log Analytics API (or Azure Monitor API, depending on the portal wording).
    2. Select Application permissions (for service-to-service) or Delegated permissions (if acting on behalf of a signed-in user).
    3. The specific permission required is: **Data.Read** ? Read Log Analytics data
    4. After adding it, click **Grant admin consent** so the app can use it without interactive user approval.
  - **Configure Role Assignment on Target Resource:**
    1. Go to your Log Analytics Workspace (or the Application Insights resource's linked workspace).
    2. Open **Access control (IAM)** ? **Add role assignment**.
    3. Assign the **Reader** role (or a custom role with `Microsoft.OperationalInsights/workspaces/query/read` permission) to the service principal.
  - Copy the Application (client) ID and Tenant ID from the app registration.
- **Service Bus Connection String & Queue Name**: Go to your Service Bus namespace > Shared access policies > RootManageSharedAccessKey > Copy the Connection string. Queue name is under Entities > Queues.

**Note:** Replace all placeholder values (e.g., `<ApplicationInsightsConnectionString>`) with your actual values from the Azure portal.

## Requirements
- .NET 9 SDK
- Azure account with required resources

For more details, refer to the source code in each service directory.

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

### 2. BlobStoragePushService
**Location:** `Service.Azure/Storage/Blob/BlobStoragePushService.cs`

Specialized for pushing batches of data to Blob Storage:
- Event-driven blob creation
- Batch operations
- Customizable blob naming

### 3. ContainerInstanceService
**Location:** `Service.Azure/Docker/ContainerInstanceService.cs`

Manages Azure Container Instances:
- Create, start, stop, and delete containers
- Configure container groups and networking
- Integrates with Azure Container Registry

### 4. CosmosDbService
**Location:** `Service.Azure/CosmosDb/CosmosDbService.cs`

Handles operations for Azure Cosmos DB:
- CRUD operations for documents and containers
- Querying with SQL-like syntax
- Partition key and throughput management

### 5. KeyVaultService
**Location:** `Service.Azure/KeyVault/KeyVaultService.cs`

Manages secrets and certificates in Azure Key Vault:
- Retrieve and set secrets
- Certificate management

### 6. ServiceBusService
**Location:** `Service.Azure/ServiceBus/ServiceBusService.cs`

Handles Azure Service Bus messaging:
- Send and receive messages to queues and topics
- Dead-letter handling
- Integrated retry policies

### 7. TableStorageService
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
      "ConnectionString": "<ApplicationInsightsConnectionString>"
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
- **Blob Storage Connection String** (for BlobStoragePushService): Go to your Storage Account > Access keys > Copy the Connection string.
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
- **Service Bus Connection String & Queue Name**: Go to your Service Bus namespace > Shared access policies > RootManageSharedAccessKey > Copy the Connection string. Queue name is under Entities > Queues.

**Note:** Replace all placeholder values (e.g., `<ApplicationInsightsConnectionString>`) with your actual values from the Azure portal.

## Requirements
- .NET 9 SDK
- Azure account with required resources

For more details, refer to the source code in each service directory.

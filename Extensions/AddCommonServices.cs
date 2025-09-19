using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Service.Azure.AppInsights;
using Service.Azure.CosmosDb;
using Service.Azure.Docker;
using Service.Azure.Entra;
using Service.Azure.KeyVault;
using Service.Azure.RetryPolicy;
using Service.Azure.ServiceBus;
using Service.Azure.Storage;
using Service.Azure.Storage.Blob;
using Service.Azure.Storage.Table;

namespace Extensions;

/// <summary>
/// Extension class for registering common services in the dependency injection container.
/// </summary>
public static class AddCommonServices
{
    private static IConfigurationRoot? config = null;
    private static IConfigurationSection? azureSection = null;
    private const string thisClass = "AddCommonServices:";
    private static readonly RunningEnvironment runningEnvironment = RunningEnvironment.Local;

    public static IServiceCollection AddCommonServiceModules(this IServiceCollection services,  string fileName)
    {
        config = new ConfigurationBuilder()
            .AddJsonFile(fileName, optional: true, reloadOnChange: true)
            //.AddEnvironmentVariables()
            .Build()
            ?? throw new ArgumentException("Cannot bind the Root configuration section.");

        azureSection = config.GetSection("Azure")
            ?? throw new ArgumentException("Cannot bind the Azure configuration section.");

        AddApplicationInsights(services);
        AddCosmosDbService(services);
        AddDockerServices(services);
        AddEntraPublicServices(services);
        AddKeyVaultService(services);
        AddServiceBusService(services);
        AddStorageService(services);
        AddRetryPolicyService(services);

        return services;
    }

    private static void AddApplicationInsights(IServiceCollection services)
    {
        AppInsightsSettings appInsightsSettings = new();

        if (runningEnvironment == RunningEnvironment.Local)
        {
            azureSection!.Bind(appInsightsSettings.ConfigurationKey, appInsightsSettings);
        }
        else
        {
            appInsightsSettings.CallerId = config!["Azure:AppInsights:CallerId"]!;
            appInsightsSettings.CallerEnvironment = config["Azure:AppInsights:CallerEnvironment"]!;
            appInsightsSettings.ConnectionString = config["Azure:AppInsights:ConnectionString"]!;
            appInsightsSettings.DeveloperMode = Convert.ToBoolean(config["Azure:AppInsights:DeveloperMode"]);
        }

        string serviceName = "Application Insights:";

        var error = appInsightsSettings switch
        {
            { CallerId: null or "" } => $"{serviceName} caller id is required",
            { CallerEnvironment: null or "" } => $"{serviceName} caller environment is required",
            { ConnectionString: null or "" } => $"{serviceName} connection string is required",
            _ => null
        };

        if (error != null)
        {
            throw new ArgumentException($"{thisClass} {error}");
        }

        services.Configure<AppInsightsSettings>(options =>
        {
            options.ConnectionString = appInsightsSettings.ConnectionString!;
            options.DeveloperMode = appInsightsSettings.DeveloperMode;
            options.CallerId = appInsightsSettings.CallerId!;
            options.CallerEnvironment = appInsightsSettings.CallerEnvironment!;
        });

        services.AddScoped<IAppInsightsTelemetryService, AppInsightsTelemetryService>();
    }

    private static void AddCosmosDbService(IServiceCollection services)
    {
        CosmosDbSettings cosmosDbSettings = new();
        var containersSection = config!.GetSection("Azure:CosmosDb:Containers")
            ?? throw new ArgumentException("Cannot find section CosmosDb containers.");

        if (runningEnvironment == RunningEnvironment.Local)
        {
            azureSection!.Bind(cosmosDbSettings.ConfigurationKey, cosmosDbSettings);
        }
        else
        {
            cosmosDbSettings.ConnectionString = config!["Azure:CosmosDb:ConnectionString"]!;
            cosmosDbSettings.DatabaseName = config["Azure:CosmosDb:DatabaseName"]!;

            if (runningEnvironment == RunningEnvironment.AzureFn)
            {
                var desirializedContainers = JsonConvert.DeserializeObject<List<CosmosContainer>>(containersSection.Value!);
                cosmosDbSettings.Containers = desirializedContainers
                    ?? throw new ArgumentException("Cannot deserialize CosmosDb containers.");
            }
            else if (runningEnvironment == RunningEnvironment.AzureWebApp)
            {
                cosmosDbSettings.Containers = containersSection.Get<List<CosmosContainer>>()
                    ?? throw new ArgumentException("Cannot bind CosmosDb containers.");
            }
        }

        string serviceName = "Cosmos Db:";

        var error = cosmosDbSettings switch
        {
            { ConnectionString: null or "" } => $"{serviceName} connection string is required",
            { DatabaseName: null or "" } => $"{serviceName} database name is required",
            _ => null
        };

        if (error != null)
        {
            throw new ArgumentException($"{thisClass} {error}");
        }

        services.Configure<CosmosDbSettings>(options =>
        {
            options.ConnectionString = cosmosDbSettings.ConnectionString!;
            options.DatabaseName = cosmosDbSettings.DatabaseName!;
            options.Containers = cosmosDbSettings.Containers!;
        });

        services.AddScoped<ICosmosDbService, CosmosDbService>();
    }

    private static void AddEntraPublicServices(IServiceCollection services)
    {
        EntraSettings entraSettings = new();

        if (runningEnvironment == RunningEnvironment.Local)
        {
            azureSection!.Bind(entraSettings.ConfigurationKey, entraSettings);
        }
        else
        {
            entraSettings.PublicAuth.ClientId = config!["Entra:PublicAuth:ClientId"]!;
            entraSettings.KeyVaultAuth.ClientId = config["Entra:KeyVaultAuth:ClientId"]!;
            entraSettings.KeyVaultAuth.Secret = config["Entra:KeyVaultAuth:Secret"]!;
            entraSettings.KeyVaultAuth.TenantId = config["Entra:KeyVaultAuth:TenantId"]!;
            entraSettings.KeyVaultAuth.KeyVaultUrl = config["Entra:KeyVaultAuth:KeyVaultUrl"]!;
        }

        string serviceName = "Entra:";

        var error = entraSettings switch
        {
            { PublicAuth.ClientId: "" } => $"{serviceName} PublicAuth client id is required",
            _ => null
        };

        if (error != null)
        {
            throw new ArgumentException($"{thisClass} {error}");
        }

        services.Configure<EntraSettings>(options =>
        {
            options.PublicAuth.ClientId = entraSettings.PublicAuth.ClientId;
            options.KeyVaultAuth.ClientId = entraSettings.KeyVaultAuth.ClientId;
            options.KeyVaultAuth.Secret = entraSettings.KeyVaultAuth.Secret;
            options.KeyVaultAuth.TenantId = entraSettings.KeyVaultAuth.TenantId;
            options.KeyVaultAuth.KeyVaultUrl = entraSettings.KeyVaultAuth.KeyVaultUrl;
        });
    }

    private static void AddDockerServices(IServiceCollection services)
    {
        var containersSection = config!.GetSection("Azure");
        var dockerSettings = containersSection.Get<DockerSettings>() ?? throw new ArgumentException("Cannot bind Docker settings.");

        string serviceName = "Docker:";

        var error = dockerSettings.Docker?.Registry switch
        {
            { ContainerGroupName: null or "" } => $"{serviceName} Container group name is required",
            { Image: null or "" } => $"{serviceName} Image is required",
            { Location: null or "" } => $"{serviceName} Location is required",
            { Password: null or "" } => $"{serviceName} Password is required",
            { ResourceGroupName: null or "" } => $"{serviceName} Resource group name is required",
            { Server: null or "" } => $"{serviceName} Server is required",
            { SubscriptionId: null or "" } => $"{serviceName} Subscription id is required",
            { TenantId: null or "" } => $"{serviceName} Tenant id is required",
            { UserName: null or "" } => $"{serviceName} User name is required",
            _ => null
        };

        if (error != null)
        {
            throw new ArgumentException($"{thisClass} {error}");
        }

        services.Configure<DockerSettings>(options =>
        {
            options.Docker = new Docker
            {
                Registry = new Registry
                {
                    ContainerGroupName = dockerSettings.Docker!.Registry.ContainerGroupName,
                    Image = dockerSettings.Docker.Registry.Image,
                    Location = dockerSettings.Docker.Registry.Location,
                    Password = dockerSettings.Docker.Registry.Password,
                    ResourceGroupName = dockerSettings.Docker.Registry.ResourceGroupName,
                    Server = dockerSettings.Docker.Registry.Server,
                    SubscriptionId = dockerSettings.Docker!.Registry.SubscriptionId,
                    TenantId = dockerSettings.Docker.Registry.TenantId,
                    UserName = dockerSettings.Docker.Registry.UserName
                }
            };
        });

        services.AddScoped<IContainerInstanceService, ContainerInstanceService>();
    }

    private static void AddKeyVaultService(IServiceCollection services)
    {
        services.AddScoped<IKeyVaultService, KeyVaultService>();
    }

    private static void AddRetryPolicyService(IServiceCollection services)
    {
        RetryPolicyServiceSettings retryPolicyServiceSettings = new();

        var retryPolicySection = GetRetryPolicySection;

        if (runningEnvironment == RunningEnvironment.Local)
        {
            config!.Bind(retryPolicyServiceSettings.ConfigurationKey, retryPolicyServiceSettings);
        }
        else
        {
            retryPolicyServiceSettings.MaxRetries = Convert.ToInt32(retryPolicySection["MaxRetries"]);
            retryPolicyServiceSettings.DelayMilliseconds = Convert.ToInt32(retryPolicySection["DelayMilliseconds"]);
            retryPolicyServiceSettings.MaxDelayMilliseconds = Convert.ToInt32(retryPolicySection["MaxDelayMilliseconds"]);
        }

        services.Configure<RetryPolicyServiceSettings>(options =>
        {
            options.MaxRetries = retryPolicyServiceSettings.MaxRetries;
            options.DelayMilliseconds = retryPolicyServiceSettings.DelayMilliseconds;
            options.MaxDelayMilliseconds = retryPolicyServiceSettings.MaxDelayMilliseconds;
        });

        services.AddScoped<IRetryPolicyService, RetryPolicyService>();
    }

    private static void AddServiceBusService(IServiceCollection services)
    {
        ServiceBusSettings serviceBusSettings = new();

        var serviceBusSection = config!.GetSection("Azure:ServiceBus")
            ?? throw new ArgumentException("Cannot bind the Azure Service Bus configuration section.");

        if (runningEnvironment == RunningEnvironment.Local)
        {
            azureSection!.Bind(serviceBusSettings.ConfigurationKey, serviceBusSettings);
        }
        else
        {
            serviceBusSettings.ConnectionString = config!["Azure:ServiceBus:ConnectionString"]!;
            serviceBusSettings.TranscriptQueue = config["Azure:ServiceBus:TranscriptQueue"]!;
        }

        string serviceName = "Azure Service Bus:";

        var error = serviceBusSettings switch
        {
            { ConnectionString: null or "" } => $"{serviceName} connection string is required",
            { TranscriptQueue: null or "" } => $"{serviceName} transcript queue is required",
            _ => null
        };

        if (error != null)
        {
            throw new ArgumentException($"{thisClass} {error}");
        }

        services.Configure<ServiceBusSettings>(options =>
        {
            options.ConnectionString = serviceBusSettings.ConnectionString!;
            options.TranscriptQueue = serviceBusSettings.TranscriptQueue!;
        });

        services.AddScoped<IServiceBusService, ServiceBusService>();
    }

    private static void AddStorageService(IServiceCollection services)
    {
        StorageSettings storageSettings = new();

        var storageSection = config!.GetSection("Storage")
            ?? throw new ArgumentException("Cannot bind the Storage configuration section.");

        if (runningEnvironment == RunningEnvironment.Local)
        {
            azureSection!.Bind(storageSettings.ConfigurationKey, storageSettings);
        }
        else
        {
            storageSettings.ConnectionString = storageSection["ConnectionString"]!;
            storageSettings.ApiVersion = storageSection["ApiVersion"]!;
        }

        string serviceName = "Blob Storage:";

        var error = storageSettings switch
        {
            { ApiVersion: null or "" } => $"{serviceName} API version is required",
            { ConnectionString: null or "" } => $"{serviceName} private connection string is required",
            _ => null
        };

        if (error != null)
        {
            throw new ArgumentException($"{thisClass} {error}");
        }

        services.Configure<StorageSettings>(options =>
        {
            options.ConnectionString = storageSettings.ConnectionString!;
            options.ApiVersion = storageSettings.ApiVersion!;
        });

        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<ITableStorageService, TableStorageService>();
        services.AddScoped<IBlobStoragePushService, BlobStoragePushService>();
    }

    private static IConfigurationSection GetRetryPolicySection => config!.GetSection("RetryPolicy")
    ?? throw new ArgumentException("Cannot bind the Retry Policy configuration section.");

    private enum RunningEnvironment
    {
        AzureFn,
        AzureWebApp,
        Local
    }
}

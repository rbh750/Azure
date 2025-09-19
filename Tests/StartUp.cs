using Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

// https://github.com/pengweiqhca/Xunit.DependencyInjection
public class Startup
{
    private const string fileName = "appsettings.json";

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddCommonServiceModules(fileName);
    }
}

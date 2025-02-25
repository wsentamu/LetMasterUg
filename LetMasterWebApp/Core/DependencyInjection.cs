using LetMasterWebApp.Services;
using System.Reflection;

namespace LetMasterWebApp.Core;
public static class DependencyInjection
{
    private static bool Skippable(MemberInfo type, IEnumerable<string> exclude)
    {
        var exists=exclude.Any(t=>type.Name.Contains(t));
        return exists;
    }
    public static void ConfigureDependencyInjection(this IServiceCollection services, IEnumerable<Assembly> assemblies, List<string>? toSkip = null)
    {
        var typesToSkip = new List<string>
            {
                "ApiClient",
                "ServiceBase",
                "GenericRepository",
                "VendorResponse"
            };
        if (toSkip != null)
        {
            typesToSkip.AddRange(toSkip);
        }
        var skip = typesToSkip.ToHashSet();
        foreach (var assembly in assemblies)
        {
            var types = assembly
                .GetExportedTypes()
                .Where(it => it.IsClass && !Skippable(it, skip))
                .Select(it =>
                {
                    // get interface class, with similar name
                    var inter = it
                        .GetInterfaces()
                        .FirstOrDefault(ot => ot.Name.Contains(it.Name));
                    // Return a pair
                    return (classType: it, interfaceType: inter);
                })
                //Remove types without interfaces
                .Where(it => it.interfaceType != null);

            foreach (var (classType, interfaceType) in types)
            {
                Console.WriteLine($@"Service: {classType.FullName} impl:{interfaceType!.FullName}");
                services.AddScoped(interfaceType, classType);
            }
        }
        //Backgound service addition
        services.AddSingleton<TenantBillingService>();
        services.AddHostedService(provider => provider.GetRequiredService<TenantBillingService>());
    }
    public static void ConfigureAutoMapper(this WebApplicationBuilder builder, params Assembly[] assemblies)
    {
        builder.Services.AddAutoMapper(config =>
        {
            var exportedTypes = assemblies.SelectMany(it => it.GetExportedTypes()).ToList();
            var viewModels = exportedTypes
                .Where(it => it.IsClass && (
                    it.Name.EndsWith("ViewModel") ||
                    it.Name.EndsWith("UpdateModel") ||
                    it.Name.EndsWith("CreateModel")
                ))
                .ToList();
            var viewNames = viewModels.Select(it => it.Name);
            var modelNames = viewNames
                .Where(it => it.EndsWith("ViewModel"))
                .Select(it => it.Replace("ViewModel", ""))
                .ToList();
            var models = exportedTypes
                .Where(it => it.IsClass && modelNames.Contains(it.Name))
                .ToList();
            viewModels.ForEach(viewModel =>
            {
                var modelName = viewModel.Name
                    .Replace("ViewModel", "")
                    .Replace("UpdateModel", "")
                    .Replace("CreateModel", "");
                var model = models.FirstOrDefault(m => m.Name == modelName);
                if (model == null) return;

                Console.WriteLine($@"AutoMapper >> Model:{model.Name}, View:{viewModel.Name}");
                config.CreateMap(model, viewModel);
                config.CreateMap(viewModel, model);
            });
        }, assemblies);
    }
}

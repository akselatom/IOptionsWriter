﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace IOptionsWriter;

public static class ServiceCollectionExtensions
{
    /// <param name="forceReloadAfterWrite">If you have some problem with config file reloading</param>
    public static IServiceCollection ConfigureWritable<TOptions>(this IServiceCollection services,
        string sectionName = null, string settingsFile = "appsettings.json", bool forceReloadAfterWrite = false)
        where TOptions : class
    {
        sectionName??=typeof(TOptions).Name;

        services.AddOptions();
        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(provider =>
        {
            var configurationSection = provider.GetRequiredService<IConfiguration>().GetSection(sectionName);
            return new ConfigurationChangeTokenSource<TOptions>(typeof(TOptions).Name, configurationSection);
        });
        services.AddSingleton<IConfigureOptions<TOptions>>(provider =>
        {
            var configurationSection = provider.GetRequiredService<IConfiguration>().GetSection(sectionName);
            return new NamedConfigureFromConfigurationOptions<TOptions>(sectionName, configurationSection,
                _ => { });
        });
        services.AddSingleton<IOptionsWritable<TOptions>>(provider =>
        {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            var options = provider.GetRequiredService<IOptionsMonitor<TOptions>>();
            var configurationRoot = (IConfigurationRoot) provider.GetRequiredService<IConfiguration>();
            return new OptionsWritable<TOptions>(environment, options, configurationRoot, sectionName, settingsFile,
                forceReloadAfterWrite);
        });

        return services;
    }
}
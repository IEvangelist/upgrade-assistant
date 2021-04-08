// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class UpgraderExtensions
    {
        public static void AddStepManagement(this IServiceCollection services)
        {
            services.AddScoped<UpgraderManager>();
            services.AddTransient<IUpgradeStepOrderer, UpgradeStepOrderer>();
            services.AddTransient<ITargetTFMSelector, TargetTFMSelector>();
            services.AddReadinessChecks();
            services.AddContextTelemetry();
        }

        public static void AddContextTelemetry(this IServiceCollection services)
        {
            services.AddSingleton<UpgradeContextTelemetry>();
            services.AddTransient<ITelemetryInitializer>(ctx => ctx.GetRequiredService<UpgradeContextTelemetry>());
            services.AddTransient<IUpgradeContextAccessor>(ctx => ctx.GetRequiredService<UpgradeContextTelemetry>());
        }

        public static void AddReadinessChecks(this IServiceCollection services)
        {
            services.AddTransient<IUpgradeReadyCheck, CanLoadProjectFile>();
            services.AddTransient<IUpgradeReadyCheck, CentralPackageManagementCheck>();
            services.AddTransient<IUpgradeReadyCheck, TargetFrameworkCheck>();
        }
    }
}

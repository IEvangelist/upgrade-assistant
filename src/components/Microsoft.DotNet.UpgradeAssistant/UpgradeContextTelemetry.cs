// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UpgradeContextTelemetry : ITelemetryInitializer, IUpgradeContextAccessor
    {
        private readonly AsyncLocal<IUpgradeContext?> _context = new AsyncLocal<IUpgradeContext?>();

        public IUpgradeContext? Current
        {
            get => _context.Value;
            set => _context.Value = value;
        }

        public void Initialize(ApplicationInsights.Channel.ITelemetry telemetry)
        {
            if (telemetry is not ISupportProperties t)
            {
                return;
            }

            if (Current is not IUpgradeContext context)
            {
                return;
            }

            TryAdd(t.Properties, "Permanent Solution Id", context.PermanentSolutionId);
            TryAdd(t.Properties, "Solution Id", context.SolutionId);
            TryAdd(t.Properties, "Entrypoint Id", context.EntryPoint?.Id);
            TryAdd(t.Properties, "Project Id", context.CurrentProject?.Id);
            TryAdd(t.Properties, "Step Id", context.CurrentStep?.Id);

            static void TryAdd(IDictionary<string, string> dict, string name, string? value)
            {
                if (value is not null && !dict.ContainsKey(name))
                {
                    dict.Add(name, value);
                }
            }
        }
    }
}

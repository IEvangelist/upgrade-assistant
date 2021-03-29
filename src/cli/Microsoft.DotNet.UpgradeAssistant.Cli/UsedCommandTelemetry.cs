// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Console apps don't have a synchronization context")]

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    internal class UsedCommandTelemetry : IUpgradeStartup
    {
        private readonly ITelemetry _telemetry;
        private readonly ParseResult _result;

        public UsedCommandTelemetry(ITelemetry telemetry, ParseResult result)
        {
            _telemetry = telemetry;
            _result = result;
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            foreach (var child in _result.CommandResult.Children)
            {
                var type = child switch
                {
                    ArgumentResult => "argument",
                    OptionResult => "option",
                    _ => "unknown"
                };

                var properties = new Dictionary<string, string>
                {
                    { "Name", child.Symbol.Name },
                    { "Type", type },
                };

                _telemetry.TrackEvent("cli/command", properties);
            }

            return Task.FromResult(true);
        }
    }
}

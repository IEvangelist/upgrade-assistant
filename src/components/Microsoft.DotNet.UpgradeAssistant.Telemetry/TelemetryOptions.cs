// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public class TelemetryOptions
    {
        public string ProductVersion { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        private string ToolName => DisplayName.Replace(" ", string.Empty);

        public string SentinelSuffix => $"dotnet{ToolName}FirstUseSentinel";

        public string UserLevelCache => $"dotnet{ToolName}UserLevelCache";

        public string InstrumentationKey { get; set; } = string.Empty;

        public string TelemetryOptout => $"DOTNET_{ToolName.ToUpperInvariant()}_TELEMTRY_OPTOUT";

        public string ProducerNamespace => $"dotnet/{ToolName}";

        public string DetailsLink { get; set; } = string.Empty;

        public string CurrentSessionId { get; set; } = string.Empty;

        public string SkipFirstTime => $"DOTNET_{ToolName.ToUpperInvariant()}_SKIP_FIRST_TIME_EXPERIENCE";
    }
}

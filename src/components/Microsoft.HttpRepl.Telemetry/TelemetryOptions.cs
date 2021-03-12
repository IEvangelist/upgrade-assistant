// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public class TelemetryOptions
    {
        public string ProductVersion { get; set; } = string.Empty;

        public string InstrumentationKey { get; set; } = "469489a6-628b-4bb9-80db-ec670f70d874";

        public string TelemetryOptout { get; set; } = "DOTNET_HTTPREPL_TELEMETRY_OPTOUT";

        public string CurrentSessionId { get; set; } = string.Empty;
    }
}

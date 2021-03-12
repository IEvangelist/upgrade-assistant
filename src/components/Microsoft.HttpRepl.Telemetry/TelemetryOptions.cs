// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public class TelemetryOptions
    {
        public string ProductVersion { get; set; } = string.Empty;

        public string SentinelSuffix { get; set; } = string.Empty;

        public string UserLevelCache { get; set; } = string.Empty;

        public string InstrumentationKey { get; set; } = string.Empty;

        public string TelemetryOptout { get; set; } = string.Empty;

        public string CurrentSessionId { get; set; } = string.Empty;

        public string SkipFirstTime { get; set; } = string.Empty;
    }
}

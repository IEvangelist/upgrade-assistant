// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class TelemetryExtensions
    {
        public static void TrackProjectProperties(this ITelemetry telemetry, IUpgradeContext context)
        {
            if (telemetry is null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!telemetry.Enabled)
            {
                return;
            }

            foreach (var project in context.Projects)
            {
                try
                {
                    var properties = new PropertyBag
                    {
                        { "outputType", project.OutputType.ToString() },
                        { "tfms", project.TFM.Name },
                        { "components", project.Components.ToString() },
                        { "types", string.Join(";", project.ProjectTypes) },
                    };

                    telemetry.TrackEvent("project", properties);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    telemetry.TrackEvent("project/error", new PropertyBag { { "message", e.Message } });
                }
            }
        }
    }
}

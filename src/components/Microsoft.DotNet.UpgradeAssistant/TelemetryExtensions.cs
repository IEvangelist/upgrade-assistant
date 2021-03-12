// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class TelemetryExtensions
    {
        public static PropertyBag EnrichWithContext(this PropertyBag bag, IUpgradeContext context)
        {
            if (bag is null)
            {
                throw new ArgumentNullException(nameof(bag));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            bag.Add("upgradeId", HashString(context.InputPath));

            if (context.CurrentProject is not null)
            {
                bag.Add("projectId", HashString(context.CurrentProject.FilePath));
            }

            return bag;

            string HashString(string input)
            {
                using var hasher = SHA512.Create();

                var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

                return Convert.ToBase64String(hash);
            }
        }

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

                    telemetry.TrackEvent("projectInfo", properties.EnrichWithContext(context));
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    telemetry.TrackEvent("projectInfoError", new PropertyBag { { "message", e.Message } });
                }
            }
        }
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We don't want any errors in telemetry to cause failures in the product.")]
    [SuppressMessage("Naming", "CA1724: Type names should not match namespaces", Justification = "Keeping it consistent with source implementations.")]
    internal sealed class Telemetry : ITelemetry
    {
        internal static string? CurrentSessionId;

        private TelemetryClient? _client;
        private Dictionary<string, string>? _commonProperties;
        private Dictionary<string, double>? _commonMeasurements;
        private Task _trackEventTask;

        private TelemetryOptions _options;

        public Telemetry(
            IOptions<TelemetryOptions> options,
            IFirstTimeUseNoticeSentinel sentinel,
            string? sessionId = null)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;

            FirstTimeUseNoticeSentinel = sentinel;
            Enabled = !EnvironmentHelper.GetEnvironmentVariableAsBool(_options.TelemetryOptout) && PermissionExists(FirstTimeUseNoticeSentinel);

            if (!Enabled)
            {
                _trackEventTask = Task.CompletedTask;
                return;
            }

            // Store the session ID in a static field so that it can be reused
            CurrentSessionId = sessionId ?? Guid.NewGuid().ToString();

            // Initialize in task to offload to parallel thread
            _trackEventTask = Task.Factory.StartNew(() => InitializeTelemetry(options.Value.ProductVersion), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public bool Enabled { get; }

        public IFirstTimeUseNoticeSentinel FirstTimeUseNoticeSentinel { get; }

        public static bool SkipFirstTimeExperience => EnvironmentHelper.GetEnvironmentVariableAsBool(UpgradeAssistant.Telemetry.FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName, false);

        private static bool PermissionExists(IFirstTimeUseNoticeSentinel? sentinel)
        {
            if (sentinel == null)
            {
                return false;
            }

            return sentinel.Exists();
        }

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, double> measurements)
        {
            if (!Enabled)
            {
                return;
            }

            // Continue task in existing parallel thread
            _trackEventTask = _trackEventTask.ContinueWith(
                x => TrackEventTask(eventName, properties, measurements),
                TaskScheduler.Default);
        }

        private void ThreadBlockingTrackEvent(string eventName, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, double> measurements)
        {
            if (!Enabled)
            {
                return;
            }

            TrackEventTask(eventName, properties, measurements);
        }

        private void InitializeTelemetry(string productVersion)
        {
            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                _client = new TelemetryClient();
#pragma warning restore CS0618 // Type or member is obsolete
                _client.InstrumentationKey = _options.InstrumentationKey;
                _client.Context.Session.Id = CurrentSessionId;
                _client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;

                _commonProperties = new TelemetryCommonProperties(productVersion).GetTelemetryCommonProperties();
                _commonMeasurements = new Dictionary<string, double>();
            }
            catch (Exception e)
            {
                _client = null;

                // We don't want to fail the tool if telemetry fails.
                Debug.Fail(e.ToString());
            }
        }

        private void TrackEventTask(
            string eventName,
            IReadOnlyDictionary<string, string> properties,
            IReadOnlyDictionary<string, double> measurements)
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                var eventProperties = GetEventProperties(properties);
                var eventMeasurements = GetEventMeasures(measurements);

                _client.TrackEvent(PrependProducerNamespace(eventName), eventProperties, eventMeasurements);
                _client.Flush();
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
            }
        }

        private static string PrependProducerNamespace(string eventName)
        {
            return "dotnet/upgradeassistant/" + eventName;
        }

        private Dictionary<string, double> GetEventMeasures(IReadOnlyDictionary<string, double> measurements)
        {
            var eventMeasurements = new Dictionary<string, double>(_commonMeasurements);

            if (measurements != null)
            {
                foreach (KeyValuePair<string, double> measurement in measurements)
                {
                    eventMeasurements[measurement.Key] = measurement.Value;
                }
            }

            return eventMeasurements;
        }

        private Dictionary<string, string>? GetEventProperties(IReadOnlyDictionary<string, string> properties)
        {
            if (properties != null)
            {
                var eventProperties = new Dictionary<string, string>(_commonProperties);

                foreach (KeyValuePair<string, string> property in properties)
                {
                    eventProperties[property.Key] = property.Value;
                }

                return eventProperties;
            }
            else
            {
                return _commonProperties;
            }
        }
    }
}

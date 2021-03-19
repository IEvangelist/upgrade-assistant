// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
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
        private TelemetryClient? _client;
        private Dictionary<string, string>? _commonProperties;
        private Dictionary<string, double>? _commonMeasurements;
        private Task _trackEventTask;

        private TelemetryOptions _options;

        public Telemetry(
            IOptions<TelemetryOptions> options,
            TelemetryCommonProperties commonProperties,
            IFirstTimeUseNoticeSentinel sentinel)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;

            Enabled = !EnvironmentHelper.GetEnvironmentVariableAsBool(_options.TelemetryOptout) && PermissionExists(sentinel);

            if (!Enabled)
            {
                _trackEventTask = Task.CompletedTask;
                return;
            }

            // Initialize in task to offload to parallel thread
            _trackEventTask = Task.Factory.StartNew(() => InitializeTelemetry(commonProperties), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public bool Enabled { get; }

        private static bool PermissionExists(IFirstTimeUseNoticeSentinel? sentinel)
        {
            if (sentinel == null)
            {
                return false;
            }

            return sentinel.Exists();
        }

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string>? properties, IReadOnlyDictionary<string, double>? measurements)
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

        private void InitializeTelemetry(TelemetryCommonProperties commonProperties)
        {
            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                _client = new TelemetryClient();
#pragma warning restore CS0618 // Type or member is obsolete
                _client.InstrumentationKey = _options.InstrumentationKey;
                _client.Context.Session.Id = _options.CurrentSessionId;
                _client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;

                _commonProperties = commonProperties.GetTelemetryCommonProperties();
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
            IReadOnlyDictionary<string, string>? properties,
            IReadOnlyDictionary<string, double>? measurements)
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

        private string PrependProducerNamespace(string eventName) => $"{_options.ProducerNamespace}/{eventName}";

        private Dictionary<string, double> GetEventMeasures(IReadOnlyDictionary<string, double>? measurements)
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

        private Dictionary<string, string>? GetEventProperties(IReadOnlyDictionary<string, string>? properties)
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

        public async ValueTask DisposeAsync()
        {
            _client?.Flush();
            await _trackEventTask.ConfigureAwait(false);
        }

        public IDisposable AddProperty(string name, string value, bool hash)
        {
            if (_commonProperties is null)
            {
                _commonProperties = new Dictionary<string, string>();
            }

            // Continue task in existing parallel thread
            _trackEventTask = _trackEventTask.ContinueWith(
                _ => _commonProperties[name] = hash ? HashString(value) : value,
                TaskScheduler.Default);

            return new RemoveEntry(() =>
            {
                // Continue task in existing parallel thread
                _trackEventTask = _trackEventTask.ContinueWith(
                    _ => _commonProperties?.Remove(name),
                    TaskScheduler.Default);
            });

            static string HashString(string input)
            {
                if (input.Length == 0)
                {
                    return string.Empty;
                }

                using var hasher = SHA512.Create();
                var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hash);
            }
        }

        private sealed class RemoveEntry : IDisposable
        {
            private readonly Action _action;

            public RemoveEntry(Action action)
            {
                _action = action;
            }

            public void Dispose() => _action();
        }
    }
}

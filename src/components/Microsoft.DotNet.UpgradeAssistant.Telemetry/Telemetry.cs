// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    [SuppressMessage("Naming", "CA1724: Type names should not match namespaces", Justification = "Keeping it consistent with source implementations.")]
    internal sealed class Telemetry : ITelemetry
    {
        private readonly IStringHasher _hasher;
        private readonly TelemetryConfiguration? _telemetryConfig;
        private readonly SerializedQueue<TelemetryClient>? _queue;
        private readonly TelemetryOptions _options;

        private Dictionary<string, string>? _commonProperties;
        private Dictionary<string, double>? _commonMeasurements;

        public Telemetry(
            IOptions<TelemetryOptions> options,
            TelemetryCommonProperties commonProperties,
            IStringHasher hasher,
            IFirstTimeUseNoticeSentinel sentinel)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _options = options.Value;

            Enabled = !EnvironmentHelper.GetEnvironmentVariableAsBool(_options.TelemetryOptout) && PermissionExists(sentinel);

            if (!Enabled)
            {
                return;
            }

            _telemetryConfig = TelemetryConfiguration.CreateDefault();
            _queue = new SerializedQueue<TelemetryClient>(() =>
            {
                var client = new TelemetryClient(_telemetryConfig);

                client.InstrumentationKey = _options.InstrumentationKey;
                client.Context.Session.Id = _options.CurrentSessionId;
                client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;

                _commonProperties = commonProperties.GetTelemetryCommonProperties();
                _commonMeasurements = new Dictionary<string, double>();

                return client;
            });
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
            if (!Enabled || _queue is null)
            {
                return;
            }

            _queue.Add(client =>
            {
                var eventProperties = GetEventProperties(properties);
                var eventMeasurements = GetEventMeasures(measurements);

                client.TrackEvent(PrependProducerNamespace(eventName), eventProperties, eventMeasurements);
                client.Flush();
            });
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
            _telemetryConfig?.Dispose();

            if (_queue is not null)
            {
                await _queue.DisposeAsync().ConfigureAwait(false);
            }
        }

        public IDisposable AddProperty(string name, string value, bool hash)
        {
            if (_queue is null)
            {
                return new RemoveEntry(static () => { });
            }

            if (_commonProperties is null)
            {
                _commonProperties = new Dictionary<string, string>();
            }

            _queue.Add(_ => _commonProperties[name] = hash ? _hasher.Hash(value) : value);

            return new RemoveEntry(() => _queue.Add(_ => _commonProperties?.Remove(name)));
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

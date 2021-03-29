// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
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

        public Telemetry(
            IOptions<TelemetryOptions> options,
            IEnumerable<ITelemetryInitializer> initializers,
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

            foreach (var initializer in initializers)
            {
                _telemetryConfig.TelemetryInitializers.Add(initializer);
            }

            _queue = new SerializedQueue<TelemetryClient>(() =>
            {
                var client = new TelemetryClient(_telemetryConfig);

                client.InstrumentationKey = _options.InstrumentationKey;
                client.Context.Session.Id = _options.CurrentSessionId;
                client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;

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

        public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
        {
            if (!Enabled || _queue is null)
            {
                return;
            }

            _queue.Add(client =>
            {
                client.TrackEvent(PrependProducerNamespace(eventName), properties, measurements);
                client.Flush();
            });
        }

        private string PrependProducerNamespace(string eventName) => $"{_options.ProducerNamespace}/{eventName}";

        public async ValueTask DisposeAsync()
        {
            _telemetryConfig?.Dispose();

            if (_queue is not null)
            {
                await _queue.DisposeAsync().ConfigureAwait(false);
            }
        }

        public IDisposable AddHashedProperty(string name, Func<string> value)
            => AddProperty(name, () => _hasher.Hash(value()));

        public IDisposable AddProperty(string name, Func<string> value)
        {
            if (_queue is null)
            {
                return new DelegateDisposable(static () => { });
            }

            var initializer = new CustomPropertyInitializer(name, value);

            _queue.Add(_ => _telemetryConfig?.TelemetryInitializers.Add(initializer));

            return new DelegateDisposable(() => _telemetryConfig?.TelemetryInitializers.Remove(initializer));
        }

        private sealed class DelegateDisposable : IDisposable
        {
            private readonly Action _action;

            public DelegateDisposable(Action action)
            {
                _action = action;
            }

            public void Dispose() => _action();
        }

        private class CustomPropertyInitializer : ITelemetryInitializer
        {
            private readonly string _name;
            private readonly Func<string> _value;

            public CustomPropertyInitializer(string name, Func<string> value)
            {
                _name = name;
                _value = value;
            }

            public void Initialize(ApplicationInsights.Channel.ITelemetry telemetry)
            {
                if (telemetry is ISupportProperties properties)
                {
                    properties.Properties[_name] = _value();
                }
            }
        }
    }
}

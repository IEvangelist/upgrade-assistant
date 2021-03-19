// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public static class TelemetryExtensions
    {
        public static IDisposable TimeEvent(this ITelemetry telemetry, string eventName, IReadOnlyDictionary<string, string>? properties = null, IReadOnlyDictionary<string, double>? measurements = null)
            => new TimedEvent(telemetry, eventName, properties, measurements);

        private class TimedEvent : IDisposable
        {
            private readonly ITelemetry _telemetry;
            private readonly string _eventName;
            private readonly IReadOnlyDictionary<string, string>? _properties;
            private readonly IReadOnlyDictionary<string, double>? _measurements;
            private readonly Stopwatch _stopwatch;

            public TimedEvent(ITelemetry telemetry, string eventName, IReadOnlyDictionary<string, string>? properties, IReadOnlyDictionary<string, double>? measurements)
            {
                _telemetry = telemetry;
                _eventName = eventName;
                _properties = properties;
                _measurements = measurements;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();

                var measurements = new Dictionary<string, double> { { "duration", _stopwatch.ElapsedTicks } };

                if (_measurements is not null)
                {
                    foreach (var m in measurements)
                    {
                        measurements.Add(m.Key, m.Value);
                    }
                }

                _telemetry.TrackEvent(_eventName, _properties, measurements);
            }
        }
    }
}

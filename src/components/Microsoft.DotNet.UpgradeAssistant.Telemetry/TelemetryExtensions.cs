// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public static class TelemetryExtensions
    {
        public static IDisposable TimeEvent(this ITelemetry telemetry, string eventName, PropertyBag? properties = null, MeasurementBag? measurements = null, Action<PropertyBag, MeasurementBag>? onComplete = null)
            => new TimedEvent(telemetry, eventName, properties, measurements, onComplete);

        private class TimedEvent : IDisposable
        {
            private readonly ITelemetry _telemetry;
            private readonly string _eventName;
            private readonly PropertyBag? _properties;
            private readonly MeasurementBag? _measurements;
            private readonly Action<PropertyBag, MeasurementBag>? _onComplete;
            private readonly Stopwatch _stopwatch;

            public TimedEvent(ITelemetry telemetry, string eventName, PropertyBag? properties, MeasurementBag? measurements, Action<PropertyBag, MeasurementBag>? onComplete)
            {
                _telemetry = telemetry;
                _eventName = eventName;
                _properties = properties;
                _measurements = measurements;
                _onComplete = onComplete;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();

                var measurements = _measurements ?? new MeasurementBag();
                var properties = _properties ?? new PropertyBag();

                measurements.Add("Duration", _stopwatch.ElapsedTicks);

                if (_onComplete is not null)
                {
                    _onComplete(properties, measurements);
                }

                _telemetry.TrackEvent(_eventName, properties, measurements);
            }
        }
    }
}

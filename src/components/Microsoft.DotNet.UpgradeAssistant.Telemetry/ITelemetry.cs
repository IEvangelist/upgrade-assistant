// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public interface ITelemetry : IAsyncDisposable
    {
        bool Enabled { get; }

        IDisposable AddProperty(string name, Func<string> value);

        IDisposable AddHashedProperty(string name, Func<string> value);

        void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? measurements = null);
    }
}

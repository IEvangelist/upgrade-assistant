// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public interface ITelemetry : IAsyncDisposable
    {
        bool Enabled { get; }

        IDisposable AddProperty(string name, string value, bool hash = true);

        void TrackEvent(string eventName, IReadOnlyDictionary<string, string>? properties = null, IReadOnlyDictionary<string, double>? measurements = null);
    }
}

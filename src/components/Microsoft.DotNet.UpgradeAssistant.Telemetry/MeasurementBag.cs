// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public class MeasurementBag : ValueBag<double>
    {
        public MeasurementBag()
        {
        }

        public MeasurementBag(IReadOnlyDictionary<string, double>? other)
            : base(other)
        {
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public class PropertyBag : ValueBag<string>
    {
        public PropertyBag()
        {
        }

        public PropertyBag(IReadOnlyDictionary<string, string>? other)
            : base(other)
        {
        }
    }
}

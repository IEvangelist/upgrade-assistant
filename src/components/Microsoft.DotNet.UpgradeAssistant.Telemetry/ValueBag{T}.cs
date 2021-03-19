// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public abstract class ValueBag<T> : Dictionary<string, T>
    {
        protected ValueBag()
            : base()
        {
        }

        protected ValueBag(IReadOnlyDictionary<string, T>? other)
        {
            AddAll(other);
        }

        public void AddAll(IReadOnlyDictionary<string, T>? other)
        {
            if (other is null)
            {
                return;
            }

            foreach (var item in other)
            {
                this[item.Key] = item.Value;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant
{
    internal class KnownStepsHasher : Sha256Hasher
    {
        private readonly Lazy<HashSet<string>> _knownSteps;

        public KnownStepsHasher(Lazy<IUpgradeStepOrderer> steps)
        {
            // Lazily create this so that it will be accessed after MSBuild is registered.
            _knownSteps = new(() => new(steps.Value.UpgradeSteps.Select(s => s.Id), StringComparer.Ordinal));
        }

        public override string Hash(string text)
        {
            if (_knownSteps.Value.Contains(text))
            {
                return text;
            }

            return base.Hash(text);
        }
    }
}

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
        private readonly HashSet<string> _knownSteps;

        public KnownStepsHasher(IUpgradeStepOrderer steps)
        {
            _knownSteps = new HashSet<string>(steps.UpgradeSteps.Select(s => s.Id), StringComparer.Ordinal);
        }

        public override string Hash(string text)
        {
            if (_knownSteps.Contains(text))
            {
                return text;
            }

            return base.Hash(text);
        }
    }
}

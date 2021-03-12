// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class ConsoleTelemetryOptIn : IUpgradeStartup
    {
        private readonly IFirstTimeUseNoticeSentinel _sentinel;
        private readonly IUserInput _input;

        public ConsoleTelemetryOptIn(IFirstTimeUseNoticeSentinel sentinel, IUserInput input)
        {
            _sentinel = sentinel ?? throw new ArgumentNullException(nameof(sentinel));
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public async Task<bool> StartupAsync(CancellationToken token)
        {
            if (!_sentinel.Exists())
            {
                var result = await _input.ChooseAsync("Allow gathering telemetry", UpgradeCommand.CreateFromEnum<Options>(), token);

                if (result.Value == Options.Yes)
                {
                    _sentinel.CreateIfNotExists();
                }
            }

            return true;
        }

        private enum Options
        {
            Yes,
            No
        }
    }
}

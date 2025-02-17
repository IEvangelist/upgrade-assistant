﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.MSBuild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant.Fixtures
{
    public class MSBuildRegistrationFixture
    {
        public MSBuildRegistrationFixture()
        {
            // Register MSBuild
            var msBuildRegistrar = new MSBuildRegistrationStartup(new NullLogger<MSBuildRegistrationStartup>(), MSBuildPath.MSBuildPathInstance.Locator);
            msBuildRegistrar.RegisterMSBuildInstance();
        }
    }
}

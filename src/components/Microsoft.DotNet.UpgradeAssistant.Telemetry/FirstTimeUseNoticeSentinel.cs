// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public sealed class FirstTimeUseNoticeSentinel : IFirstTimeUseNoticeSentinel
    {
        private readonly TelemetryOptions _options;
        private readonly IFileManager _fileManager;
        private readonly string _sentinel;
        private readonly string _dotnetTryUserProfileFolderPath;

        private string SentinelPath => Path.Combine(_dotnetTryUserProfileFolderPath, _sentinel);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "IOptions is always set")]
        public FirstTimeUseNoticeSentinel(IOptions<TelemetryOptions> options, IFileManager fileManager)
        {
            _options = options.Value;
            _fileManager = fileManager;
            _sentinel = $"{_options.ProductVersion}.{_options.SentinelSuffix}";
            _dotnetTryUserProfileFolderPath = Paths.DotnetUserProfileFolderPath;
        }

        public bool SkipFirstTimeExperience => EnvironmentHelper.GetEnvironmentVariableAsBool(_options.SkipFirstTime, false);

        public string Title => LocalizedStrings.Title;

        public string DisclosureText => string.Format(CultureInfo.InvariantCulture, LocalizedStrings.CollectionDisclosure, _options.TelemetryOptout, _options.DisplayName, _options.DetailsLink);

        public bool Exists() => _fileManager.FileExists(SentinelPath);

        public void CreateIfNotExists()
        {
            if (!Exists())
            {
                if (!_fileManager.DirectoryExists(_dotnetTryUserProfileFolderPath))
                {
                    _fileManager.CreateDirectory(_dotnetTryUserProfileFolderPath);
                }

                _fileManager.WriteAllBytes(SentinelPath, Array.Empty<byte>());
            }
        }
    }
}

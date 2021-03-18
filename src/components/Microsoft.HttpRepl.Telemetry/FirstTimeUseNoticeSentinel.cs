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
        private readonly string _sentinel;
        private readonly string _dotnetTryUserProfileFolderPath;
        private readonly Func<string, bool> _fileExists;
        private readonly Func<string, bool> _directoryExists;
        private readonly Action<string> _createDirectory;
        private readonly Action<string> _createEmptyFile;

        private string SentinelPath => Path.Combine(_dotnetTryUserProfileFolderPath, _sentinel);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "IOptions is always set")]
        public FirstTimeUseNoticeSentinel(IOptions<TelemetryOptions> options)
            : this(
                options.Value,
                Paths.DotnetUserProfileFolderPath,
                File.Exists,
                Directory.Exists,
                path => Directory.CreateDirectory(path),
                path => File.WriteAllBytes(path, Array.Empty<byte>()))
        {
        }

        private FirstTimeUseNoticeSentinel(
            TelemetryOptions options,
            string dotnetTryUserProfileFolderPath,
            Func<string, bool> fileExists,
            Func<string, bool> directoryExists,
            Action<string> createDirectory,
            Action<string> createEmptyFile)
        {
            _options = options;
            _sentinel = $"{options.ProductVersion}.{options.SentinelSuffix}";
            _dotnetTryUserProfileFolderPath = dotnetTryUserProfileFolderPath;
            _fileExists = fileExists;
            _directoryExists = directoryExists;
            _createDirectory = createDirectory;
            _createEmptyFile = createEmptyFile;
        }

        public bool SkipFirstTimeExperience => EnvironmentHelper.GetEnvironmentVariableAsBool(_options.SkipFirstTime, false);

        public string Title => LocalizedStrings.Title;

        public string DisclosureText => string.Format(CultureInfo.InvariantCulture, LocalizedStrings.CollectionDisclosure, _options.TelemetryOptout, _options.DisplayName, _options.DetailsLink);

        public bool Exists()
        {
            return _fileExists(SentinelPath);
        }

        public void CreateIfNotExists()
        {
            if (!Exists())
            {
                if (!_directoryExists(_dotnetTryUserProfileFolderPath))
                {
                    _createDirectory(_dotnetTryUserProfileFolderPath);
                }

                _createEmptyFile(SentinelPath);
            }
        }
    }
}

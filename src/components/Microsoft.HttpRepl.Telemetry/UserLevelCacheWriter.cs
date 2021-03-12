// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public sealed class UserLevelCacheWriter : IUserLevelCacheWriter
    {
        private readonly TelemetryOptions _options;
        private readonly string _dotnetUpgradeAssistantUserProfileFolderPath;
        private readonly Func<string, bool> _fileExists;
        private readonly Func<string, bool> _directoryExists;
        private readonly Action<string> _createDirectory;
        private readonly Action<string, string> _writeAllText;
        private readonly Func<string, string> _readAllText;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Options are always provided")]
        public UserLevelCacheWriter(IOptions<TelemetryOptions> options)
            : this(
                options.Value,
                Paths.DotnetUserProfileFolderPath,
                File.Exists,
                Directory.Exists,
                path => Directory.CreateDirectory(path),
                File.WriteAllText,
                File.ReadAllText)
        {
        }

        private UserLevelCacheWriter(
            TelemetryOptions options,
            string dotnetUserProfileFolderPath,
            Func<string, bool> fileExists,
            Func<string, bool> directoryExists,
            Action<string> createDirectory,
            Action<string, string> writeAllText,
            Func<string, string> readAllText)
        {
            _options = options;
            _dotnetUpgradeAssistantUserProfileFolderPath = dotnetUserProfileFolderPath;
            _fileExists = fileExists;
            _directoryExists = directoryExists;
            _createDirectory = createDirectory;
            _writeAllText = writeAllText;
            _readAllText = readAllText;
        }

        public string RunWithCache(string cacheKey, Func<string> getValueToCache)
        {
            _ = getValueToCache ?? throw new ArgumentNullException(nameof(getValueToCache));

            var cacheFilepath = GetCacheFilePath(cacheKey);
            try
            {
                if (!_fileExists(cacheFilepath))
                {
                    if (!_directoryExists(_dotnetUpgradeAssistantUserProfileFolderPath))
                    {
                        _createDirectory(_dotnetUpgradeAssistantUserProfileFolderPath);
                    }

                    var runResult = getValueToCache();

                    _writeAllText(cacheFilepath, runResult);
                    return runResult;
                }
                else
                {
                    return _readAllText(cacheFilepath);
                }
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException
                    || ex is PathTooLongException
                    || ex is IOException)
                {
                    return getValueToCache();
                }

                throw;
            }
        }

        private string GetCacheFilePath(string cacheKey)
        {
            return Path.Combine(_dotnetUpgradeAssistantUserProfileFolderPath, $"{_options.ProductVersion}_{cacheKey}.{_options.UserLevelCache}");
        }
    }
}

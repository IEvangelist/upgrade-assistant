﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class FileUpgradeStateFactory : IUpgradeStateManager
    {
        private readonly string _path;
        private readonly ILogger<FileUpgradeStateFactory> _logger;

        public FileUpgradeStateFactory(
            UpgradeOptions options,
            ILogger<FileUpgradeStateFactory> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _path = Path.Combine(options.Project.DirectoryName!, ".upgrade-assistant");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LoadStateAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var state = await GetStateAsync(token).ConfigureAwait(false);

            if (state is not null)
            {
                context.EntryPoints = state.EntryPoints.Select(e => FindProject(e)).Where(e => e != null)!;
                context.SetCurrentProject(FindProject(state.CurrentProject));
                foreach (var item in state.Properties)
                {
                    context.Properties.SetPropertyValue(item.Key, item.Value, true);
                }
            }

            IProject? FindProject(string? path)
                => path is null ? null : context.Projects.FirstOrDefault(p => NormalizePath(p.FileInfo.FullName) == path);
        }

        private async ValueTask<UpgradeState?> GetStateAsync(CancellationToken token)
        {
            if (File.Exists(_path))
            {
                _logger.LogInformation("Loading upgrade progress file at {Path}", _path);

                using var stream = File.OpenRead(_path);

                try
                {
                    var result = await JsonSerializer.DeserializeAsync<UpgradeState>(stream, cancellationToken: token).ConfigureAwait(false);

                    if (result is not null)
                    {
                        return result;
                    }

                    _logger.LogWarning("Contents of state file were empty.");
                }
                catch (JsonException e)
                {
                    _logger.LogWarning(e, "Could not deserialize upgrade progress.");
                }
            }

            return null;
        }

        public async Task SaveStateAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger.LogInformation("Saving upgrade progress file at {Path}", _path);

            using var stream = File.OpenWrite(_path);
            stream.SetLength(0);

            var state = new UpgradeState
            {
                EntryPoints = context.EntryPoints.Select(e => NormalizePath(e.FileInfo)),
                CurrentProject = NormalizePath(context.CurrentProject?.FileInfo),
                Properties = context.Properties.GetPersistentPropertyValues().ToDictionary(k => k.Key, v => v.Value)
            };

            await JsonSerializer.SerializeAsync(stream, state, cancellationToken: token).ConfigureAwait(false);
        }

        private static string NormalizePath(FileInfo? file) => file is null ? string.Empty : file.Name;

        private static string NormalizePath(string? path) => path is null ? string.Empty : Path.GetFileName(path);

        private class UpgradeState
        {
            public string Build { get; set; } = Constants.Version;

            public string? CurrentProject { get; set; }

            public IEnumerable<string> EntryPoints { get; set; } = Enumerable.Empty<string>();

            public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        }
    }
}

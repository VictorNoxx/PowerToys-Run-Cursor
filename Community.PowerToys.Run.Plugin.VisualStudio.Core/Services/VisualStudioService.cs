// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.VisualStudio.Core.Models;
using Community.PowerToys.Run.Plugin.VisualStudio.Models.Json;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Services
{
    public class VisualStudioService
    {
        private const string VsWhereDir = @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer";
        private const string VsWhereBin = "vswhere.exe";
        private const string VisualStudioDataDir = @"%LOCALAPPDATA%\Microsoft\VisualStudio";

        private readonly ILogger _logger;
        private readonly List<VisualStudioInstance> _instances;
        private readonly VSCodeWorkspacesApi _vsCodeWorkspacesApi;

        public ReadOnlyCollection<VisualStudioInstance> Instances => _instances.AsReadOnly();

        public VisualStudioService(ILogger logger)
        {
            _logger = logger;
            _instances = [];
            _vsCodeWorkspacesApi = new VSCodeWorkspacesApi(logger);
        }

        public void InitInstances(string[] excludedVersions)
        {
            _instances.Clear();

            // Discover Visual Studio instances
            DiscoverVisualStudioInstances(excludedVersions);

            // Discover VS Code projects using the comprehensive API
            DiscoverVsCodeProjects();
        }

        public IEnumerable<CodeContainer> GetResults(bool showPrerelease)
        {
            if (_instances == null)
            {
                return Enumerable.Empty<CodeContainer>();
            }

            var query = _instances.AsEnumerable();

            if (!showPrerelease)
            {
                query = query.Where(i => !i.IsPrerelease);
            }

            return query.SelectMany(i => i.GetCodeContainers()).OrderBy(c => c.Name).ThenBy(c => c.Instance.IsPrerelease);
        }

        private void DiscoverVisualStudioInstances(string[] excludedVersions)
        {
            var paths = new string?[] { null, VsWhereDir };
            var exceptions = new List<(string? Path, Exception Exception)>(paths.Length);

            foreach (var path in paths)
            {
                try
                {
                    var vsWherePath = VsWhereBin;

                    if (path != null)
                    {
                        vsWherePath = Path.Combine(path, VsWhereBin);
                    }

                    vsWherePath = Environment.ExpandEnvironmentVariables(vsWherePath);

                    var startInfo = new ProcessStartInfo(vsWherePath, "-all -prerelease -format json")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    };

                    using var process = Process.Start(startInfo);
                    if (process == null)
                    {
                        continue;
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(TimeSpan.FromSeconds(5));
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        continue;
                    }

                    var instancesJson = JsonSerializer.Deserialize(output, VisualStudioInstanceSerializerContext.Default.ListVisualStudioInstance);
                    if (instancesJson == null)
                    {
                        continue;
                    }

                    foreach (var instance in instancesJson)
                    {
                        var applicationPrivateSettingsPath = GetApplicationPrivateSettingsPathByInstanceId(instance.InstanceId);
                        if (string.IsNullOrWhiteSpace(applicationPrivateSettingsPath))
                        {
                            continue;
                        }

                        if (excludedVersions.Contains(instance.Catalog.ProductLineVersion))
                        {
                            continue;
                        }

                        _instances.Add(new VisualStudioInstance(instance, applicationPrivateSettingsPath));
                    }

                    break;
                }
                catch (Exception ex)
                {
                    exceptions.Add((path, ex));
                }
            }

            // Log errors only if no Visual Studio instances are found
            if (!_instances.Any(i => !string.IsNullOrEmpty(i.ApplicationPrivateSettingsPath)))
            {
                foreach (var ex in exceptions)
                {
                    _logger.LogError(ex.Exception, $"Failed to execute vswhere.exe from {ex.Path ?? "PATH"}", typeof(VisualStudioService));
                }
            }
        }

        private void DiscoverVsCodeProjects()
        {
            try
            {
                var workspaces = _vsCodeWorkspacesApi.GetWorkspaces();
                var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var workspace in workspaces)
                {
                    if (workspace.WorkspaceEnvironment == WorkspaceEnvironment.Local && processedPaths.Add(workspace.RelativePath))
                    {
                        var vsCodeInstance = new VsCodeProjectInstance(workspace.RelativePath);
                        _instances.Add(vsCodeInstance);
                        _logger.LogInformation($"Added VS Code workspace: {workspace.RelativePath} ({workspace.FolderName})", typeof(VisualStudioService));
                    }
                }

                _logger.LogInformation($"Total VS Code projects/files discovered: {processedPaths.Count}", typeof(VisualStudioService));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover VS Code projects", typeof(VisualStudioService));
            }
        }

        private string? GetApplicationPrivateSettingsPathByInstanceId(string instanceId)
        {
            var dataPath = Environment.ExpandEnvironmentVariables(VisualStudioDataDir);
            var directory = Directory.EnumerateDirectories(dataPath, $"*{instanceId}", SearchOption.TopDirectoryOnly)
                .Select(d => new DirectoryInfo(d))
                .Where(d => !d.Name.StartsWith("SettingsBackup_", StringComparison.Ordinal))
                .ToArray();

            if (directory.Length == 1)
            {
                var applicationPrivateSettingspath = Path.Combine(directory[0].FullName, "ApplicationPrivateSettings.xml");

                if (File.Exists(applicationPrivateSettingspath))
                {
                    return applicationPrivateSettingspath;
                }
            }

            _logger.LogError($"Failed to find ApplicationPrivateSettings.xml for instance {instanceId}", typeof(VisualStudioService));

            return null;
        }
    }
}

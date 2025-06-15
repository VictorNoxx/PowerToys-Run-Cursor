// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.VisualStudio.Core.Models;
using Microsoft.Data.Sqlite;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Services
{
    public class VSCodeWorkspacesApi
    {
        private readonly ILogger _logger;

        public VSCodeWorkspacesApi(ILogger logger)
        {
            _logger = logger;
        }

        public List<VSCodeWorkspace> GetWorkspaces()
        {
            var results = new List<VSCodeWorkspace>();
            var userAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var vsCodePaths = new[]
            {
                Path.Combine(userAppDataPath, "Code"),
                Path.Combine(userAppDataPath, "Code - Insiders"),
                Path.Combine(userAppDataPath, "VSCodium"),
            };

            foreach (var vsCodePath in vsCodePaths)
            {
                if (!Directory.Exists(vsCodePath))
                {
                    continue;
                }

                _logger.LogInformation($"Checking VS Code path: {vsCodePath}", typeof(VSCodeWorkspacesApi));

                // storage.json contains opened Workspaces
                var vscode_storage = Path.Combine(vsCodePath, "User", "globalStorage", "storage.json");

                // User/globalStorage/state.vscdb - history.recentlyOpenedPathsList - vscode v1.64 or later
                var vscode_storage_db = Path.Combine(vsCodePath, "User", "globalStorage", "state.vscdb");

                if (File.Exists(vscode_storage))
                {
                    var storageResults = GetWorkspacesInJson(vscode_storage);
                    results.AddRange(storageResults);
                    _logger.LogInformation($"Found {storageResults.Count} workspaces in storage.json at {vscode_storage}", typeof(VSCodeWorkspacesApi));
                }

                if (File.Exists(vscode_storage_db))
                {
                    var storageDbResults = GetWorkspacesInVscdb(vscode_storage_db);
                    results.AddRange(storageDbResults);
                    _logger.LogInformation($"Found {storageDbResults.Count} workspaces in state.vscdb at {vscode_storage_db}", typeof(VSCodeWorkspacesApi));
                }
            }

            _logger.LogInformation($"Total VS Code workspaces found: {results.Count}", typeof(VSCodeWorkspacesApi));
            return results;
        }

        private VSCodeWorkspace? ParseVSCodeUriAndAuthority(string? uri, string? authority, bool isWorkspace = false)
        {
            if (uri is null)
            {
                return null;
            }

            var rfc3986Uri = Rfc3986Uri.Parse(Uri.UnescapeDataString(uri));
            if (rfc3986Uri is null)
            {
                return null;
            }

            var (workspaceEnv, machineName) = ParseVSCodeAuthority.GetWorkspaceEnvironment(authority ?? rfc3986Uri.Authority);
            if (workspaceEnv is null)
            {
                return null;
            }

            var path = rfc3986Uri.Path;

            // Remove preceding '/' from local (Windows) path
            if (workspaceEnv == WorkspaceEnvironment.Local)
            {
                path = path[1..];
            }

            if (!DoesPathExist(path, workspaceEnv.Value))
            {
                return null;
            }

            var folderName = Path.GetFileName(path);

            // Check we haven't returned '' if we have a path like C:\
            if (string.IsNullOrEmpty(folderName))
            {
                DirectoryInfo dirInfo = new(path);
                folderName = dirInfo.Name.TrimEnd(':');
            }

            return new VSCodeWorkspace()
            {
                Path = uri,
                WorkspaceType = isWorkspace ? WorkspaceType.WorkspaceFile : WorkspaceType.ProjectFolder,
                RelativePath = path,
                FolderName = folderName,
                ExtraInfo = machineName,
                WorkspaceEnvironment = workspaceEnv ?? default,
            };
        }

        private bool DoesPathExist(string path, WorkspaceEnvironment workspaceEnv)
        {
            if (workspaceEnv == WorkspaceEnvironment.Local)
            {
                return Directory.Exists(path) || File.Exists(path);
            }

            // If the workspace environment is not Local or WSL, assume the path exists
            return true;
        }

        private List<VSCodeWorkspace> GetWorkspacesInJson(string filePath)
        {
            var storageFileResults = new List<VSCodeWorkspace>();

            try
            {
                var fileContent = File.ReadAllText(filePath);
                var vscodeStorageFile = JsonSerializer.Deserialize(fileContent, VSCodeJsonContext.Default.VSCodeStorageFile);

                if (vscodeStorageFile?.OpenedPathsList != null)
                {
                    // for previous versions of vscode
                    if (vscodeStorageFile.OpenedPathsList.Workspaces3 != null)
                    {
                        foreach (var workspaceUri in vscodeStorageFile.OpenedPathsList.Workspaces3)
                        {
                            var workspace = ParseVSCodeUriAndAuthority(workspaceUri, null);
                            if (workspace != null)
                            {
                                storageFileResults.Add(workspace);
                            }
                        }
                    }

                    // vscode v1.55.0 or later
                    if (vscodeStorageFile.OpenedPathsList.Entries != null)
                    {
                        foreach (var entry in vscodeStorageFile.OpenedPathsList.Entries)
                        {
                            bool isWorkspaceFile = false;
                            var uri = entry.FolderUri;
                            if (entry.Workspace?.ConfigPath != null)
                            {
                                isWorkspaceFile = true;
                                uri = entry.Workspace.ConfigPath;
                            }

                            var workspace = ParseVSCodeUriAndAuthority(uri, entry.RemoteAuthority, isWorkspaceFile);
                            if (workspace != null)
                            {
                                storageFileResults.Add(workspace);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to deserialize {filePath}", typeof(VSCodeWorkspacesApi));
            }

            return storageFileResults;
        }

        private List<VSCodeWorkspace> GetWorkspacesInVscdb(string filePath)
        {
            var dbFileResults = new List<VSCodeWorkspace>();
            SqliteConnection? sqliteConnection = null;
            try
            {
                sqliteConnection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly;");
                sqliteConnection.Open();

                if (sqliteConnection.State == System.Data.ConnectionState.Open)
                {
                    var sqlite_cmd = sqliteConnection.CreateCommand();
                    sqlite_cmd.CommandText = "SELECT value FROM ItemTable WHERE key LIKE 'history.recentlyOpenedPathsList'";

                    var sqlite_datareader = sqlite_cmd.ExecuteReader();

                    if (sqlite_datareader.Read())
                    {
                        string entries = sqlite_datareader.GetString(0);
                        if (!string.IsNullOrEmpty(entries))
                        {
                            var vscodeStorageEntries = JsonSerializer.Deserialize(entries, VSCodeJsonContext.Default.VSCodeStorageEntries);
                            if (vscodeStorageEntries?.Entries != null)
                            {
                                var validEntries = vscodeStorageEntries.Entries.Where(x => x != null).ToList();
                                foreach (var entry in validEntries)
                                {
                                    bool isWorkspaceFile = false;
                                    var uri = entry.FolderUri;
                                    if (entry.Workspace?.ConfigPath != null)
                                    {
                                        isWorkspaceFile = true;
                                        uri = entry.Workspace.ConfigPath;
                                    }

                                    var workspace = ParseVSCodeUriAndAuthority(uri, entry.RemoteAuthority, isWorkspaceFile);
                                    if (workspace != null)
                                    {
                                        dbFileResults.Add(workspace);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to retrieve workspaces from db: {filePath}", typeof(VSCodeWorkspacesApi));
            }
            finally
            {
                sqliteConnection?.Close();
            }

            return dbFileResults;
        }
    }
}

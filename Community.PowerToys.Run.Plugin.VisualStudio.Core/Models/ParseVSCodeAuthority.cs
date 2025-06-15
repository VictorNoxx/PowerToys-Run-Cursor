// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Models
{
    public class ParseVSCodeAuthority
    {
        private static readonly Dictionary<string, WorkspaceEnvironment> EnvironmentTypes = new()
        {
            { string.Empty, WorkspaceEnvironment.Local },
            { "ssh-remote", WorkspaceEnvironment.RemoteSSH },
            { "wsl", WorkspaceEnvironment.RemoteWSL },
            { "vsonline", WorkspaceEnvironment.Codespaces },
            { "dev-container", WorkspaceEnvironment.DevContainer },
            { "tunnel", WorkspaceEnvironment.RemoteTunnel },
        };

        public static (WorkspaceEnvironment? WorkspaceEnvironment, string? MachineName) GetWorkspaceEnvironment(string? authority)
        {
            var remoteName = GetRemoteName(authority);
            var machineName = remoteName != null && remoteName.Length < (authority?.Length ?? 0) ? authority?[(remoteName.Length + 1)..] : null;
            return EnvironmentTypes.TryGetValue(remoteName ?? string.Empty, out WorkspaceEnvironment workspace) ?
                (workspace, machineName) :
                (null, null);
        }

        private static string? GetRemoteName(string? authority)
        {
            if (authority is null)
            {
                return null;
            }

            var pos = authority.IndexOf('+');
            if (pos < 0)
            {
                return authority;
            }

            return authority[..pos];
        }
    }
}

// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Models
{
    public class VSCodeWorkspace
    {
        public required string Path { get; set; }

        public required string RelativePath { get; set; }

        public required string FolderName { get; set; }

        public string? ExtraInfo { get; set; }

        public WorkspaceEnvironment WorkspaceEnvironment { get; set; }

        public WorkspaceType WorkspaceType { get; set; }

        public string WorkspaceEnvironmentToString()
        {
            switch (WorkspaceEnvironment)
            {
                case WorkspaceEnvironment.Local: return "Local";
                case WorkspaceEnvironment.Codespaces: return "Codespaces";
                case WorkspaceEnvironment.RemoteSSH: return "SSH";
                case WorkspaceEnvironment.RemoteWSL: return "WSL";
                case WorkspaceEnvironment.DevContainer: return "DevContainer";
                case WorkspaceEnvironment.RemoteTunnel: return "Tunnel";
            }

            return string.Empty;
        }
    }

    public enum WorkspaceEnvironment
    {
        Local = 1,
        Codespaces = 2,
        RemoteWSL = 3,
        RemoteSSH = 4,
        DevContainer = 5,
        RemoteTunnel = 6,
    }

    public enum WorkspaceType
    {
        ProjectFolder = 1,
        WorkspaceFile = 2,
    }
}

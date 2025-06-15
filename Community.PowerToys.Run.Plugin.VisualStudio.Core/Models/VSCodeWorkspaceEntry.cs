// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Models
{
    public class VSCodeWorkspaceEntry
    {
        [JsonPropertyName("folderUri")]
        public string? FolderUri { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("remoteAuthority")]
        public string? RemoteAuthority { get; set; }

        [JsonPropertyName("workspace")]
        public VSCodeWorkspaceProperty? Workspace { get; set; }
    }
}

// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Models
{
    public class OpenedPathsList
    {
        [JsonPropertyName("workspaces3")]
        public List<string>? Workspaces3 { get; set; }

        [JsonPropertyName("entries")]
        public List<VSCodeWorkspaceEntry>? Entries { get; set; }
    }
}

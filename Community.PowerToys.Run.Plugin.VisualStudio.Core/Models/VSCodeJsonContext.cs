// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Models
{
    [JsonSerializable(typeof(VSCodeStorageFile))]
    [JsonSerializable(typeof(VSCodeStorageEntries))]
    [JsonSerializable(typeof(OpenedPathsList))]
    [JsonSerializable(typeof(VSCodeWorkspaceEntry))]
    [JsonSerializable(typeof(VSCodeWorkspaceProperty))]
    public partial class VSCodeJsonContext : JsonSerializerContext
    {
    }
}

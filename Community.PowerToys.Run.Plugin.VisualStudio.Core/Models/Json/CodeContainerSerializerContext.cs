// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Community.PowerToys.Run.Plugin.VisualStudio.Core.Models.Json;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Json
{
    [JsonSerializable(typeof(List<CodeContainer>))]
    public sealed partial class CodeContainerSerializerContext : JsonSerializerContext
    {
    }
}

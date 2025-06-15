// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Models
{
    // Use regex to parse URI since System.Uri is not compliant with RFC 3986, see https://github.com/dotnet/runtime/issues/64707
    public partial class Rfc3986Uri
    {
        // The following regex is referenced from https://www.rfc-editor.org/rfc/rfc3986.html#appendix-B
        [GeneratedRegex(@"^((?<scheme>[^:/?#]+):)?(//(?<authority>[^/?#]*))?(?<path>[^?#]*)(\?(?<query>[^#]*))?(#(?<fragment>.*))?$")]
        private static partial Regex Rfc3986();

        public string Scheme { get; private set; } = string.Empty;

        public string Authority { get; private set; } = string.Empty;

        public string Path { get; private set; } = string.Empty;

        public string Query { get; private set; } = string.Empty;

        public string Fragment { get; private set; } = string.Empty;

        public static Rfc3986Uri? Parse([StringSyntax("Uri")] string uriString)
        {
            return uriString is not null && Rfc3986().Match(uriString) is { Success: true } match
                ? new Rfc3986Uri()
                {
                    Scheme = match.Groups["scheme"].Value,
                    Authority = match.Groups["authority"].Value,
                    Path = match.Groups["path"].Value,
                    Query = match.Groups["query"].Value,
                    Fragment = match.Groups["fragment"].Value,
                }
                : null;
        }
    }
}

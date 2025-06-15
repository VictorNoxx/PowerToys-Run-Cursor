// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml;
using Community.PowerToys.Run.Plugin.VisualStudio.Json;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Models
{
    public class VisualStudioInstance
    {
        public string InstancePath { get; }

        public bool IsPrerelease { get; }

        public string DisplayName { get; }

        public string ApplicationPrivateSettingsPath { get; }

        public VisualStudioInstance(Json.VisualStudioInstance json, string applicationPrivateSettingsPath)
        {
            InstancePath = GetCursorPath();
            IsPrerelease = json.IsPrerelease;
            DisplayName = json.DisplayName;
            ApplicationPrivateSettingsPath = applicationPrivateSettingsPath;
        }

        public virtual IEnumerable<CodeContainer> GetCodeContainers()
        {
            var codeContainersString = GetCodeContainersString();
            if (codeContainersString != null)
            {
                var codeContainers = JsonSerializer.Deserialize(codeContainersString, CodeContainerSerializerContext.Default.ListCodeContainer);
                if (codeContainers != null)
                {
                    foreach (var c in codeContainers)
                    {
                        if (Path.Exists(c.Value.LocalProperties.FullPath))
                        {
                            yield return new CodeContainer(c, this);
                        }
                    }
                }
            }
        }

        private static string GetCursorPath()
        {
            var possiblePaths = new[]
            {
                @"%LOCALAPPDATA%\Programs\cursor\Cursor.exe",
                @"%PROGRAMFILES%\Cursor\Cursor.exe",
                @"%PROGRAMFILES(X86)%\Cursor\Cursor.exe",
            };

            foreach (var path in possiblePaths)
            {
                var expandedPath = Environment.ExpandEnvironmentVariables(path);
                if (File.Exists(expandedPath))
                {
                    return expandedPath;
                }
            }

            return "cursor";
        }

        private string? GetCodeContainersString()
        {
            if (ApplicationPrivateSettingsPath == null)
            {
                return null;
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(ApplicationPrivateSettingsPath);

            using var collectionNodes = xmlDoc.GetElementsByTagName("collection");
            var collectionName = "CodeContainers.Offline";
            var collectionNode = null as XmlNode;

            foreach (XmlNode node in collectionNodes)
            {
                var nameAttribute = node.Attributes?["name"];
                if (nameAttribute != null && nameAttribute.Value == collectionName)
                {
                    collectionNode = node;
                    break;
                }
            }

            if (collectionNode != null)
            {
                var valueNode = collectionNode?.SelectSingleNode("value");
                return valueNode?.InnerText;
            }

            return null;
        }
    }
}

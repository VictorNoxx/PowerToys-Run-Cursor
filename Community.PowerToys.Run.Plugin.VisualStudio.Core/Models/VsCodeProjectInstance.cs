// Copyright (c) Davide Giacometti. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Community.PowerToys.Run.Plugin.VisualStudio.Core.Models
{
    public class VsCodeProjectInstance : VisualStudioInstance
    {
        public string ProjectPath { get; }

        public VsCodeProjectInstance(string projectPath)
            : base(CreateDummyJsonInstance(projectPath), string.Empty)
        {
            ProjectPath = projectPath;
        }

        public override IEnumerable<CodeContainer> GetCodeContainers()
        {
            var isFile = File.Exists(ProjectPath);
            var name = isFile ? Path.GetFileName(ProjectPath) : Path.GetFileName(ProjectPath.TrimEnd('\\', '/'));

            yield return new CodeContainer(
                new Json.CodeContainer
                {
                    Key = ProjectPath,
                    Value = new Json.Value
                    {
                        LocalProperties = new Json.LocalProperties
                        {
                            FullPath = ProjectPath,
                        },
                        IsFavorite = false,
                        LastAccessed = File.GetLastAccessTime(ProjectPath),
                    },
                },
                this);
        }

        private static Json.VisualStudioInstance CreateDummyJsonInstance(string projectPath)
        {
            return new Json.VisualStudioInstance
            {
                InstanceId = "vscode-cursor",
                ProductPath = "cursor", // This will be overridden by GetCursorPath()
                IsPrerelease = false,
                DisplayName = "VS Code Project (Cursor)",
                Catalog = new Json.Catalog
                {
                    ProductLineVersion = "vscode",
                },
            };
        }
    }
}

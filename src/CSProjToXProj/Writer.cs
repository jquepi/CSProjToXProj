﻿using System;
using System.Collections.Generic;
using System.IO;
using CSProjToXProj.Plumbing;
using System.Linq;
using CSProjToXProj.SourceFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CSProjToXProj
{
    public class Writer
    {
        private readonly IFileSystem _fileSystem;

        public Writer(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void WriteXProj(string xprojPath, ProjectMetadata projectMetaData, bool replaceExisting)
        {
            var projectGuid = replaceExisting ? projectMetaData.Guid : Guid.NewGuid();
            var contents = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">14.0</VisualStudioVersion>
    <VSToolsPath Condition=""'$(VSToolsPath)' == ''"">$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)</VSToolsPath>
  </PropertyGroup>
  <Import Project=""$(VSToolsPath)\DotNet\Microsoft.DotNet.Props"" Condition=""'$(VSToolsPath)' != ''"" />
  <PropertyGroup Label=""Globals"">
    <ProjectGuid>{projectGuid:D}</ProjectGuid>
    <RootNamespace>{projectMetaData.RootNamespace}</RootNamespace>
    <BaseIntermediateOutputPath Condition=""'$(BaseIntermediateOutputPath)'=='' "">.\obj</BaseIntermediateOutputPath>
    <OutputPath Condition=""'$(OutputPath)'=='' "">.\bin\</OutputPath>
  </PropertyGroup>
  <PropertyGroup>
    <SchemaVersion>2.0</SchemaVersion>
  </PropertyGroup>
  <Import Project=""$(VSToolsPath)\DotNet\Microsoft.DotNet.targets"" Condition=""'$(VSToolsPath)' != ''"" />
</Project>";

            _fileSystem.WriteAllText(xprojPath, contents);
        }

        public void WriteProjectJson(string directory, ProjectMetadata projectMetadata, IReadOnlyList<PackageEntry> packages)
        {
            var doc = new JObject
            {
                ["version"] = "0.0.0-*",
                ["dependencies"] = new JObject()
            };

            foreach (var package in packages)
                doc["dependencies"][package.Id] = package.Version;
            foreach (var reference in projectMetadata.ProjectReferences)
                doc["dependencies"][reference] = "*";

            var framework = "net" + projectMetadata.TargetFrameworkVersion.Replace("v", "").Replace(".", "");
            var frameworkElement = new JObject();
            doc["frameworks"] = new JObject { [framework] = frameworkElement };

            if (projectMetadata.FrameworkReferences.Any())
            {
                frameworkElement["frameworkAssemblies"] = new JObject();
                foreach (var reference in projectMetadata.FrameworkReferences)
                    frameworkElement["frameworkAssemblies"][reference] = "*";
            }

            if (projectMetadata.OutputType.Equals("Exe", StringComparison.CurrentCultureIgnoreCase))
            {
                doc["buildOptions"] = new JObject { ["emitEntryPoint"] = true };
            }

            var json = JsonConvert.SerializeObject(doc, Formatting.Indented);
            _fileSystem.WriteAllText(Path.Combine(directory, "project.json"), json);
        }
    }
}
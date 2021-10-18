/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using HhotateA.AvatarModifyTools.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

namespace HhotateA.AvatarModifyTools.DebugTools
{
    public class PackageExporter
    {
        const string toolsFolder = "Assets/HhotateA";
        const string coreToolFolder = "Assets/HhotateA/AvatarModifyTool";
        [MenuItem("Window/HhotateA/Debug/PackageExporter",false,2)]
        static void Export()
        {
            var path = EditorUtility.SaveFilePanel("Export", "Assets", "_v"+EnvironmentVariable.version,"unitypackage");
            if (String.IsNullOrWhiteSpace(path)) return;
            string dir = Path.GetDirectoryName(path);
            string version = System.IO.Path.GetFileNameWithoutExtension(path);
            var fullpackagePath = Path.Combine(dir, "FullPackage" + version + ".unitypackage");
            AssetDatabase.ExportPackage(toolsFolder, fullpackagePath,ExportPackageOptions.Recurse);
            var folders = AssetDatabase.GetSubFolders(toolsFolder);
            foreach (var folder in folders)
            {
                if (folder != coreToolFolder)
                {
                    var toolName = folder.Replace(toolsFolder, "").Replace("/","");
                    var exportFolder = Path.Combine(dir, toolName+version);
                    
                    System.IO.Directory.CreateDirectory(exportFolder);
                    File.Copy(fullpackagePath,Path.Combine(exportFolder, "FullPackage" + version + ".unitypackage"));
                    
                    AssetDatabase.ExportPackage(new string[]{coreToolFolder,folder},Path.Combine(exportFolder,toolName+version+".unitypackage"), ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);

                    var readmePath = Application.dataPath.Replace("Assets", Path.Combine(folder,"Readme.md"));
                    Debug.Log(readmePath);
                    if (File.Exists(readmePath))
                    {
                        File.Copy(readmePath,Path.Combine(exportFolder,"Readme.txt"));
                    }

                    var manualPath = Application.dataPath.Replace("Assets", Path.Combine(folder, "Manual"));
                    if (Directory.Exists(readmePath))
                    {
                        FolderCopy(manualPath, Path.Combine(exportFolder, "Manual"));
                    }
                }
            }
        }
        
        static void FolderCopy(string src, string dst)
        {
            DirectoryInfo srcDir = new DirectoryInfo(src);

            if (srcDir.Exists)
            {
                Directory.CreateDirectory(dst);
            }


            DirectoryInfo[] folders = srcDir.GetDirectories();
            FileInfo[] files = srcDir.GetFiles();

            foreach (FileInfo file in files)
            {
                if(file.Name.Contains(".meta")) continue;
                string path = Path.Combine(dst, file.Name);
                file.CopyTo(path, true);
            }

            foreach (DirectoryInfo subfolder in folders)
            {
                string path = Path.Combine(dst, subfolder.Name);
                FolderCopy(subfolder.FullName, path);
            }
        }
    }
}

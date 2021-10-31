/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using HhotateA.AvatarModifyTools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

namespace HhotateA.AvatarModifyTools.DebugTools
{
    public class TextureArrayMaker : WindowBase
    {
        [OnOpenAssetAttribute(0)]
        public static bool OpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(TextureArrayData))
            {
                ShowWindow(EditorUtility.InstanceIDToObject(instanceID) as TextureArrayData);
            }
            return false;
        }
        
        [MenuItem("Window/HhotateA/DebugTools/TextureArrayMaker",false,4)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TextureArrayMaker>();
            wnd.titleContent = new GUIContent("TextureArrayMaker");
            
            wnd.data = CreateInstance<TextureArrayData>();
            wnd.withAsset = false;
        }
        public static void ShowWindow(TextureArrayData d)
        {
            var wnd = GetWindow<TextureArrayMaker>();
            wnd.titleContent = new GUIContent("TextureArrayMaker");
            if (d == null)
            {
                wnd.data = CreateInstance<TextureArrayData>();
                wnd.withAsset = false;
            }
            else
            {
                wnd.data = d;
                wnd.withAsset = true;
            }
        }

        public bool withAsset = false;
        private TextureArrayData data;
        private List<Texture> textures 
        { 
            get => data.textures;
            set => data.textures = value;
        }
        
        private void OnGUI()
        {
            TitleStyle("TextureArrayMaker");

            var tc = EditorGUILayout.IntField("Texture Array Size",textures.Count);
            if (tc < textures.Count)
            {
                data.textures = textures.GetRange(0, tc);
            }
            else
            if(tc > textures.Count)
            {
                textures.AddRange(new Texture[tc-textures.Count].ToList());
            }
            
            for(int i = 0; i < textures.Count; i ++)
            {
                textures[i] = (Texture) EditorGUILayout.ObjectField(textures[i], typeof(Texture), true);
            }

            withAsset = EditorGUILayout.Toggle("SaveWithAsset", withAsset);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("SaveCombine"))
                {
                    var path = EditorUtility.SaveFilePanel("Save", "Assets", "CombineTexture", "png");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    
                    try
                    {
                        TextureCombinater.CombinateSaveTexture(
                            textures.Select(t=>
                                TextureCombinater.Texture2Texture2D(t)).ToArray(),path);
                        if (withAsset)
                        {
                            data = Instantiate(data);
                            AssetDatabase.CreateAsset(data, 
                                Path.Combine(
                                    Path.GetDirectoryName(FileUtil.GetProjectRelativePath(path)),
                                Path.GetFileNameWithoutExtension(FileUtil.GetProjectRelativePath(path))+".asset"));
                        }
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                    OnSave();
                }
                if (GUILayout.Button("SaveArray"))
                {
                    var path = EditorUtility.SaveFilePanel("Save", "Assets", "TextureArray", "asset");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    
                    try
                    {
                        var asset = TextureCombinater.CreateTexture2DArray(
                            textures.Select(t=>
                                TextureCombinater.Texture2Texture2D(t)).ToArray());
                        AssetDatabase.CreateAsset(asset, FileUtil.GetProjectRelativePath(path));
                        if (withAsset)
                        {
                            data = Instantiate(data);
                            AssetDatabase.AddObjectToAsset(data, FileUtil.GetProjectRelativePath(path));
                        }
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                    OnSave();
                }
            }

            status.Display();
            Signature();
        }
    }
}
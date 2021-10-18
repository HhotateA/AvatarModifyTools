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
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HhotateA.AvatarModifyTools.DebugTools
{
    public class TextureArrayMaker : WindowBase
    {
        public static void ShowWindow(AvatarModifyData data)
        {
            var wnd = GetWindow<TextureArrayMaker>();
            wnd.titleContent = new GUIContent("TextureArrayMaker");
        }
        
        [MenuItem("Window/HhotateA/Debug/TextureArrayMaker",false,4)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TextureArrayMaker>();
            wnd.titleContent = new GUIContent("AvatarModifyTool");
        }

        private List<Texture> textures = new List<Texture>();
        
        private void OnGUI()
        {
            TitleStyle("TextureArrayMaker");

            var tc = EditorGUILayout.IntField("Texture Array Size",textures.Count);
            if (tc < textures.Count)
            {
                textures = textures.GetRange(0, tc);
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
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                    OnSave();
                }
                if (GUILayout.Button("SaveeArray"))
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
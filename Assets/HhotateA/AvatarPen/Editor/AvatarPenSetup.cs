/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.AvatarPen
{
    public class AvatarPenSetup : WindowBase
    {
        [MenuItem("Window/HhotateA/アバターペンセットアップ(AvatarPenSetup)",false,101)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<AvatarPenSetup>();
            wnd.titleContent = new GUIContent("AvatarPenSetup");
            wnd.maxSize = wnd.minSize = new Vector2(340, 340);
        }
        private bool isLeftHand = false;

        private void OnGUI()
        {
            TitleStyle("アバターペンセットアップ");
            DetailStyle("アバターに指ペンを実装する，簡単なセットアップツールです．",EnvironmentGUIDs.readme);
#if VRC_SDK_VRCSDK3

            EditorGUILayout.Space();
            AvatartField("Avatar");
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            isLeftHand = EditorGUILayout.Toggle("Left Hand", isLeftHand);
            EditorGUILayout.Space();

            if (ShowOptions())
            {
                
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(avatar==null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Setup"))
                    {
                        try
                        {
                            var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(
                                isLeftHand ? EnvironmentGUIDs.penModifyData_Left : EnvironmentGUIDs.penModifyData_right);
                            var mod = new AvatarModifyTool(avatar);
                            ApplySettings(mod).ModifyAvatar(asset,EnvironmentGUIDs.prefix);
                            OnFinishSetup();
                            DetectAnimatorError();
                        }
                        catch (Exception e)
                        {
                            OnError(e);
                            throw;
                        }
                    }

                    if (expandOptions)
                    {
                        if (GUILayout.Button("Revert"))
                        {
                            var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(
                                isLeftHand ? EnvironmentGUIDs.penModifyData_Left : EnvironmentGUIDs.penModifyData_right);
                            var mod = new AvatarModifyTool(avatar);
                            mod.RevertByAssets(asset);
                            OnFinishRevert();
                        }
                    }
                }
            }
            status.Display();
#else
            VRCErrorLabel();
#endif
            Signature();
        }
    }
}
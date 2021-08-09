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
    public class AvatarPenSetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/アバターペンセットアップ(AvatarPenSetup)",false,1)]

        public static void ShowWindow()
        {
            var wnd = GetWindow<AvatarPenSetup>();
            wnd.titleContent = new GUIContent("AvatarPenSetup");
            wnd.maxSize = wnd.minSize = new Vector2(340, 250);
        }

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#endif
        private bool isLeftHand = false;

        private bool writeDefault = false;
        private bool notRecommended = false;
        private bool keepOldAsset = false;

        private void OnGUI()
        {
#if VRC_SDK_VRCSDK3
            AssetUtility.TitleStyle("アバターペンセットアップ");
            
            avatar = (VRCAvatarDescriptor) EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);

            EditorGUILayout.Space();
            isLeftHand = EditorGUILayout.Toggle("Left Hand", isLeftHand);
            EditorGUILayout.Space();
            notRecommended = EditorGUILayout.Foldout(notRecommended,"VRChat Not Recommended");
            if (notRecommended)
            {
                writeDefault = EditorGUILayout.Toggle("Write Default", writeDefault); 
                keepOldAsset = EditorGUILayout.Toggle("Keep Old Asset", keepOldAsset); 
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(avatar==null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Setup"))
                    {
                        var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(
                            isLeftHand ? EnvironmentGUIDs.penModifyData_Left : EnvironmentGUIDs.penModifyData_right);
                        var mod = new AvatarModifyTool(avatar);
                        if (writeDefault)
                        {
                            mod.WriteDefaultOverride = true;
                        }
                        mod.ModifyAvatar(asset,true,keepOldAsset);
                    }

                    if (keepOldAsset)
                    {
                        if (GUILayout.Button("Revert"))
                        {
                            var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(
                                isLeftHand ? EnvironmentGUIDs.penModifyData_Left : EnvironmentGUIDs.penModifyData_right);
                            var mod = new AvatarModifyTool(avatar);
                            mod.RevertAvatar(asset);
                        }
                    }
                }
            }
            
            AssetUtility.Signature();
#else
            EditorGUILayout.LabelField("Please import VRCSDK3.0 in your project.");
#endif
        }
    }
}
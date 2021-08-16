/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.Core
{
    public class TestWindow : EditorWindow
    {
        [OnOpenAssetAttribute(0)]
        public static bool step0(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(AvatarModifyData))
            {
                TestWindow.ShowWindow(EditorUtility.InstanceIDToObject(instanceID) as AvatarModifyData);
            }
            return false;
        }
        public static void ShowWindow(AvatarModifyData data)
        {
            var wnd = GetWindow<TestWindow>();
            wnd.titleContent = new GUIContent("HhotateA.AvatarModifyTools.Core");
            wnd.data = data;
        }
        
#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#endif
        private AvatarModifyData data;
        private bool writeDefault = false;
        private bool saveOrigin = true;
        private bool keepOldAsset = false;
        private void OnGUI()
        {
            AssetUtility.TitleStyle("HhotateA.AvatarModifyTools.Core");
#if VRC_SDK_VRCSDK3
            if(data==null) return;
            EditorGUILayout.LabelField("DATA : " + data.name);
            avatar = (VRCAvatarDescriptor) EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);
            writeDefault = EditorGUILayout.Toggle("Write Default", writeDefault); 
            saveOrigin = EditorGUILayout.Toggle("Save Origin", saveOrigin); 
            keepOldAsset = EditorGUILayout.Toggle("Keep Old Asset", keepOldAsset); 
            if (GUILayout.Button("Setup"))
            {
                var mod = new AvatarModifyTool(avatar);
                if (writeDefault) mod.WriteDefaultOverride = writeDefault;
                mod.ModifyAvatar(data,saveOrigin,keepOldAsset);
            }
            if (GUILayout.Button("Revert"))
            {
                var mod = new AvatarModifyTool(avatar);
                mod.RevertAvatar(data);
            }
            AssetUtility.Signature();
#else
            EditorGUILayout.LabelField("Please Import VRCSDK");
#endif
        }
    }
}
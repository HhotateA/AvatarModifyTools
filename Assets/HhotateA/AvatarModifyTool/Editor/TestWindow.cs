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
    public class TestWindow : WindowBase
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
        
        private AvatarModifyData data;
        private bool saveOrigin = true;
        private void OnGUI()
        {
            TitleStyle("HhotateA.AvatarModifyTools.Core");
#if VRC_SDK_VRCSDK3
            if(data==null) return;
            EditorGUILayout.LabelField("DATA : " + data.saveName);
            AvatartField("Avatar");
            if (ShowOptions())
            {
                saveOrigin = EditorGUILayout.Toggle("Save Origin", saveOrigin); 
            }
            if (GUILayout.Button("Setup"))
            {
                var mod = new AvatarModifyTool(avatar);
                ApplySettings(mod).ModifyAvatar(data,"");
            }
            if (GUILayout.Button("Revert"))
            {
                var mod = new AvatarModifyTool(avatar);
                mod.RevertByAssets(data);
            }
            status.Display();
            Signature();
#else
            EditorGUILayout.LabelField("Please Import VRCSDK");
#endif
        }
    }
}
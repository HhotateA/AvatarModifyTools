/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
#if VRC_SDK_VRCSDK3
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace HhotateA.AvatarModifyTools.Core
{
    public class TestWindow : WindowBase
    {
        [OnOpenAssetAttribute]
        public static bool OpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(AvatarModifyData))
            {
                ShowWindow(EditorUtility.InstanceIDToObject(instanceID) as AvatarModifyData);
            }
            return false;
        }
        
        public static void ShowWindow(AvatarModifyData data)
        {
            var wnd = GetWindow<TestWindow>();
            wnd.titleContent = new GUIContent("AvatarModifyTool");
            wnd.data = data;
        }
        
        [MenuItem("Window/HhotateA/DebugTools/AvatarModifyTool",false,1)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TestWindow>();
            wnd.titleContent = new GUIContent("AvatarModifyTool");
            wnd.data = CreateInstance<AvatarModifyData>();
        }
        
        private AvatarModifyData data;
        private bool extendItems = true;
        private bool saveOrigin = true;
        private string prefix = "";
        private void OnGUI()
        {
            TitleStyle("HhotateA.AvatarModifyTools.Core");
#if VRC_SDK_VRCSDK3
            if(data==null) return;
            // EditorGUILayout.LabelField("DATA : " + data.saveName);
            data.saveName = EditorGUILayout.TextField("DATA : ",prefix);
            AvatartField("Avatar");

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                data.locomotion_controller = (AnimatorController) EditorGUILayout.ObjectField("locomotion : ",
                    data.locomotion_controller, typeof(AnimatorController), true);
                data.idle_controller = (AnimatorController) EditorGUILayout.ObjectField("idle : ", data.idle_controller,
                    typeof(AnimatorController), true);
                data.gesture_controller = (AnimatorController) EditorGUILayout.ObjectField("gesture : ",
                    data.gesture_controller, typeof(AnimatorController), true);
                data.action_controller = (AnimatorController) EditorGUILayout.ObjectField("action : ",
                    data.action_controller, typeof(AnimatorController), true);
                data.fx_controller = (AnimatorController) EditorGUILayout.ObjectField("fx : ", data.fx_controller,
                    typeof(AnimatorController), true);

                data.parameter = (VRCExpressionParameters) EditorGUILayout.ObjectField("params : ", data.parameter,
                    typeof(VRCExpressionParameters), true);
                data.menu = (VRCExpressionsMenu) EditorGUILayout.ObjectField("menu : ", data.menu,
                    typeof(VRCExpressionsMenu), true);

                extendItems = EditorGUILayout.Foldout(extendItems, "Items");
                if (extendItems)
                {
                    var count = EditorGUILayout.IntField("ItemCount", data.items.Length);
                    count = Mathf.Max(0, count);

                    if (data.items.Length < count)
                    {
                        var itemList = data.items.ToList();
                        for (int i = data.items.Length; i < count; i++)
                        {
                            itemList.Add(new Item());
                        }

                        data.items = itemList.ToArray();
                    }
                    else if (data.items.Length > count)
                    {
                        var itemList = data.items.ToList();
                        itemList = itemList.GetRange(0, count);
                        data.items = itemList.ToArray();
                    }

                    foreach (var item in data.items)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            item.prefab =
                                (GameObject) EditorGUILayout.ObjectField(item.prefab, typeof(GameObject), true);
                            item.target = (HumanBodyBones) EditorGUILayout.EnumPopup(item.target);
                        }
                    }
                }
            }

            if (ShowOptions())
            {
                saveOrigin = EditorGUILayout.Toggle("Save Origin", saveOrigin); 
                prefix = EditorGUILayout.TextField("Prefix : ",prefix);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Setup"))
                {
                    try
                    {
                        var mod = new AvatarModifyTool(avatar);
                        ApplySettings(mod).ModifyAvatar(data, "");
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                    OnFinishSetup();
                }

                if (GUILayout.Button("Revert"))
                {
                    try
                    {
                        var mod = new AvatarModifyTool(avatar);
                        mod.RevertByAssets(data);
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                    OnFinishRevert();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save"))
                {
                    var path = EditorUtility.SaveFilePanel("Save", data.GetAssetDir(), data.saveName, "asset");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    
                    try
                    {
                        data = Instantiate(data);
                        AssetDatabase.CreateAsset(data, FileUtil.GetProjectRelativePath(path));
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                    OnSave();
                }
                if (GUILayout.Button("Load"))
                {
                    var path = EditorUtility.OpenFilePanel("Load", data.GetAssetDir(), "asset");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    
                    try
                    {
                        var d = AssetDatabase.LoadAssetAtPath<AvatarModifyData>(FileUtil.GetProjectRelativePath(path));
                        if (d == null)
                        {
                            status.Warning("Load Failure");
                            return;
                        }
                        else
                        {
                            data = d;
                        }
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                    OnLoad();
                }
            }

            status.Display();
            Signature();
#else
            EditorGUILayout.LabelField("Please Import VRCSDK");
#endif
        }
    }
}
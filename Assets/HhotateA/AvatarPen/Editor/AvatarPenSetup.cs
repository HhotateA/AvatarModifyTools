using HhotateA.AvatarModifyTools.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.AvatarPen
{
    public class AvatarPenSetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/AvatarPenSetup")]

        public static void ShowWindow()
        {
            var wnd = GetWindow<AvatarPenSetup>();
            wnd.titleContent = new GUIContent("AvatarPenSetup");
            wnd.maxSize = wnd.minSize = new Vector2(340, 200);
        }

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#endif
        private string assetDatabaseGUID = "e5cd1ff11d13fed44bd5d0b8b4a2be8c";
        private string msg = "OK";
        GUIStyle msgStyle = new GUIStyle(GUIStyle.none);

        private void OnGUI()
        {
#if VRC_SDK_VRCSDK3
            GUIStyle titleStyle = new GUIStyle(GUIStyle.none);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 15;
            titleStyle.fontStyle = FontStyle.Bold;
            
            GUIStyle instructions = new GUIStyle(GUI.skin.label);
            instructions.fontSize = 10;
            instructions.wordWrap = true;
            
            GUIStyle signature = new GUIStyle(GUI.skin.label);
            signature.alignment = TextAnchor.LowerRight;
            signature.fontSize = 10;
            
            GUIStyle status = new GUIStyle(GUIStyle.none);
            status.alignment = TextAnchor.MiddleRight;
            status.fontSize = 9;
            msgStyle.alignment = TextAnchor.MiddleRight;
            msgStyle.fontSize = 9;
            
            GUILayout.Label("AvatarPenSetupTool",titleStyle);
            
            GUILayout.Label("シーン上のアバターをドラッグ＆ドロップ");
            
            EditorGUILayout.BeginHorizontal();
            {
                avatar = (VRCAvatarDescriptor) EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(avatar==null))
            {
                if (GUILayout.Button("Setup"))
                {
                    try
                    {
                        string path = AssetDatabase.GUIDToAssetPath(assetDatabaseGUID);
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            AvatarModifyData assets = AssetDatabase.LoadAssetAtPath<AvatarModifyData>(path);
                            var mod = new AvatarModifyTool(avatar);
                            mod.ModifyAvatar(assets);
                            msgStyle.normal = new GUIStyleState()
                            {
                                textColor = Color.green
                            };
                            msg = "Success!";
                        }
                        else
                        {
                            msg = "AvatarPen : AssetDatabase file not found. Please reimport.";
                            Debug.LogError(msg);
                            msgStyle.normal = new GUIStyleState()
                            {
                                textColor = Color.red
                            };
                        }
                    }
                    catch (Exception e)
                    {
                        msgStyle.normal = new GUIStyleState()
                        {
                            textColor = Color.red
                        };
                        msg = e.Message;
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Status: ",status);
                GUILayout.Label(msg,msgStyle);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            
            GUILayout.Label("上ボタンを押すと，アバターのFX用AnimatorController，ExpressionParamrter，ExpressionMenuに改変を加えます．",instructions);
            GUILayout.Space(10);
            
            GUILayout.Label("操作は元に戻せないので，必ずバックアップをとっていることを確認してください．",instructions);
            GUILayout.Space(20);
            
            GUILayout.Label("powered by @HhotateA_xR",signature);
#else
            EditorGUILayout.LabelField("Please import VRCSDK3.0 in your project.");
#endif
        }
    }
}
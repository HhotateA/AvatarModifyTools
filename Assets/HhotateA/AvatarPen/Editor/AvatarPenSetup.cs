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
            wnd.maxSize = wnd.minSize = new Vector2(340, 220);
        }

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#endif
        private string msg = "OK";
        GUIStyle msgStyle = new GUIStyle(GUIStyle.none);
        private bool isLeftHand = false;

        private bool writeDefault = false;
        private bool notRecommended = false;

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

            EditorGUILayout.Space();
            isLeftHand = EditorGUILayout.Toggle("Left Hand", isLeftHand);
            EditorGUILayout.Space();
            notRecommended = EditorGUILayout.Foldout(notRecommended,"VRChat Not Recommended");
            if (notRecommended)
            {
                writeDefault = EditorGUILayout.Toggle("Write Default", writeDefault); 
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(avatar==null))
            {
                if (GUILayout.Button("Setup"))
                {
                    try
                    {
                        var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(
                            isLeftHand ? EnvironmentGUIDs.penModifyData_Left : EnvironmentGUIDs.penModifyData_right);
                        var mod = new AvatarModifyTool(avatar);
                        if (writeDefault)
                        {
                            mod.WriteDefaultOverride = true;
                        }
                        mod.ModifyAvatar(asset);
                        msgStyle.normal = new GUIStyleState()
                        {
                            textColor = Color.green
                        };
                        msg = "Success!";
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
            
            using(new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Status: ",status);
                EditorGUILayout.LabelField(msg,msgStyle);
            }
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("上ボタンを押すと，アバターのFX用AnimatorController，ExpressionParamrter，ExpressionMenuに改変を加えます．",instructions);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("操作は元に戻せないので，必ずバックアップをとっていることを確認してください．",instructions);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("powered by @HhotateA_xR",signature);
#else
            EditorGUILayout.LabelField("Please import VRCSDK3.0 in your project.");
#endif
        }
    }
}
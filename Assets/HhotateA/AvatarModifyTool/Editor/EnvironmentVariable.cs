/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HhotateA.AvatarModifyTools.Core
{
    public static class EnvironmentVariable
    {
        public static string computeShader = "8e33ed767aaabf04eae3c3866bece392";
        
        public static int maxCaches = 16;
        
        public static string idleAnimation = "b0f4aa27579b9c442a87f46f90d20192";
        public static string baseAnimator = "4e4e1a372a526074884b7311d6fc686b";
        public static string idleAnimator = "573a1373059632b4d820876efe2d277f";
        public static string gestureAnimator = "404d228aeae421f4590305bc4cdaba16";
        public static string actionAnimator = "3e479eeb9db24704a828bffb15406520";
        public static string fxAnimator = "d40be620cf6c698439a2f0a5144919fe";
        public static string arrowIcon = "ab0f6a0e53ae8fd4aab1efed5effa7eb";

        public static string linkIcon = "20b4b9db03a839148b2a2166e53c9123";

        public static string nottingAvatarMask = "fb3cb20bd9fa4fa47ba68b49d8db8a43";

        public static string texturePreviewShader = "e422dd8b39cd79343b42ffba228bb53b";
        public static string texturePainterShader = "3cfd9a4da725f0c41b16979b05bd5a53";
    }

    public static class AssetUtility
    {
        public static T LoadAssetAtGuid<T>(string guid) where T : Object
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset;
        }

        public static string GetAssetGuid(Object obj)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (!String.IsNullOrWhiteSpace(path))
            {
                return AssetDatabase.AssetPathToGUID(path);
            }

            return "";
        }
        
        public static string GetRelativePath(Transform root,Transform o)
        {
            if (o.gameObject == root.gameObject)
            {
                return "";
            }
            string path = o.gameObject.name;
            Transform parent = o.transform.parent;
            while (parent != null)
            {
                if(parent.gameObject == root.gameObject) break;
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public static GUIStyle TitleStyle(string title = "",int fontSize = 17,int outline = 1)
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = fontSize;
            titleStyle.fontStyle = FontStyle.Normal;
            titleStyle.normal = new GUIStyleState()
            {
                textColor = new Color(0.9960784f,0.7254902f,0.7686275f)
            };
            
            GUIStyle outlineStyle = new GUIStyle(GUI.skin.label);
            outlineStyle.alignment = TextAnchor.MiddleCenter;
            outlineStyle.fontSize = fontSize;
            outlineStyle.fontStyle = FontStyle.Normal;
            outlineStyle.normal = new GUIStyleState()
            {
                textColor = Color.black
            };
            
            if (!String.IsNullOrWhiteSpace(title))
            {
                var rect = GUILayoutUtility.GetRect(new GUIContent(title), titleStyle);
                var r = rect;
                r.x += outline;
                EditorGUI.LabelField(r,title,outlineStyle);
                r = rect;
                r.x -= outline;
                EditorGUI.LabelField(r,title,outlineStyle);
                r = rect;
                r.y += outline;
                EditorGUI.LabelField(r,title,outlineStyle);
                r = rect;
                r.y -= outline;
                EditorGUI.LabelField(r,title,outlineStyle);
                EditorGUI.LabelField(rect,title,titleStyle);
            }
            return titleStyle;
        }
        
        public static GUIStyle DetailStyle(string title = "",string readme = "")
        {
            GUIStyle detailStyle = new GUIStyle(GUI.skin.label);
            detailStyle.alignment = TextAnchor.LowerCenter;
            detailStyle.fontSize = 10;
            detailStyle.fontStyle = FontStyle.Normal;
            detailStyle.normal = new GUIStyleState()
            {
                textColor = new Color(0.9960784f,0.7254902f,0.7686275f)
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!String.IsNullOrWhiteSpace(title))
                {
                    EditorGUILayout.LabelField(title,detailStyle);
                }
                if (!String.IsNullOrWhiteSpace(readme))
                {
                    if (GUILayout.Button(AssetUtility.LoadAssetAtGuid<Texture>(EnvironmentVariable.linkIcon),
                        GUILayout.Width(25),GUILayout.Height(25)))
                    {
                        var md = AssetUtility.LoadAssetAtGuid<Object>(readme);
                        Selection.objects = new Object[]{md};
                    }
                }
            }
            EditorGUILayout.Space();
            return detailStyle;
        }

        public static void Signature()
        {
            GUIStyle instructions = new GUIStyle(GUI.skin.label);
            instructions.fontSize = 10;
            instructions.wordWrap = true;
            
            GUIStyle signature = new GUIStyle(GUI.skin.label);
            signature.alignment = TextAnchor.LowerRight;
            signature.fontSize = 10;
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("ボタンを押すと，アバターのFX用AnimatorController，ExpressionParamrter，ExpressionMenuに改変を加えます．",instructions);
            EditorGUILayout.LabelField("操作は元に戻せないので，必ずバックアップをとっていることを確認してください．",instructions);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("powered by AvatarModifyTool @HhotateA_xR",signature);
        }
    }
}
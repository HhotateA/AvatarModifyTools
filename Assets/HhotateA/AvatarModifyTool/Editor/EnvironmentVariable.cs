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

        public static string[] VRChatParams = new string[]
        {
            "IsLocal",
            "Viseme",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
            "Expression1",
            "Expression2",
            "Expression3",
            "Expression4",
            "Expression5",
            "Expression6",
            "Expression7",
            "Expression8",
            "Expression9",
            "Expression10",
            "Expression11",
            "Expression12",
            "Expression13",
            "Expression14",
            "Expression15",
            "Expression16",
        };
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

        public static string GetProjectRelativePath(string path)
        {
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/"))
            {
                path = FileUtil.GetProjectRelativePath(path);
            }

            return path;
        }
        public static Transform RecursiveFindChild(this Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if(child.name == childName)
                {
                    return child;
                }
                else
                {
                    Transform found = RecursiveFindChild(child, childName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }
    }
}
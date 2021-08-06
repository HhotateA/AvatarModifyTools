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

        public static string nottingAvatarMask = "fb3cb20bd9fa4fa47ba68b49d8db8a43";
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
    }
}
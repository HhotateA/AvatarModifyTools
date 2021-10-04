/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Text;

namespace HhotateA.AvatarModifyTools.Core
{
    /// <summary>
    /// 便利関数用staticクラス
    /// </summary>
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

        public static string GetRelativePath(Transform root, Transform o)
        {
            if (o.gameObject == root.gameObject)
            {
                return "";
            }

            string path = o.gameObject.name;
            Transform parent = o.transform.parent;
            while (parent != null)
            {
                if (parent.gameObject == root.gameObject) break;
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

        public static Transform FindInChildren(this Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                else
                {
                    Transform found = FindInChildren(child, childName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        public static void RecursionInChildren(this Transform parent, Action<Transform> onFind)
        {
            onFind?.Invoke(parent);
            foreach (Transform child in parent)
            {
                child.RecursionInChildren(onFind);
            }
        }

        public static string GetAssetDir(this ScriptableObject asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (String.IsNullOrWhiteSpace(path))
            {
                return "Assets";
            }

            return Path.GetDirectoryName(path);
        }
        
        public static string GetAssetName(this ScriptableObject asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (String.IsNullOrWhiteSpace(path))
            {
                return asset.name;
            }

            return Path.GetFileName(path).Split('.')[0];
            return Path.GetFileNameWithoutExtension(path);
        }

        public static Transform[] GetBones(this GameObject root)
        {
            Transform[] bones = new Transform[Enum.GetValues(typeof(HumanBodyBones)).Length];
            var anim = root.GetComponent<Animator>();
            foreach (HumanBodyBones humanBone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                Transform bone = null;
                if (humanBone != HumanBodyBones.LastBone)
                {
                    if (anim != null)
                    {
                        if (anim.isHuman)
                        {
                            bone = anim.GetBoneTransform(humanBone);
                        }
                    }
                }

                if (bone == null)
                {
                    var boneNames = EnvironmentVariable.boneNamePatterns.FirstOrDefault(b => b[0] == humanBone.ToString());
                    if(boneNames == null) continue;
                    foreach (var boneName in boneNames)
                    {
                        root.transform.RecursionInChildren(t =>
                        {
                            if (bone == null)
                            {
                                string s = boneName.Replace(".", "").Replace("_", "").Replace(" ", "").ToUpper();
                                string d = t.gameObject.name.Replace(".", "").Replace("_", "").Replace(" ", "").ToUpper();
                                if (d.Contains(s))
                                {
                                    bone = t;
                                }
                            }
                        });
                        if (bone != null) break;
                    }
                }

                bones[(int) humanBone] = bone;
            }

            return bones;
        }
        
        // パラメータ文字列から2バイト文字の除去を行う
        public static string GetSafeParam(this string param,string prefix = "",bool hash = true)
        {
            if (String.IsNullOrWhiteSpace(param)) return "";
            if (EnvironmentVariable.vrchatParams.Contains(param)) return param;
            if (hash)
            {
                param = GetNihongoHash(param);
            }
            if (!param.StartsWith(prefix))
            {
                param = prefix + param;
            }

            return param;
        }

        public static string GetNihongoHash(string origin)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char ch in origin ) {
                if ( "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHJIJKLMNOPQRSTUVWXYZ!\"#$%&'()=-~^|\\`@{[}]*:+;_?/>.<,".IndexOf(ch) >= 0 ) {
                    builder.Append(ch);
                }
                else
                {
                    int hash = ch.GetHashCode();
                    int code = hash % 26;
                    code += (int)'A';
                    code = Mathf.Clamp(code, (int) 'A', (int) 'Z');
                    builder.Append((char)code);
                }
            }

            return builder.ToString();
        }
    }
}
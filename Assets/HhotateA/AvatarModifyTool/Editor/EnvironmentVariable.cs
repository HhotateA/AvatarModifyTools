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
using Object = UnityEngine.Object;

namespace HhotateA.AvatarModifyTools.Core
{
    public static class EnvironmentVariable
    {
        public static string version = "1.30.3";
        public static string githubLink = "https://github.com/HhotateA/AvatarModifyTools";
        public static string icon = "1549a00a4e9d1734ca9f8862981c623f";
        public static string iconMat = "d8f2ec63ea255c24f8fe567fac92c852";
            
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
        public static string nottingAnim = "1927de9efc6d12140be0499624f05160";

        public static string texturePreviewShader = "e422dd8b39cd79343b42ffba228bb53b";
        public static string texturePainterShader = "3cfd9a4da725f0c41b16979b05bd5a53";

        public static string[] vrchatParams = new string[]
        {
            "IsLocal",
            "Viseme",
            "GestureLeft",
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

        public static string[][] boneNamePatterns = new string[][]
        {
            new string[] { "Hips","Hip"},
            new string[] { "LeftUpperLeg","Leg_L","Leg_Left","UpperLeg_L","UpperLeg_Left"},
            new string[] { "RightUpperLeg","Leg_R","Leg_Right","UpperLeg_R","UpperLeg_Right"},
            new string[] { "LeftLowerLeg","Knee_L","Knee_Left","LowerLeg_L","LowerLeg_Left"},
            new string[] { "RightLowerLeg","Knee_R","Knee_Right","LowerLeg_R","LowerLeg_Right"},
            new string[] { "LeftFoot","Foot_L","Foot_Left"},
            new string[] { "RightFoot","Foot_R","Foot_Right"},
            new string[] { "Spines","Spine"},
            new string[] { "Chest","Chest"},
            new string[] { "Neck"},
            new string[] { "Head"},
            new string[] { "LeftShoulder","Shoulder_L","Shoulder_Left"},
            new string[] { "RightShoulder","Shoulder_R","Shoulder_Right"},
            new string[] { "LeftUpperArm","Arm_L","Arm_Left","UpperArm_L","UpperArm_Left"},
            new string[] { "RightUpperArm","Arm_R","Arm_Right","UpperArm_R","UpperArm_Right"},
            new string[] { "LeftLowerArm","LowerArm_L","LowerArm_Left"},
            new string[] { "RightLowerArm","LowerArm_R","LowerArm_Right"},
            new string[] { "LeftHand","Hand_L","Hand_Left","Hand_Left"},
            new string[] { "RightHand","Hand_R","Hand_R","Hand_Right"},
            new string[] { "LeftToes","ToeIK_L","Toe_L","Toe_Left","Toes_L","Toes_Left"},
            new string[] { "RightToes","ToeIK_R","Toe_R","Toe_Right","Toes_R","Toes_Right"},
            new string[] { "LeftEye","Eye_L","Eye_Left"},
            new string[] { "RightEye","Eye_R","Eye_Right"},
            new string[] { "Jaw"},
            new string[] { "LeftThumbProximal","ProximalThumb_L","ProximalThumb_Left"},
            new string[] { "LeftThumbIntermediate","IntermediateThumb_L","IntermediateThumb_Left"},
            new string[] { "LeftThumbDistal","DistalThumb_L","DistalThumb_Left"},
            new string[] { "LeftIndexProximal","ProximalIndex_L","ProximalIndex_Left"},
            new string[] { "LeftIndexIntermediate","IntermediateIndex_L","IntermediateIndex_Left"},
            new string[] { "LeftIndexDistal","DistalIndex_L","DistalIndex_Left"},
            new string[] { "LeftMiddleProximal","ProximalMiddle_L","ProximalMiddle_Left"},
            new string[] { "LeftMiddleIntermediate","IntermediateMiddle_L","IntermediateMiddle_Left"},
            new string[] { "LeftMiddleDistal","DistalMiddle_L","DistalMiddle_Left"},
            new string[] { "LeftRingProximal","ProximalRing_L","ProximalRing_Left"},
            new string[] { "LeftRingIntermediate","IntermediateRing_L","IntermediateRing_Left"},
            new string[] { "LeftRingDistal","DistalRing_L","DistalRing_Left"},
            new string[] { "LeftLittleProximal","ProximalLittle_L","ProximalLittle_Left"},
            new string[] { "LeftLittleIntermediate","IntermediateLittle_L","IntermediateLittle_Left"},
            new string[] { "LeftLittleDistal","DistalLittle_L","DistalLittle_Left"},
            new string[] { "RightThumbProximal","ProximalThumb_R","ProximalThumb_Right"},
            new string[] { "RightThumbIntermediate","IntermediateThumb_R","IntermediateThumb_Right"},
            new string[] { "RightThumbDistal","DistalThumb_R","DistalThumb_Right"},
            new string[] { "RightIndexProximal","ProximalIndex_R","ProximalIndex_Right"},
            new string[] { "RightIndexIntermediate","IntermediateIndex_R","IntermediateIndex_Right"},
            new string[] { "RightIndexDistal","DistalIndex_R","DistalIndex_Right"},
            new string[] { "RightMiddleProximal","ProximalMiddle_R","ProximalMiddle_Right"},
            new string[] { "RightMiddleIntermediate","IntermediateMiddle_R","IntermediateMiddle_Right"},
            new string[] { "RightMiddleDistal","DistalMiddle_R","DistalMiddle_Right"},
            new string[] { "RightRingProximal","ProximalRing_R","ProximalRing_Right"},
            new string[] { "RightRingIntermediate","IntermediateRing_R","IntermediateRing_Right"},
            new string[] { "RightRingDistal","DistalRing_R","DistalRing_Right"},
            new string[] { "RightLittleProximal","ProximalLittle_R","ProximalLittle_Right"},
            new string[] { "RightLittleIntermediate","IntermediateLittle_R","IntermediateLittle_Right"},
            new string[] { "RightLittleDistal","DistalLittle_R","DistalLittle_Right"},
            new string[] { "UpperChest"},
            new string[] { "LastBone"},
        };

        public static Transform[] GetBones(this GameObject root)
        {
            Transform[] bones = new Transform[Enum.GetValues(typeof(HumanBodyBones)).Length];
            var anim = root.GetComponent<Animator>();
            foreach (HumanBodyBones humanBone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (humanBone == HumanBodyBones.LastBone) continue;
                Transform bone = null;
                if (anim != null)
                {
                    if (anim.isHuman)
                    {
                        bone = anim.GetBoneTransform(humanBone);
                    }
                }

                if (bone == null)
                {
                    var boneNames = boneNamePatterns.FirstOrDefault(b => b[0] == humanBone.ToString());
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
        public static Transform FindInChildren(this Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if(child.name == childName)
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
            return System.IO.Path.GetDirectoryName (path);
        }
    }
}
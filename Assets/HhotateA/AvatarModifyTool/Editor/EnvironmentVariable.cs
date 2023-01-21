/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

namespace HhotateA.AvatarModifyTools.Core
{
    /// <summary>
    /// 環境変数用staticクラス
    /// </summary>
    public static class EnvironmentVariable
    {
        public static string version = "1.31.01";
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
            new string[] { "LeftUpperLeg","UpperLeg_Left","UpperLeg_L","Leg_Left","Leg_L"},
            new string[] { "RightUpperLeg","UpperLeg_Right","UpperLeg_R","Leg_Right","Leg_R"},
            new string[] { "LeftLowerLeg","LowerLeg_Left","LowerLeg_L","Knee_Left","Knee_L"},
            new string[] { "RightLowerLeg","LowerLeg_Right","LowerLeg_R","Knee_Right","Knee_R"},
            new string[] { "LeftFoot","Foot_Left","Foot_L"},
            new string[] { "RightFoot","Foot_Right","Foot_R"},
            new string[] { "Spine"},
            new string[] { "Chest"},
            new string[] { "Neck"},
            new string[] { "Head"},
            new string[] { "LeftShoulder","Shoulder_Left","Shoulder_L"},
            new string[] { "RightShoulder","Shoulder_Right","Shoulder_R"},
            new string[] { "LeftUpperArm","UpperArm_Left","UpperArm_L","Arm_Left","Arm_L"},
            new string[] { "RightUpperArm","UpperArm_Right","UpperArm_R","Arm_Right","Arm_R"},
            new string[] { "LeftLowerArm","LowerArm_Left","LowerArm_L"},
            new string[] { "RightLowerArm","LowerArm_Right","LowerArm_R"},
            new string[] { "LeftHand","Hand_Left","Hand_L"},
            new string[] { "RightHand","Hand_Right","Hand_R"},
            new string[] { "LeftToes","Toes_Left","Toe_Left","ToeIK_L","Toes_L","Toe_L"},
            new string[] { "RightToes","Toes_Right","Toe_Right","ToeIK_R","Toes_R","Toe_R"},
            new string[] { "LeftEye","Eye_Left","Eye_L"},
            new string[] { "RightEye","Eye_Right","Eye_R"},
            new string[] { "Jaw"},
            new string[] { "LeftThumbProximal","ProximalThumb_Left","ProximalThumb_L"},
            new string[] { "LeftThumbIntermediate","IntermediateThumb_Left","IntermediateThumb_L"},
            new string[] { "LeftThumbDistal","DistalThumb_Left","DistalThumb_L"},
            new string[] { "LeftIndexProximal","ProximalIndex_Left","ProximalIndex_L"},
            new string[] { "LeftIndexIntermediate","IntermediateIndex_Left","IntermediateIndex_L"},
            new string[] { "LeftIndexDistal","DistalIndex_Left","DistalIndex_L"},
            new string[] { "LeftMiddleProximal","ProximalMiddle_Left","ProximalMiddle_L"},
            new string[] { "LeftMiddleIntermediate","IntermediateMiddle_Left","IntermediateMiddle_L"},
            new string[] { "LeftMiddleDistal","DistalMiddle_Left","DistalMiddle_L"},
            new string[] { "LeftRingProximal","ProximalRing_Left","ProximalRing_L"},
            new string[] { "LeftRingIntermediate","IntermediateRing_Left","IntermediateRing_L"},
            new string[] { "LeftRingDistal","DistalRing_Left","DistalRing_L"},
            new string[] { "LeftLittleProximal","ProximalLittle_Left","ProximalLittle_L"},
            new string[] { "LeftLittleIntermediate","IntermediateLittle_Left","IntermediateLittle_L"},
            new string[] { "LeftLittleDistal","DistalLittle_Left","DistalLittle_L"},
            new string[] { "RightThumbProximal","ProximalThumb_Right","ProximalThumb_R"},
            new string[] { "RightThumbIntermediate","IntermediateThumb_Right","IntermediateThumb_R"},
            new string[] { "RightThumbDistal","DistalThumb_Right","DistalThumb_R"},
            new string[] { "RightIndexProximal","ProximalIndex_Right","ProximalIndex_R"},
            new string[] { "RightIndexIntermediate","IntermediateIndex_Right","IntermediateIndex_R"},
            new string[] { "RightIndexDistal","DistalIndex_Right","DistalIndex_R"},
            new string[] { "RightMiddleProximal","ProximalMiddle_Right","ProximalMiddle_R"},
            new string[] { "RightMiddleIntermediate","IntermediateMiddle_Right","IntermediateMiddle_R"},
            new string[] { "RightMiddleDistal","DistalMiddle_Right","DistalMiddle_R"},
            new string[] { "RightRingProximal","ProximalRing_Right","ProximalRing_R"},
            new string[] { "RightRingIntermediate","IntermediateRing_Right","IntermediateRing_R"},
            new string[] { "RightRingDistal","DistalRing_Right","DistalRing_R"},
            new string[] { "RightLittleProximal","ProximalLittle_Right","ProximalLittle_R"},
            new string[] { "RightLittleIntermediate","IntermediateLittle_Right","IntermediateLittle_R"},
            new string[] { "RightLittleDistal","DistalLittle_Right","DistalLittle_R"},
            new string[] { "UpperChest"},
            new string[] { "LastBone","Armature"}, // 本来的ではないけど，Rootもhitさせたい
        };
    }
}
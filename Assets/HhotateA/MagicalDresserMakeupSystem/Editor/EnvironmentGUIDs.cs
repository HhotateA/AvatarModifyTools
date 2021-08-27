/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System;
using HhotateA.AvatarModifyTools.Core;
using UnityEngine;
namespace HhotateA.AvatarModifyTools.MagicalDresserMakeupSystem
{
    public class EnvironmentGUIDs
    {
        public static string prefix = "HhotateA_MDM_";

        public static string readme = "8a5ca7da2ee6869468bed6aa839b68cf";
        public static string dresserIcon = "f2090e7d4a6654c42ab620402215324f";
        public static string rotateHIcon = "9041b681958a0bd43a5af42807df1af8";
        public static string rotateSIcon = "08899721d9396a24b9dfa42ae2ebfe5e";
        public static string rotateVIcon = "a194b0f31b1600540a7f717aa10c2a46";
        public static string rotateRIcon = "934af29dc12262b4eb4a86bd4d90fd6d";
        public static string rotateGIcon = "9d6a180f241a0944caf42ebfed91ffc1";
        public static string rotateBIcon = "69400a33ae8d5e147a34dbcfce213697";
        public static string blendShapeIcon = "bcb432c69b04b6a448ab309494503c55";
        
        public static string filterShader = "829a20cd89ea690429e2b5c4fd2375fd";
        public static string clippingShader = "39ecdfde388d64d41993fec7a136da70";
        
        public static Vector3[] rotateHSV = new Vector3[7]
        {
            new Vector3(0.0f,-1.0f,-0.9f),
            new Vector3(0.1f, 0.5f, 0.0f),
            new Vector3(0.3f, 0.5f, 0.0f),
            new Vector3(0.5f, 0.5f, 0.0f),
            new Vector3(0.7f, 0.5f, 0.0f),
            new Vector3(0.9f, 0.5f, 0.0f),
            new Vector3(1.0f,-1.0f, 0.9f), 
        };
    }
}
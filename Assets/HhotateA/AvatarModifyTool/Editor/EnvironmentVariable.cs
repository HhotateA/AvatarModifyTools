using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HhotateA.AvatarModifyTools.Core
{
    public static class EnvironmentVariable
    {
        public static ComputeShader GetComputeShader()
        {
            var computePath = AssetDatabase.GUIDToAssetPath("8e33ed767aaabf04eae3c3866bece392");
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(computePath);
            return compute;
        }
        
        public static int maxCaches = 16;
    }
}
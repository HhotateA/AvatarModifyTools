using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace HhotateA.AvatarModifyTools.Core
{
    public class ParametersCreater
    {
#if VRC_SDK_VRCSDK3
        private VRCExpressionParameters asset;
        List<AnimatorControllerParameter> parameters = new List<AnimatorControllerParameter>();
        public ParametersCreater(string name)
        {
            asset = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            asset.name = name;
        }

        public void LoadParams(AnimatorControllerCreator origin)
        {
            foreach (var param in origin.GetParameter())
            {
                if (parameters.All(p => p.name != param.name))
                {
                    parameters.Add(param);
                }
            }
        }

        public VRCExpressionParameters Create()
        {
            asset.parameters = parameters.Select(p => new VRCExpressionParameters.Parameter()
            {
                name = p.name,
                valueType =
                    p.type == AnimatorControllerParameterType.Bool ? VRCExpressionParameters.ValueType.Bool :
                    p.type == AnimatorControllerParameterType.Float ? VRCExpressionParameters.ValueType.Float :
                    p.type == AnimatorControllerParameterType.Int ? VRCExpressionParameters.ValueType.Int :
                    VRCExpressionParameters.ValueType.Bool,
                defaultValue = 0f,
                saved = true
            }).ToArray();
            return asset;
        }
        
        public VRCExpressionParameters CreateAsset(string path = null, bool subAsset = false)
        {
            Create();
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (subAsset)
                {
                    AssetDatabase.AddObjectToAsset(asset,path);
                }
                else
                {
                    AssetDatabase.CreateAsset(asset,
                        AssetDatabase.GenerateUniqueAssetPath(
                            Path.Combine(path,"_"+asset.name+".asset")));
                }
            }

            return asset;
        }
#endif
    }
}
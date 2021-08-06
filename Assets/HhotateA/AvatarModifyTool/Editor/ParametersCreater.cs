using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace HhotateA.AvatarModifyTools.Core
{
    public class ParametersCreater
    {
#if VRC_SDK_VRCSDK3
        private VRCExpressionParameters asset;
        List<AnimatorControllerParameter> parameters = new List<AnimatorControllerParameter>();
        List<AnimatorControllerParameter> savedParameters = new List<AnimatorControllerParameter>();
        public ParametersCreater(string name)
        {
            asset = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            asset.name = name;
        }

        public void AddParam(string param,
            float defaultValue = 0f, bool saved = false)
        {
            var old = savedParameters.FirstOrDefault(p => p.name == param);
            if (old != null) savedParameters.Remove(old);
            old = parameters.FirstOrDefault(p => p.name == param);
            if (old != null) parameters.Remove(old);
            
            if (saved)
            {
                savedParameters.Add(new AnimatorControllerParameter()
                {
                    name = param,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = defaultValue,
                });
            }
            else
            {
                parameters.Add(new AnimatorControllerParameter()
                {
                    name = param,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = defaultValue,
                });
            }
        }
        
        public void AddParam(string param,
            int defaultValue, bool saved = false)
        {
            var old = savedParameters.FirstOrDefault(p => p.name == param);
            if (old != null) savedParameters.Remove(old);
            old = parameters.FirstOrDefault(p => p.name == param);
            if (old != null) parameters.Remove(old);
            
            if (saved)
            {
                savedParameters.Add(new AnimatorControllerParameter()
                {
                    name = param,
                    type = AnimatorControllerParameterType.Int,
                    defaultInt = defaultValue,
                });
            }
            else
            {
                parameters.Add(new AnimatorControllerParameter()
                {
                    name = param,
                    type = AnimatorControllerParameterType.Int,
                    defaultInt = defaultValue,
                });
            }
        }
        
        public void AddParam(string param,
            bool defaultValue, bool saved = false)
        {
            var old = savedParameters.FirstOrDefault(p => p.name == param);
            if (old != null) savedParameters.Remove(old);
            old = parameters.FirstOrDefault(p => p.name == param);
            if (old != null) parameters.Remove(old);
            
            if (saved)
            {
                savedParameters.Add(new AnimatorControllerParameter()
                {
                    name = param,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = defaultValue,
                });
            }
            else
            {
                parameters.Add(new AnimatorControllerParameter()
                {
                    name = param,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = defaultValue,
                });
            }
        }

        public void LoadParams(AnimatorControllerCreator origin,bool saved = false)
        {
            foreach (var p in origin.GetParameter())
            {
                Debug.Log(p.name+":"+p.type);
                if (p.type == AnimatorControllerParameterType.Float)
                {
                    AddParam(p.name,p.defaultFloat,saved);
                }
                else if (p.type == AnimatorControllerParameterType.Int)
                {
                    AddParam(p.name,p.defaultInt,saved);
                }
                else if (p.type == AnimatorControllerParameterType.Bool)
                {
                    AddParam(p.name,p.defaultBool,saved);
                }
            }
        }

        public VRCExpressionParameters Create()
        {
            var ps = parameters.Select(p => new VRCExpressionParameters.Parameter()
            {
                name = p.name,
                valueType =
                    p.type == AnimatorControllerParameterType.Bool ? VRCExpressionParameters.ValueType.Bool :
                    p.type == AnimatorControllerParameterType.Float ? VRCExpressionParameters.ValueType.Float :
                    p.type == AnimatorControllerParameterType.Int ? VRCExpressionParameters.ValueType.Int :
                    VRCExpressionParameters.ValueType.Bool,
                defaultValue =
                    p.type == AnimatorControllerParameterType.Bool ? (p.defaultBool ? 1f : 0f) :
                    p.type == AnimatorControllerParameterType.Float ? p.defaultFloat :
                    p.type == AnimatorControllerParameterType.Int ? (float) p.defaultInt :  0f,
                saved = false
            }).ToList();
            foreach (var p in savedParameters)
            {
                ps.Add(
                    new VRCExpressionParameters.Parameter()
                    {
                        name = p.name,
                        valueType =
                            p.type == AnimatorControllerParameterType.Bool ? VRCExpressionParameters.ValueType.Bool :
                            p.type == AnimatorControllerParameterType.Float ? VRCExpressionParameters.ValueType.Float :
                            p.type == AnimatorControllerParameterType.Int ? VRCExpressionParameters.ValueType.Int :
                            VRCExpressionParameters.ValueType.Bool,
                        defaultValue =
                            p.type == AnimatorControllerParameterType.Bool ? (p.defaultBool ? 1f : 0f) :
                            p.type == AnimatorControllerParameterType.Float ? p.defaultFloat :
                            p.type == AnimatorControllerParameterType.Int ? (float) p.defaultInt :  0f,
                        saved = true
                    });
                
                
            }
            asset.parameters = ps.ToArray();
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
                    if (path.EndsWith(".asset"))
                    {
                    }
                    else
                    {
                        path = Path.Combine(path, asset.name+".asset");
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                    }
                    AssetDatabase.CreateAsset(asset,path);
                }
            }
            AssetDatabase.SaveAssets();
            return asset;
        }
#endif
    }
}
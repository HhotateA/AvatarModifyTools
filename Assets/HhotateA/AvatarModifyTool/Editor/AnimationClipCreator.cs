using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HhotateA.AvatarModifyTools.Core
{
    public class AnimationClipCreator
    {
        private AnimationClip asset;
        private GameObject root;
        private bool isLiner;
        private bool isSmooth;
        private bool isLoop;
        
        public AnimationClipCreator(string name,GameObject root = null,bool liner = true, bool smooth = true,bool loop = false)
        {
            asset = new AnimationClip();
            asset.name = name;
            this.root = root;
            isLiner = liner;
            isSmooth = smooth;
            isLoop = loop;
        }

        public struct KeyFrameTarget
        {
            public string path;
            public Type type;
            public string propety;
        }

        public struct KeyframeValue
        {
            public float time;
            public float value;
            public float weight;
        }

        private Dictionary<KeyFrameTarget, List<KeyframeValue>> keyframesList = new Dictionary<KeyFrameTarget, List<KeyframeValue>>();
        private Dictionary<KeyFrameTarget, List<ObjectReferenceKeyframe>> objectReferenceKeyframes = new Dictionary<KeyFrameTarget, List<ObjectReferenceKeyframe>>();

        public List<string> GetMembers(Type type)
        {
            MemberInfo[] members = type.GetMembers(BindingFlags.Public);
            
            List<string> l = new List<string>();
            foreach (MemberInfo m in members)
            {
                l.Add(m.Name);
            }

            return l;
        }

        public void AddKeyframe_Gameobject(GameObject go, float time, bool active)
        {
            KeyFrameTarget target = new KeyFrameTarget()
            {
                path = GetRelativePath(go),
                type = go.GetType(),
                propety = "m_IsActive",
            };
            KeyframeValue frame = new KeyframeValue()
            {
                time = time,
                value = active ? 1f : 0f,
                weight = 1f
            };
            
            AddKeyframe(target,frame);
        }

        public void AddKeyframe_Transform(float time, Transform tra, bool rot = true, bool pos = true,bool scale = true,float weight = 1f)
        {
            if (rot)
            {
                AddKeyframe_Rot(time,tra,tra.localRotation.eulerAngles, weight);
            }

            if (pos)
            {
                AddKeyframe_Pos(time,tra,tra.localPosition, weight);
            }
            
            if (scale)
            {
                AddKeyframe_Scale(time,tra,tra.localScale, weight);
            }
        }

        public void AddKeyframe_Rot(float time, Transform tra, Vector3 rot,float weight = 1f)
        {
            AddNormalizeKeyframe(time, tra, "localEulerAnglesRaw.x", rot.x, 360f, weight);
            AddNormalizeKeyframe(time, tra, "localEulerAnglesRaw.y", rot.y, 360f, weight);
            AddNormalizeKeyframe(time, tra, "localEulerAnglesRaw.z", rot.z, 360f, weight);
        }
        public void AddKeyframe_Pos(float time, Transform tra, Vector3 pos,float weight = 1f)
        {
            AddKeyframe(time, tra, "m_LocalPosition.x", pos.x, weight);
            AddKeyframe(time, tra, "m_LocalPosition.y", pos.y, weight);
            AddKeyframe(time, tra, "m_LocalPosition.z", pos.z, weight);
        }
        public void AddKeyframe_Scale(float time, Transform tra, Vector3 scale,float weight = 1f)
        {
            AddKeyframe(time, tra, "m_LocalScale.x", scale.x, weight);
            AddKeyframe(time, tra, "m_LocalScale.y", scale.y, weight);
            AddKeyframe(time, tra, "m_LocalScale.z", scale.z, weight);
        }

        public void AddKeyframe_Humanoid(Animator human,Transform bone, float time,float weight = 1f)
        {
            if (!human.isHuman) return;
            var bones = human.GetHumanBones().ToList();
            var boneid = bones.FindIndex(b=>b==bone);
            if(boneid != -1)
            {
                var handler = new HumanPoseHandler(human.avatar, human.transform);
                var humanpose = new HumanPose();
                string[] muscleBoneNames = HumanTrait.BoneName.Select(s=>s.Replace(" ","")).ToArray();
                string[] humanBoneNames = Enum.GetNames(typeof(HumanBodyBones)).Select(s=>s.Replace(" ","")).ToArray();
                handler.GetHumanPose(ref humanpose);
                for (int i = 0; i < HumanTrait.MuscleCount; i++)
                {
                    if (muscleBoneNames[HumanTrait.BoneFromMuscle(i)].Contains(humanBoneNames[boneid]))
                    {
                        AddKeyframe( time, human, HumanTrait.MuscleName[i], humanpose.muscles[i], weight);
                    }
                }
            }
        }
        
        public void AddKeyframe_Material(Renderer rend, Material mat, float time,int index = 0)
        {
            KeyFrameTarget target = new KeyFrameTarget()
            {
                path = GetRelativePath(rend.gameObject),
                type = rend.GetType(),
                propety = "m_Materials.Array.data["+index+"]",
            };
            ObjectReferenceKeyframe frame = new ObjectReferenceKeyframe()
            {
                time = time,
                value = mat
            };
            
            if (objectReferenceKeyframes.ContainsKey(target))
            {
                objectReferenceKeyframes[target].Add(frame);
            }
            else
            {
                var ks = new List<ObjectReferenceKeyframe>();
                ks.Add(frame);
                objectReferenceKeyframes.Add(target,ks);
            }
        }

        public void AddKeyframe_MaterialParam(float time, Renderer rend, string param, float value,float weight = 1f)
        {
            AddKeyframe(time, rend, "material."+param, value, weight);
        }
        
        public void AddKeyframe_MaterialParam(float time, Renderer rend, string param, Color value,float weight = 1f)
        {
            AddKeyframe(time, rend, "material."+param+".r", value.r, weight);
            AddKeyframe(time, rend, "material."+param+".g", value.g, weight);
            AddKeyframe(time, rend, "material."+param+".b", value.b, weight);
            AddKeyframe(time, rend, "material."+param+".a", value.a, weight);
        }
        
        public void AddNormalizeKeyframe(float time, Component o, string property, float value, float clamp = 360f, float weight = 1f)
        {
            KeyFrameTarget target = new KeyFrameTarget()
            {
                path = GetRelativePath(o.gameObject),
                type = o.GetType(),
                propety = property,
            };
            KeyframeValue frame = new KeyframeValue()
            {
                time = time,
                value = value,
                weight = weight
            };
            
            if (keyframesList.ContainsKey(target))
            {
                var vs = keyframesList[target].OrderBy(v => v.time).ToList();
                for (int i = 0; i < vs.Count; i++)
                {
                    if (i + 1 < vs.Count)
                    {
                        if (vs[i].time < time && time < vs[i].time)
                        {
                            while (frame.value > Mathf.Max(vs[i].value, vs[i + 1].value))
                            {
                                frame.value -= clamp;
                            }
                            while (frame.value < Mathf.Min(vs[i].value, vs[i + 1].value))
                            {
                                frame.value += clamp;
                            }
                        }
                    }
                    else
                    {
                        while (vs[i].value+clamp*0.5f<frame.value)
                        {
                            frame.value -= clamp;
                        }
                        while (vs[i].value-clamp*0.5f>frame.value)
                        {
                            frame.value += clamp;
                        }
                    }
                }
                keyframesList[target].Add(frame);
            }
            else
            {
                var ks = new List<KeyframeValue>();
                ks.Add(frame);
                keyframesList.Add(target,ks);
            }
        }
        public void AddKeyframe(float time, Component o, string property, float value, float weight = 1f)
        {
            KeyFrameTarget target = new KeyFrameTarget()
            {
                path = GetRelativePath(o.gameObject),
                type = o.GetType(),
                propety = property,
            };
            KeyframeValue frame = new KeyframeValue()
            {
                time = time,
                value = value,
                weight = weight
            };
            
            AddKeyframe(target,frame);
        }

        public void AddKeyframe(KeyFrameTarget target, KeyframeValue frame)
        {
            if (keyframesList.ContainsKey(target))
            {
                keyframesList[target].Add(frame);
            }
            else
            {
                var ks = new List<KeyframeValue>();
                ks.Add(frame);
                keyframesList.Add(target,ks);
            }
        }

        void ApplyKeyframes()
        {
            foreach (var keyframes in keyframesList)
            {
                var keyframeReorder = keyframes.Value.OrderBy(v => v.time).ToList();
                var ks = keyframeReorder.Select(k => 
                    new Keyframe(k.time, k.value){weightedMode = isLiner ? WeightedMode.Both : WeightedMode.None}).ToArray();
                AnimationCurve c = new AnimationCurve(ks);
                if (isSmooth)
                {
                    for (int i = 0; i < keyframeReorder.Count; i++)
                    {
                        if (keyframeReorder[i].weight > 0)
                        {
                            c.SmoothTangents(i,keyframeReorder[i].weight);
                        }
                    }
                }
                
                if (isLoop)
                {
                    c.preWrapMode = WrapMode.PingPong;
                    c.postWrapMode = WrapMode.PingPong;
                }
                else
                {
                    c.preWrapMode = WrapMode.Loop;
                    c.postWrapMode = WrapMode.Loop;
                }
                
                asset.SetCurve(keyframes.Key.path,keyframes.Key.type,keyframes.Key.propety,c);
            }

            foreach (var objectReferenceKeyframe in objectReferenceKeyframes)
            {
                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(objectReferenceKeyframe.Key.path, objectReferenceKeyframe.Key.type, objectReferenceKeyframe.Key.propety);
                AnimationUtility.SetObjectReferenceCurve(asset, binding, objectReferenceKeyframe.Value.ToArray());
            }
        }

        string GetRelativePath(GameObject o)
        {
            return AssetUtility.GetRelativePath(root.transform, o.transform);
        }
        
        public AnimationClip Create()
        {
            ApplyKeyframes();
            if (isLoop)
            {
                asset.wrapMode = WrapMode.Loop;
                var s = new SerializedObject (asset);
                s.FindProperty ("m_AnimationClipSettings.m_LoopTime").boolValue = true;
                s.ApplyModifiedProperties ();
            }
            //asset.EnsureQuaternionContinuity();
            return asset;
        }
        
        /// <summary>
        /// ディレクトリにアセットを作成する
        /// </summary>
        public AnimationClip CreateAsset(string path = null, bool subAsset = false)
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
                    if (path.EndsWith(".anim"))
                    {
                    }
                    else
                    {
                        path = Path.Combine(path, asset.name+".anim");
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                    }
                    AssetDatabase.CreateAsset(asset,path);
                }
            }
            AssetDatabase.SaveAssets();
            return asset;
        }
    }

    public class BlendTreeCreator
    {
        static BlendTree Simple1D(string x,Motion[] motions,string path="")
        {
            var bt = new BlendTree()
            {
                blendParameter = x,
                blendType = BlendTreeType.Simple1D,
                maxThreshold = 1f,
                minThreshold = 0f,
                useAutomaticThresholds = true,
            };
            foreach (var m in motions)
            {
                bt.AddChild(m);
            }

            return bt;
        }
        
        static BlendTree Simple2D(string x,string y,Dictionary<Motion,Vector2> motions,string path="")
        {
            var bt = new BlendTree()
            {
                blendParameter = x,
                blendParameterY = y,
                blendType = BlendTreeType.SimpleDirectional2D,
                maxThreshold = 1f,
                minThreshold = 0f,
                useAutomaticThresholds = true,
            };
            foreach (var m in motions)
            {
                bt.AddChild(m.Key,m.Value);
            }

            return bt;
        }
    }
}
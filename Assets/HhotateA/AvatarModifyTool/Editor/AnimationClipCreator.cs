using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Reflection;

namespace HhotateA.AvatarModifyTools.Core
{
    public class AnimationClipCreator
    {
        private AnimationClip asset;
        private GameObject root;
        
        public AnimationClipCreator(string name,GameObject root = null)
        {
            asset = new AnimationClip();
            asset.name = name;
            this.root = root;
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
            public float inTangent;
            public float outTangent;
            public WeightedMode mode;
        }

        private Dictionary<KeyFrameTarget, List<KeyframeValue>> keyframesList = new Dictionary<KeyFrameTarget, List<KeyframeValue>>();

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
            };
            frame.mode = WeightedMode.None;
            frame.inTangent = 0f;
            frame.outTangent = 0f;
            
            AddKeyframe(target,frame);
        }

        public void AddKeyframe(Component o, string property, float time, float value, float weight = Single.MaxValue)
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
            };
            
            if (weight == Single.MaxValue)
            {
                frame.mode = WeightedMode.None;
                frame.inTangent = weight;
                frame.outTangent = weight;
            }
            else
            {
                frame.mode = WeightedMode.Both;
                frame.inTangent = weight;
                frame.outTangent = weight;
            }
            
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
                var ks = keyframes.Value.OrderBy(v => v.time).ToList();

                var c = AnimationCurve.Linear(ks[0].time, ks[0].value, ks[ks.Count-1].time, ks[ks.Count-1].value);
                for (int i = 0; i < ks.Count; i++)
                {
                    Keyframe k = new Keyframe(ks[i].time,ks[i].value);
                    k.weightedMode = ks[i].mode;
                    k.inTangent = ks[i].inTangent;
                    k.outTangent = ks[i].outTangent;
                    c.AddKey(k);
                }
                asset.SetCurve(keyframes.Key.path,keyframes.Key.type,keyframes.Key.propety,c);
            }
        }

        public void CreateAnimation(Component o, string property,
            float t0, float v0, float duration=0f,Dictionary<float,float> keys = null)
        {
            AnimationCurve curve = AnimationCurve.Constant(t0, t0 + duration, v0);
            
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    Keyframe k = new Keyframe(key.Key,key.Value);
                    curve.AddKey(k);
                }
            }
            
            asset.SetCurve(GetRelativePath(o.gameObject),o.GetType(),property,curve);
        }
        public void CreateAnimation(GameObject o, string property,
            float t0, float v0, float duration=0f,Dictionary<float,float> keys = null)
        {
            AnimationCurve curve = AnimationCurve.Constant(t0, t0 + duration, v0);
            
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    Keyframe k = new Keyframe(key.Key,key.Value);
                    curve.AddKey(k);
                }
            }
            
            asset.SetCurve(GetRelativePath(o.gameObject),o.GetType(),property,curve);
        }
        
        public void CreateAnimation(Component o, string property,
            float t0, float v0,
            float t1, float v1,
            Dictionary<float,float> keys = null)
        {
            AnimationCurve curve = AnimationCurve.Linear(t0,v0,t1,v1);
            
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    Keyframe k = new Keyframe(key.Key,key.Value);
                    curve.AddKey(k);
                }
            }
            
            asset.SetCurve(GetRelativePath(o.gameObject),o.GetType(),property,curve);
        }
        
        public void CreateAnimation(GameObject o, string property,
            float t0, float v0,
            float t1, float v1,
            Dictionary<float,float> keys = null)
        {
            AnimationCurve curve = AnimationCurve.Linear(t0,v0,t1,v1);
            
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    Keyframe k = new Keyframe(key.Key,key.Value);
                    curve.AddKey(k);
                }
            }
            
            asset.SetCurve(GetRelativePath(o.gameObject),o.GetType(),property,curve);
        }

        string GetRelativePath(GameObject o)
        {
            string path = o.gameObject.name;
            Transform parent = o.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
                if (parent == null) break;
                if(parent.gameObject == root) break;
            }

            return path;
        }
        
        public AnimationClip Create()
        {
            ApplyKeyframes();
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
                    if(!path.EndsWith(".anim")) path = Path.Combine(path, asset.name+".anim");
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CreateAsset(asset,path);
                }
            }
            
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
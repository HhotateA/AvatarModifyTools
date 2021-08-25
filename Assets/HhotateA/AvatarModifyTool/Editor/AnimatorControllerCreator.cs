/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Random = UnityEngine.Random;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
#endif

namespace HhotateA.AvatarModifyTools.Core
{
    public class AnimatorControllerCreator
    {
        private AnimatorController asset;

        private AnimatorStateMachine stateMachine;
        private int editLayer = -1;

        AnimatorStateMachine GetStateMachine()
        {
            return asset.layers[editLayer].stateMachine;
        }
        
        public AnimatorControllerCreator(string name,bool defaultLayer = true)
        {
            asset = new AnimatorController()
            {
                name = name,
            };
            if (defaultLayer)
            {
                asset.AddLayer(
                    new AnimatorControllerLayer()
                    {
                        avatarMask = null,
                        blendingMode = AnimatorLayerBlendingMode.Override,
                        defaultWeight = 1f,
                        iKPass =  false,
                        name = name,
                        stateMachine = new AnimatorStateMachine(),
                    });
                editLayer++;
            }
        }
        public AnimatorControllerCreator(string name,string layerName)
        {
            asset = new AnimatorController()
            {
                name = name,
            };
            asset.AddLayer(
                new AnimatorControllerLayer()
                {
                    avatarMask = null,
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    defaultWeight = 1f,
                    iKPass =  false,
                    name = layerName,
                    stateMachine = new AnimatorStateMachine(),
                });
            editLayer++;
        }
        
        /// <summary>
        /// パラメーターを追加する
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public void AddParameter(string name,AnimatorControllerParameterType type)
        {
            asset.AddParameter(name,type);
        }
        public void AddParameter(string name,bool value)
        {
            if (asset.parameters.All(p=>p.name!=name))
            {
                asset.AddParameter(name,AnimatorControllerParameterType.Bool);
                var ps = asset.parameters;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].name == name)
                    {
                        ps[i].defaultBool = value;
                    }
                }
            }
        }
        public void AddParameter(string name,int value)
        {
            if (asset.parameters.All(p=>p.name!=name))
            {
                asset.AddParameter(name,AnimatorControllerParameterType.Int);
                var ps = asset.parameters;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].name == name)
                    {
                        ps[i].defaultInt = value;
                    }
                }
            }
        }
        public void AddParameter(string name,float value)
        {
            if (asset.parameters.All(p=>p.name!=name))
            {
                asset.AddParameter(name,AnimatorControllerParameterType.Float);
                var ps = asset.parameters;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].name == name)
                    {
                        ps[i].defaultFloat = value;
                    }
                }
                asset.parameters = ps;
            }
        }

        public AnimatorControllerParameter[] GetParameter()
        {
            return asset.parameters;
        }

        public class MotionConditionPair
        {
            public Motion motion;
            public AnimatorCondition[] condition;
        }

        public void AnyStateToDefault(string name,Motion motion, string param, int value)
        {
            if (asset.parameters.All(p=>p.name!=param))
            {
                AddParameter(param,AnimatorControllerParameterType.Int);
            }
            AnyStateToDefault(name, motion,
                new AnimatorCondition[1]
                {
                    new AnimatorCondition()
                    {
                        mode = AnimatorConditionMode.Equals,
                        parameter = param,
                        threshold = value,
                    }
                },
                new AnimatorCondition[1]
                {
                    new AnimatorCondition()
                    {
                        mode = AnimatorConditionMode.NotEqual,
                        parameter = param,
                        threshold = value,
                    }
                });
        }

        /// <summary>
        /// AnyStateからDefaultStateにいたる遷移を追加する
        /// </summary>
        /// <param name="m"></param>
        /// <param name="entryCondition"></param>
        /// <param name="exitCondition"></param>
        /// <returns></returns>
        public AnimatorState AnyStateToDefault(string name,
            Motion motion,
            AnimatorCondition[] entryCondition,
            AnimatorCondition[] exitCondition = null,
            List<MotionConditionPair> motions = null)
        {
            List<AnimatorState> childStates = new List<AnimatorState>();
            if (motions == null) motions = new List<MotionConditionPair>();
            motions.InsertRange(0,new List<MotionConditionPair>()
            {
                new MotionConditionPair() {motion = motion, condition = exitCondition}
            });
            
            for (int i = 0; i < motions.Count; i++)
            {
                childStates.Add(GetStateMachine().AddState(name,RandomPosition()));
                {
                    childStates[i].motion = motions[i].motion;
                    childStates[i].writeDefaultValues = false;
                }
            }
            
            AnimatorState deststate = GetStateMachine().defaultState;
            for (int i = 0; i < motions.Count; i++)
            {
                if (i == motions.Count - 1) deststate = GetStateMachine().defaultState;
                else deststate = childStates[i + 1];
                
                if(motions[i].condition==null)
                {
                    var exit = childStates[i].AddTransition(deststate);
                    {
                        exit.canTransitionToSelf = false;
                        exit.duration = 0f;
                        exit.exitTime = 1f;
                        exit.hasExitTime = true;
                        exit.hasFixedDuration = true;
                        exit.conditions = null;
                    };
                }
                else
                {
                    var exit = childStates[i].AddTransition(deststate);
                    {
                        exit.canTransitionToSelf = false;
                        exit.duration = 0f;
                        exit.exitTime = 0f;
                        exit.hasExitTime = false;
                        exit.hasFixedDuration = true;
                        exit.conditions = motions[i].condition;
                    };
                }
            }
            
            var entry = GetStateMachine().AddAnyStateTransition(childStates[0]);
            {
                entry.canTransitionToSelf = false;
                entry.duration = 0f;
                entry.exitTime = 0f;
                entry.hasExitTime = false;
                entry.hasFixedDuration = true;
                entry.conditions = entryCondition;
            };

            return childStates[0];
        }
        
        /// <summary>
        /// DefaultStateを追加する．
        /// </summary>
        /// <param name="name"></param>
        /// <param name="m"></param>
        public void AddDefaultState(string name,Motion m = null)
        {
            AddState(name,m);
            SetDefaultState(name);
        }
        
        /// <summary>
        /// Stateを追加する．
        /// </summary>
        /// <param name="name"></param>
        /// <param name="motion"></param>
        public AnimatorState AddState(string name,Motion motion = null,bool writeDefault = false)
        {
            var s = GetStateMachine().AddState(name, RandomPosition());
            s.name = name;
            s.motion = motion;
            s.writeDefaultValues = writeDefault;
            return GetState("name");
        }

        public AnimatorState SetWriteDefault(string name, bool writeDefault = false)
        {
            var state = GetState(name);
            state.writeDefaultValues = writeDefault;
            return state;
        }
        
        /// <summary>
        /// DefaultStateを設定する
        /// </summary>
        /// <param name="name"></param>
        public void SetDefaultState(string name)
        {
            var d = GetState(name);
            if (d != null) GetStateMachine().defaultState = d;
        }

        /// <summary>
        /// fromStateからtoStateへのTransitionを作成する
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="condition"></param>
        /// <param name="hasExitTime"></param>
        /// <param name="exitTime"></param>
        /// <param name="duration"></param>
        public void AddTransition(
            string from, string to,
            AnimatorCondition[] conditions,
            bool hasExitTime = true,
            float exitTime = 1f,
            float duration = 0f)
        {
            var f = GetState(from);
            var t = GetState(to);
            AnimatorStateTransition transition;
            if(from == "Any" && t != null)
            {
                transition = GetStateMachine().AddAnyStateTransition(t);

            }
            else if (to == "Exit" && f != null)
            {
                transition = f.AddExitTransition();
            }
            else if(t != null && f != null)
            {
                transition = f.AddTransition(t);
            }
            else
            {
                return;
            }

            transition.conditions = conditions;
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
        }
        
        public void AddTransition(
            string from, string to,
            bool hasExitTime = true,
            float exitTime = 1f,
            float duration = 0f)
        {
            AddTransition(from, to, new AnimatorCondition[]{}, hasExitTime, exitTime, duration);
        }
        
        public void AddTransition(
            string from, string to,
            string param,
            int value,
            bool equal = true,
            bool hasExitTime = false,
            float exitTime = 0f,
            float duration = 0f)
        {
            var conditions = new AnimatorCondition()
            {
                mode = equal ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual,
                parameter = param,
                threshold = value
            };
            
            if (asset.parameters.All(p=>p.name!=param))
            {
                AddParameter(param,AnimatorControllerParameterType.Int);
            }

            AddTransition(from, to, new AnimatorCondition[1] {conditions}, hasExitTime, exitTime, duration);
        }
        
        public void AddTransition(
            string from, string to,
            string param,
            bool value,
            bool hasExitTime = false,
            float exitTime = 0f,
            float duration = 0f)
        {
            var conditions = new AnimatorCondition()
            {
                mode = value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                parameter = param,
                threshold = value ? 1 : 0
            };
            
            if (asset.parameters.All(p=>p.name!=param))
            {
                AddParameter(param,AnimatorControllerParameterType.Bool);
            }

            AddTransition(from, to, new AnimatorCondition[1] {conditions}, hasExitTime, exitTime, duration);
        }
        
        public void AddTransition(
            string from, string to,
            string param,
            float value,
            bool greater = true,
            bool hasExitTime = false,
            float exitTime = 0f,
            float duration = 0f)
        {
            var conditions = new AnimatorCondition()
            {
                mode = greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less,
                parameter = param,
                threshold = value
            };
            
            if (asset.parameters.All(p=>p.name!=param))
            {
                AddParameter(param,AnimatorControllerParameterType.Float);
            }

            AddTransition(from, to, new AnimatorCondition[1] {conditions}, hasExitTime, exitTime, duration);
        }


        /// <summary>
        /// 名前からStateを探す
        /// </summary>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public AnimatorState GetState(string name, int layer = -1)
        {
            if (layer == -1)
            {
                return GetStateMachine().states.FirstOrDefault(s => s.state.name == name).state;
            }
            else
            {
                return asset.layers[layer].stateMachine.states.FirstOrDefault(s => s.state.name == name).state;
            }
        }

        public void SetEditLayer(int value)
        {
            Debug.Log(value);
            if (value >= 0)
            {
                editLayer = value;
            }
        }

        public int GetEditLayer(string name)
        {
            return asset.layers.ToList().FindIndex(l => l.name == name);
        }
        
        /// <summary>
        /// 新たなレイヤーを作る
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public AnimatorController CreateLayer(string layerName = "")
        {
            if (!String.IsNullOrWhiteSpace(layerName))
            {
                asset.AddLayer(
                    new AnimatorControllerLayer()
                    {
                        avatarMask = null,
                        blendingMode = AnimatorLayerBlendingMode.Override,
                        defaultWeight = 1f,
                        iKPass =  false,
                        name = layerName,
                        stateMachine = new AnimatorStateMachine(),
                    });
                editLayer = asset.layers.Length - 1;
            }
            return asset;
        }

        public AnimatorController Create()
        {
            foreach (var l in asset.layers)
            {
                AlignmentMachine(l.stateMachine);
            }
            return asset;
        }
        
        /// <summary>
        /// ディレクトリにアセットを作成する
        /// </summary>
        public AnimatorController CreateAsset(string path = null, bool subAsset = false)
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
                    if (path.EndsWith(".controller"))
                    {
                    }
                    else
                    {
                        path = Path.Combine(path, asset.name+".controller");
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                    }
                    AssetDatabase.CreateAsset(asset,path);
                }

                foreach (var l in asset.layers)
                {
                    SaveLayer(l, path);
                }

            }
            AssetDatabase.SaveAssets();
            return asset;
        }

        void SaveLayer(AnimatorControllerLayer l, string path)
        {
            if (l.avatarMask)
            {
                if (AssetUtility.GetAssetGuid(l.avatarMask) != EnvironmentVariable.nottingAvatarMask)
                {
                    AssetDatabase.AddObjectToAsset(l.avatarMask,path);
                }
            }
            SaveStateMachine(l.stateMachine,path);
        }

        void SaveStateMachine(AnimatorStateMachine machine,string path)
        {
            AssetDatabase.AddObjectToAsset(machine,path);
            foreach (var s in machine.states)
            {
                AssetDatabase.AddObjectToAsset(s.state,path);
                SaveMotion(s.state.motion,path);
                foreach (var t in s.state.transitions)
                {
                    AssetDatabase.AddObjectToAsset(t,path);
                }
#if VRC_SDK_VRCSDK3
                foreach (var b in s.state.behaviours)
                {
                    AssetDatabase.AddObjectToAsset(b,path);
                }
#endif
            }

            foreach (var t in machine.entryTransitions)
            {
                AssetDatabase.AddObjectToAsset(t,path);
            }
            foreach (var t in machine.anyStateTransitions)
            {
                AssetDatabase.AddObjectToAsset(t,path);
            }
            foreach (var m in machine.stateMachines)
            {
                SaveStateMachine(m.stateMachine,path);
#if VRC_SDK_VRCSDK3
                foreach (var b in m.stateMachine.behaviours)
                {
                    AssetDatabase.AddObjectToAsset(b,path);
                }
#endif
            }
        }

        void SaveMotion(Motion motion,string path)
        {
            if (motion == null) return;
            if (motion is BlendTree)
            {
                BlendTree tree = (BlendTree) motion;
                AssetDatabase.AddObjectToAsset(tree,path);
                foreach (var m in tree.children)
                {
                    SaveMotion(m.motion, path);
                }
            }
        }

        void AlignmentMachine(AnimatorStateMachine machine)
        {
            machine.exitPosition = new Vector3(0f,-100f,0f);
            
            Vector2 cursor = Vector3.zero;
            machine.entryPosition = cursor;
            cursor = AlignmentState(machine.defaultState, cursor, machine);

            machine.anyStatePosition = cursor;
            foreach (var transition in machine.anyStateTransitions)
            {
                cursor = AlignmentState(transition.destinationState, cursor, machine);
            }

            foreach (var state in machine.states)
            {
                cursor = AlignmentStateReversal(state.state,cursor,machine);
            }
        }

        Vector2 AlignmentState(AnimatorState state, Vector2 cursor, AnimatorStateMachine machine)
        {
            int index = Array.FindIndex(machine.states, s => s.state.name == state.name);
            if (index < 0) return Vector2.zero;
            var list = machine.states.ToList();
            if (machine.states[index].position.y < -999f)
            {
                list.Remove(machine.states[index]);
                cursor += new Vector2(300f, 0f);
                var childState = new ChildAnimatorState()
                {
                    state = state,
                    position = new Vector3(cursor.x,cursor.y,0f)
                };
                list.Add(childState);
                machine.states = list.ToArray();
                foreach (var transition in state.transitions)
                {
                    if (transition.destinationState != null)
                    {
                        cursor += new Vector2(0f, 100f);
                        cursor = AlignmentState(transition.destinationState, cursor, machine);
                    }

                    if (transition.isExit)
                    {
                        Vector2 newpos = cursor + new Vector2(300f, 0f);
                        newpos = new Vector2(Mathf.Max(newpos.x,machine.exitPosition.x),newpos.y);
                        machine.exitPosition = newpos;
                    }
                }
                cursor -= new Vector2(300f, 0f);
            }
            return cursor;
        }
        Vector2 AlignmentStateReversal(AnimatorState state, Vector2 cursor, AnimatorStateMachine machine)
        {
            int index = Array.FindIndex(machine.states, s => s.state.name == state.name);
            var list = machine.states.ToList();
            if (machine.states[index].position.y < -999f)
            {
                list.Remove(machine.states[index]);
                
                // 子State検知
                int c = -1;
                if (state.transitions != null)
                {
                    if (state.transitions.Length > 0)
                    {
                        if (state.transitions[0].destinationState != null)
                        {
                            c = Array.FindIndex(machine.states, s => s.state.name == state.transitions[0].name);
                        }
                    }
                }
                cursor += new Vector2(300f, 0f);
                var newPos = cursor;
                if (c > -1)
                {
                    newPos = new Vector2(machine.states[c].position.x-300f, machine.states[c].position.y+50f);
                }

                int p = Array.FindIndex(machine.states,
                    s => s.state.transitions.FirstOrDefault(
                        t => t.destinationState?.name == state.name) != null);
                
                // 重なり検知
                while (true)
                {
                    int o = Array.FindIndex(machine.states, s => Vector2.Distance(new Vector2(s.position.x, s.position.y), newPos)<10f);
                    if(o == -1) break;
                    newPos += new Vector2(-300f,0f);
                }

                // 孤立State
                if (p == -1 && state.transitions.Length == 0)
                {
                    newPos = new Vector2(Random.Range(-500f,500f),Random.Range(-900f,-200f));
                }
                
                var childState = new ChildAnimatorState()
                {
                    state = state,
                    position = new Vector3(newPos.x,newPos.y,0f)
                };
                list.Add(childState);
                machine.states = list.ToArray();
                foreach (var transition in state.transitions)
                {
                    if (transition.destinationState != null)
                    {
                        cursor += new Vector2(0f, 100f);
                        cursor = AlignmentState(transition.destinationState, cursor, machine);
                    }

                    if (transition.isExit)
                    {
                        newPos = cursor + new Vector2(300f, 0f);
                        newPos = new Vector2(Mathf.Max(newPos.x,machine.exitPosition.x),newPos.y);
                        machine.exitPosition = newPos;
                    }
                }
                cursor -= new Vector2(300f, 0f);
            }
            return cursor;
        }

        Vector2 RandomPosition()
        {
            var x = (float)Random.Range(-100, 100) *100f;
            var y = -1000f;
            var z = 0f;
            return new Vector3( x, y, z);
        }

        public void LayerMask(AvatarMaskBodyPart part,bool active,bool? other = null)
        {
            AnimatorControllerLayer[] layers = asset.layers;
            var avatarMask = layers[editLayer].avatarMask;
            if (avatarMask == null)
            {
                avatarMask = new AvatarMask();
                avatarMask.name = part.ToString() + "_Mask";
            }
            if (other != null)
            {
                foreach (AvatarMaskBodyPart p in Enum.GetValues(typeof(AvatarMaskBodyPart)))
                {
                    if (p != AvatarMaskBodyPart.LastBodyPart)
                    {
                        avatarMask.SetHumanoidBodyPartActive(p,other ?? false);
                    }
                }
            }
            avatarMask.SetHumanoidBodyPartActive(part,active);
            layers[editLayer].avatarMask = avatarMask;
            asset.layers = layers;
        }
        
        public void LayerTransformMask(GameObject root,List<GameObject> objs,bool active)
        {
            if(objs==null) objs = new List<GameObject>();
            AnimatorControllerLayer[] layers = asset.layers;
            var avatarMask = layers[editLayer].avatarMask;
            if (avatarMask == null)
            {
                avatarMask = new AvatarMask();
                avatarMask.name = root.name + "_Mask";
            }
            
            var ts = Childs(root.transform);
            avatarMask.transformCount = ts.Count;
            for (int i = 0; i < ts.Count; i++)
            {
                avatarMask.SetTransformPath(i,GetRelativePath(root,ts[i].gameObject));
                avatarMask.SetTransformActive(i,
                    objs.Contains(ts[i].gameObject) ? active : !active);
            }
            
            layers[editLayer].avatarMask = avatarMask;
            asset.layers = layers;
        }
        
        public void LayerTransformMask(GameObject root,bool active)
        {
            LayerTransformMask(root, new List<GameObject>(), !active);
        }
        
        public void ObjectOnlyLayerMask()
        {
            AnimatorControllerLayer[] layers = asset.layers;
            layers[editLayer].avatarMask = AssetUtility.LoadAssetAtGuid<AvatarMask>(EnvironmentVariable.nottingAvatarMask);
            asset.layers = layers;
        }
        
        List<Transform> Childs(Transform p,bool root = false)
        {
            var l = new List<Transform>();
            if (!root)
            {
                l.Add(p);
            }
            foreach (Transform c in p)
            {
                l.AddRange(Childs(c));
            }
            return l;
        }
        
        public string GetRelativePath(GameObject root,GameObject obj)
        {
            string path;
            if (obj == root)
            {
                path = "";
            }
            else
            {
                path = obj.gameObject.name;
                Transform parent = obj.transform.parent;
                while (parent != null)
                {
                    if(parent.gameObject == root) break;
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
            }

            return path;
        }
        
        public void EditStateMachineBehaviour <T>(string stateName,Action<T> edit,bool asNew = false) where T : StateMachineBehaviour 
        {
            var state = GetState(stateName);
            if (state == null) return;
            var bs = state.behaviours.ToList();
            T behaviour;
            if (asNew)
            {
                behaviour = ScriptableObject.CreateInstance<T>();
            }
            else
            if (bs.Any(b => b.GetType() == typeof(T)))
            {
                behaviour = (T) bs.FirstOrDefault(b => b.GetType() == typeof(T));
                bs.Remove(behaviour);
            }
            else
            {
                behaviour = ScriptableObject.CreateInstance<T>();
            }

            edit(behaviour);
            
            bs.Add(behaviour);
            state.behaviours = bs.ToArray();
        }

        public void SetStateSpeed(string stateName,string param = "")
        {
            var state = GetState(stateName);
            if (!String.IsNullOrWhiteSpace(param))
            {
                AddParameter(param,AnimatorControllerParameterType.Float);
                state.speedParameter = param;
                state.speedParameterActive = true;
            }
            else
            {
                state.speedParameterActive = false;
            }
        }
        public void SetStateSpeed(string stateName,float speed)
        {
            var state = GetState(stateName);
            state.speed = speed;
            state.speedParameterActive = false;
        }
        public void SetStateTime(string stateName,string param = "")
        {
            var state = GetState(stateName);
            if (!String.IsNullOrWhiteSpace(param))
            {
                if (asset.parameters.All(p => p.name != param))
                {
                    AddParameter(param, AnimatorControllerParameterType.Float);
                }

                state.timeParameter = param;
                state.timeParameterActive = true;
            }
            else
            {
                state.timeParameterActive = false;
            }
        }
        
#if VRC_SDK_VRCSDK3
        public void SetAnimationTracking(string stateName,VRCTrackingMask mask,bool isAnimationTracking = true)
        {
            EditStateMachineBehaviour<VRCAnimatorTrackingControl>(stateName, (tracking) =>
            {
                var type = isAnimationTracking
                    ? VRC_AnimatorTrackingControl.TrackingType.Animation
                    : VRC_AnimatorTrackingControl.TrackingType.Tracking;
                if (mask == VRCTrackingMask.Haad) tracking.trackingHead = type;
                if (mask == VRCTrackingMask.LeftHand) tracking.trackingLeftHand = type;
                if (mask == VRCTrackingMask.RightHand) tracking.trackingRightHand = type;
                if (mask == VRCTrackingMask.Hip) tracking.trackingHip = type;
                if (mask == VRCTrackingMask.LeftFoot) tracking.trackingLeftFoot = type;
                if (mask == VRCTrackingMask.RightFoot) tracking.trackingRightFoot = type;
                if (mask == VRCTrackingMask.LeftFingers) tracking.trackingLeftFingers = type;
                if (mask == VRCTrackingMask.RightFingers) tracking.trackingRightFingers = type;
                if (mask == VRCTrackingMask.Eyes) tracking.trackingEyes = type;
                if (mask == VRCTrackingMask.Mouth) tracking.trackingMouth = type;
            },false);
        }
        
        public void SetLayerControll(string stateName,VRCLayers layer,float weight,float duration = 0.25f)
        {
            EditStateMachineBehaviour<VRCPlayableLayerControl>(stateName, (controll) =>
            {
                if (layer == VRCLayers.Action) controll.layer = VRC_PlayableLayerControl.BlendableLayer.Action;
                if (layer == VRCLayers.Fx) controll.layer = VRC_PlayableLayerControl.BlendableLayer.FX;
                if (layer == VRCLayers.Gesture) controll.layer = VRC_PlayableLayerControl.BlendableLayer.Gesture;
                if (layer == VRCLayers.Idle) controll.layer = VRC_PlayableLayerControl.BlendableLayer.Additive;

                controll.goalWeight = weight;
                controll.blendDuration = duration;
            },true);
        }
        public void SetLayerControll(string stateName,VRCLayers layer,int layerNum,float weight,float duration = 0.25f)
        {
            EditStateMachineBehaviour<VRCAnimatorLayerControl>(stateName, (controll) =>
            {
                if (layer == VRCLayers.Action) controll.playable = VRC_AnimatorLayerControl.BlendableLayer.Action;
                if (layer == VRCLayers.Fx) controll.playable = VRC_AnimatorLayerControl.BlendableLayer.FX;
                if (layer == VRCLayers.Gesture) controll.playable = VRC_AnimatorLayerControl.BlendableLayer.Gesture;
                if (layer == VRCLayers.Idle) controll.playable = VRC_AnimatorLayerControl.BlendableLayer.Additive;
                
                controll.layer = layerNum;
                controll.goalWeight = weight;
                controll.blendDuration = duration;
            },true);
        }
        
        public void SetLayerLocomotion(string stateName,bool active)
        {
            EditStateMachineBehaviour<VRCAnimatorLocomotionControl>(stateName, (controll) =>
            {
                controll.disableLocomotion = !active;
            },false);
        }
        
        public void ParameterDriver(string stateName,string param,float? value,bool add = false,bool local = false)
        {
            EditStateMachineBehaviour<VRCAvatarParameterDriver>(stateName, (controll) =>
            {
                controll.parameters.Add(new VRC_AvatarParameterDriver.Parameter()
                {
                    type = value==null ? VRC_AvatarParameterDriver.ChangeType.Random : 
                        add ? VRC_AvatarParameterDriver.ChangeType.Add : VRC_AvatarParameterDriver.ChangeType.Set,
                    value = value ?? 0f,
                    valueMin = 0f,
                    valueMax = value ?? 1f,
                    name = param
                });
                controll.localOnly = local;
            },false);
        }
        
        public void ParameterDriver(string stateName,string param,float from,float to,float chance = 0f,bool local = false)
        {
            EditStateMachineBehaviour<VRCAvatarParameterDriver>(stateName, (controll) =>
            {
                controll.parameters.Add(new VRC_AvatarParameterDriver.Parameter()
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Random,
                    value = 0f,
                    valueMin = from,
                    valueMax = to,
                    name = param
                });
                controll.localOnly = local;
            },false);
        }

        public enum VRCTrackingMask
        {
            Haad,
            LeftHand,
            RightHand,
            Hip,
            LeftFoot,
            RightFoot,
            LeftFingers,
            RightFingers,
            Eyes,
            Mouth
        }

        public enum VRCLayers
        {
            Locomotion,
            Idle,
            Gesture,
            Action,
            Fx,
        }
#endif
    }
}

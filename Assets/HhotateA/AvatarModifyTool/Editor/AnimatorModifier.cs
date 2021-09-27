/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using AnimatorLayerType = HhotateA.AvatarModifyTools.Core.AnimatorUtility.AnimatorLayerType;

#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
#endif


namespace HhotateA.AvatarModifyTools.Core
{

    public class AnimatorModifier
    {
        public AnimatorModifier(AnimatorController c = null)
        {
            this.currentController = c;
        }
        
        public AnimatorModifier SetOrigin(AnimatorController c)
        {
            this.currentController = c;
            return this;
        }

        public AnimatorController currentController;
        public event Func<string,string> onFindParam;
        public event Func<AnimationClip,AnimationClip> onFindAnimationClip;
        public event Func<AvatarMask,AvatarMask> onFindAvatarMask;
        public Dictionary<string, string> animRepathList = new Dictionary<string, string>();
        public bool? writeDefaultOverride { get; set; } = null;
        
        public Dictionary<AnimatorLayerType, int> layerOffset = new Dictionary<AnimatorLayerType, int>();
        
        /// <summary>
        /// AnimatorControllerのStateMachineとParameterの結合
        /// </summary>
        /// <param name="origin"></param>
        public AnimatorController ModifyAnimatorController(AnimatorController origin)
        {
            if (currentController == null) return null;
            if (currentController == origin) return null;
            
            int originLayerCount = currentController.layers.Length;
            CloneLayers(origin);
            CloneAnimatorParamaters(currentController, origin);
            for (int i = originLayerCount; i < currentController.layers.Length; i++)
            {
                SaveLayer(currentController.layers[i], AssetDatabase.GetAssetPath(currentController));
            }
            EditorUtility.SetDirty(currentController);
            return currentController;
        }

        public AnimatorController RevertAnimator(AnimatorController origin)
        {
            if (currentController == null) return null;
            if (currentController == origin) return null;
            
            var newLayers = new List<AnimatorControllerLayer>();
            foreach (var layer in currentController.layers)
            {
                if (origin.layers.Any(l => OnFindParam(l.name) == OnFindParam(layer.name)))
                {
                }
                else
                {
                    newLayers.Add(layer);
                }
            }
            currentController.layers = newLayers.ToArray();
            EditorUtility.SetDirty(currentController);
            return currentController;
        }
        
        public AnimatorController RevertAnimator(string keyword)
        {
            if (currentController == null) return null;
            currentController.parameters = currentController.parameters.Where(p => !p.name.StartsWith(keyword)).ToArray();
            currentController.layers = currentController.layers.Where(l => !l.name.StartsWith(keyword)).ToArray();
            EditorUtility.SetDirty(currentController);
            return currentController;
        }

        public List<string> WriteDefaultLayers()
        {
            var layers = new List<string>();
            if (currentController == null) return layers;
            
            foreach (var layer in currentController.layers)
            {
                if (HaWriteDefaultStateMachine(layer.stateMachine))
                {
                    layers.Add(layer.name);
                }
            }

            return layers;
        }
        
        public List<string> HasKeyframeLayers(string[] path, string attribute = "")
        {
            var layers = new List<string>();
            if (currentController == null) return layers;
            
            foreach (var layer in currentController.layers)
            {
                if (HasKeyFrameStateMachine(layer.stateMachine, path, attribute))
                {
                    layers.Add(layer.name);
                }
            }
            return layers;
        }

        public AnimatorController AnimatorControllerParameterRename()
        {
            if (currentController == null) return null;
            currentController.parameters = currentController.parameters.Select(p => new AnimatorControllerParameter()
            {
                name = OnFindParam(p.name),
                type = p.type,
                defaultBool = p.defaultBool,
                defaultFloat = p.defaultFloat,
                defaultInt = p.defaultInt,
            }).ToArray();
            currentController.layers = currentController.layers.Select(l =>
            {
                l.name = OnFindParam(l.name);
                l.stateMachine = StateMachineParameterRename(l.stateMachine);
                return l;
            }).ToArray();
            EditorUtility.SetDirty(currentController);
            return currentController;
        }

        public AnimatorController RepathAnims(string from, string to)
        {
            if (currentController == null) return null;
            foreach (var layer in currentController.layers)
            {
                RePathStateMachine(layer.stateMachine, from, to);
            }
            EditorUtility.SetDirty(currentController);
            return currentController;
        }
        

        #region AnimatorCombinator

        /// <summary>
        /// AnimatorController全レイヤーの安全な結合
        /// </summary>
        /// <param name="originController"></param>
        void CloneLayers(AnimatorController originController)
        {
            if (currentController != originController && currentController != null && originController != null)
            {
                foreach (var layer in originController.layers)
                {
                    // すでに同名レイヤーがあれば削除する
                    int index = Array.FindIndex(currentController.layers, l => l.name == OnFindParam(layer.name));
                    if (index > -1) currentController.RemoveLayer(index);
                    // レイヤーの複製
                    var newLayer = CloneLayer(layer);
                    currentController.AddLayer(newLayer);
                }
            }
        }

        /// <summary>
        /// AnimatorControllerのLayerごとの複製
        /// </summary>
        /// <param name="originLayer"></param>
        /// <returns></returns>
        AnimatorControllerLayer CloneLayer(AnimatorControllerLayer originLayer)
        {
            var cloneLayer = new AnimatorControllerLayer()
            {
                avatarMask = OnFindAvatarMask(originLayer.avatarMask),
                blendingMode = originLayer.blendingMode,
                defaultWeight = originLayer.defaultWeight,
                iKPass = originLayer.iKPass,
                name = OnFindParam(originLayer.name),
                syncedLayerAffectsTiming = originLayer.syncedLayerAffectsTiming,
                syncedLayerIndex = originLayer.syncedLayerIndex,
                // StateMachineは別途複製
                stateMachine = CloneStateMachine(originLayer.stateMachine),
            };
            CloneTrasitions(cloneLayer.stateMachine, originLayer.stateMachine);
            return cloneLayer;
        }

        /// <summary>
        /// Statemachineの複製
        /// </summary>
        /// <param name="originMachine"></param>
        /// <returns></returns>
        AnimatorStateMachine CloneStateMachine(AnimatorStateMachine originMachine)
        {
            var cloneChildMachine = originMachine.stateMachines.Select(cs => new ChildAnimatorStateMachine()
            {
                position = cs.position,
                stateMachine = CloneStateMachine(cs.stateMachine)
            }).ToList();
            var cloneStates = CloneStates(originMachine);

            // StateMachineの複製
            var cloneStateMachine = new AnimatorStateMachine
            {
                anyStatePosition = originMachine.anyStatePosition,
                entryPosition = originMachine.entryPosition,
                exitPosition = originMachine.exitPosition,
                hideFlags = originMachine.hideFlags,
                name = originMachine.name,
                parentStateMachinePosition = originMachine.parentStateMachinePosition,
                // ChildAnimatorStateMachineは別途複製
                states = cloneStates.ToArray(),
                stateMachines = cloneChildMachine.ToArray(),
                defaultState = cloneStates.FirstOrDefault(s => s.state.name == originMachine.defaultState.name).state,
            };

            return cloneStateMachine;
        }

        /// <summary>
        /// ChildAnimationStateの複製
        /// </summary>
        /// <param name="originMachine"></param>
        /// <returns></returns>
        List<ChildAnimatorState> CloneStates(AnimatorStateMachine originMachine)
        {
            // Stateの複製
            var cloneStates = new List<ChildAnimatorState>();
            foreach (var animstate in originMachine.states)
            {
                cloneStates.Add(CloneChildAnimatorState(animstate));
            }

            return cloneStates;
        }

        void CloneTrasitions(AnimatorStateMachine clone, AnimatorStateMachine origin)
        {
            // リスト作成
            var cloneStates = new List<AnimatorState>();
            var originStates = new List<AnimatorState>();
            var cloneMachines = GetStatemachines(clone);
            var originMachines = GetStatemachines(origin);
            foreach (var m in cloneMachines)
            {
                cloneStates.AddRange(m.states.Select(s => s.state).ToList());
            }

            foreach (var m in originMachines)
            {
                originStates.AddRange(m.states.Select(s => s.state).ToList());
            }

            // Transitionの複製
            foreach (var originState in originStates)
            {
                var cloneState = cloneStates.FirstOrDefault(s => s.name == originState.name);
                if (cloneState != null)
                {
                    foreach (var originTransition in originState.transitions)
                    {
                        var cloneTransition = new AnimatorStateTransition();

                        if (originTransition.isExit)
                        {
                            cloneTransition = cloneState.AddExitTransition();
                        }

                        if (originTransition.destinationState != null)
                        {
                            var destinationState =
                                cloneStates.FirstOrDefault(s => s.name == originTransition.destinationState.name);
                            cloneTransition = cloneState.AddTransition(destinationState);
                        }

                        if (originTransition.destinationStateMachine != null)
                        {
                            var destinationState = cloneMachines.FirstOrDefault(s =>
                                s.name == originTransition.destinationStateMachine.name);
                            cloneTransition = cloneState.AddTransition(destinationState);
                        }

                        CopyAnimatorStateTransition(cloneTransition, originTransition);
                    }

                    CopyStateBehaviours(cloneState, originState);
                }
                else
                {
                    Debug.LogError("NullState Copy");
                }
            }

            foreach (var originMachine in originMachines)
            {
                var cloneMachine = cloneMachines.FirstOrDefault(m => m.name == originMachine.name);
                if (cloneMachine != null)
                {
                    foreach (var originTransition in originMachine.anyStateTransitions)
                    {
                        var cloneTransition = new AnimatorStateTransition();

                        if (originTransition.destinationState != null)
                        {
                            var destinationState =
                                cloneStates.FirstOrDefault(s => s.name == originTransition.destinationState.name);
                            cloneTransition = cloneMachine.AddAnyStateTransition(destinationState);
                        }

                        if (originTransition.destinationStateMachine != null)
                        {
                            var destinationState = cloneMachines.FirstOrDefault(s =>
                                s.name == originTransition.destinationStateMachine.name);
                            cloneTransition = cloneMachine.AddAnyStateTransition(destinationState);
                        }

                        CopyAnimatorStateTransition(cloneTransition, originTransition);
                    }

                    foreach (var originTransition in originMachine.entryTransitions)
                    {
                        var cloneTransition = new AnimatorTransition();

                        if (originTransition.destinationState != null)
                        {
                            var destinationState =
                                cloneStates.FirstOrDefault(s => s.name == originTransition.destinationState.name);
                            cloneTransition = cloneMachine.AddEntryTransition(destinationState);
                        }

                        if (originTransition.destinationStateMachine != null)
                        {
                            var destinationState = cloneMachines.FirstOrDefault(s =>
                                s.name == originTransition.destinationStateMachine.name);
                            cloneTransition = cloneMachine.AddEntryTransition(destinationState);
                        }

                        CopyAnimatorTransition(cloneTransition, originTransition);
                    }
                }
                else
                {
                    Debug.LogError("NullStateMachine Copy");
                }
            }
        }


        List<AnimatorStateMachine> GetStatemachines(AnimatorStateMachine root)
        {
            var l = new List<AnimatorStateMachine>();
            l.Add(root);
            foreach (var stateMachine in root.stateMachines)
            {
                l.AddRange(GetStatemachines(stateMachine.stateMachine));
            }

            return l;
        }

        AnimatorStateTransition CopyAnimatorStateTransition(AnimatorStateTransition clone,
            AnimatorStateTransition origin)
        {
            clone.duration = origin.duration;
            clone.offset = origin.offset;
            clone.interruptionSource = origin.interruptionSource;
            clone.orderedInterruption = origin.orderedInterruption;
            clone.exitTime = origin.exitTime;
            clone.hasExitTime = origin.hasExitTime;
            clone.hasFixedDuration = origin.hasFixedDuration;
            clone.canTransitionToSelf = origin.canTransitionToSelf;

            CopyAnimatorTransition(clone, origin);
            return clone;
        }

        AnimatorTransitionBase CopyAnimatorTransition(AnimatorTransitionBase clone, AnimatorTransitionBase origin)
        {
            clone.name = origin.name;
            clone.hideFlags = origin.hideFlags;
            clone.solo = origin.solo;
            clone.mute = origin.mute;
            clone.isExit = origin.isExit;

            foreach (var originCondition in origin.conditions)
            {
                clone.AddCondition(originCondition.mode, originCondition.threshold,
                    OnFindParam(originCondition.parameter));
            }

            return clone;
        }

        ChildAnimatorState CloneChildAnimatorState(ChildAnimatorState origin)
        {
            var clone = new ChildAnimatorState()
            {
                position = origin.position,
                state = CloneAnimationState(origin.state)
            };
            return clone;
        }

        AnimatorState CloneAnimationState(AnimatorState origin)
        {
            var clone = new AnimatorState()
            {
                cycleOffset = origin.cycleOffset,
                cycleOffsetParameter = OnFindParam(origin.cycleOffsetParameter),
                cycleOffsetParameterActive = origin.cycleOffsetParameterActive,
                hideFlags = origin.hideFlags,
                iKOnFeet = origin.iKOnFeet,
                mirror = origin.mirror,
                mirrorParameter = OnFindParam(origin.mirrorParameter),
                mirrorParameterActive = origin.mirrorParameterActive,
                motion = CloneMotion(origin.motion),
                name = origin.name,
                speed = origin.speed,
                speedParameter = OnFindParam(origin.speedParameter),
                speedParameterActive = origin.speedParameterActive,
                tag = origin.tag,
                timeParameter = OnFindParam(origin.timeParameter),
                timeParameterActive = origin.timeParameterActive,
                writeDefaultValues = writeDefaultOverride ?? origin.writeDefaultValues
            };
            return clone;
        }

        Motion CloneMotion(Motion origin)
        {
            if (origin == null)
            {
                return OnFindAnimationClip(null);
                /*if (OverrideNullAnimation)
                {
                    return AssetUtility.LoadAssetAtGuid<AnimationClip>(EnvironmentVariable.nottingAnim);
                }
                else
                {
                    return null;
                }*/
            }

            if (origin is BlendTree)
            {
                var o = origin as BlendTree;
                BlendTree c = new BlendTree()
                {
                    blendParameter = OnFindParam(o.blendParameter),
                    blendParameterY = OnFindParam(o.blendParameterY),
                    children = o.children.Select(m =>
                        new ChildMotion()
                        {
                            cycleOffset = m.cycleOffset,
                            directBlendParameter = OnFindParam(m.directBlendParameter),
                            mirror = m.mirror,
                            motion = CloneMotion(m.motion),
                            position = m.position,
                            threshold = m.threshold,
                            timeScale = m.timeScale,
                        }).ToArray(),
                    blendType = o.blendType,
                    hideFlags = o.hideFlags,
                    maxThreshold = o.maxThreshold,
                    minThreshold = o.minThreshold,
                    name = o.name,
                    useAutomaticThresholds = o.useAutomaticThresholds,
                };
                return c;
            }
            else if (origin is AnimationClip)
            {
                if (animRepathList.Count > 0)
                {
                    return RePathAnimation((AnimationClip) origin);
                }

                return origin;
            }

            return origin;
        }

        AnimatorState CopyStateBehaviours(AnimatorState clone, AnimatorState origin)
        {
            var behaviours = new List<StateMachineBehaviour>();
            foreach (var behaviour in origin.behaviours)
            {
#if VRC_SDK_VRCSDK3
                if (behaviour is VRCAnimatorLayerControl)
                {
                    VRCAnimatorLayerControl o = behaviour as VRCAnimatorLayerControl;
                    var c = ScriptableObject.CreateInstance<VRCAnimatorLayerControl>();
                    {
                        c.ApplySettings = o.ApplySettings;
                        c.debugString = o.debugString;
                        c.playable = o.playable;
                        if (layerOffset == null)
                        {
                            c.layer = o.layer;
                        }
                        else
                        {
                            c.layer = o.layer + layerOffset[o.playable.GetAnimatorLayerType()]; // レイヤーが増えた分加算する
                        }
                        c.blendDuration = o.blendDuration;
                        c.goalWeight = o.goalWeight;
                        c.playable = o.playable;
                    }
                    behaviours.Add(c);
                }
                else
                if (behaviour is VRCAvatarParameterDriver)
                {
                    VRCAvatarParameterDriver o = behaviour as VRCAvatarParameterDriver;
                    var c = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
                    {
                        c.name = o.name;
                        c.parameters = o.parameters.Select(p =>
                        {
                            return new VRC_AvatarParameterDriver.Parameter()
                            {
                                name = OnFindParam(p.name),
                                chance = p.chance,
                                type = p.type,
                                value = p.value,
                                valueMin = p.valueMin,
                                valueMax = p.valueMax
                            };
                        }).ToList();
                        c.debugString = o.debugString;
                        c.hideFlags = o.hideFlags;
                        c.localOnly = o.localOnly;
                        c.ApplySettings = o.ApplySettings;
                    }
                    behaviours.Add(c);
                }
                else
                {
                    var c = ScriptableObject.Instantiate(behaviour);
                    behaviours.Add(c);
                }
#else
                var c = ScriptableObject.Instantiate(behaviour);
                behaviours.Add(c);
#endif
            }

            clone.behaviours = behaviours.ToArray();

            return clone;
        }

        void SaveLayer(AnimatorControllerLayer l, string path)
        {
            // if(l.avatarMask) AssetDatabase.AddObjectToAsset(l.avatarMask,path);
            SaveStateMachine(l.stateMachine, path);
        }

        void SaveStateMachine(AnimatorStateMachine machine, string path)
        {
            machine.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(machine, path);
            foreach (var s in machine.states)
            {
                AssetDatabase.AddObjectToAsset(s.state, path);
                SaveMotion(s.state.motion, path);
                foreach (var t in s.state.transitions)
                {
                    t.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(t, path);
                }

                foreach (var b in s.state.behaviours)
                {
                    b.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(b, path);
                }
            }

            foreach (var t in machine.entryTransitions)
            {
                t.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(t, path);
            }

            foreach (var t in machine.anyStateTransitions)
            {
                t.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(t, path);
            }

            foreach (var m in machine.stateMachines)
            {
                SaveStateMachine(m.stateMachine, path);
                foreach (var b in m.stateMachine.behaviours)
                {
                    b.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(b, path);
                }
            }
        }

        void SaveMotion(Motion motion, string path)
        {
            if (motion == null) return;
            if (motion is BlendTree)
            {
                BlendTree tree = (BlendTree) motion;
                tree.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(tree, path);
                foreach (var m in tree.children)
                {
                    SaveMotion(m.motion, path);
                }
            }
        }

        /// <summary>
        /// AnimatorControllerのパラメータの安全な結合
        /// </summary>
        /// <param name="cloneController"></param>
        /// <param name="originController"></param>
        void CloneAnimatorParamaters(AnimatorController cloneController, AnimatorController originController)
        {
            if (cloneController != originController || cloneController != null || originController != null)
            {
                foreach (var parameter in originController.parameters)
                {
                    // すでに同名パラメーターがあれば削除する
                    int index = Array.FindIndex(cloneController.parameters,
                        p => p.name == OnFindParam(parameter.name));
                    if (index > -1) cloneController.RemoveParameter(cloneController.parameters[index]);
                    // パラメーターのコピー
                    cloneController.AddParameter(new AnimatorControllerParameter()
                    {
                        defaultBool = parameter.defaultBool,
                        defaultFloat = parameter.defaultFloat,
                        defaultInt = parameter.defaultInt,
                        name = OnFindParam(parameter.name),
                        type = parameter.type,
                    });
                }
            }
        }

        #endregion

        #region RenameParams

        AnimatorStateMachine StateMachineParameterRename(AnimatorStateMachine machine)
        {
            machine.states = machine.states.Select(s =>
            {
                if (s.state.motion is BlendTree)
                {
                    BlendTree BlendTreeParameterRename(BlendTree b)
                    {
                        b.blendParameter = OnFindParam(b.blendParameter);
                        b.blendParameterY = OnFindParam(b.blendParameterY);
                        b.children = b.children.Select(c =>
                        {
                            if (c.motion is BlendTree)
                            {
                                c.motion = BlendTreeParameterRename((BlendTree) c.motion);
                            }

                            return c;
                        }).ToArray();
                        return b;
                    }

                    s.state.motion = BlendTreeParameterRename((BlendTree) s.state.motion);
                }

                s.state.timeParameter = OnFindParam(s.state.timeParameter);
                s.state.speedParameter = OnFindParam(s.state.speedParameter);
                s.state.mirrorParameter = OnFindParam(s.state.mirrorParameter);
                s.state.cycleOffsetParameter = OnFindParam(s.state.cycleOffsetParameter);

                s.state.transitions = s.state.transitions.Select(t =>
                {
                    t.conditions = t.conditions.Select(c => new AnimatorCondition()
                    {
                        mode = c.mode,
                        parameter = OnFindParam(c.parameter),
                        threshold = c.threshold
                    }).ToArray();
                    return t;
                }).ToArray();

                s.state.behaviours = s.state.behaviours.Select(b =>
                {
#if VRC_SDK_VRCSDK3
                    if (b is VRCAvatarParameterDriver)
                    {
                        var p = (VRCAvatarParameterDriver) b;
                        p.parameters = p.parameters.Select(e =>
                        {
                            e.name = OnFindParam(e.name);
                            return e;
                        }).ToList();
                    }
#endif
                    return b;
                }).ToArray();
                return s;
            }).ToArray();

            machine.entryTransitions = machine.entryTransitions.Select(t =>
            {
                t.conditions = t.conditions.Select(c => new AnimatorCondition()
                {
                    mode = c.mode,
                    parameter = OnFindParam(c.parameter),
                    threshold = c.threshold
                }).ToArray();
                return t;
            }).ToArray();

            machine.anyStateTransitions = machine.anyStateTransitions.Select(t =>
            {
                t.conditions = t.conditions.Select(c => new AnimatorCondition()
                {
                    mode = c.mode,
                    parameter = OnFindParam(c.parameter),
                    threshold = c.threshold
                }).ToArray();
                return t;
            }).ToArray();

            machine.stateMachines = machine.stateMachines.Select(m =>
            {
                StateMachineParameterRename(m.stateMachine);
                m.stateMachine.behaviours = m.stateMachine.behaviours.Select(b =>
                {
#if VRC_SDK_VRCSDK3
                    if (b is VRCAvatarParameterDriver)
                    {
                        var p = (VRCAvatarParameterDriver) b;
                        p.parameters = p.parameters.Select(e =>
                        {
                            e.name = OnFindParam(e.name);
                            return e;
                        }).ToList();
                    }
#endif
                    return b;
                }).ToArray();
                return m;
            }).ToArray();
            return machine;
        }

        #endregion

        #region RepathAnim

        /// <summary>
        /// アニメーションのパスを書き換える
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        AnimationClip RePathAnimation(AnimationClip clip)
        {
            bool hasPath = false;
            using (var o = new SerializedObject(clip))
            {
                var i = o.GetIterator();
                while (i.Next(true))
                {
                    if (i.name == "path" && i.propertyType == SerializedPropertyType.String)
                    {
                        foreach (var ft in animRepathList)
                        {
                            if (i.stringValue.StartsWith(ft.Key))
                            {
                                hasPath = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (hasPath)
            {
                clip = OnFindAnimationClip(clip);
                using (var o = new SerializedObject(clip))
                {
                    var i = o.GetIterator();
                    while (i.Next(true))
                    {
                        if (i.name == "path" && i.propertyType == SerializedPropertyType.String)
                        {
                            foreach (var ft in animRepathList)
                            {
                                if (i.stringValue.StartsWith(ft.Key))
                                {
                                    i.stringValue = i.stringValue.Replace(ft.Key, ft.Value);
                                }
                            }
                        }
                    }

                    o.ApplyModifiedProperties();
                }
            }

            return clip;
        }

        void RePathStateMachine(AnimatorStateMachine machine, string from, string to)
        {
            foreach (var s in machine.states)
            {
                if (s.state.motion)
                {
                    RePathMotion(s.state.motion, from, to);
                }
            }

            foreach (var m in machine.stateMachines)
            {
                RePathStateMachine(m.stateMachine, from, to);
            }
        }

        void RePathMotion(Motion motion, string from, string to)
        {
            if (motion is BlendTree)
            {
                BlendTree tree = (BlendTree) motion;
                foreach (var m in tree.children)
                {
                    RePathMotion(m.motion, from, to);
                }
            }
            else if (motion is AnimationClip)
            {
                AnimationClip clip = (AnimationClip) motion;
                RePathAnimation(clip, from, to);
            }
        }

        AnimationClip RePathAnimation(AnimationClip clip, string from, string to)
        {
            bool hasPath = false;
            using (var o = new SerializedObject(clip))
            {
                var i = o.GetIterator();
                while (i.Next(true))
                {
                    if (i.name == "path" && i.propertyType == SerializedPropertyType.String)
                    {
                        if (i.stringValue.StartsWith(from))
                        {
                            hasPath = true;
                            break;
                        }
                    }
                }
            }

            if (hasPath)
            {
                clip = OnFindAnimationClip(clip);
                using (var o = new SerializedObject(clip))
                {
                    var i = o.GetIterator();
                    while (i.Next(true))
                    {
                        if (i.name == "path" && i.propertyType == SerializedPropertyType.String)
                        {
                            if (i.stringValue.StartsWith(from))
                            {
                                i.stringValue = i.stringValue.Replace(from, to);
                            }
                        }
                    }

                    o.ApplyModifiedProperties();
                }
            }

            return clip;
        }

        #endregion

        #region AnimatorChecker
        
        bool HaWriteDefaultStateMachine(AnimatorStateMachine machine)
        {
            foreach (var s in machine.states)
            {
                if (s.state.motion)
                {
                    return s.state.writeDefaultValues;
                }
            }

            foreach (var m in machine.stateMachines)
            {
                if (HaWriteDefaultStateMachine(m.stateMachine))
                {
                    return true;
                }
            }

            return false;
        }

        bool HasKeyFrameStateMachine(AnimatorStateMachine machine, string[] path, string attribute = "")
        {
            foreach (var s in machine.states)
            {
                if (s.state.motion)
                {
                    if (HasKeyframeMotion(s.state.motion, path, attribute))
                    {
                        return true;
                    }
                }
            }

            foreach (var m in machine.stateMachines)
            {
                if (HasKeyFrameStateMachine(m.stateMachine, path, attribute))
                {
                    return true;
                }
            }

            return false;
        }

        bool HasKeyframeMotion(Motion motion, string[] path, string attribute = "")
        {
            if (motion is BlendTree)
            {
                BlendTree tree = (BlendTree) motion;
                foreach (var m in tree.children)
                {
                    if (HasKeyframeMotion(m.motion, path, attribute))
                    {
                        return true;
                    }
                }
            }
            else if (motion is AnimationClip)
            {
                AnimationClip clip = (AnimationClip) motion;
                if (HasKeyframeAnimation(clip, path, attribute))
                {
                    return true;
                }
            }

            return false;
        }
        
        bool HasKeyframeAnimation(AnimationClip clip, string[] path, string attribute = "")
        {
            using (var o = new SerializedObject(clip))
            {
                var curves = o.FindProperty("m_FloatCurves");
                for (int i = curves.arraySize - 1; i >= 0; i--)
                {
                    var pathProp = curves.GetArrayElementAtIndex(i).FindPropertyRelative("path");
                    var attributeProp = curves.GetArrayElementAtIndex(i).FindPropertyRelative("attribute");
                    if (pathProp != null)
                    {
                        if (path.Contains(pathProp.stringValue))
                        {
                            if (String.IsNullOrWhiteSpace(attribute))
                            {
                                return true;
                            }
                            else
                            {
                                if (attributeProp.stringValue.Contains(attribute))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        string OnFindParam(string param)
        {
            if (onFindParam != null)
            {
                return onFindParam(param);
            }
            return param;
        }

        AnimationClip OnFindAnimationClip(AnimationClip origin)
        {
            if (onFindAnimationClip != null)
            {
                return onFindAnimationClip(origin);
            }

            return origin;
        }

        AvatarMask OnFindAvatarMask(AvatarMask origin)
        {
            if (onFindAvatarMask != null)
            {
                return onFindAvatarMask(origin);
            }

            return origin;
        }
    }
    public static class AnimatorUtility
    {
        public enum AnimatorLayerType
        {
            Locomotion,
            Idle,
            Gesture,
            Action,
            Fx
        }
        
#if VRC_SDK_VRCSDK3
        public static VRCAvatarDescriptor.AnimLayerType GetVRChatAnimatorLayerType(this AnimatorLayerType type)
        {
            switch (type)
            {
                case AnimatorLayerType.Locomotion : return VRCAvatarDescriptor.AnimLayerType.Base;
                case AnimatorLayerType.Idle : return VRCAvatarDescriptor.AnimLayerType.Additive; 
                case AnimatorLayerType.Gesture : return VRCAvatarDescriptor.AnimLayerType.Gesture; 
                case AnimatorLayerType.Action : return VRCAvatarDescriptor.AnimLayerType.Action; 
                case AnimatorLayerType.Fx : return VRCAvatarDescriptor.AnimLayerType.FX; 
            }
            return VRCAvatarDescriptor.AnimLayerType.Base;
        }
        
        public static AnimatorLayerType GetAnimatorLayerType(this VRCAvatarDescriptor.AnimLayerType type)
        {
            switch (type)
            {
                case VRCAvatarDescriptor.AnimLayerType.Base : return AnimatorLayerType.Locomotion;
                case VRCAvatarDescriptor.AnimLayerType.Additive : return AnimatorLayerType.Idle; 
                case VRCAvatarDescriptor.AnimLayerType.Gesture : return AnimatorLayerType.Gesture; 
                case VRCAvatarDescriptor.AnimLayerType.Action : return AnimatorLayerType.Action; 
                case VRCAvatarDescriptor.AnimLayerType.FX : return AnimatorLayerType.Fx; 
            }
            return AnimatorLayerType.Locomotion;
        }
        
        public static AnimatorLayerType GetAnimatorLayerType(this VRC_AnimatorLayerControl.BlendableLayer type)
        {
            switch (type)
            {
                case VRC_AnimatorLayerControl.BlendableLayer.Additive : return AnimatorLayerType.Idle; 
                case VRC_AnimatorLayerControl.BlendableLayer.Gesture : return AnimatorLayerType.Gesture; 
                case VRC_AnimatorLayerControl.BlendableLayer.Action : return AnimatorLayerType.Action; 
                case VRC_AnimatorLayerControl.BlendableLayer.FX : return AnimatorLayerType.Fx; 
            }
            return AnimatorLayerType.Locomotion;
        }
#endif
    }
}
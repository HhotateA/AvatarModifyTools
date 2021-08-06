using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Animations;
using UnityEngine.Animations;
using Object = UnityEngine.Object;
#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.Core
{
    /// <summary>
    /// VRCAvatarDescriptorへのアセット適応を一括して行うクラス
    /// </summary>
    public class AvatarModifyTool
    {
        
#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
        public AvatarModifyTool(VRCAvatarDescriptor a,string dir = "Assets/Export")
        {
            if (dir == "Assets/Export")
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    AssetDatabase.CreateFolder("Assets","Export");
                }
            }
            avatar = a;
            exportDir = File.GetAttributes(dir)
                .HasFlag(FileAttributes.Directory) ? dir : Path.GetDirectoryName(dir);
            Debug.Log(exportDir);
        }

        private Dictionary<VRC_AnimatorLayerControl.BlendableLayer, int> layerOffset = new Dictionary<VRC_AnimatorLayerControl.BlendableLayer, int>();

        void ComputeLayersOffset(AvatarModifyData assets)
        {
            layerOffset = new Dictionary<VRC_AnimatorLayerControl.BlendableLayer, int>();
            foreach (VRC_AnimatorLayerControl.BlendableLayer type in Enum.GetValues(typeof(VRC_AnimatorLayerControl.BlendableLayer)))
            {
                layerOffset.Add(type,GetLayerOffset(assets,type));
            }
        }

        int GetLayerOffset(AvatarModifyData assets,VRC_AnimatorLayerControl.BlendableLayer type)
        {
            VRCAvatarDescriptor.AnimLayerType t =
                type == VRC_AnimatorLayerControl.BlendableLayer.Additive ? VRCAvatarDescriptor.AnimLayerType.Additive :
                type == VRC_AnimatorLayerControl.BlendableLayer.Gesture ? VRCAvatarDescriptor.AnimLayerType.Gesture :
                type == VRC_AnimatorLayerControl.BlendableLayer.Action ? VRCAvatarDescriptor.AnimLayerType.Action :
                type == VRC_AnimatorLayerControl.BlendableLayer.FX ? VRCAvatarDescriptor.AnimLayerType.FX :
                VRCAvatarDescriptor.AnimLayerType.Base;
            var index = Array.FindIndex(avatar.baseAnimationLayers,l => l.type == t);
            if (avatar.customizeAnimationLayers == false) return 0;
            if (avatar.baseAnimationLayers[index].isDefault == true) return 0;
            if (!avatar.baseAnimationLayers[index].animatorController) return 0;
            AnimatorController a = (AnimatorController) avatar.baseAnimationLayers[index].animatorController;
            AnimatorController b =
                type == VRC_AnimatorLayerControl.BlendableLayer.Additive ? assets.idle_controller :
                type == VRC_AnimatorLayerControl.BlendableLayer.Gesture ? assets.gesture_controller :
                type == VRC_AnimatorLayerControl.BlendableLayer.Action ? assets.action_controller :
                type == VRC_AnimatorLayerControl.BlendableLayer.FX ? assets.fx_controller :
                null;
            if (a == null) return 0;
            if (b == null) return a.layers.Length;
            int i = 0;
            foreach (var la in a.layers)
            {
                if (b.layers.Any(lb => la.name == lb.name))
                {
                    continue;
                }
                else
                {
                    i++;
                }
            }
            return i;
        }
        
#else
        private MonoBehaviour avatar;
#endif
        private string exportDir = "Assets/";
        private Dictionary<string, string> animRepathList = new Dictionary<string, string>();
        public void ModifyAvatar(AvatarModifyData assets,bool keepOriginalAsset = true)
        {
            if (avatar != null)
            {
                animRepathList = new Dictionary<string,string>();
                if (assets.items != null)
                {
                    foreach (var item in assets.items)
                    {
                        ModifyGameObject(item.prefab,out var from,out var to,item.target);
                        animRepathList.Add(from,to);
                    }
                }
#if VRC_SDK_VRCSDK3
                ComputeLayersOffset(assets);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.Base,assets.locomotion_controller);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.Additive,assets.idle_controller);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.Gesture,assets.gesture_controller);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.Action,assets.action_controller);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX,assets.fx_controller);
                ModifyExpressionParameter(assets.parameter,keepOriginalAsset);
                ModifyExpressionMenu(assets.menu,false,keepOriginalAsset);
                AssetDatabase.SaveAssets();
#endif
            }
            else
            {
                throw new NullReferenceException("VRCAvatarDescriptor : avatar not found");
            }
        }

#if VRC_SDK_VRCSDK3

        void ModifyAvatarAnimatorController(
            VRCAvatarDescriptor.AnimLayerType type,
            AnimatorController controller)
        {
            if (controller == null) return;
            if (!GetAvatarAnimatorControllerExists(type))
            {
                if (type == VRCAvatarDescriptor.AnimLayerType.Base)
                {
                    SetAvatarAnimatorController(type, MakeCopy<AnimatorController>(AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.baseAnimator)));
                }
                else
                if (type == VRCAvatarDescriptor.AnimLayerType.Additive)
                {
                    SetAvatarAnimatorController(type, MakeCopy<AnimatorController>(AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.idleAnimator)));
                }
                else
                if (type == VRCAvatarDescriptor.AnimLayerType.Gesture)
                {
                    SetAvatarAnimatorController(type, MakeCopy<AnimatorController>(AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.gestureAnimator)));
                }
                else
                if (type == VRCAvatarDescriptor.AnimLayerType.Action)
                {
                    SetAvatarAnimatorController(type, MakeCopy<AnimatorController>(AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.actionAnimator)));
                }
                else
                if (type == VRCAvatarDescriptor.AnimLayerType.FX)
                {
                    SetAvatarAnimatorController(type, MakeCopy<AnimatorController>(AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.fxAnimator)));
                }
            }
            ModifyAnimatorController(
                GetAvatarAnimatorController(type),
                controller);
        }
        
        AnimatorController GetAvatarAnimatorController( VRCAvatarDescriptor.AnimLayerType type)
        {
            var index = Array.FindIndex(avatar.baseAnimationLayers,
                l => l.type == type);
            if (avatar.baseAnimationLayers[index].animatorController)
            {
                return (AnimatorController) avatar.baseAnimationLayers[index].animatorController;
            }
            return null;
        }
        
        void SetAvatarAnimatorController( VRCAvatarDescriptor.AnimLayerType type, AnimatorController controller)
        {
            avatar.customizeAnimationLayers = true;
            var index = Array.FindIndex(avatar.baseAnimationLayers,
                l => l.type == type);
            avatar.baseAnimationLayers[index].isDefault = false;
            avatar.baseAnimationLayers[index].animatorController = controller;
        }
        
        public bool GetAvatarAnimatorControllerExists(VRCAvatarDescriptor.AnimLayerType type)
        {
            var index = Array.FindIndex(avatar.baseAnimationLayers,
                l => l.type == type);
            return avatar.baseAnimationLayers[index].animatorController != null;
        }

        VRCExpressionParameters GetExpressionParameter()
        {
            return avatar.expressionParameters;
        }
        void SetExpressionParameter(VRCExpressionParameters param = null)
        {
            avatar.customExpressions = true;
            avatar.expressionParameters = param;
        }
        bool GetExpressionParameterExist()
        {
            if (avatar.customExpressions == true)
            {
                if (avatar.expressionParameters != null)
                {
                    return true;
                }
            }

            return false;
        }

        VRCExpressionsMenu GetExpressionMenu( VRCExpressionsMenu defaultMenus = null)
        {
            return avatar.expressionsMenu;
        }
        void SetExpressionMenu( VRCExpressionsMenu menu = null)
        {
            avatar.customExpressions = true;
            avatar.expressionsMenu = menu;
        }
        bool GetExpressionMenuExist()
        {
            if (avatar.customExpressions == true)
            {
                if (avatar.expressionsMenu != null)
                {
                    return true;
                }
            }

            return false;
        }
#endif

        T SafeCopy<T>(T obj) where T : Object
        {
            if (ExistAssetObject(obj))
            {
                return obj;
            }
            else
            {
                return MakeCopy<T>(obj);
            }
        }

        bool ExistAssetObject(Object obj)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            return !String.IsNullOrWhiteSpace(path);
        }

        T MakeCopy<T>(T origin,bool cloneSubAssets = true) where T : Object
        {
            string sufix = origin is AnimatorController ? ".controller" : 
                origin is AnimationClip ? ".anim" : ".asset";
            if (origin != null)
            {
                var path = AssetDatabase.GetAssetPath(origin);
                string copyPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(exportDir, origin.name + "_copy" + sufix));
                if (!String.IsNullOrWhiteSpace(path))
                {
                    if (cloneSubAssets)
                    {
                        AssetDatabase.CopyAsset(path, copyPath);
                        Debug.Log("Copy : " + origin.name + "from:" + path + " to:" + copyPath);
                    }
                    else
                    {
                        T clone = Object.Instantiate(origin);
                        AssetDatabase.CreateAsset(clone,copyPath);
                        Debug.Log("Instance : " + origin.name + " to " + copyPath);
                    }
                }
                else
                {
                    AssetDatabase.CreateAsset(origin,copyPath);
                    Debug.Log("Create : " + origin.name + " to " + copyPath);
                }
                return (T) AssetDatabase.LoadAssetAtPath<T>(copyPath);
            }

            return null;
        }

        /// <summary>
        /// AnimatorControllerのStateMachineとParameterの結合
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="origin"></param>
        public void ModifyAnimatorController(AnimatorController controller, AnimatorController origin)
        {
            if (controller != origin)
            {
                int originLayerCount = controller.layers.Length;
                CloneLayers(controller, origin);
                CloneAnimatorParamaters(controller, origin);
                for (int i = originLayerCount; i < controller.layers.Length; i++)
                {
                    SaveLayer(controller.layers[i],AssetDatabase.GetAssetPath(controller));
                }
            }
        }

        /// <summary>
        /// AnimatorController全レイヤーの安全な結合
        /// </summary>
        /// <param name="cloneController"></param>
        /// <param name="originController"></param>
        void CloneLayers(AnimatorController cloneController, AnimatorController originController)
        {
            if (cloneController != originController && cloneController != null && originController != null)
            {
                foreach (var layer in originController.layers)
                {
                    // すでに同名レイヤーがあれば削除する
                    int index = Array.FindIndex(cloneController.layers, l => l.name == layer.name);
                    if (index > -1) cloneController.RemoveLayer(index);
                    // レイヤーの複製
                    var newLayer = CloneLayer(layer);
                    cloneController.AddLayer(newLayer);
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
                avatarMask = CloneAvatarMask(originLayer.avatarMask),
                blendingMode = originLayer.blendingMode,
                defaultWeight = originLayer.defaultWeight,
                iKPass = originLayer.iKPass,
                name = originLayer.name,
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
                defaultState = cloneStates.FirstOrDefault(s=>s.state.name == originMachine.defaultState.name).state,
            };

            return cloneStateMachine;
        }

        AvatarMask CloneAvatarMask(AvatarMask origin)
        {
            if (origin)
            {
                if (AssetUtility.GetAssetGuid(origin) == EnvironmentVariable.nottingAvatarMask)
                {
                    var mask = new AvatarMask();
                    foreach (AvatarMaskBodyPart p in Enum.GetValues(typeof(AvatarMaskBodyPart)))
                    {
                        if (p != AvatarMaskBodyPart.LastBodyPart)
                        {
                            mask.SetHumanoidBodyPartActive(p,false);
                        }
                    }

                    var ts = Childs(avatar.transform);
                    mask.transformCount = ts.Count;
                    for (int i = 0; i < ts.Count; i++)
                    {
                        mask.SetTransformPath(i,AssetUtility.GetRelativePath(avatar.transform,ts[i]));
                        mask.SetTransformActive(i,false);
                    }

                    var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(exportDir,avatar.gameObject.name+"_NottingMask.mask"));
                    AssetDatabase.CreateAsset(mask,path);
                    return mask;
                }
            }
            return origin;
            /*if (origin == null) return null;
            var clone = new AvatarMask()
            {
                name = origin.name,
                transformCount = origin.transformCount
            };
            for (int i = 0; i < origin.transformCount; i++)
            {
                clone.SetTransformPath(i,
                    origin.GetTransformPath(i));
                clone.SetTransformActive(i,
                    origin.GetTransformActive(i));
            }
            foreach (AvatarMaskBodyPart p in Enum.GetValues(typeof(AvatarMaskBodyPart)))
            {
                if (p != AvatarMaskBodyPart.LastBodyPart)
                {
                    clone.SetHumanoidBodyPartActive(p,
                        origin.GetHumanoidBodyPartActive(p));
                }
            }
            return clone;*/
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

        void CloneTrasitions( AnimatorStateMachine clone,AnimatorStateMachine origin)
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

                        CopyAnimatorStateTransition(cloneTransition,originTransition);
                    }
                    cloneState = CopyVRCComponent(cloneState, originState);
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

                        CopyAnimatorStateTransition(cloneTransition,originTransition);
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

                        CopyAnimatorTransition(cloneTransition,originTransition);
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

        AnimatorStateTransition CopyAnimatorStateTransition(AnimatorStateTransition clone,AnimatorStateTransition origin)
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
        AnimatorTransitionBase CopyAnimatorTransition(AnimatorTransitionBase clone,AnimatorTransitionBase origin)
        {
            clone.name = origin.name;
            clone.hideFlags = origin.hideFlags;
            clone.solo = origin.solo;
            clone.mute = origin.mute;
            clone.isExit = origin.isExit;
            
            foreach (var originCondition in origin.conditions)
            {
                clone.AddCondition(originCondition.mode, originCondition.threshold, originCondition.parameter);
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
                cycleOffsetParameter = origin.cycleOffsetParameter,
                cycleOffsetParameterActive = origin.cycleOffsetParameterActive,
                hideFlags = origin.hideFlags,
                iKOnFeet = origin.iKOnFeet,
                mirror = origin.mirror,
                mirrorParameter = origin.mirrorParameter,
                mirrorParameterActive = origin.mirrorParameterActive,
                motion = CloneMotion(origin.motion),
                name = origin.name,
                speed = origin.speed,
                speedParameter = origin.speedParameter,
                speedParameterActive = origin.speedParameterActive,
                tag = origin.tag,
                timeParameter = origin.timeParameter,
                timeParameterActive = origin.timeParameterActive,
                writeDefaultValues = origin.writeDefaultValues
            };
            return clone;
        }

        Motion CloneMotion(Motion origin)
        {
            if (!origin) return null;
            if (origin is BlendTree)
            {
                var o = origin as BlendTree;
                BlendTree c = new BlendTree()
                {
                    blendParameter = o.blendParameter,
                    blendParameterY = o.blendParameterY,
                    children = o.children.Select(m=>
                        new ChildMotion()
                        {
                            cycleOffset = m.cycleOffset,
                            directBlendParameter = m.directBlendParameter,
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
            else if(origin is AnimationClip)
            {
                if (animRepathList.Count > 0)
                {
                    return RePathAnimation((AnimationClip) origin);
                }
                return origin;
            }

            return origin;
        }

        AnimatorState CopyVRCComponent(AnimatorState clone,AnimatorState origin)
        {
            var behaviours = new List<StateMachineBehaviour>();
            foreach (var behaviour in origin.behaviours)
            {
#if VRC_SDK_VRCSDK3
                if (behaviour.GetType() == typeof(VRCAnimatorLayerControl))
                {
                    VRCAnimatorLayerControl o = behaviour as VRCAnimatorLayerControl;
                    var c = clone.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                    c.ApplySettings = o.ApplySettings;
                    c.debugString = o.debugString;
                    c.playable = o.playable;
                    c.layer = o.layer + layerOffset[o.playable]; // レイヤーが増えた分加算する
                    c.blendDuration = o.blendDuration;
                    c.goalWeight = o.goalWeight;
                    c.playable = o.playable;
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
        
        void SaveLayer(AnimatorControllerLayer l,string path)
        {
            // if(l.avatarMask) AssetDatabase.AddObjectToAsset(l.avatarMask,path);
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
                    int index = Array.FindIndex(cloneController.parameters, p => p.name == parameter.name);
                    if (index > -1) cloneController.RemoveParameter(cloneController.parameters[index]);
                    // パラメーターのコピー
                    cloneController.AddParameter(parameter);
                }
            }
        }

#if VRC_SDK_VRCSDK3
        /// <summary>
        /// ExpressionParametersの安全な結合
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="origin"></param>
        void ModifyExpressionParameter(VRCExpressionParameters parameters,bool keepOriginalAsset = true)
        {
            if(parameters==null) return;
            if (GetExpressionParameterExist())
            {
                var current = GetExpressionParameter();
                if(parameters == current) return;
                foreach (var parameter in parameters.parameters)
                {
                    AddExpressionParameter(current, parameter.name, parameter.valueType, parameter.saved);
                }
                EditorUtility.SetDirty(current);
            }
            else
            {
                if (keepOriginalAsset)
                {
                    var current = MakeCopy<VRCExpressionParameters>(parameters,false);
                    SetExpressionParameter(current);
                    EditorUtility.SetDirty(current);
                }
                else
                {
                    SetExpressionParameter(parameters);
                }
            }
        }
        
        void AddExpressionParameter(VRCExpressionParameters parameters, string name,
            VRCExpressionParameters.ValueType type, bool saved = true)
        {
            // パラメータズのコピー作成(重複があった場合スキップ)
            var originalParm = parameters.parameters;
            var newParm = new List<VRCExpressionParameters.Parameter>();
            for (int i = 0; i < originalParm.Length; i++)
            {
                if (originalParm[i].name != "" && originalParm[i].name != name)
                {
                    newParm.Add(originalParm[i]);
                }
            }

            // 新規パラメータ追加
            newParm.Add(new VRCExpressionParameters.Parameter()
            {
                name = name,
                valueType = type,
                saved = saved,
            });

            parameters.parameters = newParm.ToArray();
            EditorUtility.SetDirty(parameters);
        }

        /// <summary>
        /// ExpressionsMenuの安全な結合
        /// </summary>
        /// <param name="menus"></param>
        /// <param name="origin"></param>
        void ModifyExpressionMenu(VRCExpressionsMenu menus, bool autoNextPage = true,bool keepOriginalAsset = true)
        {
            if (menus == null) return;
            if (GetExpressionMenuExist())
            {
                var parentnmenu = GetExpressionMenu();
                if(menus == parentnmenu) return;
                var current = parentnmenu;
                foreach (var control in menus.controls)
                {
                    if (current.controls.Count > 8 && autoNextPage) // 項目が上限に達していたら次ページに飛ぶ
                    {
                        var m = new MenuCreater("NextPage");
                        m.AddControll(current.controls[7]);
                        var submenu = m.CreateAsset(exportDir);
                        current.controls[7] = new VRCExpressionsMenu.Control()
                        {
                            name = "NextPage",
                            icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentVariable.arrowIcon),
                            type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = submenu
                        };
                        current = submenu;
                        EditorUtility.SetDirty(current);
                    }
                    var newcontroller = new VRCExpressionsMenu.Control()
                    {
                        name = control.name,
                        icon = control.icon,
                        labels = control.labels,
                        parameter = control.parameter,
                        style = control.style,
                        subMenu = control.subMenu,
                        subParameters = control.subParameters,
                        type = control.type,
                        value = control.value
                    };
                    current.controls.Add(newcontroller);
                }
                SetExpressionMenu(parentnmenu);
                EditorUtility.SetDirty(parentnmenu);
            }
            else
            {
                if (keepOriginalAsset)
                {
                    var current = MakeCopy<VRCExpressionsMenu>(menus,false);
                    SetExpressionMenu(current);
                    EditorUtility.SetDirty(current);
                }
                else
                {
                    SetExpressionMenu(menus);
                }
            }
        }
#endif
        /// <summary>
        /// prefabをボーン下にインスタンスする
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="prefab"></param>
        /// <param name="target"></param>
        void ModifyGameObject(GameObject prefab, out string fromPath, out string toPath , HumanBodyBones target = HumanBodyBones.Hips)
        {
            fromPath = "";
            toPath = "";
            // 重複防止で同名オブジェクトを削除する
            foreach (Transform child  in avatar.transform)
            {
                if(child.name == prefab.name) GameObject.DestroyImmediate(child.gameObject);
            }
            // オブジェクトのインスタンシエイト
            var instance = GameObject.Instantiate(prefab, avatar.transform);
            instance.name = prefab.name;
            
            var humanoid = avatar.GetComponent<Animator>();
            var constraint = instance.GetComponent<ParentConstraint>();
            if (constraint)
            { // コンストレイントでの設定
                constraint.constraintActive = false;
                constraint.weight = 1f;
                if (humanoid != null)
                {
                    if (humanoid.isHuman)
                    {
                        Transform bone = humanoid.GetBoneTransform(target);
                        if (constraint != null)
                        {
                            constraint.AddSource(new ConstraintSource()
                            {
                                weight = 1f,
                                sourceTransform = bone
                            });
                            constraint.constraintActive = true;
                        }
                    }
                }
            }
            else
            if(humanoid)
            { //ボーン差し替えでの設定
                fromPath = GetRelativePath(instance.transform);
                if (humanoid.isHuman)
                {
                    Transform bone = humanoid.GetBoneTransform(target);
                    if (bone)
                    {
                        foreach (Transform child  in bone)
                        {
                            if(child.name == prefab.name) GameObject.DestroyImmediate(child.gameObject);
                        }
                        instance.transform.SetParent(bone);
                        instance.transform.localPosition = new Vector3(0f,0f,0f);
                    }
                }
                toPath = GetRelativePath(instance.transform);
            }
        }
        
        string GetRelativePath(Transform o)
        {
            return AssetUtility.GetRelativePath(avatar.transform, o);
        }

        AnimationClip RePathAnimation(AnimationClip clip)
        {
            bool hasPath = false;
            using(var o = new SerializedObject(clip))
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
                clip = MakeCopy<AnimationClip>(clip,false);
                using(var o = new SerializedObject(clip))
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

#if VRC_SDK_VRCSDK3
        public void RepathAnimators(GameObject from,GameObject to)
        {
            var fromPath = GetRelativePath(from.transform);
            var toPath = GetRelativePath(to.transform);
            RepathAnimators(fromPath, toPath);
        }
        
        public void RepathAnimators(string from,string to)
        {
            foreach (var playableLayer in avatar.baseAnimationLayers)
            {
                if (playableLayer.animatorController == null) continue;
                AnimatorController ac = (AnimatorController) playableLayer.animatorController;
                if (ac)
                {
                    foreach (var layer in ac.layers)
                    {
                        RePathStateMachine(layer.stateMachine, from, to);
                    }
                }
            }
        }
#endif

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
                RePathStateMachine(m.stateMachine,from,to);
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
            else 
            if (motion is AnimationClip)
            {
                AnimationClip clip = (AnimationClip) motion;
                RePathAnimation(clip, from, to);
            }
        }
        
        AnimationClip RePathAnimation(AnimationClip clip,string from,string to)
        {
            bool hasPath = false;
            using(var o = new SerializedObject(clip))
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
                clip = MakeCopy<AnimationClip>(clip,false);
                using(var o = new SerializedObject(clip))
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
    }
}
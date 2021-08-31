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
using System.IO;
using UnityEditor.Animations;
using UnityEngine.Animations;
using Object = UnityEngine.Object;
using System.Text;
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
        
        public bool? WriteDefaultOverride { get; set; } = null;
        public bool safeOriginalAsset = true;
        public bool overrideSettings = true;
        public bool renameParameters = false;
        public bool autoAddNextPage = false;
        public bool overrideNullAnimation = true;
        private string exportDir = "Assets/";
        string prefix = "";
        private Dictionary<string, string> animRepathList = new Dictionary<string, string>();
        public void ModifyAvatar(AvatarModifyData assets,string keyword = "")
        {
            prefix = keyword;
            if (renameParameters) assets = RenameAssetsParameters(assets);
            if (overrideSettings) RevertByAssets(assets);
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
                ComputeLayersOffset(assets);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.Base,assets.locomotion_controller);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.Additive,assets.idle_controller);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.Gesture,assets.gesture_controller);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.Action,assets.action_controller);
                ModifyAvatarAnimatorController(VRCAvatarDescriptor.AnimLayerType.FX,assets.fx_controller);
                ModifyExpressionParameter(assets.parameter,safeOriginalAsset);
                ModifyExpressionMenu(assets.menu,autoAddNextPage,safeOriginalAsset);
                AssetDatabase.SaveAssets();
            }
            else
            {
                throw new NullReferenceException("VRCAvatarDescriptor : avatar not found");
            }
            EditorUtility.SetDirty( avatar );
        }

        public void RevertByAssets(AvatarModifyData assets)
        {
            if (avatar != null)
            {
                animRepathList = new Dictionary<string,string>();
                if (assets.items != null)
                {
                    foreach (var item in assets.items)
                    {
                        RevertGameObject(item.prefab,item.target);
                    }
                }
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.Base,assets.locomotion_controller);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.Additive,assets.idle_controller);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.Gesture,assets.gesture_controller);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.Action,assets.action_controller);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.FX,assets.fx_controller);
                RevertExpressionParameter(assets.parameter);
                RevertExpressionMenu(assets.menu);
                AssetDatabase.SaveAssets();
            }
            else
            {
                throw new NullReferenceException("VRCAvatarDescriptor : avatar not found");
            }
            EditorUtility.SetDirty( avatar );
        }

        public void RevertByKeyword(string keyword)
        {
            if (avatar != null)
            {
                DeleateInChild(avatar.transform,keyword);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.Base,keyword);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.Additive,keyword);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.Gesture,keyword);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.Action,keyword);
                RevertAnimator(VRCAvatarDescriptor.AnimLayerType.FX,keyword);
                RevertExpressionParameter(keyword);
                RevertExpressionMenu(keyword);
                AssetDatabase.SaveAssets();
            }
            else
            {
                throw new NullReferenceException("VRCAvatarDescriptor : avatar not found");
            }
            EditorUtility.SetDirty( avatar );
        }

        void DeleateInChild(Transform parent,string keyword)
        {
            for (int i = 0; i < parent.childCount;)
            {
                if (parent.GetChild(i).gameObject.name.StartsWith(keyword))
                {
                    GameObject.DestroyImmediate(parent.GetChild(i).gameObject);
                }
                else
                {
                    DeleateInChild(parent.GetChild(i),keyword);
                    i++;
                }
            }
        }
        
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
                writeDefaultValues = WriteDefaultOverride ?? origin.writeDefaultValues
            };
            return clone;
        }

        Motion CloneMotion(Motion origin)
        {
            if (origin == null)
            {
                if (overrideNullAnimation)
                {
                    return AssetUtility.LoadAssetAtGuid<AnimationClip>(EnvironmentVariable.nottingAnim);
                }
                else
                {
                    return null;
                }
            }
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
            machine.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(machine,path);
            foreach (var s in machine.states)
            {
                AssetDatabase.AddObjectToAsset(s.state,path);
                SaveMotion(s.state.motion,path);
                foreach (var t in s.state.transitions)
                {
                    t.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(t,path);
                }
                foreach (var b in s.state.behaviours)
                {
                    b.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(b,path);
                }
            }

            foreach (var t in machine.entryTransitions)
            {
                t.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(t,path);
            }
            foreach (var t in machine.anyStateTransitions)
            {
                t.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(t,path);
            }
            foreach (var m in machine.stateMachines)
            {
                SaveStateMachine(m.stateMachine,path);
                foreach (var b in m.stateMachine.behaviours)
                {
                    b.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(b,path);
                }
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
                    parameter.name = GetSafeParam(parameter.name);
                    // すでに同名パラメーターがあれば削除する
                    int index = Array.FindIndex(cloneController.parameters, p => p.name == parameter.name);
                    if (index > -1) cloneController.RemoveParameter(cloneController.parameters[index]);
                    // パラメーターのコピー
                    cloneController.AddParameter(parameter);
                }
            }
        }

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
                    AddExpressionParameter(current, parameter.name, parameter.valueType, parameter.saved,parameter.defaultValue);
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
            VRCExpressionParameters.ValueType type, bool saved = true, float value = 0f)
        {
            // パラメータズのコピー作成(重複があった場合スキップ)
            var originalParm = parameters.parameters;
            var newParm = new List<VRCExpressionParameters.Parameter>();
            foreach (var parameter in parameters.parameters)
            {
                if (!String.IsNullOrWhiteSpace(parameter.name) &&
                    parameter.name != name)
                {
                    newParm.Add(parameter);
                }
            }

            // 新規パラメータ追加
            newParm.Add(new VRCExpressionParameters.Parameter()
            {
                name = name,
                valueType = type,
                saved = saved,
                defaultValue = value
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
                    int menuMax = 8;
                    while (current.controls.Count >= menuMax && autoNextPage) // 項目が上限に達していたら次ページに飛ぶ
                    {
                        if (current.controls[menuMax-1].name == "NextPage" &&
                            current.controls[menuMax-1].type == VRCExpressionsMenu.Control.ControlType.SubMenu &&
                            current.controls[menuMax-1].subMenu != null)
                        {
                            current = current.controls[menuMax-1].subMenu;
                        }
                        else
                        {
                            var m = new MenuCreater("NextPage");
                            m.AddControll(current.controls[menuMax-1]);
                            var submenu = m.CreateAsset(exportDir);
                            current.controls[menuMax-1] = new VRCExpressionsMenu.Control()
                            {
                                name = "NextPage",
                                icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentVariable.arrowIcon),
                                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                                subMenu = submenu
                            };
                            current = submenu;
                        }
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

        void RevertAnimator(
            VRCAvatarDescriptor.AnimLayerType type,
            AnimatorController controller)
        {
            if(controller == null) return;
            if(GetAvatarAnimatorControllerExists(type))
            {
                var current = GetAvatarAnimatorController(type);
                if (controller == current) return;
                var newLayers = new List<AnimatorControllerLayer>();
                foreach (var layer in current.layers)
                {
                    if (controller.layers.Any(l =>
                        (l.name == layer.name)))
                    {
                        
                    }
                    else
                    {
                        newLayers.Add(layer);
                    }
                }
                current.layers = newLayers.ToArray();
                EditorUtility.SetDirty(current);
            }
        }
        
        public void RevertAnimator(
            VRCAvatarDescriptor.AnimLayerType type,
            string keyword)
        {
            if(String.IsNullOrWhiteSpace(keyword)) return;
            if(GetAvatarAnimatorControllerExists(type))
            {
                var current = GetAvatarAnimatorController(type);
                current.parameters = current.parameters.Where(p => !p.name.StartsWith(keyword)).ToArray();
                current.layers = current.layers.Where(l => !l.name.StartsWith(keyword)).ToArray();
                EditorUtility.SetDirty(current);
            }
        }
        
        void RevertExpressionParameter(VRCExpressionParameters parameters)
        {
            if(parameters == null) return;
            if (GetExpressionParameterExist())
            {
                var current = GetExpressionParameter();
                if (parameters == current)
                {
                    SetExpressionParameter(null);
                }

                var newParams = new List<VRCExpressionParameters.Parameter>();
                foreach (var parameter in current.parameters)
                {
                    if (parameters.parameters.Any(p => (
                        p.name == parameter.name &&
                        p.valueType == parameter.valueType)))
                    {
                    }
                    else
                    {
                        newParams.Add(parameter);
                    }
                }
                current.parameters = newParams.ToArray();
                EditorUtility.SetDirty(current);
            }
        }
        
        void RevertExpressionParameter(string keyword)
        {
            if(String.IsNullOrWhiteSpace(keyword)) return;
            if (GetExpressionParameterExist())
            {
                var current = GetExpressionParameter();
                current.parameters = current.parameters.Where(p => !p.name.StartsWith(keyword)).ToArray();
                EditorUtility.SetDirty(current);
            }
        }

        void RevertExpressionMenu(VRCExpressionsMenu menus)
        {
            if (menus == null) return;
            if (GetExpressionMenuExist())
            {
                var parentnmenu = GetExpressionMenu();
                if (parentnmenu == menus)
                {
                    SetExpressionMenu(null);
                }
                RevertMenu(menus, parentnmenu);
                EditorUtility.SetDirty(parentnmenu);
            }
        }

        void RevertMenu(VRCExpressionsMenu menus, VRCExpressionsMenu parentnmenu)
        {
            if (menus == null) return;
            if (parentnmenu == null) return;
            var newControll = new List<VRCExpressionsMenu.Control>();
            foreach (var controll in parentnmenu.controls)
            {
                if (menus.controls.Any(c => (
                    c.name == controll.name &&
                    c.icon == controll.icon &&
                    c.type == controll.type &&
                    c.parameter.name == controll.parameter.name)))
                {
                }
                else
                if (controll.name == "NextPage" &&
                    controll.icon == AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentVariable.arrowIcon) &&
                    controll.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                    if(controll.subMenu) RevertMenu(menus,controll.subMenu);
                }
                else
                {
                    newControll.Add(controll);
                }
            }
            
            parentnmenu.controls = newControll.ToList();
            EditorUtility.SetDirty(parentnmenu);
        }
        
        void RevertExpressionMenu(string keyword)
        {
            if(String.IsNullOrWhiteSpace(keyword)) return;
            if (GetExpressionMenuExist())
            {
                var parentnmenu = GetExpressionMenu();
                RevertMenu(keyword, parentnmenu);
                EditorUtility.SetDirty(parentnmenu);
            }
        }
        void RevertMenu(string keyword, VRCExpressionsMenu parentnmenu)
        {
            if (parentnmenu == null) return;
            var newControll = new List<VRCExpressionsMenu.Control>();
            foreach (var controll in parentnmenu.controls)
            {
                if (controll.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                    if(controll.subMenu!=null)
                    {
                        RevertMenu(keyword, controll.subMenu);
                        if (controll.subMenu.controls.Count > 0)
                        {
                            newControll.Add(controll);
                        }
                    }
                }
                else
                if(controll.parameter.name.StartsWith(keyword))
                {
                    
                }
                else
                if(controll.subParameters.Any(p=>p.name.StartsWith(keyword)))
                {
                    
                }
                else
                {
                    newControll.Add(controll);
                }
            }
            parentnmenu.controls = newControll.ToList();
            EditorUtility.SetDirty(parentnmenu);
        }
        
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
            // オブジェクトのインスタンシエイト
            var instance = GameObject.Instantiate(prefab, avatar.transform);
            if (!renameParameters)
            {
                instance.name = prefab.name;
            }
            else
            if (prefab.name.StartsWith(prefix))
            {
                instance.name = prefab.name;
            }
            else
            {
                instance.name = prefix + prefab.name;
            }
            
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
                        instance.transform.SetParent(bone);
                        instance.transform.localPosition = new Vector3(0f,0f,0f);
                    }
                }
                toPath = GetRelativePath(instance.transform);
            }
        }
        
        void RevertGameObject(GameObject prefab, HumanBodyBones target = HumanBodyBones.Hips)
        {
            // オブジェクトのインスタンシエイト
            var humanoid = avatar.GetComponent<Animator>();
            var constraint = prefab.GetComponent<ParentConstraint>();
            if (constraint)
            { // コンストレイントでの設定
                foreach (Transform child  in avatar.transform)
                {
                    if(child.name == prefab.name) GameObject.DestroyImmediate(child.gameObject);
                }
            }
            else
            if(humanoid)
            { //ボーン差し替えでの設定
                if (humanoid.isHuman)
                {
                    Transform bone = humanoid.GetBoneTransform(target);
                    if (bone)
                    {
                        foreach (Transform child  in bone)
                        {
                            if(child.name == prefab.name) GameObject.DestroyImmediate(child.gameObject);
                        }
                    }
                }
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

        AvatarModifyData RenameAssetsParameters(AvatarModifyData assets)
        {
            assets.locomotion_controller = AnimatorControllerParameterRename(assets.locomotion_controller);
            assets.idle_controller = AnimatorControllerParameterRename(assets.idle_controller);
            assets.action_controller = AnimatorControllerParameterRename(assets.action_controller);
            assets.gesture_controller = AnimatorControllerParameterRename(assets.gesture_controller);
            assets.fx_controller = AnimatorControllerParameterRename(assets.fx_controller);
            assets.menu = ExpressionMenuParameterRename(assets.menu);
            assets.parameter = ExpressionParameterRename(assets.parameter);
            return assets;
        }

        AnimatorController AnimatorControllerParameterRename(AnimatorController anim)
        {
            if(anim == null) return null;
            anim.parameters = anim.parameters.Select(p => new AnimatorControllerParameter()
            {
                name = GetSafeParam(p.name),
                type = p.type,
                defaultBool = p.defaultBool,
                defaultFloat = p.defaultFloat,
                defaultInt = p.defaultInt,
            }).ToArray();
            anim.layers = anim.layers.Select(l =>
            {
                l.name = GetSafeParam(l.name);
                l.stateMachine = StateMachineParameterRename(l.stateMachine);
                return l;
            }).ToArray();
            EditorUtility.SetDirty(anim);
            return anim;
        }
        
        AnimatorStateMachine StateMachineParameterRename(AnimatorStateMachine machine)
        {
            machine.states = machine.states.Select(s =>
            {
                if (s.state.motion is BlendTree)
                {
                    BlendTree BlendTreeParameterRename(BlendTree b)
                    {
                        b.blendParameter = GetSafeParam(b.blendParameter);
                        b.blendParameterY = GetSafeParam(b.blendParameterY);
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
                s.state.timeParameter = GetSafeParam(s.state.timeParameter);
                s.state.speedParameter = GetSafeParam(s.state.speedParameter);
                s.state.mirrorParameter = GetSafeParam(s.state.mirrorParameter);
                s.state.cycleOffsetParameter = GetSafeParam(s.state.cycleOffsetParameter);

                s.state.transitions = s.state.transitions.Select(t =>
                {
                    t.conditions = t.conditions.Select(c => new AnimatorCondition()
                    {
                        mode = c.mode,
                        parameter = GetSafeParam(c.parameter),
                        threshold = c.threshold
                    }).ToArray();
                    return t;
                }).ToArray();

                s.state.behaviours = s.state.behaviours.Select(b =>
                {
                    if (b is VRCAvatarParameterDriver)
                    {
                        var p = (VRCAvatarParameterDriver) b;
                        p.parameters = p.parameters.Select(e =>
                        {
                            e.name = GetSafeParam(e.name);
                            return e;
                        }).ToList();
                    }
                    return b;
                }).ToArray();
                return s;
            }).ToArray();
            
            machine.entryTransitions = machine.entryTransitions.Select(t =>
            {
                t.conditions = t.conditions.Select(c => new AnimatorCondition()
                {
                    mode = c.mode,
                    parameter = GetSafeParam(c.parameter),
                    threshold = c.threshold
                }).ToArray();
                return t;
            }).ToArray();

            machine.anyStateTransitions = machine.anyStateTransitions.Select(t =>
            {
                t.conditions = t.conditions.Select(c => new AnimatorCondition()
                {
                    mode = c.mode,
                    parameter = GetSafeParam(c.parameter),
                    threshold = c.threshold
                }).ToArray();
                return t;
            }).ToArray();

            machine.stateMachines = machine.stateMachines.Select(m =>
            {
                StateMachineParameterRename(m.stateMachine);
                m.stateMachine.behaviours = m.stateMachine.behaviours.Select(b =>
                {
                    if (b is VRCAvatarParameterDriver)
                    {
                        var p = (VRCAvatarParameterDriver) b;
                        p.parameters = p.parameters.Select(e =>
                        {
                            e.name = GetSafeParam(e.name);
                            return e;
                        }).ToList();
                    }

                    return b;
                }).ToArray();
                return m;
            }).ToArray();
            return machine;
        }

        VRCExpressionParameters ExpressionParameterRename(VRCExpressionParameters param)
        {
            if(param == null) return null;
            if(param.parameters == null) return null;
            // param = ScriptableObject.Instantiate(param);
            param.parameters = param.parameters.Select(p =>
                new VRCExpressionParameters.Parameter()
                {
                    name = GetSafeParam(p.name),
                    saved = p.saved,
                    defaultValue = p.defaultValue,
                    valueType = p.valueType
                }
            ).ToArray();
            EditorUtility.SetDirty(param);
            return param;
        }
        
        VRCExpressionsMenu ExpressionMenuParameterRename(VRCExpressionsMenu menu)
        {
            if (menu == null) return null;
            if (menu.controls == null) return null;
            // menu = ScriptableObject.Instantiate(menu);
            menu.controls = menu.controls.Select(c =>
            {
                if (c.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                    c.subMenu = ExpressionMenuParameterRename(c.subMenu);
                }
                c.parameter = new VRCExpressionsMenu.Control.Parameter(){name = GetSafeParam(c.parameter.name)};
                if (c.subParameters != null)
                {
                    c.subParameters = c.subParameters.Select(cc =>
                    {
                        return new VRCExpressionsMenu.Control.Parameter(){name = GetSafeParam(cc.name)};
                    }).ToArray();
                }
                return c;
            }).ToList();
            EditorUtility.SetDirty(menu);
            return menu;
        }
        
        // パラメータ文字列から2バイト文字の除去を行う
        public string GetSafeParam(string param)
        {
            if (String.IsNullOrWhiteSpace(param)) return "";
            if (EnvironmentVariable.VRChatParams.Contains(param)) return param;

            if (param.StartsWith(prefix))
            {
                return GetNihongoHash(param);
            }
            else
            {
                return prefix + GetNihongoHash(param);
            }
        }

        public static string GetNihongoHash(string origin)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char ch in origin ) {
                if ( "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHJIJKLMNOPQRSTUVWXYZ!\"#$%&'()=-~^|\\`@{[}]*:+;_?/>.<,".IndexOf(ch) >= 0 ) {
                    builder.Append(ch);
                }
                else
                {
                    int hash = ch.GetHashCode();
                    int code = hash % 26;
                    code += (int)'A';
                    code = Mathf.Clamp(code, (int) 'A', (int) 'Z');
                    builder.Append((char)code);
                }
            }

            return builder.ToString();
        }
        
#endif
    }
}
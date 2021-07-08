using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
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

        public AvatarModifyTool(VRCAvatarDescriptor a)
        {
            avatar = a;
        }
#else
        private MonoBehaviour avatar;
#endif
        public void ModifyAvatar(AvatarModifyData assets)
        {
            if (avatar != null)
            {
                if(assets.prefab!=null) ModifyGameObject(assets.prefab,assets.target);
#if VRC_SDK_VRCSDK3
                if(assets.locomotion_controller!=null) ModifyAnimatorController(GetAvatarAnimatorController(assets.locomotion_controller,VRCAvatarDescriptor.AnimLayerType.Base), assets.locomotion_controller);
                if(assets.idle_controller!=null) ModifyAnimatorController(GetAvatarAnimatorController(assets.idle_controller,VRCAvatarDescriptor.AnimLayerType.Additive), assets.idle_controller);
                if(assets.gesture_controller!=null) ModifyAnimatorController(GetAvatarAnimatorController(assets.gesture_controller,VRCAvatarDescriptor.AnimLayerType.Gesture), assets.gesture_controller);
                if(assets.action_controller!=null) ModifyAnimatorController(GetAvatarAnimatorController(assets.action_controller,VRCAvatarDescriptor.AnimLayerType.Action), assets.action_controller);
                if(assets.fx_controller!=null) ModifyAnimatorController(GetAvatarAnimatorController(assets.fx_controller,VRCAvatarDescriptor.AnimLayerType.FX), assets.fx_controller);
                if(assets.parameter != null) ModifyExpressionParameter(GetExpressionParameters(assets.parameter), assets.parameter);
                if(assets.menu != null) ModifyExpressionMenu(GetExpressionMenus(assets.menu), assets.menu);
#endif
            }
            else
            {
                throw new NullReferenceException("VRCAvatarDescriptor : avatar not found");
            }
        }

#if VRC_SDK_VRCSDK3
        AnimatorController GetAvatarAnimatorController( AnimatorController defaultController = null, VRCAvatarDescriptor.AnimLayerType type = VRCAvatarDescriptor.AnimLayerType.FX)
        {
            avatar.customizeAnimationLayers = true;
            var index = Array.FindIndex(avatar.baseAnimationLayers,
                l => l.type == type);
            avatar.baseAnimationLayers[index].isDefault = false;
            if (avatar.baseAnimationLayers[index].animatorController != null)
            {
            }
            else
            {
                avatar.baseAnimationLayers[index].animatorController = MakeCopy<AnimatorController>(defaultController);
            }

            return (AnimatorController) avatar.baseAnimationLayers[index].animatorController;
        }

        VRCExpressionParameters GetExpressionParameters( VRCExpressionParameters defaultParameters = null)
        {
            avatar.customExpressions = true;
            if (avatar.expressionParameters == null)
            {
                avatar.expressionParameters = MakeCopy<VRCExpressionParameters>(defaultParameters);
            }

            return avatar.expressionParameters;
        }

        VRCExpressionsMenu GetExpressionMenus( VRCExpressionsMenu defaultMenus = null)
        {
            avatar.customExpressions = true;
            if (avatar.expressionsMenu == null)
            {
                avatar.expressionsMenu = MakeCopy<VRCExpressionsMenu>(defaultMenus);
            }

            return avatar.expressionsMenu;
        }
#endif

        T MakeCopy<T>(Object origin) where T : Object
        {
            string sufix = origin is AnimatorController ? ".controller" : ".asset";
            if (origin != null)
            {
                string path = AssetDatabase.GetAssetPath(origin);
                //string dir = Path.GetDirectoryName(path);
                string dir = "Assets";
                string copyPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dir, origin.name + "_copy" + sufix));
                if (!string.IsNullOrWhiteSpace(path))
                {
                    AssetDatabase.CopyAsset(path, copyPath);
                }
                else
                {
                    AssetDatabase.CreateAsset(origin,copyPath);
                }
                AssetDatabase.ImportAsset(copyPath);
                return AssetDatabase.LoadAssetAtPath(copyPath, typeof(T)) as T;
            }

            return null;
        }

        private string animationControllerPath;
        /// <summary>
        /// AnimatorController関係のオブジェクトを保存する
        /// </summary>
        /// <param name="o"></param>
        T SaveAnimatorControllerObject<T>(T o) where T : Object
        {
            if (!string.IsNullOrWhiteSpace(animationControllerPath))
            {
                AssetDatabase.AddObjectToAsset(o,animationControllerPath);
            }

            return o;
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
                animationControllerPath = AssetDatabase.GetAssetPath(controller);
                CloneLayers(controller, origin);
                CloneAnimatorParamaters(controller, origin);
                animationControllerPath = "";
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
                avatarMask = originLayer.avatarMask,
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

            SaveAnimatorControllerObject(cloneStateMachine);
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
#if VRC_SDK_VRCSDK3
                    cloneState = CopyVRCComponent(cloneState, originState);
#endif
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
                // animarionをコピーすべきか？問題
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
            SaveAnimatorControllerObject(clone);
            return clone;
        }

        Motion CloneMotion(Motion origin)
        {
            if (origin.GetType() == typeof(BlendTree))
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
                SaveAnimatorControllerObject(c);
                return c;
            }
            else if(origin.GetType() == typeof(AnimationClip))
            {
                return origin;
            }

            return origin;
        }

#if VRC_SDK_VRCSDK3
        AnimatorState CopyVRCComponent(AnimatorState clone,AnimatorState origin)
        {
            foreach (var behaviour in origin.behaviours)
            {
                if (behaviour.GetType() == typeof(VRCAnimatorLayerControl))
                {
                    VRCAnimatorLayerControl o = behaviour as VRCAnimatorLayerControl;
                    var c = clone.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                    c.ApplySettings = o.ApplySettings;
                    c.debugString = o.debugString;
                    c.layer = o.layer;
                    c.blendDuration = o.blendDuration;
                    c.goalWeight = o.goalWeight;
                    c.playable = o.playable;
                }
                else
                if (behaviour.GetType() == typeof(VRCAnimatorLocomotionControl))
                {
                    VRCAnimatorLocomotionControl o = behaviour as VRCAnimatorLocomotionControl;
                    var c = clone.AddStateMachineBehaviour<VRCAnimatorLocomotionControl>();
                    c.ApplySettings = o.ApplySettings;
                    c.debugString = o.debugString;
                    c.disableLocomotion = o.disableLocomotion;
                }
                else
                if (behaviour.GetType() == typeof(VRCAnimatorTemporaryPoseSpace))
                {
                    VRCAnimatorTemporaryPoseSpace o = behaviour as VRCAnimatorTemporaryPoseSpace;
                    var c = clone.AddStateMachineBehaviour<VRCAnimatorTemporaryPoseSpace>();
                    c.ApplySettings = o.ApplySettings;
                    c.debugString = o.debugString;
                    c.delayTime = o.delayTime;
                    c.fixedDelay = o.fixedDelay;
                    c.enterPoseSpace = o.enterPoseSpace;
                }
                else
                if (behaviour.GetType() == typeof(VRCAnimatorTrackingControl))
                {
                    VRCAnimatorTrackingControl o = behaviour as VRCAnimatorTrackingControl;
                    var c = clone.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
                    c.ApplySettings = o.ApplySettings;
                    c.debugString = o.debugString;
                    c.trackingEyes = o.trackingEyes;
                    c.trackingHead = o.trackingHead;
                    c.trackingHip = o.trackingHip;
                    c.trackingMouth = o.trackingMouth;
                    c.trackingLeftFingers = o.trackingLeftFingers;
                    c.trackingLeftFoot = o.trackingLeftFoot;
                    c.trackingLeftHand = o.trackingLeftHand;
                    c.trackingRightFingers = o.trackingRightFingers;
                    c.trackingRightFoot = o.trackingRightFoot;
                    c.trackingRightHand = o.trackingRightHand;
                }
                else
                if (behaviour.GetType() == typeof(VRCAvatarParameterDriver))
                {
                    VRCAvatarParameterDriver o = behaviour as VRCAvatarParameterDriver;
                    var c = clone.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                    c.ApplySettings = o.ApplySettings;
                    c.debugString = o.debugString;
                    c.parameters = o.parameters.Select(p => new VRC_AvatarParameterDriver.Parameter()
                    {
                        name = p.name,
                        value = p.value,
                        chance = p.chance,
                        type = p.type,
                        valueMax = p.valueMax,
                        valueMin = p.valueMin
                    }).ToList();
                    c.localOnly = o.localOnly;
                }
                else
                if (behaviour.GetType() == typeof(VRCPlayableLayerControl))
                {
                    VRCPlayableLayerControl o = behaviour as VRCPlayableLayerControl;
                    var c = clone.AddStateMachineBehaviour<VRCPlayableLayerControl>();
                    c.ApplySettings = o.ApplySettings;
                    c.debugString = o.debugString;
                    c.layer = o.layer;
                    c.blendDuration = o.blendDuration;
                    c.goalWeight = o.goalWeight;
                    c.outputParamHash = o.outputParamHash;
                }
            }

            return clone;
        }
#endif 
        
        void SaveStateMachine(AnimatorStateMachine machine,string path)
        {
            AssetDatabase.AddObjectToAsset(machine,path);
            foreach (var s in machine.states)
            {
                AssetDatabase.AddObjectToAsset(s.state,path);
                foreach (var t in s.state.transitions)
                {
                    AssetDatabase.AddObjectToAsset(t,path);
                }
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
        void ModifyExpressionParameter(VRCExpressionParameters parameters, VRCExpressionParameters origin)
        {
            if (parameters != origin && parameters != null && origin != null)
            {
                foreach (var parameter in origin.parameters)
                {
                    AddExpressionParameter(parameters, parameter.name, parameter.valueType, parameter.saved);
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
        void ModifyExpressionMenu(VRCExpressionsMenu menus, VRCExpressionsMenu origin)
        {
            if (menus != origin && menus != null && origin != null)
            {
                foreach (var control in origin.controls)
                {
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
                    
                    var overlap = menus.controls.FindIndex(c => c.name == control.name);
                    if ( overlap > -1)
                    {
                        menus.controls[overlap] = newcontroller;
                    }
                    else
                    {
                        menus.controls.Add(newcontroller);
                    }
                }
            }

            EditorUtility.SetDirty(menus);
        }
#endif

        /// <summary>
        /// prefabをアバター直下にインスタンスする
        /// target指定でボーンに追従するように紐づける
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="prefab"></param>
        /// <param name="target"></param>
        void ModifyGameObject(GameObject prefab , HumanBodyBones target = HumanBodyBones.Hips)
        {
            // 重複防止で同名オブジェクトを削除する
            foreach (Transform child  in avatar.transform)
            {
                if(child.name == prefab.name) GameObject.DestroyImmediate(child.gameObject);
            }
            // オブジェクトのインスタンシエイト
            var instance = GameObject.Instantiate(prefab, avatar.transform);
            instance.name = prefab.name;
            instance.transform.localPosition = new Vector3(0f,0f,0f);
            
            // Constraintの設定
            var humanoid = avatar.GetComponent<Animator>();
            var constraint = instance.GetComponent<ParentConstraint>();
            if (constraint != null)
            {
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
        }
    }
}
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif
#if HAS_MA
using nadena.dev.modular_avatar.core;
#endif
using System.Linq;
using System;

namespace HhotateA.AvatarModifyTools.Core
{
    public static class ModularAvatarUtil
    {
#if HAS_MA
        public static bool MAEnabled => true;

        public static void MergeAnimatorController(GameObject go, VRCAvatarDescriptor.AnimLayerType animLayerType, AnimatorController animatorController)
        {
            var mergeAnimators = go.GetComponents<ModularAvatarMergeAnimator>();
            var mergeAnimator = mergeAnimators.FirstOrDefault(ma => ma.layerType == animLayerType);
            if (mergeAnimator == null)
            {
                if (animatorController == null)
                {
                    return;
                }
                mergeAnimator = go.AddComponent<ModularAvatarMergeAnimator>();
                mergeAnimator.layerType = animLayerType;
                Undo.RegisterCreatedObjectUndo(mergeAnimator, "Merge Animator Controller");
            }
            else
            {
                if (animatorController == null)
                {
                    Undo.DestroyObjectImmediate(mergeAnimator);
                    return;
                }
                Undo.RecordObject(mergeAnimator, "Merge Animator Controller");
            }
            mergeAnimator.animator = animatorController;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            mergeAnimator.matchAvatarWriteDefaults = true;
        }

        public static void MenuInstaller(GameObject go, VRCExpressionsMenu menu)
        {
            var menuInstaller = go.GetComponent<ModularAvatarMenuInstaller>();
            if (menuInstaller == null)
            {
                menuInstaller = go.AddComponent<ModularAvatarMenuInstaller>();
                Undo.RegisterCreatedObjectUndo(menuInstaller, "Menu Installer");
            }
            else
            {
                Undo.RecordObject(menuInstaller, "Menu Installer");
            }
            menuInstaller.menuToAppend = menu;
        }

        public static void Parameter(GameObject go, VRCExpressionParameters parameters)
        {
            var maParameters = go.GetComponent<ModularAvatarParameters>();
            if (maParameters == null)
            {
                maParameters = go.AddComponent<ModularAvatarParameters>();
                Undo.RegisterCreatedObjectUndo(maParameters, "Parameters");
            }
            else
            {
                Undo.RecordObject(maParameters, "Parameters");
            }
            maParameters.parameters.Clear();
            foreach (var parameter in parameters.parameters)
            {
                maParameters.parameters.Add(new ParameterConfig
                {
                    nameOrPrefix = parameter.name,
                    saved = parameter.saved,
                    syncType = parameter.valueType.SyncType(),
                    defaultValue = parameter.defaultValue,
#if HAS_MA_1_9
                    hasExplicitDefaultValue = true,
#endif
#if HAS_MA_1_5
                    localOnly = !parameter.networkSynced,
#endif
                });
            }
        }

        static ParameterSyncType SyncType(this VRCExpressionParameters.ValueType parameterValueType)
        {
            switch (parameterValueType)
            {
                case VRCExpressionParameters.ValueType.Int: return ParameterSyncType.Int;
                case VRCExpressionParameters.ValueType.Float: return ParameterSyncType.Float;
                case VRCExpressionParameters.ValueType.Bool: return ParameterSyncType.Bool;
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterValueType), parameterValueType, null);
            }
        }
#else
        public static bool MAEnabled => false;
#if VRC_SDK_VRCSDK3
        public static void MergeAnimatorController(GameObject go, VRCAvatarDescriptor.AnimLayerType animLayerType, AnimatorController animatorController) { }
        public static void MenuInstaller(GameObject go, VRCExpressionsMenu menu) { }
        public static void Parameter(GameObject go, VRCExpressionParameters parameters) { }
#endif
#endif
    }
}

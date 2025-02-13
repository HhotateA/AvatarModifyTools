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
using AnimatorLayerType = HhotateA.AvatarModifyTools.Core.AnimatorUtility.AnimatorLayerType;

#if VRC_SDK_VRCSDK3
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
#else
        private Animator avatar;
#endif
        
        private AnimatorModifier animMod;
        public bool DuplicateAssets {private get; set; } = true;
        public bool OverrideSettings {private get; set; } = true;
        public bool RenameParameters {private get; set; } = false;
        public bool ModifyOriginalAsset {private get; set; } = false;
        public bool AutoAddNextPage {private get; set; } = false;
        public bool OverrideNullAnimation {private get; set; } = true;
        private string exportDir = "Assets/";
        string prefix = "";
        public bool? WriteDefaultOverride
        {
            set
            {
                animMod.writeDefaultOverride = value;
            }
        }
        
        // コンストラクタ,VRCSDKない環境でもうまく動くよう引数を調節
#if VRC_SDK_VRCSDK3
        public AvatarModifyTool(VRCAvatarDescriptor a, string dir = "Assets/Export")
        {
            avatar = a;
            Init(dir);
        }
#else
        public AvatarModifyTool(Animator a, string dir = "Assets/Export")
        {
            avatar = a;
            Init(dir);
        }
#endif
        
        public AvatarModifyTool(MonoBehaviour a, string dir = "Assets/Export")
        {
#if VRC_SDK_VRCSDK3
            avatar = a.GetComponent<VRCAvatarDescriptor>();
#else
            avatar = a.GetComponent<Animator>();
#endif
            Init(dir);
        }

        void Init(string dir = "Assets/Export")
        {
            if (avatar == null)
            {
                throw new NullReferenceException("Avatar reference missing");
            }
            
            if (dir == "Assets/Export")
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    AssetDatabase.CreateFolder("Assets", "Export");
                }
            }
            exportDir = File.GetAttributes(dir)
                .HasFlag(FileAttributes.Directory)
                ? dir
                : Path.GetDirectoryName(dir);
            
            animMod = new AnimatorModifier();
            animMod.onFindParam += GetSafeParam;
            animMod.onFindAnimationClip += clip => MakeCopy<AnimationClip>(clip);
            animMod.onFindAvatarMask += CloneAvatarMask;
        }

        public void ModifyAvatarByMA(AvatarModifyData assets, string keyword = "")
        {
#if VRC_SDK_VRCSDK3
            if (!ModularAvatarUtil.MAEnabled)
            {
                return;
            }
            prefix = keyword;
            if (ModifyOriginalAsset) assets = RenameAssetsParameters(assets);

            animMod.animRepathList = new Dictionary<string, string>();
            // オフセットの記録
            animMod.layerOffset = ComputeLayersOffset(assets);

            var path = AssetDatabase.GetAssetPath(assets);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("AvatarModifyData : assets not found");
            }
            path = Path.ChangeExtension(path, ".prefab");
            if (!File.Exists(path))
            {
                var go = new GameObject();
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }
            var prefab = PrefabUtility.LoadPrefabContents(path);
            ModularAvatarUtil.MergeAnimatorController(prefab, VRCAvatarDescriptor.AnimLayerType.Base, assets.locomotion_controller);
            ModularAvatarUtil.MergeAnimatorController(prefab, VRCAvatarDescriptor.AnimLayerType.Additive, assets.idle_controller);
            ModularAvatarUtil.MergeAnimatorController(prefab, VRCAvatarDescriptor.AnimLayerType.Gesture, assets.gesture_controller);
            ModularAvatarUtil.MergeAnimatorController(prefab, VRCAvatarDescriptor.AnimLayerType.Action, assets.action_controller);
            ModularAvatarUtil.MergeAnimatorController(prefab, VRCAvatarDescriptor.AnimLayerType.FX, assets.fx_controller);
            ModularAvatarUtil.Parameter(prefab, assets.parameter);
            ModularAvatarUtil.MenuInstaller(prefab, assets.menu);

            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var hasPrefabInstance = false;
            foreach (Transform t in avatar.transform)
            {
                var p = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (p == prefab)
                {
                    hasPrefabInstance = true;
                    break;
                }
            }
            if (!hasPrefabInstance)
            {
                PrefabUtility.InstantiatePrefab(prefab, avatar.transform);
            }
#endif
        }

        /// <summary>
        /// アバターにmodを適応する
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="keyword"></param>
        /// <exception cref="NullReferenceException"></exception>
        public void ModifyAvatar(AvatarModifyData assets, string keyword = "")
        {
            prefix = keyword;
            if (ModifyOriginalAsset) assets = RenameAssetsParameters(assets);
            if (OverrideSettings) RevertByAssets(assets);
            
            animMod.animRepathList = new Dictionary<string, string>();
            if (assets.items != null)
            {
                foreach (var item in assets.items)
                {
                    ModifyGameObject(item.prefab, out var from, out var to, item.target);
                    animMod.animRepathList.Add(from, to);
                }
            }
#if VRC_SDK_VRCSDK3
            // オフセットの記録
            animMod.layerOffset = ComputeLayersOffset(assets);
#endif
            // Animatorの改変
            ModifyAvatarAnimatorController(AnimatorLayerType.Locomotion,assets.locomotion_controller);
            ModifyAvatarAnimatorController(AnimatorLayerType.Idle,assets.idle_controller);
            ModifyAvatarAnimatorController(AnimatorLayerType.Gesture,assets.gesture_controller);
            ModifyAvatarAnimatorController(AnimatorLayerType.Action,assets.action_controller);
            ModifyAvatarAnimatorController(AnimatorLayerType.Fx,assets.fx_controller);
#if VRC_SDK_VRCSDK3
            // Avatar項目の改変
            ModifyExpressionParameter(assets.parameter);
            ModifyExpressionMenu(assets.menu);
#endif
            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(avatar);
        }

        /// <summary>
        /// アバターのmodを取り除く
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="keyword"></param>
        /// <exception cref="NullReferenceException"></exception>
        public void RevertByAssets(AvatarModifyData assets, string keyword = "")
        {
            prefix = keyword;
            if (ModifyOriginalAsset) assets = RenameAssetsParameters(assets);
            if (avatar != null)
            {
                animMod.animRepathList = new Dictionary<string, string>();
                if (assets.items != null)
                {
                    foreach (var item in assets.items)
                    {
                        RevertGameObject(item.prefab, item.target);
                    }
                }
                RevertAnimator(AnimatorLayerType.Locomotion,assets.locomotion_controller);
                RevertAnimator(AnimatorLayerType.Idle,assets.idle_controller);
                RevertAnimator(AnimatorLayerType.Gesture,assets.gesture_controller);
                RevertAnimator(AnimatorLayerType.Action,assets.action_controller);
                RevertAnimator(AnimatorLayerType.Fx,assets.fx_controller);
#if VRC_SDK_VRCSDK3
                RevertExpressionParameter(assets.parameter);
                RevertExpressionMenu(assets.menu);
#endif
                AssetDatabase.SaveAssets();
            }
            else
            {
                throw new NullReferenceException("VRCAvatarDescriptor : avatar not found");
            }

            EditorUtility.SetDirty(avatar);
        }

        /// <summary>
        /// keywordを元に，アバターの改変を取り除く
        /// </summary>
        /// <param name="keyword"></param>
        /// <exception cref="NullReferenceException"></exception>
        public void RevertByKeyword(string keyword)
        {
            if (avatar != null)
            {
                DeleateInChild(avatar.transform, keyword);
                RevertAnimator(AnimatorLayerType.Locomotion, keyword);
                RevertAnimator(AnimatorLayerType.Idle, keyword);
                RevertAnimator(AnimatorLayerType.Gesture, keyword);
                RevertAnimator(AnimatorLayerType.Action, keyword);
                RevertAnimator(AnimatorLayerType.Fx, keyword);
#if VRC_SDK_VRCSDK3
                RevertExpressionParameter(keyword);
                RevertExpressionMenu(keyword);
#endif
                AssetDatabase.SaveAssets();
            }
            else
            {
                throw new NullReferenceException("VRCAvatarDescriptor : avatar not found");
            }

            EditorUtility.SetDirty(avatar);
        }


        public void RepathAnimators(GameObject from, GameObject to)
        {
            var fromPath = GetRelativePath(from.transform);
            var toPath = GetRelativePath(to.transform);
            RepathAnimators(fromPath, toPath);
        }

        public void RepathAnimators(string from, string to)
        {
#if VRC_SDK_VRCSDK3
            foreach (var playableLayer in avatar.baseAnimationLayers)
            {
                if (playableLayer.animatorController == null) continue;
                animMod.SetOrigin((AnimatorController) playableLayer.animatorController).RepathAnims(from,to);
            }
#else
            animMod.SetOrigin((AnimatorController) avatar.runtimeAnimatorController).RepathAnims(from,to);
#endif
        }

        public List<string> HasWriteDefaultLayers()
        {
            var layers = new List<string>();
#if VRC_SDK_VRCSDK3
            foreach (var playableLayer in avatar.baseAnimationLayers)
            {
                if (playableLayer.animatorController == null) continue;
                layers.AddRange(animMod.SetOrigin((AnimatorController) playableLayer.animatorController).WriteDefaultLayers());
            }
#else
            layers.AddRange(animMod.SetOrigin((AnimatorController) avatar.runtimeAnimatorController).WriteDefaultLayers());
#endif
            return layers;
        }

        public List<string> HasActivateKeyframeLayers(GameObject[] obj)
        {
            var path = obj.Where(o => o != null).Select(o => GetRelativePath(o.transform)).ToArray();
            return HasKeyframeLayers(path, "m_IsActive");
        }

        public List<string> HasMaterialKeyframeLayers(GameObject[] obj)
        {
            var path = obj.Where(o => o != null).Select(o => GetRelativePath(o.transform)).ToArray();
            return HasKeyframeLayers(path, "m_Materials");
        }

        public List<string> HasKeyframeLayers(string[] path, string attribute = "")
        {
            var layers = new List<string>();
#if VRC_SDK_VRCSDK3
            foreach (var playableLayer in avatar.baseAnimationLayers)
            {
                if (playableLayer.animatorController == null) continue;
                layers.AddRange(
                    animMod.
                        SetOrigin((AnimatorController) playableLayer.animatorController).
                        HasKeyframeLayers(path, attribute));
            }
#else
            layers.AddRange(
                animMod.
                    SetOrigin((AnimatorController) avatar.runtimeAnimatorController).
                    HasKeyframeLayers(path, attribute));
#endif

            return layers;
        }

        AvatarModifyData RenameAssetsParameters(AvatarModifyData assets)
        {
            assets.locomotion_controller = animMod.SetOrigin(assets.locomotion_controller).AnimatorControllerParameterRename();
            assets.idle_controller = animMod.SetOrigin(assets.idle_controller).AnimatorControllerParameterRename();
            assets.action_controller = animMod.SetOrigin(assets.action_controller).AnimatorControllerParameterRename();
            assets.gesture_controller = animMod.SetOrigin(assets.gesture_controller).AnimatorControllerParameterRename();
            assets.fx_controller = animMod.SetOrigin(assets.fx_controller).AnimatorControllerParameterRename();
#if VRC_SDK_VRCSDK3
            assets.menu = ExpressionMenuParameterRename(assets.menu);
            assets.parameter = ExpressionParameterRename(assets.parameter);
#endif
            return assets;
        }

        #region ObjectCombinator

        /// <summary>
        /// prefabをボーン下にインスタンスする
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="prefab"></param>
        /// <param name="target"></param>
        void ModifyGameObject(GameObject prefab, out string fromPath, out string toPath,
            HumanBodyBones target = HumanBodyBones.Hips)
        {
            fromPath = "";
            toPath = "";
            // オブジェクトのインスタンシエイト
            var instance = GameObject.Instantiate(prefab, avatar.transform);
            instance.name = prefab.name;

            fromPath = GetRelativePath(instance.transform);

            if (!RenameParameters)
            {
                instance.name = prefab.name;
            }
            else if (prefab.name.StartsWith(prefix))
            {
                instance.name = prefab.name;
            }
            else
            {
                instance.name = prefix + prefab.name;
            }

            toPath = GetRelativePath(instance.transform);

            var humanoid = avatar.GetComponent<Animator>();
            var constraint = instance.GetComponent<ParentConstraint>();
            if (constraint)
            {
                // コンストレイントでの設定
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
            else if (humanoid)
            {
                //ボーン差し替えでの設定
                if (humanoid.isHuman)
                {
                    Transform bone = humanoid.GetBoneTransform(target);
                    if (bone)
                    {
                        instance.transform.SetParent(bone);
                        instance.transform.localPosition = new Vector3(0f, 0f, 0f);
                    }
                }

                toPath = GetRelativePath(instance.transform);
            }

            if (fromPath == toPath)
            {
                fromPath = "";
                toPath = "";
            }
        }

        void RevertGameObject(GameObject prefab, HumanBodyBones target = HumanBodyBones.Hips)
        {
            // オブジェクトのインスタンシエイト
            var humanoid = avatar.GetComponent<Animator>();
            var constraint = prefab.GetComponent<ParentConstraint>();
            if (constraint)
            {
                // コンストレイントでの設定
                foreach (Transform child in avatar.transform)
                {
                    if (child.name == prefab.name) GameObject.DestroyImmediate(child.gameObject);
                }
            }
            else if (humanoid)
            {
                //ボーン差し替えでの設定
                if (humanoid.isHuman)
                {
                    Transform bone = humanoid.GetBoneTransform(target);
                    if (bone)
                    {
                        foreach (Transform child in bone)
                        {
                            if (child.name == prefab.name) GameObject.DestroyImmediate(child.gameObject);
                        }
                    }
                }
            }
        }

        void DeleateInChild(Transform parent, string keyword)
        {
            for (int i = 0; i < parent.childCount;)
            {
                if (parent.GetChild(i).gameObject.name.StartsWith(keyword))
                {
                    GameObject.DestroyImmediate(parent.GetChild(i).gameObject);
                }
                else
                {
                    DeleateInChild(parent.GetChild(i), keyword);
                    i++;
                }
            }
        }

        #endregion

        #region AnimatorCombinator

        AnimatorController GetAvatarAnimatorController(AnimatorLayerType type)
        {
#if VRC_SDK_VRCSDK3
            var index = Array.FindIndex(avatar.baseAnimationLayers,
                l => l.type == type.GetVRChatAnimatorLayerType());
            if (avatar.baseAnimationLayers[index].animatorController)
            {
                return (AnimatorController) avatar.baseAnimationLayers[index].animatorController;
            }
            return null;
#else
            return avatar.runtimeAnimatorController as AnimatorController;
#endif
        }

        void SetAvatarAnimatorController(AnimatorLayerType type, AnimatorController controller)
        {
#if VRC_SDK_VRCSDK3
            avatar.customizeAnimationLayers = true;
            var index = Array.FindIndex(avatar.baseAnimationLayers,
                l => l.type == type.GetVRChatAnimatorLayerType());
            if (index == -1)
            {
                Debug.LogError("Animation Layers" + type.GetVRChatAnimatorLayerType() + " Not Found");
                return;
            }
            avatar.baseAnimationLayers[index].isDefault = false;
            avatar.baseAnimationLayers[index].animatorController = controller;
#else
            avatar.runtimeAnimatorController = controller;
#endif
        }

        bool GetAvatarAnimatorControllerExists(AnimatorLayerType type)
        {
#if VRC_SDK_VRCSDK3
            var index = Array.FindIndex(avatar.baseAnimationLayers,
                l => l.type == type.GetVRChatAnimatorLayerType());
            if (index == -1)
            {
                Debug.LogError("Animation Layers" + type.GetVRChatAnimatorLayerType() + " Not Found");
                return false;
            }
            return avatar.baseAnimationLayers[index].animatorController != null;
#else
            return avatar.runtimeAnimatorController != null;
#endif
        }
        
        void ModifyAvatarAnimatorController(AnimatorLayerType type, AnimatorController controller)
        {
            if (controller == null) return;
            if (!GetAvatarAnimatorControllerExists(type))
            {
                if (type == AnimatorLayerType.Locomotion)
                {
                    SetAvatarAnimatorController(type,
                        MakeCopy<AnimatorController>(
                            AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.baseAnimator)));
                }
                else if (type == AnimatorLayerType.Idle)
                {
                    SetAvatarAnimatorController(type,
                        MakeCopy<AnimatorController>(
                            AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.idleAnimator)));
                }
                else if (type == AnimatorLayerType.Gesture)
                {
                    SetAvatarAnimatorController(type,
                        MakeCopy<AnimatorController>(
                            AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.gestureAnimator)));
                }
                else if (type == AnimatorLayerType.Action)
                {
                    SetAvatarAnimatorController(type,
                        MakeCopy<AnimatorController>(
                            AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.actionAnimator)));
                }
                else if (type == AnimatorLayerType.Fx)
                {
                    SetAvatarAnimatorController(type,
                        MakeCopy<AnimatorController>(
                            AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentVariable.fxAnimator)));
                }
            }

            animMod.SetOrigin(GetAvatarAnimatorController(type)).ModifyAnimatorController(controller);
        }

        void RevertAnimator(AnimatorLayerType type, string keyword)
        {
            if (String.IsNullOrWhiteSpace(keyword)) return;
            if (GetAvatarAnimatorControllerExists(type))
            {
                animMod.SetOrigin(GetAvatarAnimatorController(type)).RevertAnimator(keyword);
            }
        }

        void RevertAnimator(AnimatorLayerType type, AnimatorController controller)
        {
            if (controller == null) return;
            if (GetAvatarAnimatorControllerExists(type))
            {
                animMod.SetOrigin(GetAvatarAnimatorController(type)).RevertAnimator(controller);
            }
        }
        
#if VRC_SDK_VRCSDK3
        
        Dictionary<AnimatorLayerType, int> ComputeLayersOffset(AvatarModifyData assets)
        {
            var layerOffset = new Dictionary<AnimatorLayerType, int>();
            foreach (AnimatorLayerType type in Enum.GetValues(typeof(AnimatorLayerType)))
            {
                layerOffset.Add(type,GetLayerOffset(assets,type));
            }
            return layerOffset;
        }
        
        int GetLayerOffset(AvatarModifyData assets,AnimatorLayerType type)
        {
            var index = Array.FindIndex(avatar.baseAnimationLayers,l => l.type == type.GetVRChatAnimatorLayerType());
            if (index == -1)
            {
                Debug.LogError("Animation Layers" + type.GetVRChatAnimatorLayerType() + " Not Found");
                return 0;
            }
            if (avatar.customizeAnimationLayers == false) return 0;
            if (avatar.baseAnimationLayers[index].isDefault == true) return 0;
            if (!avatar.baseAnimationLayers[index].animatorController) return 0;
            AnimatorController a = (AnimatorController) avatar.baseAnimationLayers[index].animatorController;
            AnimatorController b =
                type == AnimatorLayerType.Idle ? assets.idle_controller :
                type == AnimatorLayerType.Gesture ? assets.gesture_controller :
                type == AnimatorLayerType.Action ? assets.action_controller :
                type == AnimatorLayerType.Fx ? assets.fx_controller :
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

#endif

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
                            mask.SetHumanoidBodyPartActive(p, false);
                        }
                    }

                    var ts = Childs(avatar.transform);
                    mask.transformCount = ts.Count;
                    for (int i = 0; i < ts.Count; i++)
                    {
                        mask.SetTransformPath(i, AssetUtility.GetRelativePath(avatar.transform, ts[i]));
                        mask.SetTransformActive(i, false);
                    }

                    var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(exportDir,
                        avatar.gameObject.name + "_NottingMask.mask"));
                    AssetDatabase.CreateAsset(mask, path);
                    return mask;
                }
            }

            return origin;
        }

        List<Transform> Childs(Transform p, bool root = false)
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

        #endregion

        #region MenuCombinator

#if VRC_SDK_VRCSDK3
        VRCExpressionsMenu GetExpressionMenu(VRCExpressionsMenu defaultMenus = null)
        {
            return avatar.expressionsMenu;
        }

        void SetExpressionMenu(VRCExpressionsMenu menu = null)
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

        /// <summary>
        /// ExpressionsMenuの安全な結合
        /// </summary>
        /// <param name="menus"></param>
        /// <param name="origin"></param>
        void ModifyExpressionMenu(VRCExpressionsMenu menus)
        {
            if (menus == null) return;
            if (GetExpressionMenuExist())
            {
                var parentnmenu = GetExpressionMenu();
                if (menus == parentnmenu) return;
                var current = parentnmenu;
                foreach (var control in menus.controls)
                {
                    int menuMax = 8;
                    while (current.controls.Count >= menuMax && AutoAddNextPage) // 項目が上限に達していたら次ページに飛ぶ
                    {
                        if (current.controls[menuMax - 1].name == "NextPage" &&
                            current.controls[menuMax - 1].type == VRCExpressionsMenu.Control.ControlType.SubMenu &&
                            current.controls[menuMax - 1].subMenu != null)
                        {
                            current = current.controls[menuMax - 1].subMenu;
                        }
                        else
                        {
                            var m = new MenuCreater("NextPage");
                            m.AddControll(current.controls[menuMax - 1]);
                            var submenu = m.CreateAsset(exportDir);
                            current.controls[menuMax - 1] = new VRCExpressionsMenu.Control()
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

                    var newController = new VRCExpressionsMenu.Control()
                    {
                        name = control.name,
                        icon = control.icon,
                        labels = control.labels,
                        parameter = new VRCExpressionsMenu.Control.Parameter()
                        {
                            name = GetSafeParam(control.parameter.name)
                        },
                        style = control.style,
                        subMenu = control.subMenu,
                        type = control.type,
                        value = control.value
                    };

                    if (control.subParameters != null)
                    {
                        newController.subParameters = control.subParameters.Select(cc =>
                        {
                            return new VRCExpressionsMenu.Control.Parameter() {name = GetSafeParam(cc.name)};
                        }).ToArray();
                    }

                    if (control.subMenu != null)
                    {
                        if (ModifyOriginalAsset)
                        {
                            newController.subMenu = control.subMenu;
                        }
                        else
                        {
                            var menu = Object.Instantiate(newController.subMenu);
                            AssetDatabase.CreateAsset(menu,
                                AssetDatabase.GenerateUniqueAssetPath(Path.Combine(exportDir, menu.name + ".asset")));
                            newController.subMenu = ExpressionMenuParameterRename(menu);
                        }
                    }

                    current.controls.Add(newController);
                }

                SetExpressionMenu(parentnmenu);
                EditorUtility.SetDirty(parentnmenu);
            }
            else
            {
                if (DuplicateAssets)
                {
                    var current = MakeCopy<VRCExpressionsMenu>(menus, false);
                    SetExpressionMenu(current);
                    EditorUtility.SetDirty(current);
                }
                else
                {
                    SetExpressionMenu(menus);
                }
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
                if (controll.name == "NextPage" &&
                    controll.icon == AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentVariable.arrowIcon) &&
                    controll.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                    if (controll.subMenu) RevertMenu(menus, controll.subMenu);
                }
                else if (menus.controls.Any(c => (
                    c.name == controll.name &&
                    c.icon == controll.icon &&
                    c.type == controll.type &&
                    GetSafeParam(c.parameter.name) == controll.parameter.name)))
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

        void RevertExpressionMenu(string keyword)
        {
            if (String.IsNullOrWhiteSpace(keyword)) return;
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
                    if (controll.subMenu != null)
                    {
                        RevertMenu(keyword, controll.subMenu);
                        if (controll.subMenu.controls.Count > 0)
                        {
                            newControll.Add(controll);
                        }
                    }
                }
                else if (controll.parameter.name.StartsWith(keyword))
                {
                }
                else if (controll.subParameters.Any(p => p.name.StartsWith(keyword)))
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

                c.parameter = new VRCExpressionsMenu.Control.Parameter() {name = GetSafeParam(c.parameter.name)};
                if (c.subParameters != null)
                {
                    c.subParameters = c.subParameters.Select(cc =>
                    {
                        return new VRCExpressionsMenu.Control.Parameter() {name = GetSafeParam(cc.name)};
                    }).ToArray();
                }

                return c;
            }).ToList();
            EditorUtility.SetDirty(menu);
            return menu;
        }
#endif
        
        #endregion

        #region ParametersCombinater

#if VRC_SDK_VRCSDK3
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

        /// <summary>
        /// ExpressionParametersの安全な結合
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="origin"></param>
        void ModifyExpressionParameter(VRCExpressionParameters parameters)
        {
            if (parameters == null) return;
            if (GetExpressionParameterExist())
            {
                var current = GetExpressionParameter();
                if (parameters == current) return;
                foreach (var parameter in parameters.parameters)
                {
                    AddExpressionParameter(current, parameter.name, parameter.valueType, parameter.saved,
                        parameter.defaultValue);
                }

                EditorUtility.SetDirty(current);
            }
            else
            {
                if (DuplicateAssets)
                {
                    var current = MakeCopy<VRCExpressionParameters>(parameters, false);
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
            var newParm = parameters.parameters.Where(p => p.name != GetSafeParam(name)).ToList();

            // 新規パラメータ追加
            newParm.Add(new VRCExpressionParameters.Parameter()
            {
                name = GetSafeParam(name),
                valueType = type,
                saved = saved,
                defaultValue = value
            });

            parameters.parameters = newParm.ToArray();
            EditorUtility.SetDirty(parameters);
        }

        void RevertExpressionParameter(VRCExpressionParameters parameters)
        {
            if (parameters == null) return;
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
                        GetSafeParam(p.name) == GetSafeParam(parameter.name) &&
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
            if (String.IsNullOrWhiteSpace(keyword)) return;
            if (GetExpressionParameterExist())
            {
                var current = GetExpressionParameter();
                current.parameters = current.parameters.Where(p => !p.name.StartsWith(keyword)).ToArray();
                EditorUtility.SetDirty(current);
            }
        }

        VRCExpressionParameters ExpressionParameterRename(VRCExpressionParameters param)
        {
            if (param == null) return null;
            if (param.parameters == null) return null;
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
#endif
        #endregion

        string GetRelativePath(Transform o)
        {
            return AssetUtility.GetRelativePath(avatar.transform, o);
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

        T MakeCopy<T>(T origin, bool cloneSubAssets = true) where T : Object
        {
            string sufix = origin is AnimatorController ? ".controller" :
                origin is AnimationClip ? ".anim" : ".asset";
            if (origin != null)
            {
                var path = AssetDatabase.GetAssetPath(origin);
                string copyPath =
                    AssetDatabase.GenerateUniqueAssetPath(Path.Combine(exportDir, origin.name + "_copy" + sufix));
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
                        AssetDatabase.CreateAsset(clone, copyPath);
                        Debug.Log("Instance : " + origin.name + " to " + copyPath);
                    }
                }
                else
                {
                    AssetDatabase.CreateAsset(origin, copyPath);
                    Debug.Log("Create : " + origin.name + " to " + copyPath);
                }

                return (T) AssetDatabase.LoadAssetAtPath<T>(copyPath);
            }

            return null;
        }

        // パラメータ文字列から2バイト文字の除去を行う
        public string GetSafeParam(string param)
        {
            return param.GetSafeParam(prefix, RenameParameters);
        }

    }
}
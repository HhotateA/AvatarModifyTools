/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using HhotateA.AvatarModifyTools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
using Random = UnityEngine.Random;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.EmoteMotionKit
{
    public class EmoteMotionKitSetup : WindowBase
    {
        [OnOpenAssetAttribute]
        public static bool OpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(EmoteMotionKitSaveData))
            {
                OpenSavedWindow(EditorUtility.InstanceIDToObject(instanceID) as EmoteMotionKitSaveData);
            }
            return false;
        }
        
        [MenuItem("Window/HhotateA/エモートモーションキット(EmoteMotionKit)",false,108)]
        public static void ShowWindow()
        {
            OpenSavedWindow();
        }
        
        public static void OpenSavedWindow(EmoteMotionKitSaveData saveddata = null)
        {
            var wnd = GetWindow<EmoteMotionKitSetup>();
            wnd.titleContent = new GUIContent("エモートモーションキット(EmoteMotionKit)");
            if (saveddata == null)
            {
                saveddata = CreateInstance<EmoteMotionKitSaveData>();
                saveddata.icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.emotesIcon);
            }
            // wnd.data = Instantiate(saveddata);
            wnd.data = saveddata;
            wnd.LoadReorderableList();
        }

        ReorderableList emoteReorderableList;
        
        private EmoteMotionKitSaveData data;

        private bool useMA = true;
        
        Vector2 scroll = Vector2.zero;

        void LoadReorderableList()
        {
            emoteReorderableList = new ReorderableList(data.emotes,typeof(EmoteElement),true,false,true,true)
            {
                elementHeight = 60,
                drawHeaderCallback = (r) => EditorGUI.LabelField(r,"Emotes","アニメーションを追加してください"),
                drawElementCallback = (r, i, a, f) =>
                {
                    r.height -= 4;
                    r.y += 2;
                    var d = data.emotes[i];
                    var rh = r;
                    rh.width = rh.height;
                    rh.x += 5;
                    d.icon = (Texture2D) EditorGUI.ObjectField(rh,"",d.icon,typeof(Texture2D),true);
                    rh.height /= 3;
                    rh.x += rh.width + 5;
                    rh.width = r.width - rh.width - 10;
                    rh.width = rh.width * 3 / 5;
                    d.name = EditorGUI.TextField(rh,"", d.name);
                    rh.x += rh.width;
                    rh.width = rh.width * 2 / 3;
                    d.animationSettings = (AnimationSettings) EditorGUI.EnumPopup(rh,d.animationSettings);
                    rh.x -= rh.width * 3 / 2;
                    rh.width = rh.width * 5 / 2;
                    
                    // 2列目
                    rh.y += rh.height;
                    rh.width /= 3;
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        d.anim = (AnimationClip) EditorGUI.ObjectField(rh, "", d.anim, typeof(AnimationClip), false);
                        if (check.changed)
                        {
                            SetPreviewAnimation(d.anim);
                        }
                    }
                    //if (d.animationSettings != AnimationSettings.Custom) return;
                    EditorGUI.BeginDisabledGroup(d.animationSettings != AnimationSettings.Custom);
                    rh.x += rh.width;
                    rh.x += rh.width * 1 / 5;
                    rh.width = rh.width * 4 / 5;
                    EditorGUI.LabelField(rh,"Tracking Space");
                    rh.width = rh.width * 5 / 4;
                    rh.x += rh.width * 4 / 5;
                    d.tracking = (TrackingSpace) EditorGUI.EnumPopup(rh,"",d.tracking);
                    rh.x -= rh.width*2;
                    rh.width *= 3;
                    
                    // 3列目
                    rh.y += rh.height;
                    rh.width /= 9;
                    rh.width *= 2;
                    d.isEmote = !EditorGUI.Toggle(rh, !d.isEmote);
                    EditorGUI.LabelField(rh,"     Loop");
                    rh.x += rh.width;
                    rh.width = rh.width * 3 / 2;
                    rh.x += rh.width * 1 / 8;
                    d.locomotionStop = EditorGUI.Toggle(rh, d.locomotionStop);
                    EditorGUI.LabelField(rh,"     Stop Locomotion");
                    rh.x += rh.width;
                    rh.x += rh.width * 1 / 8;
                    d.poseControll = EditorGUI.Toggle(rh, d.poseControll);
                    EditorGUI.LabelField(rh,"     Enter Pose Space");
                    EditorGUI.EndDisabledGroup();
                },
                onAddCallback = l =>
                {
                    var icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.emoteIcons[Random.Range(0, EnvironmentGUIDs.emoteIcons.Length)]);
                    data.emotes.Add(new EmoteElement(icon));
                },
                onSelectCallback = l =>
                {
                    SetPreviewAnimation(data.emotes[l.index].anim);
                }
            };
        }

        private void OnGUI()
        {
            TitleStyle("エモートモーションキット");
            DetailStyle("アイドルアニメーションやエモートを設定するツールです．",EnvironmentGUIDs.readme);
#if VRC_SDK_VRCSDK3

            EditorGUILayout.Space();
            AvatartField("", () =>
            {
                SetPreviewAnimator(avatarAnim);
            });
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                data.icon = (Texture2D) EditorGUILayout.ObjectField(data.icon, typeof(Texture2D), false,
                    GUILayout.Width(60), GUILayout.Height(60));
                using (new EditorGUILayout.VerticalScope())
                {
                    data.saveName = EditorGUILayout.TextField(data.saveName, GUILayout.Height(20));

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Layer Setting", GUILayout.Width(75),GUILayout.Height(20));
                        data.emoteLayer =
                            (EmoteLayer) EditorGUILayout.EnumPopup( data.emoteLayer, GUILayout.Width(100),GUILayout.Height(20));
                        EditorGUILayout.LabelField(" ", GUILayout.Width(20),GUILayout.Height(20));
                        data.copyToFXLayer = EditorGUILayout.Toggle(data.copyToFXLayer,GUILayout.Width(20),GUILayout.Height(20));
                        EditorGUILayout.LabelField("Use FX", GUILayout.Width(50),GUILayout.Height(20));
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        data.useMenuTemplate = EditorGUILayout.Toggle(data.useMenuTemplate,GUILayout.Width(20),GUILayout.Height(20));
                        EditorGUILayout.LabelField("Use Menu Template", GUILayout.Width(150),GUILayout.Height(20));
                        EditorGUILayout.LabelField(" ", GUILayout.Width(25),GUILayout.Height(20));
                        //EditorGUILayout.LabelField(" ", GUILayout.Width(200),GUILayout.Height(20));
                        data.isSaved = EditorGUILayout.Toggle(data.isSaved,GUILayout.Width(20),GUILayout.Height(20));
                        EditorGUILayout.LabelField("Is Saved", GUILayout.Width(50),GUILayout.Height(20));
                    }
                }
            }

            EditorGUILayout.Space();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                scroll = EditorGUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                emoteReorderableList.DoLayoutList();
                EditorGUILayout.EndScrollView();
                if (check.changed)
                {
                    foreach (var emote in data.emotes)
                    {
                        if (emote.animationSettings == AnimationSettings.Emote)
                        {
                            emote.tracking = TrackingSpace.Emote;
                            emote.isEmote = true;
                            emote.locomotionStop = false;
                            emote.poseControll = false;
                        }
                        else
                        if (emote.animationSettings == AnimationSettings.Animation)
                        {
                            emote.tracking = TrackingSpace.Emote;
                            emote.isEmote = false;
                            emote.locomotionStop = false;
                            emote.poseControll = false;
                        }
                        else
                        if (emote.animationSettings == AnimationSettings.Pose)
                        {
                            emote.tracking = TrackingSpace.Emote;
                            emote.isEmote = false;
                            emote.locomotionStop = true;
                            emote.poseControll = false;
                        }
                        else
                        if (emote.animationSettings == AnimationSettings.Locomotion)
                        {
                            emote.tracking = TrackingSpace.FootAnimation;
                            emote.isEmote = false;
                            emote.locomotionStop = true;
                            emote.poseControll = true;
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            if (ShowOptions())
            {
                if (GUILayout.Button("Force Revert"))
                {
                    var am = new AvatarModifyTool(avatar);
                    am.RevertByKeyword(EnvironmentGUIDs.prefix);
                    OnFinishRevert();
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (ModularAvatarUtil.MAEnabled)
            {
                useMA = EditorGUILayout.Toggle("Use Modular Avatar", useMA);
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Setup"))
            {
                var path = EditorUtility.SaveFilePanel("Save", data.GetAssetDir(), data.GetAssetName(), EnvironmentGUIDs.suffix+".asset");
                if (string.IsNullOrEmpty(path))
                {
                    OnCancel();
                    return;
                }
                if (String.IsNullOrWhiteSpace(data.saveName))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    data.saveName = fileName;
                }
                path = FileUtil.GetProjectRelativePath(path);
                try
                {
                    data = ScriptableObject.Instantiate(data);
                    AssetDatabase.CreateAsset(data, path);
                    Setup(path);
                    OnFinishSetup();
                    DetectAnimatorError();
                }
                catch (Exception e)
                {
                    OnError(e);
                    throw;
                }
            }
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Settings"))
                {
                    var path = EditorUtility.SaveFilePanel("Save", data.GetAssetDir(), data.GetAssetName(),EnvironmentGUIDs.suffix+".asset");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    data = Instantiate(data);
                    LoadReorderableList();
                    AssetDatabase.CreateAsset(data, FileUtil.GetProjectRelativePath(path));
                    OnSave();
                }
                if (GUILayout.Button("Load Settings"))
                {
                    var path = EditorUtility.OpenFilePanel("Load", data.GetAssetDir(), EnvironmentGUIDs.suffix+".asset");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    var d = AssetDatabase.LoadAssetAtPath<EmoteMotionKitSaveData>(FileUtil.GetProjectRelativePath(path));
                    if (d == null)
                    {
                        status.Warning("Load Failure");
                        return;
                    }
                    else
                    {
                        data = d;
                        LoadReorderableList();
                    }
                    OnLoad();
                }
            }
            status.Display();
#else
            VRCErrorLabel();
#endif
            Signature();
        }

        void Setup(string path)
        {
            string fileDir = System.IO.Path.GetDirectoryName (path);
#if VRC_SDK_VRCSDK3
            string param = data.saveName;
            var idleAnim = new AnimationClipCreator("Idle").CreateAsset(path,true);
            var p = new ParametersCreater(param);
            p.AddParam(param,0,data.isSaved);
            var ac = new AnimatorControllerCreator(param);
            ac.AddDefaultState("Default",idleAnim);
            for (int i = 0; i < data.emotes.Count; i++)
            {
                var d = data.emotes[i];
                int index = i + 1;
                ac.AddState("Emote" + index + "_" + d.name, d.anim == null ? idleAnim : d.anim);
                ac.AddState("Reset" + index, idleAnim);
                ac.AddTransition("Default","Emote" + index + "_" + d.name,param,(int)index,true,false,0f,0f);
                if (d.isEmote)
                {
                    ac.AddTransition("Emote" + index + "_" + d.name,"Reset" + index,true,1f,0f);
                    ac.ParameterDriver("Reset" + index,param,0);
                }
                else
                {
                    ac.AddTransition("Emote" + index + "_" + d.name,"Reset" + index,param,(int)index,false,false,0f,0f);
                }
                ac.AddTransition("Reset" + index,"Default");

                if (d.locomotionStop)
                {
                    ac.SetLocomotionControll("Emote" + index + "_" + d.name,false);
                    ac.SetLocomotionControll("Reset" + index,true);
                }

                if (d.poseControll)
                {
                    ac.SetEnterPoseControll("Emote" + index + "_" + d.name,true,0f);
                    ac.SetEnterPoseControll("Reset" + index,false,0f);
                }

                if (d.tracking == TrackingSpace.AnimationBase ||
                    d.tracking == TrackingSpace.BodyAnimation ||
                    d.tracking == TrackingSpace.FootAnimation ||
                    d.tracking == TrackingSpace.Emote)
                {
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.LeftFoot,true);
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.RightFoot,true);
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.Hip,true);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.LeftFoot,false);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.RightFoot,false);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.Hip,false);
                }
                if (d.tracking == TrackingSpace.AnimationBase ||
                    d.tracking == TrackingSpace.BodyAnimation ||
                    d.tracking == TrackingSpace.Emote)
                {
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.LeftHand,true);
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.RightHand,true);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.LeftHand,false);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.RightHand,false);
                }
                if (d.tracking == TrackingSpace.AnimationBase ||
                    d.tracking == TrackingSpace.Emote)
                {
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.Haad,true);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.Haad,false);
                }
                if (d.tracking == TrackingSpace.Emote)
                {
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.RightFingers,true);
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.LeftFingers,true);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.RightFingers,false);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.LeftFingers,false);
                }

                if (data.emoteLayer == EmoteLayer.Action)
                {
                    ac.SetLayerControll("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCLayers.Action,1f,0f);
                    ac.SetLayerControll("Reset" + index,AnimatorControllerCreator.VRCLayers.Action,0f,0f);
                }
            }

            var pm = new MenuCreater(param+"parent");
            if (data.useMenuTemplate)
            {
                var m = new MenuCreater(param);
                var ms = Enum.GetValues(typeof(AnimationSettings)).Cast<AnimationSettings>()
                    .ToDictionary(_=>_, _=> new MenuCreater(param));
                for (int i = 0; i < data.emotes.Count; i++)
                {
                    var d = data.emotes[i];
                    int index = i + 1;
                    ms[d.animationSettings].AddToggle(d.name,d.icon,param,index);
                }

                if (data.createResetAnimation)
                {
                    m.AddToggle("Reset",data.icon,param,2^8);
                }

                foreach (AnimationSettings item in Enum.GetValues(typeof(AnimationSettings)))
                {
                    var element = data.emotes.FirstOrDefault(e => e.animationSettings == item);
                    if (element != null)
                    {
                        m.AddSubMenu( ms[item].CreateAsset(path,true), item.ToString(), element.icon);
                    }
                }
                
                pm.AddSubMenu(m.CreateAsset(path,true),data.saveName,data.icon);
            }
            else
            {
                var m = new MenuCreater(param);
                for (int i = 0; i < data.emotes.Count; i++)
                {
                    var d = data.emotes[i];
                    int index = i + 1;
                    m.AddToggle(d.name,d.icon,param,index);
                }
                pm.AddSubMenu(m.CreateAsset(path,true),data.saveName,data.icon);
            }
            var mod = new AvatarModifyTool(avatar,fileDir);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = pm.CreateAsset(path,true);
                if (data.emoteLayer == EmoteLayer.Base)
                {
                    assets.locomotion_controller = ac.CreateAsset(path, true);
                }
                else if (data.emoteLayer == EmoteLayer.Action)
                {
                    assets.action_controller = ac.CreateAsset(path, true);
                }
                else if (data.emoteLayer == EmoteLayer.Additive)
                {
                    assets.idle_controller = ac.CreateAsset(path, true);
                }

                if (data.isSaved)
                {
                    assets.fx_controller = ac.Create();
                }
            }
            AssetDatabase.AddObjectToAsset(assets,path);
            mod = ApplySettings(mod);
            if (useMA && ModularAvatarUtil.MAEnabled)
            {
                mod.ModifyAvatarByMA(assets, EnvironmentGUIDs.prefix);
            }
            else
            {
                mod.ModifyAvatar(assets, EnvironmentGUIDs.prefix);
            }
#endif
        }

        private void OnDestroy()
        {
            SetPreviewAnimator(null);
        }

        private Animator previewAnimator;
        
        public void SetPreviewAnimator(Animator anim)
        {
            if (previewAnimator != null)
            {
                EditorApplication.update -= UpdateAnimation;
                previewAnimator.speed = 1f;
                SetPreviewAnimation(null);
                previewAnimator.Play("Reset", 0, 0f);
                previewAnimator.Update(Time.deltaTime);
                // previewAnimator.runtimeAnimatorController = null;
            }

            previewAnimator = anim;

            if (previewAnimator != null)
            {
                previewAnimator.runtimeAnimatorController = AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentGUIDs.previewController);
                previewAnimator.speed = 1f;
                previewAnimator.Play("Preview", 0, 0f);
                EditorApplication.update += UpdateAnimation;
            }
        }
 
        private void UpdateAnimation()
        {
            if (previewAnimator)
            {
                previewAnimator.Update(Time.deltaTime);
            }
        }

        void SetPreviewAnimation(AnimationClip anim)
        {
            if (anim == null)
            {
                anim = AssetUtility.LoadAssetAtGuid<AnimationClip>(EnvironmentGUIDs.tPoseAnimation);
            }
            var ac = AssetUtility.LoadAssetAtGuid<AnimatorController>(EnvironmentGUIDs.previewController);
            var state = ac.layers[0].stateMachine.states.FirstOrDefault(s => s.state.name == "Preview").state;
            state.motion = anim;
        }
    }
}
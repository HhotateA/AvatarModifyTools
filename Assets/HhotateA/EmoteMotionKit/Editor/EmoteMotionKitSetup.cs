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
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
using Random = UnityEngine.Random;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.EmoteMotionKit
{
    public class EmoteMotionKitSetup : WindowBase
    {
        [MenuItem("Window/HhotateA/エモートモーションキット(EmoteMotionKit)",false,2)]

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
            wnd.data = saveddata;
            wnd.emoteReorderableList = new ReorderableList(saveddata.emotes,typeof(EmoteElement),true,false,true,true)
            {
                elementHeight = 60,
                drawHeaderCallback = (r) => EditorGUI.LabelField(r,"Emotes","アニメーションを追加してください"),
                drawElementCallback = (r, i, a, f) =>
                {
                    r.height -= 4;
                    r.y += 2;
                    var d = saveddata.emotes[i];
                    var rh = r;
                    rh.width = rh.height;
                    rh.x += 5;
                    d.icon = (Texture2D) EditorGUI.ObjectField(rh,"",d.icon,typeof(Texture2D),true);
                    rh.height /= 3;
                    rh.x += rh.width + 5;
                    rh.width = r.width - rh.width - 10;
                    d.name = EditorGUI.TextField(rh,"", d.name);
                    rh.y += rh.height;
                    rh.width /= 3;
                    d.anim = (AnimationClip) EditorGUI.ObjectField(rh,"",d.anim,typeof(AnimationClip));
                    rh.x += rh.width*3/2;
                    d.tracking = (TrackingType) EditorGUI.EnumPopup(rh,"",d.tracking);
                    rh.x -= rh.width*3/2;
                    rh.width *= 3;
                    rh.y += rh.height;
                    rh.width /= 9;
                    rh.width *= 2;
                    d.isEmote = EditorGUI.Toggle(rh, d.isEmote);
                    EditorGUI.LabelField(rh,"     Is Emote");
                    rh.x += rh.width*3/2;
                    d.locomotionStop = EditorGUI.Toggle(rh, d.locomotionStop);
                    EditorGUI.LabelField(rh,"     Locomotion Stop");
                    rh.x += rh.width*3/2;
                    d.poseControll = EditorGUI.Toggle(rh, d.poseControll);
                    EditorGUI.LabelField(rh,"     Pose Controll");
                },
                onAddCallback = l =>
                {
                    var icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.emoteIcons[Random.Range(0, EnvironmentGUIDs.emoteIcons.Length)]);
                    saveddata.emotes.Add(new EmoteElement(icon));
                }
            };
        }

        ReorderableList emoteReorderableList;
        
        private EmoteMotionKitSaveData data;
        
        Vector2 scroll = Vector2.zero;

        private void OnGUI()
        {
            TitleStyle("エモートモーションキットβ");
            DetailStyle("エモートとアイドルアニメーションを設定するツールです．",EnvironmentGUIDs.readme);

            EditorGUILayout.Space();
            
            AvatartField();
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                data.icon = (Texture2D) EditorGUILayout.ObjectField(data.icon, typeof(Texture2D), false,
                    GUILayout.Width(60), GUILayout.Height(60));
                data.saveName = EditorGUILayout.TextField(data.saveName, GUILayout.Height(20));
            }

            EditorGUILayout.Space();
            
            scroll = EditorGUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
            emoteReorderableList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();

            if (GUILayout.Button("Setup"))
            {
                var path = EditorUtility.SaveFilePanel("Save", "Assets",String.IsNullOrWhiteSpace(data.saveName) ? "EmojiSetupData" : data.saveName , "asset");
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
                data = ScriptableObject.Instantiate(data);
                AssetDatabase.CreateAsset(data, path);
                Setup(path);
                OnFinishSetup();
            }
            status.Display();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            Signature();
        }

        void Setup(string path)
        {
            string fileDir = System.IO.Path.GetDirectoryName (path);
            string param = data.saveName;
            var idleAnim = new AnimationClipCreator("Idle").CreateAsset(path,true);
            var p = new ParametersCreater(param);
            p.AddParam(param,0,data.isSaved);
            var m = new MenuCreater(param);
            var ac = new AnimatorControllerCreator(param);
            ac.AddDefaultState("Default",idleAnim);
            for (int i = 0; i < data.emotes.Count; i++)
            {
                var d = data.emotes[i];
                int index = i + 1;
                ac.AddState("Emote" + index + "_" + d.name, d.anim == null ? idleAnim : d.anim);
                ac.AddState("Reset" + index, idleAnim);
                ac.AddTransition("Any","Emote" + index + "_" + d.name,param,(int)index,true,false,0f,0f);
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

                if (d.tracking == TrackingType.Animation)
                {
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.Haad,true);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.Haad,false);
                }
                else
                if(d.tracking == TrackingType.HeadTracking)
                {
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.LeftHand,true);
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.RightHand,true);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.LeftHand,false);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.RightHand,false);
                }
                else
                if(d.tracking == TrackingType.HandTracking)
                {
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.LeftFoot,true);
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.RightFoot,true);
                    ac.SetAnimationTracking("Emote" + index + "_" + d.name,AnimatorControllerCreator.VRCTrackingMask.Hip,true);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.LeftFoot,false);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.RightFoot,false);
                    ac.SetAnimationTracking("Reset" + index,AnimatorControllerCreator.VRCTrackingMask.Hip,false);
                }
                else
                if(d.tracking == TrackingType.FullTracking)
                {
                    
                }
                m.AddToggle(d.name,d.icon,param,index);
            }

            var pm = new MenuCreater(param+"parent");
            pm.AddSubMenu(m.CreateAsset(path,true),data.saveName,data.icon);
            var am = new AvatarModifyTool(avatar,fileDir);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.locomotion_controller = ac.CreateAsset(path, true);
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = pm.CreateAsset(path,true);
            }
            AssetDatabase.AddObjectToAsset(assets,path);
            if (writeDefault)
            {
                am.WriteDefaultOverride = true;
            }
            am.ModifyAvatar(assets,false,keepOldAsset,true,EnvironmentGUIDs.prefix);
        }
        
        [OnOpenAssetAttribute(3)]
        public static bool step3(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(EmoteMotionKitSaveData))
            {
                EmoteMotionKitSetup.OpenSavedWindow(EditorUtility.InstanceIDToObject(instanceID) as EmoteMotionKitSaveData);
            }
            return false;
        }
    }
}
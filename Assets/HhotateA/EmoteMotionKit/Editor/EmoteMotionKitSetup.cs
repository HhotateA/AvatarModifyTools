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
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.EmoteMotionKit
{
    public class EmoteMotionKitSetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/EmoteMotionKit",false,2)]

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
                //onRemoveCallback = l => saveddata.emojis.RemoveAt(l.index),
                //onAddCallback = l => saveddata.emojis.Add(new IconElement("",null))
            };
        }

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#endif
        ReorderableList emoteReorderableList;

        private bool writeDefault = false;
        private bool notRecommended = false;
        private bool keepOldAsset = false;
        
        private EmoteMotionKitSaveData data;

        private void OnGUI()
        {
            AssetUtility.TitleStyle("エモートモーションキットβ");
            AssetUtility.DetailStyle("エモートとアイドルアニメーションを設定するツールです．",EnvironmentGUIDs.readme);

            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                data.icon = (Texture2D) EditorGUILayout.ObjectField(data.icon, typeof(Texture2D), false,
                    GUILayout.Width(60), GUILayout.Height(60));
                data.saveName = EditorGUILayout.TextField(data.saveName, GUILayout.Height(20));
            }

            EditorGUILayout.Space();
            
            emoteReorderableList.DoLayoutList();
            
            EditorGUILayout.Space();
            

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            AssetUtility.Signature();
//#else
            EditorGUILayout.LabelField("Please import VRCSDK3.0 in your project.");
//#endif
        }

        void Setup(string path)
        {
            string fileDir = System.IO.Path.GetDirectoryName (path);

            string param = "HHotateA_EMK_" + data.saveName;
            var idleAnim = new AnimationClipCreator("Idle").CreateAsset(path,true);
            var ac = new AnimatorControllerCreator(param);
            ac.AddDefaultState("Default");
            
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
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
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Task = System.Threading.Tasks.Task;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.MagicalDresserMakeupSystem
{
    public class MagicalDresserSetup : WindowBase
    {
        [MenuItem("Window/HhotateA/マジックドレッサーメイクアップ(MDMakeup)",false,7)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<MagicalDresserSetup>();
            wnd.titleContent = new GUIContent("マジックドレッサーメイクアップ(MDMakeup)");
            wnd.Show();
        }
        private Renderer makeupRenderer;
        private ColorRotateMode matMode = ColorRotateMode.Texture;
        private ShapeRotateMode shapeMode = ShapeRotateMode.None;
        bool[] mats = new bool[0];
        bool[] shapes = new bool[0];
        Vector2 matScroll = Vector2.zero;
        Vector2 shapeScroll = Vector2.zero;
        
        private bool writeDefault = false;
        private bool notRecommended = false;
        private bool keepOldAsser = false;

        int taskDone = 0;
        int taskTodo = 0;

        private string saveName = "MagicDresser";

        enum ColorRotateMode
        {
            None,
            Texture,
            RGB,
            HSV,
        }
        enum ShapeRotateMode
        {
            None,
            Radial,
            Toggle,
        }
        private float threshold = 1f / 60f;
        private void OnGUI()
        {
            TitleStyle("マジックドレッサーメイクアップ(MDMakeup)");
            DetailStyle("VRChat上でのメニューからの色変えや，BlendShapeの切り替えを設定するツールです．",EnvironmentGUIDs.readme);
            EditorGUILayout.Space();
            
            AvatartField();
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            var rend = EditorGUILayout.ObjectField("Renderer", makeupRenderer, typeof(Renderer), true) as Renderer;
            if (makeupRenderer != rend)
            {
                makeupRenderer = rend;
                if (makeupRenderer)
                {
                    mats = Enumerable.Range(0, makeupRenderer.sharedMaterials.Length).Select(_ => true).ToArray();
                    shapes = Enumerable.Range(0, makeupRenderer.GetMesh().blendShapeCount).Select(_ => false).ToArray();
                    saveName = "MagicDresser_" + makeupRenderer.name;
                }
            }
            
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    matMode = (ColorRotateMode) EditorGUILayout.EnumPopup("Color Change", matMode);
                    matScroll = EditorGUILayout.BeginScrollView(matScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                    using (new EditorGUI.DisabledScope(matMode == ColorRotateMode.None))
                    {
                        for (int i = 0; i < mats.Length; i++)
                        {
                            mats[i] = EditorGUILayout.Toggle(makeupRenderer.sharedMaterials[i].name, mats[i]);
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }

                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    if (shapes.Length > 0)
                    {
                        shapeMode = (ShapeRotateMode) EditorGUILayout.EnumPopup("Shape Change", shapeMode);
                        shapeScroll = EditorGUILayout.BeginScrollView(shapeScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                        using (new EditorGUI.DisabledScope(shapeMode == ShapeRotateMode.None))
                        {
                            for (int i = 0; i < shapes.Length; i++)
                            {
                                shapes[i] = EditorGUILayout.Toggle(makeupRenderer.GetMesh().GetBlendShapeName(i),
                                    shapes[i]);
                            }
                        }

                        EditorGUILayout.EndScrollView();
                    }
                }
            }
            
            EditorGUILayout.Space();
            if (ShowNotRecommended())
            {
                if (GUILayout.Button("Force Revert"))
                {
#if VRC_SDK_VRCSDK3
                    var mod = new AvatarModifyTool(avatar);
                    mod.RevertByKeyword(EnvironmentGUIDs.prefix);
                    OnFinishRevert();
#endif
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Setup"))
            {
                taskDone = 0;
                taskTodo = 1;
#if VRC_SDK_VRCSDK3
                var path = EditorUtility.SaveFilePanel("Save", "Assets", "MagicDresser_" + makeupRenderer.name,"controller");
                if (string.IsNullOrWhiteSpace(path)) return;
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                saveName = fileName;
                path = FileUtil.GetProjectRelativePath(path);

                string fileDir = System.IO.Path.GetDirectoryName (path);
                
                AnimatorControllerCreator animAsset = new AnimatorControllerCreator(fileName,false);
                animAsset.CreateAsset(path);
                MenuCreater menuAsset = new MenuCreater(fileName);
                ParametersCreater paramAsset = new ParametersCreater(fileName);
                
                if (matMode == ColorRotateMode.Texture)
                {
                    SetupTexture(path, makeupRenderer,mats,
                        ref animAsset,
                        ref menuAsset,
                        ref paramAsset);
                }
                else
                if(matMode == ColorRotateMode.HSV)
                {
                    SetupHSV(path, makeupRenderer,mats,
                        ref animAsset,
                        ref menuAsset,
                        ref paramAsset);
                }
                else
                if(matMode == ColorRotateMode.RGB)
                {
                    SetupColor(path, makeupRenderer,mats,
                        ref animAsset,
                        ref menuAsset,
                        ref paramAsset);
                }

                if (shapeMode == ShapeRotateMode.Radial)
                {
                    SetupBlendShapeRadial(path, makeupRenderer,shapes,
                        ref animAsset,
                        ref menuAsset,
                        ref paramAsset);
                }
                else
                if(shapeMode == ShapeRotateMode.Toggle)
                {
                    SetupBlendShapeToggle(path, makeupRenderer,shapes,
                        ref animAsset,
                        ref menuAsset,
                        ref paramAsset);
                }
                
                var am = new AvatarModifyTool(avatar,fileDir);
                var assets = ScriptableObject.CreateInstance<AvatarModifyData>();
                {
                    assets.fx_controller = animAsset.Create();
                    assets.menu = menuAsset.CreateAsset(path, true);
                    assets.parameter = paramAsset.CreateAsset(path, true);
                };
                AssetDatabase.AddObjectToAsset(assets,path);
                if (writeDefault)
                {
                    am.WriteDefaultOverride = true;
                }
                am.ModifyAvatar(assets,false,keepOldAsser,true,EnvironmentGUIDs.prefix);
                taskDone += 1;
                OnFinishSetup();
#endif
            }
            status.Display();
            EditorGUILayout.LabelField("Task " + taskDone + " / " + taskTodo);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            Signature();
        }

#if VRC_SDK_VRCSDK3
        void SetupHSV(string path,Renderer rend, bool[] mat,
            ref AnimatorControllerCreator animAsset,
            ref MenuCreater menuAsset,
            ref ParametersCreater paramAsset)
        {
            var paramH = saveName + "_H";
            var paramS = saveName + "_S";
            var paramV = saveName + "_V";
            
            animAsset.AddParameter(paramH,0.5f);
            animAsset.AddParameter(paramS,0.5f);
            animAsset.AddParameter(paramV,0.5f);
            
            paramAsset.AddParam(paramH,0.5f,true);
            paramAsset.AddParam(paramS,0.5f,true);
            paramAsset.AddParam(paramV,0.5f,true);
            
            var idleAnim = new AnimationClipCreator(saveName+"_Idle",avatarAnim.gameObject).CreateAsset(path,true);
            var activateAnim = new AnimationClipCreator(saveName+"_ChangeMaterial",avatarAnim.gameObject);
            var inactivateAnim = new AnimationClipCreator(saveName+"_RevertMaterial",avatarAnim.gameObject);
            var rotateHAnim = new AnimationClipCreator(saveName+"_RotateH",avatarAnim.gameObject);
            var rotateSAnim = new AnimationClipCreator(saveName+"_RotateS",avatarAnim.gameObject);
            var rotateVAnim = new AnimationClipCreator(saveName+"_RotateV",avatarAnim.gameObject);

            var filterMat = new Material(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.filterShader));
            var clippingMat = new Material(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.clippingShader));
            filterMat.name = "Filter";
            clippingMat.name = "Clipping";
            AssetDatabase.AddObjectToAsset(filterMat,path);
            AssetDatabase.AddObjectToAsset(clippingMat,path);
            
            // フィルターオブジェクトの作成
            var clone = rend.transform.FindInChildren(rend.name + "(clone)_" + saveName + "_Filter")?.gameObject;
            if (clone == null)
            {
                clone = Instantiate(rend.gameObject, rend.transform);
                clone.name = rend.name + "(clone)_" + saveName + "_Filter";
                foreach (Transform child in clone.transform)
                {
                    DestroyImmediate(child.gameObject);
                }

                var r = clone.GetComponent<Renderer>();
                var rms = mats.Select(e=>e?filterMat:clippingMat).ToArray();
                r.sharedMaterials = rms;
            }

            var cloneRend = clone.GetComponent<Renderer>();
            activateAnim.AddKeyframe_Gameobject(clone.gameObject,0f,true);
            inactivateAnim.AddKeyframe_Gameobject(clone.gameObject,0f,false);
            activateAnim.AddKeyframe_Gameobject(clone.gameObject,1f/60f,true);
            inactivateAnim.AddKeyframe_Gameobject(clone.gameObject,1f/60f,false);
            rotateHAnim.AddKeyframe_MaterialParam(0f,cloneRend,"_H",0f);
            rotateHAnim.AddKeyframe_MaterialParam(256f/60f,cloneRend,"_H",1f);
            rotateSAnim.AddKeyframe_MaterialParam(0f,cloneRend,"_S",0f);
            rotateSAnim.AddKeyframe_MaterialParam(256f/60f,cloneRend,"_S",1f);
            rotateVAnim.AddKeyframe_MaterialParam(0f,cloneRend,"_V",0f);
            rotateVAnim.AddKeyframe_MaterialParam(256f/60f,cloneRend,"_V",1f);

            animAsset.CreateLayer(saveName);
            animAsset.AddDefaultState("Idle");
            animAsset.AddState("Activate", activateAnim.CreateAsset(path,true));
            animAsset.AddState("Inactive", inactivateAnim.CreateAsset(path,true));
            animAsset.AddState("Controll");
            animAsset.AddTransition("Idle", "Activate", paramH, 0.5f + threshold,true);
            animAsset.AddTransition("Idle", "Activate", paramH, 0.5f - threshold,false);
            animAsset.AddTransition("Idle", "Activate", paramS, 0.5f + threshold,true);
            animAsset.AddTransition("Idle", "Activate", paramS, 0.5f - threshold,false);
            animAsset.AddTransition("Idle", "Activate", paramV, 0.5f + threshold,true);
            animAsset.AddTransition("Idle", "Activate", paramV, 0.5f - threshold,false);
            animAsset.AddTransition("Activate","Controll");
            animAsset.AddTransition("Controll","Inactive",new AnimatorCondition[6]
            {
                new AnimatorCondition() {mode = AnimatorConditionMode.Less,parameter = paramH,threshold = 0.5f+threshold}, 
                new AnimatorCondition() {mode = AnimatorConditionMode.Greater,parameter = paramH,threshold = 0.5f-threshold}, 
                new AnimatorCondition() {mode = AnimatorConditionMode.Less,parameter = paramS,threshold = 0.5f+threshold}, 
                new AnimatorCondition() {mode = AnimatorConditionMode.Greater,parameter = paramS,threshold = 0.5f-threshold},
                new AnimatorCondition() {mode = AnimatorConditionMode.Less,parameter = paramV,threshold = 0.5f+threshold}, 
                new AnimatorCondition() {mode = AnimatorConditionMode.Greater,parameter = paramV,threshold = 0.5f-threshold},
            });
            animAsset.AddTransition("Inactive","Idle");
            
            animAsset.CreateLayer(paramH);
            animAsset.AddState("Idle",idleAnim);
            animAsset.AddState("Controll", rotateHAnim.CreateAsset(path, true));
            animAsset.AddTransition("Idle", "Controll", paramH, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramH, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramS, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramS, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramV, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramV, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Controll","Idle",false);
            animAsset.SetStateTime("Controll",paramH);
            animAsset.SetStateSpeed("Controll",threshold);
            
            animAsset.CreateLayer(paramS);
            animAsset.AddState("Idle",idleAnim);
            animAsset.AddState("Controll", rotateSAnim.CreateAsset(path, true));
            animAsset.AddTransition("Idle", "Controll", paramH, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramH, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramS, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramS, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramV, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramV, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Controll","Idle",false);
            animAsset.SetStateTime("Controll",paramS);
            animAsset.SetStateSpeed("Controll",threshold);
            
            animAsset.CreateLayer(paramV);
            animAsset.AddState("Idle",idleAnim);
            animAsset.AddState("Controll", rotateVAnim.CreateAsset(path, true));
            animAsset.AddTransition("Idle", "Controll", paramH, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramH, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramS, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramS, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramV, 0.5f + threshold,true,true,1f);
            animAsset.AddTransition("Idle", "Controll", paramV, 0.5f - threshold,false,true,1f);
            animAsset.AddTransition("Controll","Idle",false);
            animAsset.SetStateTime("Controll",paramV);
            animAsset.SetStateSpeed("Controll",threshold);
            
            var menu = new MenuCreater(saveName);
            menu.AddRadial("色相",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.rotateHIcon),paramH);
            menu.AddRadial("彩度",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.rotateSIcon),paramS);
            menu.AddRadial("明度",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.rotateVIcon),paramV);
            
            menuAsset.AddSubMenu(menu.CreateAsset(path, true),rend.name + "Color",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.dresserIcon));
        }
        
        void SetupColor(string path,Renderer rend, bool[] mat,
            ref AnimatorControllerCreator animAsset,
            ref MenuCreater menuAsset,
            ref ParametersCreater paramAsset)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileDir = System.IO.Path.GetDirectoryName (path);

            var paramR = fileName + "_R";
            var paramG = fileName + "_G";
            var paramB = fileName + "_B";
            
            animAsset.AddParameter(paramR,0f);
            animAsset.AddParameter(paramG,0f);
            animAsset.AddParameter(paramB,0f);
            
            paramAsset.AddParam(paramR,0f,true);
            paramAsset.AddParam(paramG,0f,true);
            paramAsset.AddParam(paramB,0f,true);
            
            var idleAnim = new AnimationClipCreator(fileName+"_Idle",avatarAnim.gameObject).CreateAsset(path,true);
            var activateAnim = new AnimationClipCreator(fileName+"_ChangeMaterial",avatarAnim.gameObject);
            var inactivateAnim = new AnimationClipCreator(fileName+"_RevertMaterial",avatarAnim.gameObject);
            var rotateRAnim = new AnimationClipCreator(fileName+"_RotateR",avatarAnim.gameObject);
            var rotateGAnim = new AnimationClipCreator(fileName+"_RotateG",avatarAnim.gameObject);
            var rotateBAnim = new AnimationClipCreator(fileName+"_RotateB",avatarAnim.gameObject);

            for (int i = 0; i < rend.sharedMaterials.Length; i++)
            {
                if(mat[i] == false) continue;
                var colorChangeMaterial = rend.sharedMaterials[i];
                if (colorChangeMaterial.mainTexture)
                {
                    var grayMat = new Material(colorChangeMaterial);
                    SyncGrayTexture(colorChangeMaterial.mainTexture,
                        Path.Combine(fileDir, fileName + colorChangeMaterial.mainTexture.name + ".png"),
                        t => grayMat.mainTexture = t);
                    activateAnim.AddKeyframe_Material(rend, grayMat, 0f, i);
                    inactivateAnim.AddKeyframe_Material(rend, colorChangeMaterial, 0f, i);
                    activateAnim.AddKeyframe_Material(rend, grayMat, 1f / 60f, i);
                    inactivateAnim.AddKeyframe_Material(rend, colorChangeMaterial, 1f / 60f, i);
                    AssetDatabase.AddObjectToAsset(grayMat,path);
                }
            }
            
            foreach (var param in Enum.GetValues(typeof(ColorParamsEnum)))
            {
                if (!rend.sharedMaterial.HasProperty(param.ToString())) continue;
                var current = rend.sharedMaterial.GetColor(param.ToString());
                rotateRAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".r", current.r);
                rotateRAnim.AddKeyframe_MaterialParam(1f/60f, rend, param.ToString()+".r", current.r*0.1f);
                rotateRAnim.AddKeyframe_MaterialParam(256f/60f, rend, param.ToString()+".r", current.r*1.1f);
                rotateGAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".g", current.g);
                rotateGAnim.AddKeyframe_MaterialParam(1f/60f, rend, param.ToString()+".g", current.g*0.1f);
                rotateGAnim.AddKeyframe_MaterialParam(256f/60f, rend, param.ToString()+".g", current.g*1.1f);
                rotateBAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".b", current.b);
                rotateBAnim.AddKeyframe_MaterialParam(1f/60f, rend, param.ToString()+".b", current.b*0.1f);
                rotateBAnim.AddKeyframe_MaterialParam(256f/60f, rend, param.ToString()+".b", current.b*1.1f);
                /*resetRAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".r", current.r);
                resetRAnim.AddKeyframe_MaterialParam(1f/60f, rend, param.ToString()+".r", current.r);
                resetGAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".g", current.g);
                resetGAnim.AddKeyframe_MaterialParam(1f/60f, rend, param.ToString()+".g", current.g);
                resetBAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".b", current.b);
                resetBAnim.AddKeyframe_MaterialParam(1f/60f, rend, param.ToString()+".b", current.b);
                inactivateAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".r", current.r);
                inactivateAnim.AddKeyframe_MaterialParam(1f, rend, param.ToString()+".r", current.r);
                inactivateAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".g", current.g);
                inactivateAnim.AddKeyframe_MaterialParam(1f, rend, param.ToString()+".g", current.g);
                inactivateAnim.AddKeyframe_MaterialParam(0f, rend, param.ToString()+".b", current.b);
                inactivateAnim.AddKeyframe_MaterialParam(1f, rend, param.ToString()+".b", current.b);*/
            }
            
            animAsset.CreateLayer(fileName);
            animAsset.AddDefaultState("Idle");
            animAsset.AddState("Activate", activateAnim.CreateAsset(path,true));
            animAsset.AddState("Inactive", inactivateAnim.CreateAsset(path,true));
            animAsset.AddState("Controll");
            animAsset.AddTransition("Idle","Activate",paramR,threshold);
            animAsset.AddTransition("Idle","Activate",paramG,threshold);
            animAsset.AddTransition("Idle","Activate",paramB,threshold);
            animAsset.AddTransition("Activate","Controll");
            animAsset.AddTransition("Controll","Inactive",new AnimatorCondition[3]
            {
                new AnimatorCondition() {mode = AnimatorConditionMode.Less,parameter = paramR,threshold = threshold}, 
                new AnimatorCondition() {mode = AnimatorConditionMode.Less,parameter = paramG,threshold = threshold}, 
                new AnimatorCondition() {mode = AnimatorConditionMode.Less,parameter = paramB,threshold = threshold}
            });
            animAsset.AddTransition("Inactive","Idle");
            
            animAsset.CreateLayer(paramR);
            animAsset.AddState("Idle",idleAnim);
            animAsset.AddState("Controll", rotateRAnim.CreateAsset(path, true));
            animAsset.AddTransition("Idle","Controll",paramR,threshold,true,true,1f);
            animAsset.AddTransition("Idle","Controll",paramG,threshold,true,true,1f);
            animAsset.AddTransition("Idle","Controll",paramB,threshold,true,true,1f);
            animAsset.AddTransition("Controll","Idle",false);
            animAsset.SetStateTime("Controll",paramR);
            animAsset.SetStateSpeed("Controll",0f);
            
            animAsset.CreateLayer(paramG);
            animAsset.AddState("Idle",idleAnim);
            animAsset.AddState("Controll", rotateGAnim.CreateAsset(path, true));
            animAsset.AddTransition("Idle","Controll",paramR,threshold,true,true,1f);
            animAsset.AddTransition("Idle","Controll",paramG,threshold,true,true,1f);
            animAsset.AddTransition("Idle","Controll",paramB,threshold,true,true,1f);
            animAsset.AddTransition("Controll","Idle",false);
            animAsset.SetStateTime("Controll",paramG);
            animAsset.SetStateSpeed("Controll",0f);
            
            animAsset.CreateLayer(paramB);
            animAsset.AddState("Idle",idleAnim);
            animAsset.AddState("Controll", rotateBAnim.CreateAsset(path, true));
            animAsset.AddTransition("Idle","Controll",paramR,threshold,true,true,1f);
            animAsset.AddTransition("Idle","Controll",paramG,threshold,true,true,1f);
            animAsset.AddTransition("Idle","Controll",paramB,threshold,true,true,1f);
            animAsset.AddTransition("Controll","Idle",false);
            animAsset.SetStateTime("Controll",paramB);
            animAsset.SetStateSpeed("Controll",0f);
            
            var m = new MenuCreater("ColorMenu");
            m.AddRadial("赤",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.rotateRIcon),paramR);
            m.AddRadial("緑",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.rotateGIcon),paramG);
            m.AddRadial("青",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.rotateBIcon),paramB);
            m.CreateAsset(path, true);
            menuAsset.AddSubMenu(m.Create(),rend.name + "Color",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.dresserIcon));
        }

        public void SetupTexture(string path,Renderer rend, bool[] mat,
            ref AnimatorControllerCreator animAsset,
            ref MenuCreater menuAsset,
            ref ParametersCreater paramAsset)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileDir = System.IO.Path.GetDirectoryName (path);
            
            var param = fileName + "_RotationColor";
            
            var idleAnim = new AnimationClipCreator(fileName+"_Idle",avatarAnim.gameObject).CreateAsset(path,true);
            var controllAnim = new AnimationClipCreator(fileName+"_Controll",avatarAnim.gameObject);
            var matlist = new Dictionary<Material, Material[]>();
            for (int i = 0; i < rend.sharedMaterials.Length; i++)
            {
                if(mat[i] == false) continue;
                controllAnim.AddKeyframe_Material(rend, rend.sharedMaterials[i], 0f, i);

                if (!matlist.ContainsKey(rend.sharedMaterials[i]))
                {
                    taskTodo += EnvironmentGUIDs.rotateHSV.Length;
                    matlist.Add(
                        rend.sharedMaterials[i],
                        Enumerable.Range(0,EnvironmentGUIDs.rotateHSV.Length).Select(j =>
                        {
                            var rotateMat = new Material(rend.sharedMaterials[i]);
                            SyncHSVTexture(
                                rend.sharedMaterials[i].mainTexture,
                                Path.Combine(fileDir,
                                    fileName + rend.sharedMaterials[i].mainTexture.name + "_" + i + "_" + j + ".png"),
                                EnvironmentGUIDs.rotateHSV[j],
                                t =>
                                {
                                    rotateMat.mainTexture = t;
                                    taskTodo += 1;
                                    Repaint();
                                });
                            AssetDatabase.AddObjectToAsset(rotateMat,path);
                            return rotateMat;
                        }).ToArray());
                }
                for (int j = 0; j < EnvironmentGUIDs.rotateHSV.Length; j++)
                {
                    controllAnim.AddKeyframe_Material(rend,matlist[rend.sharedMaterials[i]][j],(float)j+1f,i); 
                }
            }
            
            animAsset.CreateLayer(fileName);
            animAsset.AddDefaultState("Idle",idleAnim);
            animAsset.AddState("Controll", controllAnim.CreateAsset(path, true));
            animAsset.AddTransition("Idle","Controll",param,threshold,true);
            animAsset.AddTransition("Controll","Idle",false);
            animAsset.SetStateTime("Controll",param);
            animAsset.SetStateSpeed("Controll",0f);
            animAsset.Create();
            
            paramAsset.AddParam(param,0f,true);
            menuAsset.AddRadial(rend.name + "Color",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.dresserIcon),param);
        }

        void SetupBlendShapeRadial(string path, Renderer rend, bool[] shape,
            ref AnimatorControllerCreator animAsset,
            ref MenuCreater menuAsset,
            ref ParametersCreater paramAsset)
        {
            if (!shape.Any(e=>e==true)) return;
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileDir = System.IO.Path.GetDirectoryName (path);
            
            var param = fileName + "_BlendShape";
            var idleAnim = new AnimationClipCreator(fileName+"_Idle",avatarAnim.gameObject).CreateAsset(path,true);
            var controllAnim = new AnimationClipCreator(fileName+"_Controll",avatarAnim.gameObject);
            for (int i = 0; i < shape.Length; i++)
            {
                if(shape[i] == false) continue;
                controllAnim.AddKeyframe(0f,rend, "blendShape."+rend.GetMesh().GetBlendShapeName(i) , 0f);
                controllAnim.AddKeyframe(256f/60f,rend, "blendShape."+rend.GetMesh().GetBlendShapeName(i) , 100f);
            }
            
            animAsset.CreateLayer(fileName);
            animAsset.AddDefaultState("Idle",idleAnim);
            animAsset.AddState("Controll", controllAnim.CreateAsset(path, true));
            animAsset.AddTransition("Idle","Controll",param,threshold,true);
            animAsset.AddTransition("Controll","Idle",false);
            animAsset.SetStateTime("Controll",param);
            animAsset.SetStateSpeed("Controll",0f);
            animAsset.Create();
            
            paramAsset.AddParam(param,0f,true);
            menuAsset.AddRadial(rend.name + "_Shape",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.blendShapeIcon),param);
        }
        
        void SetupBlendShapeToggle(string path, Renderer rend, bool[] shape,
            ref AnimatorControllerCreator animAsset,
            ref MenuCreater menuAsset,
            ref ParametersCreater paramAsset)
        {
            if (!shape.Any(e=>e==true)) return;
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileDir = System.IO.Path.GetDirectoryName (path);
            
            for (int i = 0; i < shape.Length; i++)
            {
                if(shape[i] == false) continue;
                var param = fileName + "_BlendShapeToggle_" + rend.GetMesh().GetBlendShapeName(i);
                
                var idleAnim = new AnimationClipCreator(fileName+"_Idle",avatarAnim.gameObject).CreateAsset(path,true);
                var activateAnim = new AnimationClipCreator(fileName+"_Activate_"+rend.GetMesh().GetBlendShapeName(i),avatarAnim.gameObject);
                var inactivateAnim = new AnimationClipCreator(fileName+"_Inactivate_"+rend.GetMesh().GetBlendShapeName(i),avatarAnim.gameObject);
                
                activateAnim.AddKeyframe(0f,rend, "blendShape."+rend.GetMesh().GetBlendShapeName(i) , 100f);
                activateAnim.AddKeyframe(1f/60f,rend, "blendShape."+rend.GetMesh().GetBlendShapeName(i) , 100f);
                inactivateAnim.AddKeyframe(0f,rend, "blendShape."+rend.GetMesh().GetBlendShapeName(i) , 0f);
                inactivateAnim.AddKeyframe(1f/60f,rend, "blendShape."+rend.GetMesh().GetBlendShapeName(i) , 0f);
            
                animAsset.CreateLayer( fileName + "_" + i.ToString() + rend.GetMesh().GetBlendShapeName(i));
                animAsset.AddDefaultState("Idle",idleAnim);
                animAsset.AddState("Active",idleAnim);
                animAsset.AddState("Inactive",idleAnim);
                animAsset.AddState("Activate", activateAnim.CreateAsset(path, true));
                animAsset.AddState("Inactivate", inactivateAnim.CreateAsset(path, true));
                
                animAsset.AddTransition("Idle","Activate",param,true);
                animAsset.AddTransition("Idle","Inactivate",param,false);
                animAsset.AddTransition("Activate","Active");
                animAsset.AddTransition("Inactivate","Inactive");
                animAsset.AddTransition("Inactive","Activate",param,true);
                animAsset.AddTransition("Active","Inactivate",param,false);
                animAsset.Create();
            
                paramAsset.AddParam(param,false,true);
                menuAsset.AddToggle(rend.GetMesh().GetBlendShapeName(i),AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.blendShapeIcon),param);
            }
        }
#endif
        
        
        async Task SyncHSVTexture(Texture tex,string path,Vector3 hsv,Action<Texture> onSave)
        {
            var texcreater = new TextureCreator(tex);
            await Task.Delay(100);
            texcreater.AddLayer(Color.white);
            {
                var layer = texcreater.GetLayerData(0);
                layer.layerMode = BlendMode.HSV;
                layer.settings = new Vector4(hsv.x,hsv.y,hsv.z,0f);
                texcreater.LayersUpdate();
            }
            await Task.Delay(100);
            var t = texcreater.SaveTexture(path);
            onSave?.Invoke(t);
        }
        
        async Task SyncGrayTexture(Texture tex,string path,Action<Texture> onSave)
        {
            var texcreater = new TextureCreator(tex);
            await Task.Delay(100);
            texcreater.AddLayer(Color.white);
            var layer = texcreater.GetLayerData(0);
            layer.layerMode = BlendMode.HSV;
            layer.settings = new Vector4(0f,-1f,0f,0f);
            texcreater.LayersUpdate();
            await Task.Delay(100);
            var t = texcreater.SaveTexture(path);
            onSave?.Invoke(t);
        }

        enum ColorParamsEnum
        {
            _Color,
            //UTS
            _BaseColor,
            _1st_ShadeColor,
            _2nd_ShadeColor,
        }
        
        
    }
}
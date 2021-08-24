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

namespace HhotateA.AvatarModifyTools.EmojiParticle
{
    public class EmojiParticleSetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/絵文字パーティクルセットアップ(EmojiParticleSetup)",false,2)]

        public static void ShowWindow()
        {
            OpenSavedWindow();
        }
        
        public static void OpenSavedWindow(EmojiSaveData saveddata = null)
        {
            var wnd = GetWindow<EmojiParticleSetup>();
            wnd.titleContent = new GUIContent("EmojiParticleSetup");
            if (saveddata == null)
            {
                saveddata = CreateInstance<EmojiSaveData>();
            }
            wnd.data = saveddata;
            wnd.emojiReorderableList = new ReorderableList(saveddata.emojis,typeof(IconElement),true,false,true,true)
            {
                elementHeight = 60,
                drawHeaderCallback = (r) => EditorGUI.LabelField(r,"Emojis","絵文字を追加してください"),
                drawElementCallback = (r, i, a, f) =>
                {
                    r.height -= 4;
                    r.y += 2;
                    var d = saveddata.emojis[i];
                    var nameRect = r;
                    var emojiRect = r;
                    emojiRect.width = emojiRect.height;
                    emojiRect.x = r.width - emojiRect.width;
                    nameRect.height /= 3;
                    nameRect.y += nameRect.height;
                    nameRect.width = r.width - emojiRect.width - 30;
                    nameRect.x += 0;
                    d.name = EditorGUI.TextField(nameRect,"", d.name);
                    d.emoji = (Texture2D) EditorGUI.ObjectField(emojiRect,"",d.emoji,typeof(Texture2D),true);
                },
                onRemoveCallback = l => saveddata.emojis.RemoveAt(l.index),
                onAddCallback = l => saveddata.emojis.Add(new IconElement("",null))
            };
        }

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#endif
        private Target target;
        ReorderableList emojiReorderableList;

        private bool writeDefault = false;
        private bool notRecommended = false;
        private bool keepOldAsset = false;

        enum Target
        {
            Hip,
            Head,
            RightHand,
            LeftHand
        }
        
        private EmojiSaveData data;

        private void OnGUI()
        {
            AssetUtility.TitleStyle("絵文字パーティクルセットアップ");
            AssetUtility.DetailStyle("アバターに好きな画像の絵文字を実装する，簡単なセットアップツールです．",EnvironmentGUIDs.readme);
#if VRC_SDK_VRCSDK3
            avatar = (VRCAvatarDescriptor) EditorGUILayout.ObjectField("Avatar", avatar,
                typeof(VRCAvatarDescriptor), true);
            
            EditorGUILayout.Space();
            
            data.saveName = EditorGUILayout.TextField("Save Name",data.saveName);
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            target = (Target) EditorGUILayout.EnumPopup("Target", target);
            
            EditorGUILayout.Space();
            
            emojiReorderableList.DoLayoutList();
            
            EditorGUILayout.Space();
            notRecommended = EditorGUILayout.Foldout(notRecommended,"VRChat Not Recommended");
            if (notRecommended)
            {
                writeDefault = EditorGUILayout.Toggle("Write Default", writeDefault); 
                keepOldAsset = EditorGUILayout.Toggle("Keep Old Asset", keepOldAsset); 
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(avatar==null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Setup"))
                    {
                        var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(EnvironmentGUIDs.emojiModifyData);
                        asset = Instantiate(asset);
                        
                        var path = EditorUtility.SaveFilePanel("Save", "Assets","EmojiSetupData" , "asset");
                        if (string.IsNullOrEmpty(path)) return;
                        if (String.IsNullOrWhiteSpace(data.saveName))
                        {
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                            data.saveName = fileName;
                        }
                        path = FileUtil.GetProjectRelativePath(path);

                        data = Instantiate(data);
                        AssetDatabase.CreateAsset(data, path);

                        var modifyAsset = Setup(asset);
                        data.assets = modifyAsset;
                        AssetDatabase.AddObjectToAsset(modifyAsset,path);

                        var mod = new AvatarModifyTool(avatar, AssetDatabase.GetAssetPath(data));
                        if (writeDefault)
                        {
                            mod.WriteDefaultOverride = true;
                        }

                        mod.ModifyAvatar(modifyAsset,true,keepOldAsset);

                        // ゴミ処理忘れずに
                        DestroyImmediate(modifyAsset.items[0].prefab);
                        AssetDatabase.SaveAssets();
                    }

                    if (keepOldAsset && data.assets)
                    {
                        if (GUILayout.Button("Revert"))
                        {
                            var mod = new AvatarModifyTool(avatar);
                            mod.RevertAvatar(data.assets);
                        }
                    }
                }
            }
            
            AssetUtility.Signature();
#else
            EditorGUILayout.LabelField("Please import VRCSDK3.0 in your project.");
#endif
        }

#if VRC_SDK_VRCSDK3
        AvatarModifyData Setup(AvatarModifyData assets)
        {
            var settingsPath = AssetDatabase.GetAssetPath(data);
            string fileDir = System.IO.Path.GetDirectoryName (settingsPath);

            string param = "EmojiParticle_" + data.saveName;
            
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(settingsPath)) {
                if (AssetDatabase.IsSubAsset(asset)) {
                    DestroyImmediate(asset, true);
                }
            }
            AssetDatabase.Refresh();

            int tilling = 1;
            while (data.emojis.Count > tilling * tilling) tilling++;
    
            // 結合テクスチャの作成
            var textures = data.emojis.Select(icon=>icon.ToTexture2D()).ToArray();
            var combinatedTexture = TextureCombinater.CombinateSaveTexture(textures,Path.Combine(fileDir,data.saveName+"_tex"+".png"),tilling);
            combinatedTexture.name = data.saveName+"_tex";
            // マテリアルの作成
            var combinatedMaterial = SaveParticleMaterial(combinatedTexture);
            combinatedMaterial.name = data.saveName+"_mat";
            AssetDatabase.AddObjectToAsset(combinatedMaterial,settingsPath);
            
            // param
            var pc = new ParametersCreater("EmojiParticleParam");
            pc.AddParam(param,0);
            
            // メニューの作成
            var iconMenus = new MenuCreater(data.saveName+"_icons",true);
            for (int i = 0; i < data.emojis.Count; i++)
            {
                // 0はデフォルトなので+1
                iconMenus.AddButton(data.emojis[i].name,data.emojis[i].ToTexture2D(),param,i+1);
            }
            // Modify用メニュー作成
            var menu = new MenuCreater("mainMenu");
            menu.AddSubMenu(iconMenus.CreateAsset(settingsPath,true),"EmojiParticles",TextureCombinater.ResizeSaveTexture(
                Path.Combine(fileDir,data.saveName+"_icon"+".png"),
                combinatedTexture,
                256,256));
            
            // オリジナルアセットのパーティクルコンポーネント差し替え
            var prefab = Instantiate(assets.items[0].prefab);
            prefab.name = assets.items[0].prefab.name + "_" + data.saveName;
            var ps = prefab.GetComponentsInChildren<ParticleSystem>()[0];
            ps.GetComponent<ParticleSystemRenderer>().material = combinatedMaterial;
            var ts = ps.textureSheetAnimation;
            ts.enabled = true;
            ts.numTilesX = tilling;
            ts.numTilesY = tilling;
            ps.gameObject.SetActive(false);
            
            // 追従先差し替え
            switch (target)
            {
                case Target.Hip: 
                    assets.items[0].target = HumanBodyBones.Hips;
                    ps.transform.localPosition = new Vector3(0.0f,0.25f,0.25f);
                    break;
                case Target.Head: 
                    assets.items[0].target = HumanBodyBones.Head;
                    ps.transform.localPosition = new Vector3(0.0f,0.25f,0.25f);
                    break;
                case Target.RightHand: 
                    assets.items[0].target = HumanBodyBones.RightHand;
                    ps.transform.localPosition = Vector3.zero;
                    break;
                case Target.LeftHand: 
                    assets.items[0].target = HumanBodyBones.LeftHand;
                    ps.transform.localPosition = Vector3.zero;
                    break;
            }
            
            // AnimationClipの作成
            var controller = new AnimatorControllerCreator("Emoji_Controller","EmojiParticle"+data.saveName);
            controller.AddParameter(param,AnimatorControllerParameterType.Int);
            var reset = new AnimationClipCreator("Emoji_Anim_Reset",avatar.gameObject);
            reset.AddKeyframe_Gameobject(ps.gameObject, 0f, false);
            reset.AddKeyframe_Gameobject(ps.gameObject, 1f/60f, false);
            reset.AddKeyframe(0f, ps,"UVModule.startFrame.scalar",0f);
            reset.AddKeyframe(1f/60f, ps,"UVModule.startFrame.scalar",0f);
            //reset.CreateAnimation(ps.gameObject,"m_IsActive",0f,0f,1f);
            //reset.CreateAnimation(ps,"UVModule.startFrame.scalar",0f,0f,0f);
            var r = reset.CreateAsset(settingsPath, true);
            controller.AddState("Idle",r);
            controller.AddState("Reset",r);
            controller.SetDefaultState("Idle");
            controller.AddTransition("Reset","Idle",true);
            for (int i = 0; i < data.emojis.Count; i++)
            {
                var anim = new AnimationClipCreator("Emoji_Anim"+i,avatar.gameObject);
                var v = (float) i / (float) (tilling * tilling);
                /*
                anim.CreateAnimation(ps,"UVModule.startFrame.scalar",0f,v,0f);
                var k = new Dictionary<float, float>();
                k.Add(0f,0f);
                k.Add(1f/60f,1f);
                k.Add(2.1f,1f);
                anim.CreateAnimation(ps.gameObject, "m_IsActive", 0f, 0f, 0f, k);
                */
                anim.AddKeyframe(0f, ps,"UVModule.startFrame.scalar",v,0f);
                anim.AddKeyframe_Gameobject(ps.gameObject, 0f, false);
                anim.AddKeyframe_Gameobject(ps.gameObject, 1f/60f, true);
                anim.AddKeyframe_Gameobject(ps.gameObject, 2f, true);
                var a = anim.CreateAsset(settingsPath, true);
                controller.AddState("Emoji_"+i,a);
                // 0はデフォルトなので+1
                controller.AddTransition("Any", "Emoji_" + i, param, i + 1, true,false);
                controller.AddTransition("Emoji_" + i,"Reset",true );
            }
            controller.LayerMask(AvatarMaskBodyPart.Body,false,false);
            controller.LayerTransformMask(avatar.gameObject,false);

            AvatarModifyData newAssets = CreateInstance<AvatarModifyData>();
            {
                newAssets.fx_controller = controller.CreateAsset(settingsPath, true);
                newAssets.parameter = pc.CreateAsset(settingsPath,true);
                newAssets.menu = menu.CreateAsset(settingsPath,true);
                newAssets.items = new Item[1]{new Item()
                {
                    target = assets.items[0].target,
                    prefab = prefab,
                }};
            }

            return newAssets;
        }
#endif

        Material SaveParticleMaterial(Texture combinatedTextures,string path = null)
        {
            var material = new Material(Shader.Find("Particles/Standard Unlit"));
            material.SetTexture("_MainTex",combinatedTextures);
            material.SetFloat("_Mode",2);
            {
                // from https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/6a63f93bc1f20ce6cd47f981c7494e8328915621/Editor/StandardParticlesShaderGUI.cs#L579
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_BlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.DisableKeyword("_ALPHAMODULATE_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            if (!String.IsNullOrWhiteSpace(path))
            {
                AssetDatabase.CreateAsset(material,path);
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            return material;
        }
        
        [OnOpenAssetAttribute(1)]
        public static bool step1(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(EmojiSaveData))
            {
                EmojiParticleSetup.OpenSavedWindow(EditorUtility.InstanceIDToObject(instanceID) as EmojiSaveData);
            }
            return false;
        }
    }
}
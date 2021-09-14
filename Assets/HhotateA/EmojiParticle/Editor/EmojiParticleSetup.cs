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
    public class EmojiParticleSetup : WindowBase
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
            // wnd.data = Instantiate(saveddata);
            wnd.data = saveddata;
            wnd.LoadReorderableList();
        }
        private Target target;
        ReorderableList emojiReorderableList;
        private EmojiSaveData data;
        Vector2 scroll = Vector2.zero;

        enum Target
        {
            Hip,
            Head,
            RightHand,
            LeftHand
        }
        

        void LoadReorderableList()
        {
            emojiReorderableList = new ReorderableList(data.emojis,typeof(IconElement),true,false,true,true)
            {
                elementHeight = 60,
                drawHeaderCallback = (r) => EditorGUI.LabelField(r,"Emojis","絵文字を追加してください"),
                drawElementCallback = (r, i, a, f) =>
                {
                    r.height -= 4;
                    r.y += 2;
                    var d = data.emojis[i];
                    var recth = r;
                    var emojiRect = r;
                    emojiRect.width = emojiRect.height + 25;
                    emojiRect.x = r.width - emojiRect.width;
                    recth.height /= 3;
                    recth.width = r.width - emojiRect.width;
                    
                    var rectw = recth;
                    rectw.width -= 50;
                    d.name = EditorGUI.TextField(rectw,"", d.name);
                    rectw.x += rectw.width;
                    rectw.width /= 2;

                    rectw = recth;
                    rectw.y += rectw.height;
                    rectw.width = recth.width / 2;
                    rectw.width /= 6;
                    rectw.x += rectw.width;
                    rectw.width *= 2;
                    EditorGUI.LabelField(rectw,"Count");
                    rectw.x += rectw.width;
                    rectw.width =  rectw.width * 2 / 3;
                    d.count = EditorGUI.IntField(rectw, d.count);
                    rectw.x += rectw.width;
                    
                    rectw.width = recth.width / 2;
                    rectw.width /= 6;
                    rectw.x += rectw.width;
                    rectw.width *= 2;
                    EditorGUI.LabelField(rectw,"Scale");
                    rectw.x += rectw.width;
                    rectw.width =  rectw.width * 2 / 3;
                    d.scale = EditorGUI.FloatField(rectw, d.scale);
                    
                    rectw = recth;
                    rectw.y += 2 * rectw.height;
                    rectw.width = recth.width / 2;
                    rectw.width /= 6;
                    rectw.x += rectw.width;
                    rectw.width *= 2;
                    EditorGUI.LabelField(rectw,"LifeTime");
                    rectw.x += rectw.width;
                    rectw.width =  rectw.width * 2 / 3;
                    d.lifetime = EditorGUI.FloatField(rectw, d.lifetime);
                    rectw.x += rectw.width;
                    
                    rectw.width = recth.width / 2;
                    rectw.width /= 6;
                    rectw.x += rectw.width;
                    rectw.width *= 2;
                    EditorGUI.LabelField(rectw,"Speed");
                    rectw.x += rectw.width;
                    rectw.width =  rectw.width * 2 / 3;
                    d.speed = EditorGUI.FloatField(rectw, d.speed);

                    emojiRect.width = emojiRect.height;
                    d.emoji = (Texture2D) EditorGUI.ObjectField(emojiRect,"",d.emoji,typeof(Texture2D),true);
                    emojiRect.x += emojiRect.width;
                    emojiRect.height /= 3;
                    EditorGUI.LabelField(emojiRect,"Options");
                    emojiRect.y += emojiRect.height;
                    d.prefab = (GameObject) EditorGUI.ObjectField(emojiRect, d.prefab,typeof(GameObject), false);
                    emojiRect.y += emojiRect.height;
                    d.audio = (AudioClip) EditorGUI.ObjectField(emojiRect, d.audio,typeof(AudioClip), false);

                    d.count = Mathf.Clamp(d.count, 1, 50);
                    if (d.scale <= 0f) d.scale = 0.4f;
                    if (d.lifetime < 1f / 60f) d.lifetime = 2f;
                },
                onRemoveCallback = l => data.emojis.RemoveAt(l.index),
                onAddCallback = l => data.emojis.Add(new IconElement("",null))
            };
        }


        private void OnGUI()
        {
            TitleStyle("絵文字パーティクルセットアップ");
            DetailStyle("アバターに好きな画像の絵文字を実装する，簡単なセットアップツールです．",EnvironmentGUIDs.readme);

#if VRC_SDK_VRCSDK3
            EditorGUILayout.Space();
            AvatartField();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            data.saveName = EditorGUILayout.TextField("Save Name",data.saveName);
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            target = (Target) EditorGUILayout.EnumPopup("Target", target);
            
            EditorGUILayout.Space();
            
            scroll = EditorGUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
            emojiReorderableList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            
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

            using (new EditorGUI.DisabledScope(avatar==null))
            {
                if (GUILayout.Button("Setup"))
                {
                    var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(EnvironmentGUIDs.emojiModifyData);
                    asset = Instantiate(asset);
                    
                    var path = EditorUtility.SaveFilePanel("Save", data.GetAssetDir(),String.IsNullOrWhiteSpace(data.saveName) ? "EmojiSetupData" : data.saveName , "emojiparticle.asset");
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
                        data = Instantiate(data);
                        AssetDatabase.CreateAsset(data, path);
                        var modifyAsset = Setup(asset);
                        data.assets = modifyAsset;
                        AssetDatabase.AddObjectToAsset(modifyAsset, path);

                        var mod = new AvatarModifyTool(avatar, AssetDatabase.GetAssetPath(data));
                        ApplySettings(mod).ModifyAvatar(modifyAsset,EnvironmentGUIDs.prefix);

                        AssetDatabase.SaveAssets();
                        OnFinishSetup();
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                }
            }
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Settings"))
                {
                    var path = EditorUtility.SaveFilePanel("Save", data.GetAssetDir(), data.saveName,"emojiparticle.asset");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    data = Instantiate(data);
                    LoadReorderableList();
                    AssetDatabase.CreateAsset(data, FileUtil.GetProjectRelativePath(path));
                    status.Success("Saved");
                }
                if (GUILayout.Button("Load Settings"))
                {
                    var path = EditorUtility.OpenFilePanel("Load", data.GetAssetDir(), "emojiparticle.asset");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    var d = AssetDatabase.LoadAssetAtPath<EmojiSaveData>(FileUtil.GetProjectRelativePath(path));
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
                    status.Success("Loaded");
                }
            }
            status.Display();
#else
            VRCErrorLabel();
#endif
            Signature();
        }

#if VRC_SDK_VRCSDK3
        AvatarModifyData Setup(AvatarModifyData assets)
        {
            var settingsPath = AssetDatabase.GetAssetPath(data);
            string fileDir = Path.GetDirectoryName (settingsPath);

            string param = data.saveName;
            
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
            var combinatedTexture = TextureCombinater.CombinateSaveTexture(textures,Path.Combine(fileDir,data.saveName+"_tex"+".png"),tilling,1);
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

            if (overrideSettings)
            {
                var oldSettings = avatar.transform.FindInChildren(EnvironmentGUIDs.prefix + data.saveName);
                if (oldSettings)
                {
                    DestroyImmediate(oldSettings);
                }
            }
            // オリジナルアセットのパーティクルコンポーネント差し替え
            var prefab = Instantiate(AssetUtility.LoadAssetAtGuid<GameObject>(EnvironmentGUIDs.particlePrefab));
            prefab.name = EnvironmentGUIDs.prefix + data.saveName;
            var ps = prefab.GetComponentsInChildren<ParticleSystem>()[0];
            ps.GetComponent<ParticleSystemRenderer>().material = combinatedMaterial;
            var ts = ps.textureSheetAnimation;
            ts.enabled = true;
            ts.numTilesX = tilling;
            ts.numTilesY = tilling;
            ps.gameObject.SetActive(false);

            var human = avatar.GetComponent<Animator>();
            if (human != null)
            {
                // 追従先差し替え
                switch (target)
                {
                    case Target.Hip: 
                        //assets.items[0].target = HumanBodyBones.Hips;
                        prefab.transform.SetParent(human.GetBoneTransform(HumanBodyBones.Hips));
                        ps.transform.localPosition = new Vector3(0.0f,0.25f,0.25f);
                        break;
                    case Target.Head: 
                        //assets.items[0].target = HumanBodyBones.Head;
                        prefab.transform.SetParent(human.GetBoneTransform(HumanBodyBones.Head));
                        ps.transform.localPosition = new Vector3(0.0f,0.25f,0.25f);
                        break;
                    case Target.RightHand: 
                        //assets.items[0].target = HumanBodyBones.RightHand;
                        prefab.transform.SetParent(human.GetBoneTransform(HumanBodyBones.RightHand));
                        ps.transform.localPosition = Vector3.zero;
                        break;
                    case Target.LeftHand: 
                        //assets.items[0].target = HumanBodyBones.LeftHand;
                        prefab.transform.SetParent(human.GetBoneTransform(HumanBodyBones.LeftHand));
                        ps.transform.localPosition = Vector3.zero;
                        break;
                }
                prefab.transform.localPosition = Vector3.zero;
                prefab.transform.localRotation = Quaternion.identity;
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
            controller.AddState("Idle",null);
            controller.AddState("Reset",null);
            controller.SetDefaultState("Idle");
            controller.AddTransition("Reset","Idle",true);
            for (int i = 0; i < data.emojis.Count; i++)
            {
                var anim = new AnimationClipCreator("Emoji_Anim"+i,avatar.gameObject);
                var v = (float) i / (float) (tilling * tilling);
                anim.AddKeyframe_Gameobject(ps.gameObject, 0f, false);
                anim.AddKeyframe(0f, ps,"UVModule.startFrame.scalar",v);
                anim.AddKeyframe(0f, ps,"InitialModule.startLifetime.scalar",data.emojis[i].lifetime);
                anim.AddKeyframe(0f, ps,"EmissionModule.m_Bursts.Array.data[0].countCurve.scalar",data.emojis[i].count);
                anim.AddKeyframe(0f, ps,"InitialModule.startSize.scalar",data.emojis[i].scale);
                anim.AddKeyframe(0f, ps,"InitialModule.startSpeed.scalar",data.emojis[i].speed);
                anim.AddKeyframe_Gameobject(ps.gameObject, 1f/60f, true);
                anim.AddKeyframe_Gameobject(ps.gameObject, data.emojis[i].lifetime+1f/60f, true);
                if (data.emojis[i].prefab != null || data.emojis[i].audio != null)
                {
                    var pre = new GameObject("Obj"+i);
                    pre.transform.SetParent(prefab.transform);
                    pre.transform.localPosition = Vector3.zero;
                    pre.transform.localRotation = Quaternion.identity;
                    pre.SetActive(false);
                    reset.AddKeyframe_Gameobject(pre,0f,false);
                    reset.AddKeyframe_Gameobject(pre,1f/60f,false);
                    anim.AddKeyframe_Gameobject(pre,0f,true);
                    anim.AddKeyframe_Gameobject(pre,data.emojis[i].lifetime,true);
                    if (data.emojis[i].prefab != null)
                    {
                        var o = Instantiate(data.emojis[i].prefab ,pre.transform);
                        o.transform.localPosition = Vector3.zero;
                        o.transform.localRotation = Quaternion.identity;
                    }
                    if(data.emojis[i].audio != null)
                    {
                        var audio = pre.AddComponent<AudioSource>();
                        audio.clip = data.emojis[i].audio;
                        audio.loop = false;
                        audio.playOnAwake = true;
                    }
                }
                var a = anim.CreateAsset(settingsPath, true);
                controller.AddState("Emoji_"+i,a);
                // 0はデフォルトなので+1
                controller.AddTransition("Any", "Emoji_" + i, param, i + 1, true,false);
                controller.AddTransition("Emoji_" + i,"Reset",true );
            }
            var r = reset.CreateAsset(settingsPath, true);
            controller.SetMotion("Idle",r);
            controller.SetMotion("Reset",r);
            
            controller.LayerMask(AvatarMaskBodyPart.Body,false,false);
            controller.LayerTransformMask(avatar.gameObject,false);

            AvatarModifyData newAssets = CreateInstance<AvatarModifyData>();
            {
                newAssets.fx_controller = controller.CreateAsset(settingsPath, true);
                newAssets.parameter = pc.CreateAsset(settingsPath,true);
                newAssets.menu = menu.CreateAsset(settingsPath,true);
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
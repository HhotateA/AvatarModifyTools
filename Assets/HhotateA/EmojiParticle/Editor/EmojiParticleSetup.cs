using HhotateA.AvatarModifyTools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor.Callbacks;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.EmojiParticle
{
    public class EmojiParticleSetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/EmojiParticleSetup")]

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
            else
            {
                saveddata = AssetDatabase.LoadAssetAtPath<EmojiSaveData>(AssetDatabase.GetAssetPath(saveddata));
            }
            wnd.data = saveddata;
        }

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#endif
        private string msg = "OK";
        GUIStyle msgStyle = new GUIStyle(GUIStyle.none);
        private Vector2 _scrollPosition = Vector2.zero;
        private Target target;

        enum Target
        {
            Hip,
            Head,
            RightHand,
            LeftHand
        }
        
        private EmojiSaveData data;
        private List<IconElement> emojis
        {
            get { return data.Emojis; }
            set { data.Emojis = value; }
        }

        private void OnGUI()
        {
#if VRC_SDK_VRCSDK3
            GUIStyle titleStyle = new GUIStyle(GUIStyle.none);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 15;
            titleStyle.fontStyle = FontStyle.Bold;
            
            GUIStyle instructions = new GUIStyle(GUI.skin.label);
            instructions.fontSize = 10;
            instructions.wordWrap = true;
            
            GUIStyle signature = new GUIStyle(GUI.skin.label);
            signature.alignment = TextAnchor.LowerRight;
            signature.fontSize = 10;
            
            GUIStyle status = new GUIStyle(GUIStyle.none);
            status.alignment = TextAnchor.MiddleRight;
            status.fontSize = 9;
            msgStyle.alignment = TextAnchor.MiddleRight;
            msgStyle.fontSize = 9;

            if (data == null)
            {
                GUILayout.Label("EmojiParticleSetupTool",titleStyle);
            }
            else
            {
                GUILayout.Label(data.name,titleStyle);
            }

            
            GUILayout.Label("シーン上のアバターをドラッグ＆ドロップ");

            EditorGUILayout.BeginHorizontal();
            {
                avatar = (VRCAvatarDescriptor) EditorGUILayout.ObjectField("Avatar", avatar,
                    typeof(VRCAvatarDescriptor), true);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            {
                target = (Target) EditorGUILayout.EnumPopup("Target", target);
            }
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < emojis.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                emojis[i].Name = EditorGUILayout.TextField(emojis[i].Name);
                
                if (GUILayout.Button("↑"))
                {
                    if (i - 1 >= 0)
                    {
                        var currentIcon = emojis[i];
                        emojis[i] = emojis[i - 1];
                        emojis[i - 1] = currentIcon;
                    }
                    EditorUtility.SetDirty(data);
                }
                
                if (GUILayout.Button("↓"))
                {
                    if (i + 1 < emojis.Count)
                    {
                        var currentIcon = emojis[i];
                        emojis[i] = emojis[i + 1];
                        emojis[i + 1] = currentIcon;
                    }
                    EditorUtility.SetDirty(data);
                }
                
                if (GUILayout.Button("×"))
                {
                    emojis.RemoveAt(i);
                    EditorUtility.SetDirty(data);
                }
                
                var emoji = EditorGUILayout.ObjectField("", emojis[i].Emoji,typeof(Texture2D),true);
                if (emoji != emojis[i].Emoji)
                {
                    emojis[i].Emoji = emoji as Texture2D;
                    EditorUtility.SetDirty(data);
                }
                EditorGUILayout.EndHorizontal();
            }
            var newicon = (Texture) EditorGUILayout.ObjectField("", null,typeof(Texture),true);
            if (newicon != null)
            {
                emojis.Add(new IconElement("",newicon));
                EditorUtility.SetDirty(data);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(avatar==null))
            {
                if (GUILayout.Button("Setup"))
                {
                    try
                    {
                        var asset = AssetUtility.LoadAssetAtGuid<AvatarModifyData>(EnvironmentGUIDs.emojiModifyData);
                        
                        if (String.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(data)))
                        {
                            // 未保存なら保存先を指定させる．
                            var path = EditorUtility.SaveFilePanel("Save", "Assets", "IconSetupData", "asset");
                            path = FileUtil.GetProjectRelativePath(path);
        
                            if (!string.IsNullOrEmpty(path)) {
                                // Assetが消されていた場合対策に新たなInstanceから保存する．
                                var d = ScriptableObject.CreateInstance<EmojiSaveData>();
                                {
                                    d.Emojis = data.Emojis;
                                }
                                data = d;
                                AssetDatabase.CreateAsset(data,path);
                                AssetDatabase.SaveAssets();
                            }
                            else
                            {
                                // 保存先を指定しないなら処理中断
                                throw new System.Exception("EmojiParticleSetup : Please save setupdata");
                            }
                        }

                        var newAssets = Setup(asset);

                        var mod = new AvatarModifyTool(avatar);
                        mod.ModifyAvatar(newAssets);
                        
                        // ゴミ処理忘れずに
                        DestroyImmediate(newAssets.items[0].prefab);
                        
                        msgStyle.normal = new GUIStyleState()
                        {
                            textColor = Color.green
                        };
                        msg = "Success!";
                    }
                    catch (Exception e)
                    {
                        msgStyle.normal = new GUIStyleState()
                        {
                            textColor = Color.red
                        };
                        msg = e.Message;
                        Debug.LogError(e);
                    }
                    AssetDatabase.SaveAssets();
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Status: ",status);
                GUILayout.Label(msg,msgStyle);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            
            GUILayout.Label("上ボタンを押すと，アバターのFX用AnimatorController，ExpressionParamrter，ExpressionMenuに改変を加えます．",instructions);
            GUILayout.Space(10);
            
            GUILayout.Label("操作は元に戻せないので，必ずバックアップをとっていることを確認してください．",instructions);
            GUILayout.Space(20);
            
            GUILayout.Label("powered by @HhotateA_xR",signature);
#else
            EditorGUILayout.LabelField("Please import VRCSDK3.0 in your project.");
#endif
        }

#if VRC_SDK_VRCSDK3
        AvatarModifyData Setup(AvatarModifyData assets)
        {
            var settingsPath = AssetDatabase.GetAssetPath(data);
            string fileName = System.IO.Path.GetFileNameWithoutExtension (settingsPath);
            string fileDir = System.IO.Path.GetDirectoryName (settingsPath);
            
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(settingsPath)) {
                if (AssetDatabase.IsSubAsset(asset)) {
                    DestroyImmediate(asset, true);
                }
            }
            AssetDatabase.Refresh();

            int tilling = 1;
            while (emojis.Count > tilling * tilling) tilling++;
    
            // 結合テクスチャの作成
            var textures = emojis.Select(icon=>icon.ToTexture2D()).ToArray();
            var combinatedTexture = TextureCombinater.CombinateSaveTexture(textures,Path.Combine(fileDir,fileName+"_tex"+".png"),tilling);
            combinatedTexture.alphaIsTransparency = true;
            combinatedTexture.name = fileName+"_tex";
            // マテリアルの作成
            var combinatedMaterial = SaveParticleMaterial(combinatedTexture);
            combinatedMaterial.name = fileName+"_mat";
            AssetDatabase.AddObjectToAsset(combinatedMaterial,settingsPath);
            
            // メニューの作成
            var iconMenus = new MenuCreater(fileName+"_icons",true);
            for (int i = 0; i < emojis.Count; i++)
            {
                // 0はデフォルトなので+1
                iconMenus.AddButton(emojis[i].Name,emojis[i].ToTexture2D(),assets.parameter.parameters[0].name,i+1);
            }
            var iconMenu = iconMenus.CreateAsset(settingsPath,true);
            // Modify用メニュー作成
            var menu = Instantiate(assets.menu);
            //menu.controls[0].icon = emojis[0].Emoji as Texture2D;
            menu.controls[0].icon = TextureCombinater.ResizeSaveTexture(
                Path.Combine(fileDir,fileName+"_icon"+".png"),
                combinatedTexture,
                256,256);
            menu.controls[0].subMenu = iconMenu;
            
            // オリジナルアセットのパーティクルコンポーネント差し替え
            var prefab = Instantiate(assets.items[0].prefab);
            prefab.name = assets.items[0].prefab.name;
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
            var controller = new AnimatorControllerCreator("Emoji_Controller","EmojiParticle");
            controller.AddParameter(assets.parameter.parameters[0].name,AnimatorControllerParameterType.Int);
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
            controller.AddTransition("Reset","Idle",new AnimatorCondition[]{},true,1f,0f );
            var anims = new List<AnimationClipCreator>();
            for (int i = 0; i < emojis.Count; i++)
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
                controller.AddTransition("Any","Emoji_"+i,
                    new AnimatorCondition[]
                            {
                                new AnimatorCondition()
                                {
                                    mode = AnimatorConditionMode.Equals,
                                    parameter = assets.parameter.parameters[0].name,
                                    threshold = i+1
                                }, 
                            },false,0.01f,0f );
                controller.AddTransition("Emoji_"+i,"Reset",new AnimatorCondition[]{},true,1f,0f );
            }
            var animator = controller.CreateAsset(settingsPath, true);

            AvatarModifyData newAssets = CreateInstance<AvatarModifyData>();
            {
                newAssets.locomotion_controller = assets.locomotion_controller;
                newAssets.idle_controller = assets.idle_controller;
                newAssets.gesture_controller = assets.gesture_controller;
                newAssets.action_controller = assets.action_controller;
                newAssets.fx_controller = animator;
                newAssets.parameter = assets.parameter;
                newAssets.menu = menu;
                newAssets.items = new Item[1];
                newAssets.items[0].target = assets.items[0].target;
                newAssets.items[0].prefab = prefab;
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
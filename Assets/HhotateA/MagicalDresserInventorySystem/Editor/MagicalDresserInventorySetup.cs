/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Callbacks;
using Object = System.Object;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.MagicalDresserInventorySystem
{
    public class MagicalDresserInventorySetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/マジックドレッサーインベントリ(MDInventorySystem)",false,6)]
        public static void ShowWindow()
        {
            OpenSavedWindow();
        }

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#else
        private Animator avatar;
#endif
        private MagicalDresserInventorySaveData data;
        private List<MenuElement> menuElements => data.menuElements;
        private SerializedProperty prop;
        ReorderableList menuReorderableList;
        private bool syncInactiveItems = true;

        private bool writeDefault = false;
        private bool notRecommended = false;
        private bool keepOldAsset = false;
        private bool displayItemMode = true;
        
        Dictionary<Shader,Dictionary<Material, Material>> matlist = new Dictionary<Shader,Dictionary<Material, Material>>();
        Dictionary<GameObject,bool> defaultActive = new Dictionary<GameObject, bool>();
        
        Vector2 scrollLeft = Vector2.zero;
        Vector2 scrollRight = Vector2.zero;
        
        Material GetAnimationMaterial(Material origin,Shader shader)
        {
            if (!matlist.ContainsKey(shader))
            {
                matlist.Add(shader,new Dictionary<Material,Material>());
            }
            if (!matlist[shader].ContainsKey(origin))
            {
                if (shader.GetTypeByShader() != FeedType.None)
                {
                    var mat = shader.GetTypeByShader().GetMaterialByType();
                    mat = Instantiate(mat);
                    if(origin.mainTexture) mat.mainTexture = origin.mainTexture;
                    matlist[shader].Add(origin,mat);
                }
                else
                {
                    var mat = new Material(origin);
                    mat.name = origin.name + "_" + shader.name;
                    mat.shader = shader;
                    matlist[shader].Add(origin,mat);
                }
            }

            return matlist[shader][origin];
        }

        public static void OpenSavedWindow(MagicalDresserInventorySaveData saveddata = null)
        {
            var wnd = GetWindow<MagicalDresserInventorySetup>();
            wnd.titleContent = new GUIContent("マジックドレッサーインベントリ(MDInventorySystem)");
            wnd.minSize = new Vector2(825, 500);
            wnd.maxSize = new Vector2(825,2000);

            if (saveddata)
            {
                wnd.data = saveddata;
            }
        }

        private void OnEnable()
        {
            LoadReorderableList();
        }

        void LoadReorderableList()
        {
            if (!data)
            {
                data = CreateInstance<MagicalDresserInventorySaveData>();
                data.icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.itemboxIcon);
            }
            menuReorderableList = new ReorderableList(menuElements, typeof(MenuElement))
            {
                drawHeaderCallback = r =>
                {
                    EditorGUI.LabelField(r,"Menu Elements");
                },
                elementHeight = 60+6,
                drawElementCallback = (r, i, a, f) =>
                {
                    var d = menuElements[i];
                    {
                        var rh = r;
                        rh.width = rh.height;
                        d.icon = (Texture2D) EditorGUI.ObjectField (rh,d.icon, typeof(Texture2D), false);
                        r.width -= rh.width;
                        r.x += rh.width+10;
                    }
                    r.height -= 6;
                    r.height /= 3;
                    r.width *= 0.95f;
                    
                    d.name = EditorGUI.TextField(r,"", d.name);

                    r.y += 2;
                    r.y += r.height;
                    
                    if (d.isToggle)
                    {
                        var rh = r;
                        rh.width = 20;
                        d.SetLayer( EditorGUI.Toggle(rh, "", d.isToggle));
                        rh.x += rh.width;
                        rh.width = 150;
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUI.EnumPopup(rh, (ToggleGroup)0);
                        }
                    }
                    else
                    {
                        var rh = r;
                        rh.width = 20;
                        d.SetLayer( EditorGUI.Toggle(rh, "", d.isToggle));
                        rh.x += rh.width;
                        rh.width = 150;
                        d.SetLayer( (LayerGroup) EditorGUI.EnumPopup(rh, d.layer));
                    }

                    r.y += 2;
                    r.y += r.height;
                    
                    if (d.isToggle)
                    {
                        var rh = r;
                        rh.width = 20;
                        d.isSaved = EditorGUI.Toggle(rh, "", d.isSaved);
                        rh.x += rh.width;
                        rh.width = 100;
                        EditorGUI.LabelField(rh, "Is Saved");
                        rh.x += rh.width + 50;
                        rh.width = 20;
                        d.isDefault = EditorGUI.Toggle(rh, "", d.isDefault);
                        rh.x += rh.width;
                        rh.width = 100;
                        EditorGUI.LabelField(rh, "Is Default");
                    }
                    else
                    {
                        var rh = r;
                        rh.width = 20;
                        data.layerSettingses[(int)d.layer].isSaved = 
                            EditorGUI.Toggle(rh, "", data.layerSettingses[(int)d.layer].isSaved);
                        rh.x += rh.width;
                        rh.width = 100;
                        EditorGUI.LabelField(rh, "Is Saved");
                        rh.x += rh.width + 50;
                        rh.width = 20;
                        if (data.layerSettingses[(int) d.layer].GetDefaultElement(menuElements) == d)
                        {
                            rh.width = 20;
                            if (!EditorGUI.Toggle(rh, "",true))
                            {
                                data.layerSettingses[(int) d.layer].SetDefaultElement(null);
                            }
                            rh.x += rh.width;
                            rh.width = 100;
                            EditorGUI.LabelField(rh, "Is Default");
                        }
                        else
                        {
                            rh.width = 20;
                            if (EditorGUI.Toggle(rh, "",false))
                            {
                                data.layerSettingses[(int) d.layer].SetDefaultElement(d);
                            }
                            rh.x += rh.width;
                            rh.width = 100;
                            EditorGUI.LabelField(rh, "Is Default");
                        }
                    }
                },
                onAddCallback = l => AddMenu(),
                onSelectCallback = l =>
                {
                    SetObjectActiveForScene(menuElements[l.index]);
                    scrollRight = Vector2.zero;
                }
            };
        }

        private void OnGUI()
        {
            AssetUtility.TitleStyle("マジックドレッサーインベントリ");
            AssetUtility.DetailStyle("アバターのメニューから，服やアイテムの入れ替えを行える設定ツールです．",EnvironmentGUIDs.readme);
#if VRC_SDK_VRCSDK3
            var a = EditorGUILayout.ObjectField("", avatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            if (a != avatar)
            {
                avatar = a;
                if (avatar)
                {
                    data.ApplyRoot(avatar.gameObject);
                    LoadReorderableList();
                }
            }
#else
            avatar = EditorGUILayout.ObjectField("", avatar, typeof(Animator), true) as Animator;
#endif
            if (!avatar)
            {
                GUILayout.Label("シーン上のアバターをドラッグ＆ドロップ");
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    data.icon = (Texture2D) EditorGUILayout.ObjectField(data.icon, typeof(Texture2D), false,
                        GUILayout.Width(60), GUILayout.Height(60));
                    data.name = EditorGUILayout.TextField(data.name, GUILayout.Height(20));
                }

                return;
            }

            EditorGUILayout.Space();
            var index = menuReorderableList.index;
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    data.icon = (Texture2D) EditorGUILayout.ObjectField(data.icon, typeof(Texture2D), false,
                        GUILayout.Width(60), GUILayout.Height(60));
                    data.name = EditorGUILayout.TextField(data.name, GUILayout.Height(20));
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space(10);
                    
                    scrollLeft = EditorGUILayout.BeginScrollView(scrollLeft, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(350)))
                    {
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            menuReorderableList.DoLayoutList();
                            if (check.changed)
                            {
                                if (0 <= index && index < menuElements.Count)
                                {
                                    SetObjectActiveForScene(menuElements[menuReorderableList.index]);
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                    
                    EditorGUILayout.Space(10);

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(400)))
                    {
                        var itemListStyle = new GUIStyle();
                        var itemListStyleNormal = itemListStyle.normal;
                        itemListStyleNormal.background = Texture2D.grayTexture;
                        itemListStyle.normal = itemListStyleNormal;
                        using (new EditorGUILayout.VerticalScope(itemListStyle))
                        {
                            if (0 <= index && index < menuElements.Count)
                            {
                                EditorGUILayout.LabelField(menuElements[index].name);
                                using (var r = new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField("");
                                    var rect = r.rect;
                                    rect.height += 5;
                                    var tabstyleActive = new GUIStyle(GUI.skin.button);
                                    tabstyleActive.stretchWidth = true;
                                    var ns = tabstyleActive.normal;
                                    ns.background = Texture2D.blackTexture;
                                    ns.textColor = Color.gray;
                                    tabstyleActive.normal = ns;
                                    var tabstyleDisable = new GUIStyle(GUI.skin.box);
                                    tabstyleDisable.stretchWidth = true;
                                    rect.width /= 2;
                                    if (GUI.Button(rect, "ON",
                                        displayItemMode ? tabstyleDisable : tabstyleActive))
                                    {
                                        displayItemMode = true;
                                    }

                                    rect.x += rect.width;
                                    if (GUI.Button(rect, "OFF",
                                        displayItemMode ? tabstyleActive : tabstyleDisable))
                                    {
                                        displayItemMode = false;
                                    }
                                }

                                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                {
                                    ItemLabelDisplay();
                                    scrollRight = EditorGUILayout.BeginScrollView(scrollRight, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                                    if (displayItemMode)
                                    {
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            foreach (var item in menuElements[index].activeItems)
                                            {
                                                ItemElementDisplay(item, true, true, true, true);

                                                if (!item.obj)
                                                {
                                                    menuElements[index].activeItems.Remove(item);
                                                    return;
                                                }
                                            }

                                            if (!menuElements[index].isToggle)
                                            {
                                                foreach (var item in ComputeLayerAnotherItems(menuElements[index]))
                                                {
                                                    using (var add = new EditorGUI.ChangeCheckScope())
                                                    {
                                                        ItemElementDisplay(item, true, false, true, true);

                                                        if (add.changed)
                                                        {
                                                            menuElements[index].activeItems.Add(item);
                                                            SetObjectActiveForScene(menuElements[index]);
                                                            return;
                                                        }
                                                    }
                                                }
                                            }

                                            if (check.changed)
                                            {
                                                SetObjectActiveForScene(menuElements[index]);
                                                SyncItemActive(menuElements[index].activeItems,
                                                    menuElements[index].inactiveItems, true);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var item in menuElements[index].inactiveItems)
                                        {
                                            ItemElementDisplay(item, false, false, true, true);

                                            if (!item.obj)
                                            {
                                                menuElements[index].inactiveItems.Remove(item);
                                                return;
                                            }
                                        }

                                        foreach (var item in ComputeLayerInactiveItems(menuElements[index]))
                                        {
                                            using (var add = new EditorGUI.ChangeCheckScope())
                                            {
                                                ItemElementDisplay(item, false, false, true, true);

                                                if (add.changed)
                                                {
                                                    menuElements[index].inactiveItems.Add(item);
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndScrollView();

                                    var newItem = (GameObject) EditorGUILayout.ObjectField("", null,
                                        typeof(GameObject), true, GUILayout.Width(370));
                                    if (newItem)
                                    {
                                        if (menuElements[index].activeItems.All(e => e.obj != newItem))
                                        {
                                            menuElements[index].activeItems.Add(new ItemElement(newItem,
                                                IsMenuElementDefault(menuElements[index])
                                                    ? newItem.gameObject.activeSelf
                                                    : !newItem.gameObject.activeSelf));
                                        }

                                        SetObjectActiveForScene(menuElements[index]);
                                    }
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("null");
                            }
                        }

                        EditorGUILayout.Space();
                        notRecommended = EditorGUILayout.Foldout(notRecommended, "VRChat Not Recommended");
                        if (notRecommended)
                        {
                            writeDefault = EditorGUILayout.Toggle("Write Default", writeDefault);
                            keepOldAsset = EditorGUILayout.Toggle("Keep Old Asset", keepOldAsset);
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.Space();

                        using (new EditorGUI.DisabledScope(avatar == null))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Setup"))
                                {
                                    RevertObjectActiveForScene();
                                    var path = EditorUtility.SaveFilePanel("Save", "Assets",
                                        data.name,
                                        "asset");
                                    if (string.IsNullOrWhiteSpace(path)) return;
                                    data.name = System.IO.Path.GetFileNameWithoutExtension(path);
                                    data = ScriptableObject.Instantiate(data);
                                    data.ApplyPath(avatar.gameObject);
                                    AssetDatabase.CreateAsset(data, FileUtil.GetProjectRelativePath(path));
                                    Setup(path);
                                }

                                if (keepOldAsset && data.assets)
                                {
                                    if (GUILayout.Button("Revert"))
                                    {
                                        RevertObjectActiveForScene();
    #if VRC_SDK_VRCSDK3
                                        var mod = new AvatarModifyTool(avatar);
                                        mod.RevertAnimator(VRCAvatarDescriptor.AnimLayerType.FX,
                                            "MDInventory" + data.name + "_");
                                        mod.RevertAvatar(data.assets);
    #endif
                                    }
                                }
                            }
                        }

                        EditorGUILayout.Space();
                        if (GUILayout.Button("Export Animation"))
                        {
                            RevertObjectActiveForScene();
                            var path = EditorUtility.SaveFilePanel("Save", "Assets", data.name,
                                "anim");
                            if (string.IsNullOrWhiteSpace(path)) return;
                            data = Instantiate(data);
                            data.ApplyPath(avatar.gameObject);
                            AssetDatabase.CreateAsset(data, FileUtil.GetProjectRelativePath(path));
                            SaveAnim(path);
                        }

                        EditorGUILayout.Space();

                        AssetUtility.Signature();
                    }
                    
                    EditorGUILayout.Space(10);
                }
            }
        }

        bool GetLayerDefaultActive(LayerGroup layer,GameObject obj)
        {
            var item = data.layerSettingses[(int) layer].GetDefaultElement(menuElements).activeItems
                .FirstOrDefault(e => e.obj == obj);
            if (item != null)
            {
                return item.active;
            }
            return true;
        }

        void ItemLabelDisplay()
        {
            GUIStyle label = new GUIStyle(GUI.skin.label);
            label.fontSize = 10;
            label.wordWrap = true;
            label.alignment = TextAnchor.LowerLeft;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("IsActive", label,GUILayout.Width(55));
                EditorGUILayout.LabelField("Object",  label,GUILayout.Width(150));
                EditorGUILayout.LabelField("Delay",  label,GUILayout.Width(50));
                EditorGUILayout.LabelField("Duration",  label,GUILayout.Width(50));
                EditorGUILayout.LabelField("Type",  label,GUILayout.Width(100));
            }
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }
        
        void ItemElementDisplay(ItemElement item,bool activeEdit = true,bool objEdit = true,bool timeEdit = true,bool typeEdit = true)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                //EditorGUILayout.LabelField("",GUILayout.Width(5));
                var r = GUILayoutUtility.GetRect(new GUIContent(""),GUIStyle.none,GUILayout.Width(25));
                item.extendOption = EditorGUI.Foldout(r,item.extendOption,"");
                using (new EditorGUI.DisabledScope(!activeEdit))
                {
                    item.active = EditorGUILayout.Toggle("", item.active, GUILayout.Width(30));
                }

                using (new EditorGUI.DisabledScope(!objEdit))
                {
                    var o = (GameObject) EditorGUILayout.ObjectField("", item.obj,
                        typeof(GameObject), true, GUILayout.Width(150));
                    if (o == null)
                    {
                        // 消されたときのみ上書き
                        item.obj = null;
                    }
                }

                using (new EditorGUI.DisabledScope(!timeEdit))
                {
                    item.delay = EditorGUILayout.FloatField("", item.delay, GUILayout.Width(50));
                    item.duration =
                        EditorGUILayout.FloatField("", item.duration, GUILayout.Width(50));
                }

                using (new EditorGUI.DisabledScope(!typeEdit))
                {
                    if (item.type == FeedType.Shader)
                    {
                        item.type = (FeedType) EditorGUILayout.EnumPopup("", item.type,
                            GUILayout.Width(50));
                        item.animationShader = (Shader) EditorGUILayout.ObjectField("", item.animationShader,typeof(Shader),true, GUILayout.Width(50));
                    }
                    else
                    {
                        item.type = (FeedType) EditorGUILayout.EnumPopup("", item.type,
                            GUILayout.Width(100));
                    }
                }
            }

            if (item.extendOption)
            {
                foreach (var rendOption in item.rendOptions)
                {
                    RendererOptionDisplay(item, rendOption);
                }
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            }
        }

        void RendererOptionDisplay(ItemElement item, RendererOption rendOption)
        {
            rendOption.extendMaterialOption = EditorGUILayout.Foldout(rendOption.extendMaterialOption,"Change Material : " + rendOption.rend.name);
            if (rendOption.extendMaterialOption)
            {
                for (int i = 0; i < rendOption.changeMaterialsOption.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("",GUILayout.Width(30));
                        var toggle = EditorGUILayout.Toggle("", rendOption.changeMaterialsOption[i] != null, GUILayout.Width(25));
                        EditorGUILayout.LabelField(rendOption.rend.sharedMaterials[i].name,  GUILayout.Width(150));
                        if (toggle)
                        {
                            if (rendOption.changeMaterialsOption[i] == null)
                            {
                                rendOption.changeMaterialsOption[i] = rendOption.rend.sharedMaterials[i];
                            }

                            rendOption.changeMaterialsOption[i] = (Material) EditorGUILayout.ObjectField("",
                                rendOption.changeMaterialsOption[i], typeof(Material), false,GUILayout.Width(200));
                        }
                        else
                        {
                            rendOption.changeMaterialsOption[i] = null;
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.ObjectField("",
                                    new Material(Shader.Find("Unlit/Color")){name = "No Change"}, typeof(Object), false,GUILayout.Width(200));
                            }
                        }
                    }
                }
            }

            rendOption.extendBlendShapeOption = EditorGUILayout.Foldout(rendOption.extendBlendShapeOption,"Blend Shape Option : " + rendOption.rend.name);
            if (rendOption.extendBlendShapeOption)
            {
                for (int i = 0; i < rendOption.changeBlendShapeOption.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("",GUILayout.Width(5));
                        var toggle = EditorGUILayout.Toggle("", rendOption.changeBlendShapeOption[i] >= 0, GUILayout.Width(50));
                        EditorGUILayout.LabelField(rendOption.rend.GetMesh().GetBlendShapeName(i),  GUILayout.Width(150));
                        if (toggle)
                        {
                            if (rendOption.changeBlendShapeOption[i] < 0)
                            {
                                rendOption.changeBlendShapeOption[i] = (rendOption.rend as SkinnedMeshRenderer)?.GetBlendShapeWeight(i) ?? 0f;
                            }

                            rendOption.changeBlendShapeOption[i] =
                                EditorGUILayout.Slider(rendOption.changeBlendShapeOption[i], 0f, 100f,GUILayout.Width(200));
                        }
                        else
                        {
                            rendOption.changeBlendShapeOption[i] = -1f;
                            using (new EditorGUI.DisabledScope(true))
                            {
                                var noChange =
                                    EditorGUILayout.Slider(rendOption.changeBlendShapeOption[i], 0f, 100f,GUILayout.Width(200));
                            }
                        }
                    }
                }
            }
        }

        void SaveAnim(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileDir = System.IO.Path.GetDirectoryName (path);
            for (int i = 0; i < menuElements.Count; i++)
            {
                var menuElement = menuElements[i];
                var activateAnim = new AnimationClipCreator(menuElement.name+"_Activate",avatar.gameObject);
                var inactivateAnim = new AnimationClipCreator(menuElement.name+"_Inactivate",avatar.gameObject);
                foreach (var item in menuElement.activeItems)
                {
                    if (item.active)
                    {
                        SaveElementActive(item,activateAnim);
                    }
                    else
                    {
                        SaveElementInactive(item,activateAnim);
                    }
                }
                foreach (var item in menuElement.inactiveItems)
                {
                    if (item.active)
                    {
                        SaveElementActive(item,inactivateAnim);
                    }
                    else
                    {
                        SaveElementInactive(item,inactivateAnim);
                    }
                }
                activateAnim.CreateAsset(Path.Combine(fileDir,fileName+"_Activate.anim"));
                inactivateAnim.CreateAsset(Path.Combine(fileDir,fileName+"_InactivateAnim.anim"));
            }
            SaveMaterials(fileDir,false);
        }

        void Setup(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileDir = System.IO.Path.GetDirectoryName (path);
#if VRC_SDK_VRCSDK3
            var p = new ParametersCreater(fileName);
            var c = new AnimatorControllerCreator(fileName,false);
            var m = new MenuCreater(fileName);
            
            var idleAnim = new AnimationClipCreator("Idle", avatar.gameObject).CreateAsset(path, true);

            var toggleMenuElements = menuElements.Where(e => e.isToggle).ToList();
            for(int i = 0; i <toggleMenuElements.Count ; i++)
            {
                var menuElement = toggleMenuElements[i];
                if (menuElement.isToggle)
                {
                    var param = "MDInventory_" + data.name + "_Toggle" + i.ToString();
                    p.AddParam(param,menuElement.isDefault,menuElement.isSaved);
                    c.CreateLayer(param);
                    c.AddParameter(param,menuElement.isDefault);
                    c.AddDefaultState("Default",null);
                    var activeAnim = new AnimationClipCreator(menuElement.name+"_Active",avatar.gameObject);
                    var activateAnim = new AnimationClipCreator(menuElement.name+"_Activate",avatar.gameObject);
                    var inactiveAnim = new AnimationClipCreator(menuElement.name+"_Inactive",avatar.gameObject);
                    var inactivateAnim = new AnimationClipCreator(menuElement.name+"_Inactivate",avatar.gameObject);
                    
                    var activeItems = menuElement.activeItems.ToList();
                    activeItems.AddRange(ComputeLayerAnotherItems(menuElement));
                    foreach (var item in activeItems)
                    {
                        if (item.active)
                        {
                            SaveElementActive(item,activateAnim,activeAnim);
                        }
                        else
                        {
                            SaveElementInactive(item,activateAnim,activeAnim);
                        }
                    }
                    
                    var inactiveItems = menuElement.inactiveItems.ToList();
                    inactiveItems.AddRange(ComputeLayerInactiveItems(menuElement));
                    foreach (var item in inactiveItems)
                    {
                        if (item.active)
                        {
                            SaveElementActive(item,inactivateAnim,inactiveAnim);
                        }
                        else
                        {
                            SaveElementInactive(item,inactivateAnim,inactiveAnim);
                        }
                    }

                    c.AddState("Active", activeAnim.CreateAsset(path,true));
                    c.AddState("Activate", activateAnim.CreateAsset(path,true));
                    c.AddState("Inactive", inactiveAnim.CreateAsset(path,true));
                    c.AddState("Inactivate", inactivateAnim.CreateAsset(path,true));
                    c.AddState("ActiveIdle",idleAnim);
                    c.AddState("InactiveIdle",idleAnim);
                    c.AddTransition("Default","Active",param,true);
                    c.AddTransition("Default","Inactive",param,false);
                    c.AddTransition("Activate","Active");
                    c.AddTransition("Active","ActiveIdle");
                    c.AddTransition("ActiveIdle","Inactivate",param,false);
                    c.AddTransition("Inactivate","Inactive");
                    c.AddTransition("Inactive","InactiveIdle");
                    c.AddTransition("InactiveIdle","Activate",param,true);
                    //m.AddToggle(menuElement.name,menuElement.icon,param);
                    menuElement.param = param;
                }
            }

            foreach (LayerGroup layer in Enum.GetValues(typeof(LayerGroup)))
            {
                var layerMenuElements = menuElements.Where(e => !e.isToggle && e.layer == layer).ToList();
                if (layerMenuElements.Count == 0) continue;
                var param = "MDInventory_" + data.name + "_" + layer.ToString();
                p.AddParam(param,0,data.layerSettingses[(int) layer].isSaved);
                c.CreateLayer(param);
                c.AddDefaultState("Default",null);
                
                var d = data.layerSettingses[(int) layer].GetDefaultElement(data.menuElements);
                if (d==null)
                {
                    var menu = new MenuElement()
                    {
                        name = "Default",
                        activeItems = ComputeDefaultItems(layer),
                        isToggle = false,
                        layer = layer,
                    };
                    layerMenuElements.Insert(0,menu);
                }
                else
                {
                    layerMenuElements.Remove(d);
                    layerMenuElements.Insert(0,d);
                }
                
                for (int i = 0; i < layerMenuElements.Count; i++)
                {
                    var menuElement = layerMenuElements[i];
                    var itemElements = menuElement.activeItems.ToList();
                    itemElements.AddRange(ComputeLayerAnotherItems(menuElement));
                    var activeAnim = new AnimationClipCreator(i.ToString() + "_Active", avatar.gameObject);
                    foreach (var item in itemElements)
                    {
                        activeAnim.AddKeyframe_Gameobject(item.obj,0f,item.active);
                        activeAnim.AddKeyframe_Gameobject(item.obj,1f/60f,item.active);
                
                        // option処理
                        foreach (var rendOption in item.rendOptions)
                        {
                            for (int j = 0; j < rendOption.changeMaterialsOption.Count; j++)
                            {
                                if (rendOption.changeMaterialsOption[j] != null)
                                {
                                    activeAnim.AddKeyframe_Material(rendOption.rend,rendOption.changeMaterialsOption[j],0f,j);
                                    activeAnim.AddKeyframe_Material(rendOption.rend,rendOption.changeMaterialsOption[j],1f/60f,j);
                                }
                            }
                            for (int j = 0; j < rendOption.changeBlendShapeOption.Count; j++)
                            {
                                if (rendOption.changeBlendShapeOption[j] >= 0f)
                                {
                                    var rs = rendOption.rend as SkinnedMeshRenderer;
                                    activeAnim.AddKeyframe(0f, rs, "blendShape."+rs.GetMesh().GetBlendShapeName(j) , rendOption.changeBlendShapeOption[j]);
                                    activeAnim.AddKeyframe(1f/60f, rs, "blendShape."+rs.GetMesh().GetBlendShapeName(j) , rendOption.changeBlendShapeOption[j]);
                                }
                            }
                        }
                    }
                    c.AddState(i.ToString() + "_Active", activeAnim.CreateAsset(path,true));
                    c.AddState(i.ToString() + "_Idle", idleAnim);
                    c.AddTransition("Default",i.ToString() + "_Active",param,i);
                    c.AddTransition(i.ToString() + "_Active",i.ToString() + "_Idle");
                    //m.AddToggle(menuElement.name,menuElement.icon,param,i);
                    menuElement.param = param;
                    menuElement.value = i;
                }
                
                for (int i = 0; i < layerMenuElements.Count; i++)
                {
                    for (int j = 0; j < layerMenuElements.Count; j++)
                    {
                        if(i==j) continue;
                        var transitionAnim = new AnimationClipCreator( j.ToString() + "to" + i.ToString() + "_Transition", avatar.gameObject);
                        var fromItems = layerMenuElements[j].activeItems.ToList();
                        fromItems.AddRange(ComputeLayerAnotherItems(layerMenuElements[j]));
                        var toItems = layerMenuElements[i].activeItems.ToList();
                        toItems.AddRange(ComputeLayerAnotherItems(layerMenuElements[i]));
                        foreach (var item in ComputeDefaultItems(layer))
                        { 
                            var fromItem = fromItems.FirstOrDefault(e => e.obj == item.obj);
                            var toItem = toItems.FirstOrDefault(e => e.obj == item.obj);
                            // レイヤー内で参照されているアイテムすべてについてとらんじちよん
                            if (fromItem == null && toItem == null)
                            {
                                
                            }
                            else if (fromItem == null)
                            {
                                SaveElementTransition(toItem,transitionAnim);
                            }
                            else if (toItem == null)
                            {
                                SaveElementTransition(fromItem,transitionAnim,true);
                            }
                            else
                            {
                                if (fromItem.active != toItem.active)
                                {
                                    SaveElementTransition(toItem,transitionAnim);
                                }
                            }
                        }
                        c.AddState(j.ToString() + "to" + i.ToString() + "_Transition", transitionAnim.CreateAsset(path,true));
                        c.AddTransition(j.ToString() + "_Idle",j.ToString() + "to" + i.ToString() + "_Transition",param,i);
                        c.AddTransition(j.ToString() + "to" + i.ToString() + "_Transition",i.ToString() + "_Active");
                    }
                }
            }

            foreach (var menuElement in menuElements)
            {
                if (menuElement.isToggle)
                {
                    m.AddToggle(menuElement.name,menuElement.icon,menuElement.param);
                }
                else
                {
                    m.AddToggle(menuElement.name,menuElement.icon,menuElement.param,menuElement.value);
                }
            }
            
            var pm = new MenuCreater("ParentMenu");
            pm.AddSubMenu(m.CreateAsset(path, true),data.name,data.icon);

            //p.LoadParams(c,true);
            var am = new AvatarModifyTool(avatar,fileDir);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.fx_controller = c.CreateAsset(path, true);
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = pm.CreateAsset(path,true);
            }
            AssetDatabase.AddObjectToAsset(assets,path);
            data.assets = assets;
            if (writeDefault)
            {
                am.WriteDefaultOverride = true;
            }
            am.RevertAnimator(VRCAvatarDescriptor.AnimLayerType.FX,"MDInventory_" + data.name + "_");
            am.ModifyAvatar(assets,false,keepOldAsset);
#endif
            SaveMaterials(path,true);
        }

        void SaveElementTransition(ItemElement element,
            AnimationClipCreator transitionAnim, bool invert = false)
        {
            if (invert)
            {
                if (element.active)
                {
                    SaveElementInactive(element,transitionAnim);
                }
                else
                {
                    SaveElementActive(element,transitionAnim);
                }
            }
            else
            {
                if (element.active)
                {
                    SaveElementActive(element,transitionAnim);
                }
                else
                {
                    SaveElementInactive(element,transitionAnim);
                }
            }
        }

        void SaveElementActive(ItemElement element,
            AnimationClipCreator transitionAnim, AnimationClipCreator setAnim = null)
        {
            if (setAnim != null)
            {
                ActiveAnimation(setAnim,element.obj,true);
                
                // option処理
                foreach (var rendOption in element.rendOptions)
                {
                    for (int i = 0; i < rendOption.changeMaterialsOption.Count; i++)
                    {
                        if (rendOption.changeMaterialsOption[i] != null)
                        {
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.changeMaterialsOption[i],0f,i);
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.changeMaterialsOption[i],1f/60f,i);
                        }
                    }
                    for (int i = 0; i < rendOption.changeBlendShapeOption.Count; i++)
                    {
                        if (rendOption.changeBlendShapeOption[i] >= 0f)
                        {
                            var rs = rendOption.rend as SkinnedMeshRenderer;
                            setAnim.AddKeyframe(0f, rs, "blendShape."+rs.GetMesh().GetBlendShapeName(i) , rendOption.changeBlendShapeOption[i]);
                            setAnim.AddKeyframe(1f/60f, rs, "blendShape."+rs.GetMesh().GetBlendShapeName(i) , rendOption.changeBlendShapeOption[i]);
                        }
                    }
                }
            }
            if (element.type == FeedType.None)
            {
                ActiveAnimation(transitionAnim,element.obj,true,element.delay);
            }
            else
            if(element.type == FeedType.Scale)
            {
                ScaleAnimation(transitionAnim, element.obj, element.delay, element.duration, true);
            }
            else
            if(element.type == FeedType.Shader)
            {
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration, element.animationShader, element.animationParam,0f,1f);
            }
            else
            {
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration,
                    element.type.GetShaderByType(), "_AnimationTime",
                    0f,1f);
                ChangeMaterialDefault(transitionAnim,element.obj,element.delay+element.duration+1f/60f);
            }
        }
        void SaveElementInactive(ItemElement element,
            AnimationClipCreator transitionAnim, AnimationClipCreator setAnim = null)
        {
            if (setAnim != null)
            {
                ActiveAnimation(setAnim,element.obj,false);
                
                // option処理
                foreach (var rendOption in element.rendOptions)
                {
                    for (int i = 0; i < rendOption.changeMaterialsOption.Count; i++)
                    {
                        if (rendOption.changeMaterialsOption[i] != null)
                        {
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[i],0f,i);
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[i],1f/60f,i);
                        }
                    }
                    for (int i = 0; i < rendOption.changeBlendShapeOption.Count; i++)
                    {
                        if (rendOption.changeBlendShapeOption[i] >= 0f)
                        {
                            var rs = rendOption.rend as SkinnedMeshRenderer;
                            setAnim.AddKeyframe(0f, rs, "blendShape."+rendOption.rend.GetMesh().GetBlendShapeName(i) , rs.GetBlendShapeWeight(i));
                            setAnim.AddKeyframe(1f/60f, rs, "blendShape."+rendOption.rend.GetMesh().GetBlendShapeName(i) , rs.GetBlendShapeWeight(i));
                        }
                    }
                }
            }
            if (element.type == FeedType.None)
            {
                ActiveAnimation(transitionAnim,element.obj,false,element.delay);
            }
            else
            if(element.type == FeedType.Scale)
            {
                ScaleAnimation(transitionAnim, element.obj, element.delay, element.duration, false);
            }
            else
            if(element.type == FeedType.Shader)
            {
                ShaderAnimation(transitionAnim, element.obj,element.delay,element.duration, element.animationShader, element.animationParam,1f,0f);
            }
            else
            {
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration,
                    element.type.GetShaderByType(), "_AnimationTime",
                    1f,0f);
                ActiveAnimation(transitionAnim,element.obj,false,element.delay+element.duration+1f/60f);
                ChangeMaterialDefault(transitionAnim,element.obj,element.delay+element.duration+2f/60f);
            }
        }

        AnimationClipCreator ScaleAnimation(AnimationClipCreator anim,GameObject obj, float delay = 0f, float duration = 1f,
            bool activate = true,bool bounce = false)
        {
            anim.AddKeyframe_Gameobject(obj,0f,true);
            var defaultScale = obj.transform.localScale;
            if (activate)
            {
                anim.AddKeyframe_Scale( delay,obj.transform,Vector3.zero);
                if (bounce)
                {
                    anim.AddKeyframe_Scale(delay+duration*0.6f,obj.transform, defaultScale*1.2f);
                    anim.AddKeyframe_Scale(delay+duration*0.8f,obj.transform, defaultScale*0.9f);
                }
                anim.AddKeyframe_Scale(delay+duration,obj.transform,defaultScale);
            }
            else
            {
                anim.AddKeyframe_Scale( delay+duration,obj.transform,Vector3.zero);
                if (bounce)
                {
                    anim.AddKeyframe_Scale(delay+duration*0.2f,obj.transform, defaultScale*1.2f);
                    anim.AddKeyframe_Scale(delay+duration*0.4f,obj.transform, defaultScale*0.9f);
                }
                anim.AddKeyframe_Scale(delay,obj.transform,defaultScale);
            }
            return anim;
        }
        
        AnimationClipCreator ShaderAnimation(AnimationClipCreator anim,GameObject obj,float delay = 0f , float duration = 1f,
            Shader shader = null,string param = "",float from = 0f, float to = 1f)
        {
            anim.AddKeyframe_Gameobject(obj,delay,true);
            ChangeMaterialShader(anim,obj,shader,delay);
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                anim.AddKeyframe_MaterialParam(delay, rend, param, from);
                anim.AddKeyframe_MaterialParam(delay+duration, rend, param, to);
            }
            
            return anim;
        }

        AnimationClipCreator ActiveAnimation(AnimationClipCreator anim, GameObject obj,bool value,float time = 0f)
        {
            if (time < 1f / 60f)
            {
                anim.AddKeyframe_Gameobject(obj,0f,value);
            }
            else
            {
                anim.AddKeyframe_Gameobject(obj,0f,!value);
            }
            anim.AddKeyframe_Gameobject(obj,time,value);
            return anim;
        }

        void ChangeMaterialDefault(AnimationClipCreator anim,GameObject obj,float time = 0f)
        {
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    anim.AddKeyframe_Material(rend,rend.sharedMaterials[i],time,i);
                }
            }
        }

        void ChangeMaterialShader(AnimationClipCreator anim,GameObject obj, Shader shader,float time = 0f)
        {
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    anim.AddKeyframe_Material(rend,
                        GetAnimationMaterial(rend.sharedMaterials[i],shader),time,i);
                }
            }
        }

        void SaveMaterials(string path,bool subAsset = false)
        {
            foreach (var mats in matlist)
            {
                foreach (var mat in mats.Value)
                {
                    if (subAsset)
                    {
                        AssetDatabase.AddObjectToAsset(mat.Value,path);
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(mat.Value, AssetDatabase.GenerateUniqueAssetPath(
                            Path.Combine(path,mat.Value.name + ".mat")));
                    }
                }
            }
            matlist = new Dictionary<Shader, Dictionary<Material, Material>>();
        }

        void SetObjectActiveForScene(MenuElement menu)
        {
            RevertObjectActiveForScene();
            foreach (var item in menu.activeItems)
            {
                if (!defaultActive.ContainsKey(item.obj))
                {
                    defaultActive.Add(item.obj,item.obj.activeSelf);
                }
                item.obj.SetActive(item.active);
            }
            foreach (var item in ComputeLayerAnotherItems(menu))
            {
                if (!defaultActive.ContainsKey(item.obj))
                {
                    defaultActive.Add(item.obj,item.obj.activeSelf);
                }
                item.obj.SetActive(item.active);
            }
        }
        
        void RevertObjectActiveForScene()
        {
            foreach (var oa in defaultActive)
            {
                oa.Key.SetActive(oa.Value);
            }
        }

        void SyncItemActive(List<ItemElement> srcs, List<ItemElement> dsts, bool invert = false)
        {
            foreach (var dst in dsts)
            {
                var src = srcs.FirstOrDefault(e => e.obj == dst.obj);
                if (src != null)
                {
                    dst.active = invert ? !src.active : src.active;
                }
                
            }
        }

        List<ItemElement> ComputeLayerInactiveItems(MenuElement menu)
        {
            var activeItems = menu.activeItems.ToList();
            if (!menu.isToggle)
            {
                activeItems.AddRange(ComputeLayerAnotherItems(menu));
            }
            
            return activeItems.Where(e=>menu.inactiveItems.All(f=>f.obj!=e.obj)).Select(e=>e.Clone(true)).ToList();
        }
        
        List<ItemElement> ComputeLayerAnotherItems(MenuElement menu)
        {
            if (!menu.isToggle)
            {
                var items = GetActiveItems(menu.layer).
                    Where(e => menu.activeItems.All(f => f.obj != e.obj)).
                    Select(e => e.Clone(true)).ToList();
                if (IsMenuElementDefault(menu))
                {
                    // デフォルトエレメントならアクティブをシーンの状態に合わせる
                    foreach (var item in items)
                    {
                        item.active = defaultActive[item.obj];
                        foreach (var rendOption in item.rendOptions)
                        {
                            foreach (var another in items.SelectMany(e=>e.rendOptions))
                            {
                                if (rendOption.rend == another.rend)
                                {
                                    // Material設定の上書き
                                    for (int i = 0; i < rendOption.changeMaterialsOption.Count; i++)
                                    {
                                        if (rendOption.changeMaterialsOption[i] != null) break;
                                        if (another.changeMaterialsOption[i] != null)
                                        {
                                            rendOption.changeMaterialsOption[i] = rendOption.rend.sharedMaterials[i];
                                        }
                                    }
                                    // BlendShapel設定の上書き
                                    for (int i = 0; i < rendOption.changeBlendShapeOption.Count; i++)
                                    {
                                        if (rendOption.changeBlendShapeOption[i] >= 0f) break;
                                        if (another.changeBlendShapeOption[i] >= 0f)
                                        {
                                            rendOption.changeBlendShapeOption[i] =
                                                (rendOption.rend as SkinnedMeshRenderer)?.GetBlendShapeWeight(i) ?? 0f;
                                        }
                                    }
                                    
                                }
                            }
                        }
                    }
                }

                var inactiveItems = GetInactiveItems(menu.layer);
                foreach (var item in items)
                {
                    var inactiveItem = inactiveItems.FirstOrDefault(e => e.obj == item.obj && e.active == item.active);
                    if (inactiveItem != null)
                    {
                        item.delay = inactiveItem.delay;
                        item.duration = inactiveItem.duration;
                        item.type = inactiveItem.type;
                        item.animationShader = inactiveItem.animationShader;
                        item.animationParam = inactiveItem.path;
                    }
                }
                return items;
            }

            return new List<ItemElement>();
        }

        bool IsMenuElementDefault(MenuElement menu)
        {
            if (menu.isToggle)
            {
                return menu.isDefault;
            }
            else
            {
                return data.layerSettingses[(int) menu.layer].GetDefaultElement(menuElements)
                       == menu;
            }
        }
        
        List<ItemElement> ComputeDefaultItems(LayerGroup layer)
        {
            var items = GetActiveItems(layer);
            foreach (var item in items)
            {
                item.active = item.obj.gameObject.activeSelf;
            }
            return items;
        }

        List<ItemElement> GetActiveItems(LayerGroup layer)
        {
            var items = new List<ItemElement>();
            foreach (var menuElement in menuElements)
            {
                if (!menuElement.isToggle && menuElement.layer == layer)
                {
                    foreach (var item in menuElement.activeItems)
                    {
                        items.Add(item.Clone());
                    }
                }
            }

            return DistinctList(items);
        }
        List<ItemElement> GetInactiveItems(LayerGroup layer)
        {
            var items = new List<ItemElement>();
            foreach (var menuElement in menuElements)
            {
                if (!menuElement.isToggle && menuElement.layer == layer)
                {
                    foreach (var item in menuElement.inactiveItems)
                    {
                        items.Add(item.Clone());
                    }
                }
            }

            return items;
        }

        List<ItemElement> DistinctList(List<ItemElement> origin)
        {
            var items = new List<ItemElement>();
            foreach (var item in origin)
            {
                if (items.All(e => e.obj != item.obj))
                {
                    items.Add(item);
                }
            }

            return items;
        }

        void AddMenu()
        {
            menuElements.Add(new MenuElement()
            {
                name = "Menu" + menuElements.Count,
                icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.itemIcon)
            });
        }
        private void OnDestroy()
        {
            RevertObjectActiveForScene();
            SaveMaterials("Assets/Export");
        }
        
        [OnOpenAssetAttribute(2)]
        public static bool step2(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(MagicalDresserInventorySaveData))
            {
                MagicalDresserInventorySetup.OpenSavedWindow(EditorUtility.InstanceIDToObject(instanceID) as MagicalDresserInventorySaveData);
            }
            return false;
        }
    }
}
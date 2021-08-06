using System;
using System.Collections.Generic;
using System.Linq;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Callbacks;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.MagicDresser
{
    public class MagicDresserSetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/MagicInventorySetup",false,4)]
        public static void ShowWindow()
        {
            OpenSavedWindow();
        }

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#else
        private Animator avatar;
#endif
        private MagicDresserSaveData data;
        private List<MenuElement> menuElements => data.menuElements;
        private SerializedProperty prop;
        ReorderableList menuReorderableList;
        
        Dictionary<Shader,Dictionary<Material, Material>> matlist = new Dictionary<Shader,Dictionary<Material, Material>>();
        Material GetAnimationMaterial(Material origin,Shader shader)
        {
            if (!matlist.ContainsKey(shader))
            {
                matlist.Add(shader,new Dictionary<Material,Material>());
            }
            if (!matlist[shader].ContainsKey(origin))
            {
                if (shader == Shader.Find("HhotateA/MagicDresser/Draw"))
                {
                    var mat = new Material(AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.drawMaterial));
                    mat.mainTexture = origin.mainTexture;
                    matlist[shader].Add(origin,mat);
                }
                else if (shader == Shader.Find("HhotateA/MagicDresser/Geom"))
                {
                    var mat = new Material(AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.geomMaterial));
                    mat.mainTexture = origin.mainTexture;
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

        public static void OpenSavedWindow(MagicDresserSaveData saveddata = null)
        {
            var wnd = GetWindow<MagicDresserSetup>();
            wnd.titleContent = new GUIContent("MagicInventory");

            if (saveddata)
            {
                wnd.data = saveddata;
            }
            else
            {
                wnd.data = CreateInstance<MagicDresserSaveData>();
            }
            wnd.menuReorderableList = new ReorderableList(wnd.menuElements, typeof(MenuElement))
            {
                elementHeight = 60,
                drawElementCallback = (r, i, a, f) =>
                {
                    var d = wnd.menuElements[i];
                    r.height /= 3;
                    d.name = EditorGUI.TextField(r,"", d.name);
                    r.y += r.height;
                    if (d.isToggle)
                    {
                        d.isToggle = EditorGUI.Toggle(r, "isToggle", d.isToggle);
                    }
                    else
                    {
                        var rh = r;
                        rh.width *= 0.5f;
                        d.isToggle = EditorGUI.Toggle(rh, "isToggle", d.isToggle);
                        rh.x += rh.width;
                        d.layer = (LayerGroup) EditorGUI.EnumPopup(rh, d.layer);
                    }
                    r.y += r.height;
                    d.isSaved = EditorGUI.Toggle(r, "isSaved", d.isSaved);
                    //d.animationParam = EditorGUI.TextField(r,"Param", d.animationParam);
                    //d.root = (GameObject) EditorGUI.ObjectField(r,"", d.root, typeof(GameObject), true);
                },
                onAddCallback = l => wnd.AddMenu(),
                drawHeaderCallback = r =>
                {
                    var rh = r;
                    rh.width = 100;
                    EditorGUI.LabelField(r,"Menu Title");
                    rh.x += rh.width;
                    rh.width = 50;
                    wnd.data.icon = (Texture2D) EditorGUI.ObjectField (rh,wnd.data.icon, typeof(Texture2D), false);
                    rh.x += rh.width;
                    rh.width = r.width - rh.x;
                    wnd.data.name = EditorGUI.TextField(rh,wnd.data.name);
                }
            };
        }

        private void OnGUI()
        {
            /*if (avatar)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Menu Title",GUILayout.Width (75));
                    data.icon = (Texture2D) EditorGUILayout.ObjectField (data.icon, typeof(Texture2D), false,GUILayout.Width (30));
                    data.name = EditorGUILayout.TextField(data.name);
                }
            }*/
#if VRC_SDK_VRCSDK3
            var a = EditorGUILayout.ObjectField("", avatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            if (a != avatar)
            {
                avatar = a;
                if(avatar) data.ApplyRoot(avatar.gameObject);
            }
#else
            avatar = EditorGUILayout.ObjectField("", avatar, typeof(Animator), true) as Animator;
#endif
            if (!avatar)
            {
                GUILayout.Label("シーン上のアバターをドラッグ＆ドロップ");
                return;
            }
            
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                menuReorderableList.DoLayoutList();
                if (check.changed)
                {
                    ReComputeItemList();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box,GUILayout.Width(position.width/2-5)))
                {
                    EditorGUILayout.LabelField("Activate Transition");
                    if (0 <= menuReorderableList.index && menuReorderableList.index < menuElements.Count)
                    {
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            var index = menuReorderableList.index;
                            foreach (var item in menuElements[index].activeItems)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    ItemElementDisplay(item);
                                }

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
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            ItemElementDisplay(item,false);
                                        }

                                        if (add.changed)
                                        {
                                            menuElements[index].activeItems.Add(item);
                                        }
                                    }
                                }
                            }

                            var newItem = (GameObject) EditorGUILayout.ObjectField("", null,
                                typeof(GameObject), true);
                            if (newItem)
                            {
                                if (menuElements[index].activeItems.All(e => e.obj != newItem))
                                {
                                    menuElements[index].activeItems.Add(new ItemElement(newItem));
                                }
                            }

                            if (check.changed)
                            {
                                ReComputeItemList();
                            }
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope(GUI.skin.box,GUILayout.Width(position.width/2-5)))
                {
                    EditorGUILayout.LabelField("Inactivate Transition");
                    if (0 <= menuReorderableList.index && menuReorderableList.index < menuElements.Count)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            var index = menuReorderableList.index;
                            foreach (var item in menuElements[index].inactiveItems)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    ItemElementDisplay(item);
                                }

                                if (!item.obj)
                                {
                                    menuElements[index].inactiveItems.Remove(item);
                                    return;
                                }
                            }

                            var newItem = (GameObject) EditorGUILayout.ObjectField("", null,
                                typeof(GameObject), true);
                            if (newItem)
                            {
                                if (menuElements[index].inactiveItems.All(e => e.obj != newItem))
                                {
                                    menuElements[index].inactiveItems.Add(new ItemElement(newItem));
                                }
                            }
                        }
                    }
                }
            }

            using (new EditorGUI.DisabledScope(avatar==null))
            {
                if (GUILayout.Button(""))
                {
                    var path = EditorUtility.SaveFilePanel("Save", "Assets", "MagicDresser", "asset");
                    if (string.IsNullOrWhiteSpace(path)) return;
                    data = Instantiate(data);
                    SaveAnim(path);
                }
            }
        }
        
        void ItemElementDisplay(ItemElement item,bool editable = true)
        {
            using (new EditorGUI.DisabledScope(!editable))
            {
                item.active = EditorGUILayout.Toggle("", item.active, GUILayout.Width(30));
                item.obj = (GameObject) EditorGUILayout.ObjectField("", item.obj,
                    typeof(GameObject), true, GUILayout.Width(150));

                item.delay = EditorGUILayout.FloatField("", item.delay, GUILayout.Width(50));
                item.duration =
                    EditorGUILayout.FloatField("", item.duration, GUILayout.Width(50));
            }

            item.type = (FeedType) EditorGUILayout.EnumPopup("", item.type,
                GUILayout.Width(50));
        }

        void SaveAnim(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileDir = System.IO.Path.GetDirectoryName (path);
#if VRC_SDK_VRCSDK3
            var p = new ParametersCreater(fileName);
            var c = new AnimatorControllerCreator(fileName,false);
            var m = new MenuCreater(fileName);
            
            data.ApplyPath(avatar.gameObject);
            AssetDatabase.CreateAsset(data,path);
            
            var idleAnim = new AnimationClipCreator("Idle", avatar.gameObject).CreateAsset(path, true);

            var toggleMenuElements = menuElements.Where(e => e.isToggle).ToList();
            for(int i = 0; i <toggleMenuElements.Count ; i++)
            {
                var menuElement = toggleMenuElements[i];
                if (menuElement.isToggle)
                {
                    var param = "MagicDresser_" + data.name + "_Toggle" + i.ToString();
                    c.CreateLayer(param);
                    c.AddParameter(param,menuElement.isDefault);
                    c.AddDefaultState("Default",null);
                    var activeAnim = new AnimationClipCreator(menuElement.name+"_Active",avatar.gameObject);
                    var activateAnim = new AnimationClipCreator(menuElement.name+"_Activate",avatar.gameObject);
                    var inactiveAnim = new AnimationClipCreator(menuElement.name+"_Inactive",avatar.gameObject);
                    var inactivateAnim = new AnimationClipCreator(menuElement.name+"_Inactivate",avatar.gameObject);
                    foreach (var item in menuElement.activeItems)
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
                    foreach (var item in menuElement.inactiveItems)
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
                var param = "MagicDresser_" + data.name + "_" + layer.ToString();
                c.CreateLayer(param);
                c.AddDefaultState("Default",null);
                if(data.layerSettingses[layer].defaultValue != LayerSettings.DefaultValue.Element)
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
                    if (data.layerSettingses[layer].defaultElement != null)
                    {
                        if (layerMenuElements.Contains(data.layerSettingses[layer].defaultElement))
                        {
                            layerMenuElements.Remove(data.layerSettingses[layer].defaultElement);
                        }
                        layerMenuElements.Insert(0,data.layerSettingses[layer].defaultElement);
                    }
                    
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
                        foreach (var item in layerMenuElements[0].activeItems)
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

            p.LoadParams(c,true);
            var am = new AvatarModifyTool(avatar,fileDir);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.fx_controller = c.CreateAsset(path, true);
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = m.CreateAsset(path, true);
            }
            am.ModifyAvatar(assets,false);
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
            if(setAnim!=null) ActiveAnimation(setAnim,element.obj,true);
            if (element.type == FeedType.None)
            {
                ActiveAnimation(transitionAnim,element.obj,true,element.delay);
            }
            else
            if(element.type == FeedType.Scale)
            {
                ActivateAnimation_Scale(transitionAnim, element.obj, element.delay,element.duration);
            }
            else
            if(element.type == FeedType.Shader)
            {
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration, element.animationShader, element.animationParam,0f,1f);
            }
            else
            if(element.type == FeedType.Feed)
            {
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration, element.animationShader, element.animationParam,0f,1f);
            }
            else
            if(element.type == FeedType.Draw)
            {
                //ActivateAnimation_Shader(transitionAnim, element.obj, element.delay, element.duration, Shader.Find("HhotateA/MagicDresser/Draw"), "_AnimationTime");
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration,
                    Shader.Find("HhotateA/MagicDresser/Draw"), "_AnimationTime",
                    0f,1f);
            }
            else
            if(element.type == FeedType.Geom)
            {
                //ActivateAnimation_Shader(transitionAnim, element.obj, element.delay, element.duration, Shader.Find("HhotateA/MagicDresser/Draw"), "_AnimationTime");
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration,
                    Shader.Find("HhotateA/MagicDresser/Geom"), "_AnimationTime",
                    0f,1f);
            }
        }
        void SaveElementInactive(ItemElement element,
            AnimationClipCreator transitionAnim, AnimationClipCreator setAnim = null)
        {
            if(setAnim!=null) ActiveAnimation(setAnim,element.obj,false);
            if (element.type == FeedType.None)
            {
                ActiveAnimation(transitionAnim,element.obj,false,element.delay);
            }
            else
            if(element.type == FeedType.Scale)
            {
                InactivateAnimation_Scale(transitionAnim, element.obj, element.delay,element.duration);
            }
            else
            if(element.type == FeedType.Shader)
            {
                ShaderAnimation(transitionAnim, element.obj,element.delay,element.duration, element.animationShader, element.animationParam,1f,0f);
            }
            else
            if(element.type == FeedType.Feed)
            {
                ShaderAnimation(transitionAnim, element.obj,element.delay,element.duration, element.animationShader, element.animationParam,1f,0f);
            }
            else
            if(element.type == FeedType.Draw)
            {
                //InactivateAnimation_Shader(transitionAnim, element.obj,element.delay,element.duration, Shader.Find("HhotateA/MagicDresser/Draw"), "_AnimationTime");
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration,
                    Shader.Find("HhotateA/MagicDresser/Draw"), "_AnimationTime",
                    1f,0f);
            }
            else
            if(element.type == FeedType.Geom)
            {
                //InactivateAnimation_Shader(transitionAnim, element.obj,element.delay,element.duration, Shader.Find("HhotateA/MagicDresser/Draw"), "_AnimationTime");
                ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration,
                    Shader.Find("HhotateA/MagicDresser/Geom"), "_AnimationTime",
                    1f,0f);
            }
        }

        AnimationClipCreator ActivateAnimation_Scale(AnimationClipCreator anim,GameObject obj, float delay = 0f, float duration = 1f,
            bool bounce = false)
        {
            anim.AddKeyframe_Gameobject(obj,0f,true);
            var defaultScale = obj.transform.localScale;
            anim.AddKeyframe_Scale(delay,obj.transform,Vector3.zero);
            if (bounce)
            {
                anim.AddKeyframe_Scale(delay+duration*0.5f,obj.transform, defaultScale*0.1f);
                anim.AddKeyframe_Scale(delay+duration*0.9f,obj.transform, defaultScale*1.3f);
            }
            anim.AddKeyframe_Scale(delay+duration,obj.transform,defaultScale);
            return anim;
        }
        
        AnimationClipCreator InactivateAnimation_Scale(AnimationClipCreator anim,GameObject obj,float delay = 0f , float duration = 1f,
            bool bounce = false)
        {
            anim.AddKeyframe_Gameobject(obj,0f,true);
            var defaultScale = obj.transform.localScale;
            anim.AddKeyframe_Scale(delay,obj.transform,defaultScale);
            if (bounce)
            {
                anim.AddKeyframe_Scale(delay+duration*0.1f,obj.transform, defaultScale*1.3f);
                anim.AddKeyframe_Scale(delay+duration*0.5f,obj.transform, defaultScale*0.1f);
            }
            anim.AddKeyframe_Scale(delay+duration,obj.transform,Vector3.zero);
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
            anim.AddKeyframe_Gameobject(obj,0f,!value);
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
                        AssetDatabase.CreateAsset(mat.Value,path+mat.Value.name+".mat");
                    }
                }
            }
            matlist = new Dictionary<Shader, Dictionary<Material, Material>>();
        }

        void ReComputeItemList()
        {
            foreach (var menuElement in menuElements)
            {
                /*if (menuElement.isToggle)
                {
                    menuElement.inactiveItems = menuElement.activeItems.Select(e=>e.Clone()).ToList();
                }
                else
                {
                    var items = new List<ItemElement>();
                    foreach (var item in GetActiveItems(menuElement.layer))
                    {
                        if (menuElement.activeItems.All(e => e.obj != item.obj))
                        {
                            items.Add(item);
                        }
                    }
                    menuElement.inactiveItems = items;
                }*/
                menuElement.inactiveItems = menuElement.activeItems.Select(e=>e.Clone(true)).ToList();
            }
        }

        List<ItemElement> ComputeLayerAnotherItems(MenuElement menu)
        {
            if (data.layerSettingses[menu.layer].defaultValue == LayerSettings.DefaultValue.Default ||
                data.layerSettingses[menu.layer].defaultValue == LayerSettings.DefaultValue.AllOn ||
                data.layerSettingses[menu.layer].defaultValue == LayerSettings.DefaultValue.AllOff)
            {
                var items = GetActiveItems(menu.layer).Where(e=>menu.activeItems.All(f=>f.obj!=e.obj)).ToList();
                foreach (var item in items)
                {
                    if (data.layerSettingses[menu.layer].defaultValue == LayerSettings.DefaultValue.Default)
                    {
                        item.active = item.obj.gameObject.activeSelf;
                    }
                    else if (data.layerSettingses[menu.layer].defaultValue == LayerSettings.DefaultValue.AllOn)
                    {
                        item.active = true;
                    }
                    else if (data.layerSettingses[menu.layer].defaultValue == LayerSettings.DefaultValue.AllOff)
                    {
                        item.active = false;
                    }
                    item.delay = 0f;
                    item.duration = 0f;
                    item.type = FeedType.None;
                }
                return items;
            }
            else
            if(data.layerSettingses[menu.layer].defaultValue == LayerSettings.DefaultValue.Element &&
               data.layerSettingses[menu.layer].defaultElement != null)
            {
                var items = data.layerSettingses[menu.layer].defaultElement.activeItems.Where(e=>menu.activeItems.All(f=>f.obj!=e.obj)).ToList();
                return items;
            }
            else
            if (data.layerSettingses[menu.layer].defaultValue == LayerSettings.DefaultValue.Nothing)
            {
                return new List<ItemElement>();
            }

            return new List<ItemElement>();
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
                name = "item" + menuElements.Count,
            });
        }
        private void OnDestroy()
        {
            SaveMaterials("Assets/aa");
        }
        
        [OnOpenAssetAttribute(1)]
        public static bool step1(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(MagicDresserSaveData))
            {
                MagicDresserSetup.OpenSavedWindow(EditorUtility.InstanceIDToObject(instanceID) as MagicDresserSaveData);
            }
            return false;
        }
    }
}
/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEngine.Serialization;
using System.Reflection;
using UnityEditor.IMGUI.Controls;

namespace HhotateA.AvatarModifyTools.MagicalDresserInventorySystem
{
    public class MagicalDresserInventorySaveData : ScriptableObject
    {
        [FormerlySerializedAs("name")] public string saveName = "MagicalDresserInventorySaveData";
        public string avatarName;
        public int avatarGUID;
        public Texture2D icon = null;
        public List<MenuElement> menuElements = new List<MenuElement>();
        public LayerSettings[] layerSettingses;
        public AvatarModifyData assets;

        public bool useMenuTemplate;
        public List<MenuTemplate> menuTemplate = new List<MenuTemplate>();

        public List<MenuTemplate> ReloadTemplates()
        {
            return MenuTemplate.ReloadTemplates(menuTemplate, menuElements);
        }

        public bool idleOverride = true;
        public bool materialOverride = true;
        public bool createAnimWhenNotChangedActive = false;

        public MagicalDresserInventorySaveData()
        {
            layerSettingses = Enumerable.Range(0,Enum.GetValues(typeof(LayerGroup)).Length).
                Select(l=>new LayerSettings()).ToArray();
        }

        public void SaveGUID(GameObject root)
        {
            avatarGUID = root.GetInstanceID();
            avatarName = root.name;
            root = root.transform.parent?.gameObject;
            while (root)
            {
                avatarName = root.name + "/" + avatarName;
                root = root.transform.parent?.gameObject;
            }
        }

        public GameObject GetRoot()
        {
            var root = GameObject.Find(avatarName);
            if (root)
            {
                if (root.GetInstanceID() == avatarGUID)
                {
                    return root;
                }
            }
            return null;
        }

        public void ApplyRoot(GameObject root)
        {
            foreach (var menuElement in menuElements)
            {
                foreach (var item in menuElement.activeItems)
                {
                    item.GetRelativeGameobject(root.transform);
                }
                // menuElement.activeItems = menuElement.activeItems.Where(e => e.obj != null).ToList();
                
                foreach (var item in menuElement.inactiveItems)
                {
                    item.GetRelativeGameobject(root.transform);
                }
                // menuElement.inactiveItems = menuElement.inactiveItems.Where(e => e.obj != null).ToList();
            }
        }
        public void ApplyPath(GameObject root)
        {
            foreach (var menuElement in menuElements)
            {
                foreach (var item in menuElement.SafeActiveItems())
                {
                    item.GetRelativePath(root);
                }
                menuElement.activeItems = menuElement.activeItems.Where(e => !String.IsNullOrWhiteSpace(e.path)).ToList();
                foreach (var item in menuElement.SafeInactiveItems())
                {
                    item.GetRelativePath(root);
                }
                menuElement.inactiveItems = menuElement.inactiveItems.Where(e => !String.IsNullOrWhiteSpace(e.path)).ToList();
            }
        }
        
        public void RepairReference(string path,GameObject obj,GameObject root)
        {
            foreach (var menuElement in menuElements)
            {
                foreach (var item in menuElement.activeItems)
                {
                    if (item.path == path)
                    {
                        item.obj = obj;
                        item.ReloadRendOption();
                        if(root != null) item.GetRelativePath(root);
                    }
                }
                foreach (var item in menuElement.inactiveItems)
                {
                    if (item.path == path)
                    {
                        item.obj = obj;
                        item.ReloadRendOption();
                        if(root != null) item.GetRelativePath(root);
                    }
                }
            }
        }
    }

    [Serializable]
    public class MenuTemplate
    {
        public string name;
        public Texture2D icon;
        public List<MenuTemplate> childs = new List<MenuTemplate>();
        public string menuGUID;
        public bool autoCreate = true;
        [SerializeField] int guid = 0;

        public int GetGuid()
        {
            if (guid == 0)
            {
                guid = Guid.NewGuid().GetHashCode();
            }
            return guid;
        }

        public MenuTemplate FIndTemplateElement(int id, Action<MenuTemplate, MenuTemplate> onFind = null)
        {
            childs = childs.Distinct().ToList();
            foreach (var child in childs)
            {
                if (child.guid == id)
                {
                    onFind?.Invoke(child,this);
                    return child;
                }
                var e = child.FIndTemplateElement(id,onFind);
                if (e != null)
                {
                    return e;
                }
            }

            return null; 
        }
        public static MenuTemplate FIndTemplateElement(List<MenuTemplate> root,int id, Action<MenuTemplate, MenuTemplate> onFind = null)
        {
            foreach (var child in root)
            {
                if (child.guid == id)
                {
                    onFind?.Invoke(child,null);
                    return child;
                }
                var e = child.FIndTemplateElement(id,onFind);
                if (e != null)
                {
                    return e;
                }
            }

            return null;
        }
        
        public MenuTemplate FindMenuElement(string id, Action<MenuTemplate, MenuTemplate> onFind = null)
        {
            childs = childs.Distinct().ToList();
            foreach (var child in childs)
            {
                if (child.menuGUID == id)
                {
                    onFind?.Invoke(child,this);
                    return child;
                }
                var e = child.FindMenuElement(id,onFind);
                if (e != null)
                {
                    return e;
                }
            }

            return null; 
        }
        
        public static MenuTemplate FindMenuElement(List<MenuTemplate> root,string id, Action<MenuTemplate, MenuTemplate> onFind = null)
        {
            foreach (var child in root)
            {
                if (child.menuGUID == id)
                {
                    onFind?.Invoke(child,null);
                    return child;
                }
                var e = child.FindMenuElement(id,onFind);
                if (e != null)
                {
                    return e;
                }
            }

            return null;
        }

        public void FindElement(Predicate<MenuTemplate> predicate,
            Action<MenuTemplate, MenuTemplate> onFind = null)
        {
            childs = childs.Distinct().ToList();
            foreach (var child in childs)
            {
                if (predicate.Invoke(child))
                {
                    onFind?.Invoke(child,this);
                }
            }
        }
        
        public static void FindElement(List<MenuTemplate> root,
            Predicate<MenuTemplate> predicate,
            Action<MenuTemplate, MenuTemplate> onFind = null)
        {
            foreach (var child in root)
            {
                if (predicate.Invoke(child))
                {
                    onFind?.Invoke(child,null);
                    child.FindElement(predicate,onFind);
                }
            }
        }

        public void DeleateOverlapElement(MenuTemplate origin)
        {
            childs = childs.Distinct().ToList();
            for (int i = 0; i < childs.Count; i ++ )
            {
                if(String.IsNullOrWhiteSpace(childs[i].menuGUID)) continue;
                if (childs[i].menuGUID == origin.menuGUID && childs[i] != origin)
                {
                    childs.Remove(childs[i]);
                    i--;
                    continue;
                }
                else
                {
                    childs[i].DeleateOverlapElement(origin);
                }
            }
        }

        public void DeleateNullMenuElement(List<MenuElement> datas)
        {
            childs = childs.Distinct().ToList();
            for (int i = 0; i < childs.Count; i ++ )
            {
                if (String.IsNullOrWhiteSpace(childs[i].menuGUID))
                {
                    childs[i].DeleateNullMenuElement(datas);
                }
                else
                {
                    var menu = datas.FirstOrDefault(e => e.guid == childs[i].menuGUID);
                    if (menu == null)
                    {
                        childs.Remove(childs[i]);
                        i--;
                        continue;
                    }
                    else
                    {
                        childs[i].icon = menu.icon;
                        childs[i].name = menu.name;
                        childs[i].DeleateNullMenuElement(datas);
                    }
                    // メニューは子を持たないので初期化
                    childs[i].childs = new List<MenuTemplate>();
                }
            }
        }
        
        public void DeleateAutoCreate()
        { 
            childs = childs.Distinct().ToList();
            for (int i = 0; i < childs.Count; i ++ )
            {
                if (!String.IsNullOrWhiteSpace(childs[i].menuGUID) && childs[i].autoCreate)
                {
                    childs.Remove(childs[i]);
                    i--;
                    continue;
                }
                else
                if (childs[i].childs.Count == 0 && childs[i].autoCreate)
                {
                    childs.Remove(childs[i]);
                    i--;
                    continue;
                }
                else
                {
                    childs[i].DeleateAutoCreate();
                }
            }
        }

        public void RecursionAutoCreateFalse()
        {
            autoCreate = false;
            foreach (var child in childs)
            {
                child.RecursionAutoCreateFalse();
            }
        }

        public static List<MenuTemplate> ReloadTemplates(List<MenuTemplate> menus,List<MenuElement> datas)
        {
            // メニュー参照切れ項目の削除
            foreach (var menu in menus)
            {
                menu.DeleateNullMenuElement(datas);
            }
            
            foreach (var data in datas)
            {
                var current = FindMenuElement(menus, data.guid);
                if (current == null)
                {
                    current = new MenuTemplate()
                    {
                        name = data.name,
                        icon = data.icon,
                        menuGUID = data.guid,
                        autoCreate = true,
                    };
                }
                else
                {
                    FindMenuElement(menus, data.guid, (e, p) =>
                    {
                        if (e.autoCreate)
                        {
                            p.childs.Remove(e);
                        }
                    });
                }
                
                if (current.autoCreate)
                {
                    var root = data.isToggle ? 
                        menus.FirstOrDefault(e => e.name == "Items" && e.autoCreate) :
                        menus.FirstOrDefault(e => e.name == data.layer.ToString() && e.autoCreate);
                    // 親オブジェクトの検知
                    MenuTemplate.FindElement(menus,e =>
                    {
                        var menu = datas.FirstOrDefault(m => m.guid == e.menuGUID);
                        if (menu == null) return false;
                        if (data.isToggle)
                        {
                            return e.autoCreate && menu.isToggle;
                        }
                        else
                        {
                            return e.autoCreate && !menu.isToggle && menu.layer == data.layer;
                        }
                    }, (e, p) =>
                    {
                        if (p != null)
                        {
                            root = p;
                        }
                    });
                    if (root == null)
                    {
                        if (data.isToggle)
                        {
                            root = new MenuTemplate()
                            {
                                name = "Items",
                                icon = data.icon,
                                autoCreate = true,
                            }; 
                        }
                        else
                        {
                            root = new MenuTemplate()
                            {
                                name = data.layer.ToString(),
                                icon = data.icon,
                                autoCreate = true,
                            };
                        }
                        menus.Add(root);
                    }
                    root.childs.Add(current);
                }
            }
            
            // root直下の処理
            for (int i = 0; i < menus.Count; i ++ )
            {
                if (String.IsNullOrWhiteSpace(menus[i].menuGUID) && menus[i].childs.Count == 0 && menus[i].autoCreate)
                {
                    menus.Remove(menus[i]);
                    i--;
                    continue;
                }

                if (!String.IsNullOrWhiteSpace(menus[i].menuGUID))
                {
                    var menu = datas.FirstOrDefault(e => e.guid == menus[i].menuGUID);
                    if (menu == null)
                    {
                        menus.Remove(menus[i]);
                        i--;
                        continue;
                    }
                }
            }
            return menus;
        }
    }

    [Serializable]
    public class LayerSettings
    {
        public bool isSaved = true;
        public bool isRandom = false;
        //public DefaultValue defaultValue;
        public string defaultElementGUID = "";


        public MenuElement GetDefaultElement(List<MenuElement> datas)
        {
            return datas.FirstOrDefault(d => d.guid == defaultElementGUID);
        }
        
        public void SetDefaultElement(MenuElement element)
        {
            if (element != null)
            {
                defaultElementGUID = element.guid;
            }
            else
            {
                defaultElementGUID = "";
            }
        }
    }
    
    [Serializable]
    public class MenuElement
    {
        public string name;
        public Texture2D icon;
        public List<ItemElement> activeItems = new List<ItemElement>();
        public List<ItemElement> SafeActiveItems()
        {
            return activeItems.Where(e => e.obj != null).ToList();
        }
        public List<ItemElement> UnSafeActiveItems()
        {
            return activeItems.Where(e => e.obj == null).ToList();
        }
        
        public List<ItemElement> inactiveItems = new List<ItemElement>();
        public List<ItemElement> SafeInactiveItems()
        {
            return inactiveItems.Where(e => e.obj != null).ToList();
        }
        public List<ItemElement> UnSafeInactiveItems()
        {
            return inactiveItems.Where(e => e.obj == null).ToList();
        }
        
        public bool isToggle = true;
        public LayerGroup layer = LayerGroup.Layer_A;
        public bool isSaved = true;
        public bool isDefault = false;
        public bool isRandom = false;
        public bool isTaboo = false;
        public string param = "";
        public int value = 0;
        public string guid;
        
        public List<SyncElement> activeSyncElements = new List<SyncElement>();
        public List<SyncElement> inactiveSyncElements = new List<SyncElement>();

        public bool extendOverrides = false;
        public bool isOverrideActivateTransition = false;
        public ItemElement overrideActivateTransition = new ItemElement();
        public bool isOverrideInactivateTransition = false;
        public ItemElement overrideInactivateTransition = new ItemElement();

        public MenuElement()
        {
            guid = System.Guid.NewGuid().ToString();
        }

        public string DefaultIcon()
        {
            return isToggle ? EnvironmentGUIDs.itemIcon :
                layer == LayerGroup.Layer_A ? EnvironmentGUIDs.dressAIcon :
                layer == LayerGroup.Layer_B ? EnvironmentGUIDs.dressBIcon :
                layer == LayerGroup.Layer_C ? EnvironmentGUIDs.dressCIcon :
                EnvironmentGUIDs.itemboxIcon;
        }

        public void SetToDefaultIcon()
        {
            icon = AssetUtility.LoadAssetAtGuid<Texture2D>(DefaultIcon());
        }

        public bool IsDefaultIcon()
        {
            return AssetDatabase.GetAssetPath(icon) == AssetDatabase.GUIDToAssetPath(DefaultIcon());
        }

        public void SetLayer(bool t)
        {
            if (IsDefaultIcon())
            {
                isToggle = t;
                SetToDefaultIcon();
            }
            else
            {
                isToggle = t;
            }
        }
        public void SetLayer(LayerGroup l)
        {
            if (IsDefaultIcon())
            {
                layer = l;
                SetToDefaultIcon();
            }
            else
            {
                layer = l;
            }
        }
    }

    [Serializable]
    public class SyncElement
    {
        public string guid;
        public float delay;
        public bool syncOn;
        public bool syncOff;

        public SyncElement(string id)
        {
            guid = id;
            delay = -1f;
            syncOn = false;
            syncOff = false;
        }
    }
    
    [Serializable]
    public class ItemElement
    {
        public string path;
        public GameObject obj;
        public bool active = true;
        public FeedType type = FeedType.None;
        public float delay = 0f;
        public float duration = 1f;

        //public Shader animationShader;
        public Material animationMaterial;
        public string animationParam = "_AnimationTime";
        public float animationParamOff = 0f;
        public float animationParamOn = 1f;

        public bool extendOption = false;
        public List<RendererOption> rendOptions = new List<RendererOption>();

        public ItemElement()
        {
        }

        public ItemElement(GameObject o,GameObject root = null,bool defaultActive = true)
        {
            obj = o;
            active = defaultActive;

            rendOptions = o?.GetComponentsInChildren<Renderer>().Select(r => new RendererOption(r, o)).ToList();
            
            if(root) GetRelativePath(root);
        }
        
        public ItemElement Clone(bool invert = false)
        {
            return new ItemElement(obj)
            {
                active = invert ? !active : active,
                path = path,
                type = type,
                delay = delay,
                duration = duration,
                animationMaterial = animationMaterial,
                animationParam = animationParam,
                rendOptions = rendOptions.Select(r=>r.Clone(obj,invert)).ToList(),
            };
        }
        
        public void GetRelativePath(GameObject root)
        {
            if (!obj) return;
            if (obj == root)
            {
                path = "";
            }
            else
            {
                path = obj.gameObject.name;
                Transform parent = obj.transform.parent;
                while (parent != null)
                {
                    if(parent.gameObject == root) break;
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
            }
        }
        
        public void GetRelativeGameobject(Transform root)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                obj = null;
                ReloadRendOption();
                return;
            }
            
            root = root.Find(path);
            if (root == null)
            {
                obj = null;
                ReloadRendOption();
                return;
            }
            
            obj = root.gameObject;
            
            ReloadRendOption();
        }

        public void ReloadRendOption()
        {
            if (obj == null)
            {
            }
            else
            {
                var currentRendOptions = rendOptions;
                var newRendOptions = obj.GetComponentsInChildren<Renderer>().Select(r => new RendererOption(r, obj)).ToList();
                for (int i = 0; i < newRendOptions.Count; i++)
                {
                    var currentRendOption = currentRendOptions.FirstOrDefault(r => r.path == newRendOptions[i].path);
                    if (currentRendOption!=null)
                    {
                        // 設定飛んでそうだったら，破棄
                        currentRendOption.GetRelativeGameobject(obj.transform);
                        if (currentRendOption.rend == newRendOptions[i].rend &&
                            currentRendOption.changeMaterialsOptions.Count ==
                            newRendOptions[i].changeMaterialsOptions.Count &&
                            currentRendOption.changeBlendShapeOptions.Count ==
                            newRendOptions[i].changeBlendShapeOptions.Count)
                        {
                            newRendOptions[i] = currentRendOption;
                        }
                    }
                }
                rendOptions = newRendOptions;
            }
        }
    }

    [Serializable]
    public class RendererOption
    {
        public bool rendActive = true;
        public string path;
        public Renderer rend;
        public bool extendOption { get; set; }
        // public bool extendMaterialOption = false;
        // public bool extendBlendShapeOption = false;
        public List<MaterialOption> changeMaterialsOptions = new List<MaterialOption>();
        public List<BlendShapeOption> changeBlendShapeOptions = new List<BlendShapeOption>();

        public RendererOption(Renderer r, GameObject root)
        {
            rend = r;
            GetRelativePath(root);
            if (r)
            {
                changeMaterialsOptions = r.sharedMaterials.Select(m => new MaterialOption(m)).ToList();
                if (r is SkinnedMeshRenderer)
                {
                    changeBlendShapeOptions = Enumerable.Range(0, r.GetMesh().blendShapeCount).Select(i =>
                        new BlendShapeOption((r as SkinnedMeshRenderer).GetBlendShapeWeight(i))).ToList();
                }
            }
        }
        public RendererOption Clone(GameObject root,bool invert = false)
        {
            var clone = new RendererOption(rend, root);
            clone.changeMaterialsOptions = changeMaterialsOptions.Select(e => e.Clone()).ToList();
            clone.changeBlendShapeOptions = changeBlendShapeOptions.Select(e=> e.Clone()).ToList();
            if (invert)
            {
                for (int i = 0; i < changeMaterialsOptions.Count; i++)
                {
                    clone.changeMaterialsOptions[i].material = rend.sharedMaterials[i];
                }

                if (rend is SkinnedMeshRenderer)
                {
                    for (int i = 0; i < changeBlendShapeOptions.Count; i++)
                    {
                        clone.changeBlendShapeOptions[i].weight = (rend as SkinnedMeshRenderer).GetBlendShapeWeight(i);
                    }
                }
            }

            return clone;
        }
        
        public void GetRelativePath(GameObject root)
        {
            if (!rend) return;
            if (rend.gameObject == root)
            {
                path = "";
            }
            else
            {
                path = rend.gameObject.name;
                Transform parent = rend.transform.parent;
                while (parent != null)
                {
                    if(parent.gameObject == root) break;
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
            }
        }
        
        public void GetRelativeGameobject(Transform root)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                root = null;
                return;
            }
            
            root = root.Find(path);
            if (root == null)
            {
                rend = null;
                return;
            }
            
            rend = root.gameObject.GetComponent<Renderer>();
        }
    }

    [Serializable]
    public class MaterialOption
    {
        public bool change = false;
        public Material material;
        public float delay = -1f;
        // public float duration = 1f;

        public MaterialOption(Material mat)
        {
            material = mat;
        }

        public MaterialOption Clone()
        {
            var clone = new MaterialOption(material);
            clone.change = change;
            clone.delay = delay;
            // clone.duration = duration;
            return clone;
        }
    }

    [Serializable]
    public class BlendShapeOption
    {
        public bool change = false;
        public float weight;
        public float delay = -1f;
        public float duration = 1f;

        public BlendShapeOption(float w)
        {
            weight = w;
        }
        
        public BlendShapeOption Clone()
        {
            var clone = new BlendShapeOption(weight);
            clone.change = change;
            clone.delay = delay;
            clone.duration = duration;
            return clone;
        }
    }

    public static class AssetLink
    {
        public static Material GetMaterialByType(this FeedType type)
        {
            if (type == FeedType.Fade)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.fadeMaterial);
            }
            if (type == FeedType.Crystallize)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.crystallizeMaterial);
            }
            if (type == FeedType.Dissolve)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.disolveMaterial);
            }
            if (type == FeedType.Draw)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.drawMaterial);
            }
            if (type == FeedType.Explosion)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.explosionMaterial);
            }
            if (type == FeedType.Geom)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.geomMaterial);
            }
            if (type == FeedType.Mosaic)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.mosaicMaterial);
            }
            if (type == FeedType.Polygon)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.polygonMaterial);
            }
            if (type == FeedType.Bounce)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.scaleMaterial);
            }
            if (type == FeedType.Leaf)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.leafMaterial);
            }
            if (type == FeedType.Bloom)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.bloomMaterial);
            }

            return null;
        }

        public static FeedType GetTypeByMaterial(this Material mat)
        {
            var guid = AssetUtility.GetAssetGuid(mat);
            if (guid == EnvironmentGUIDs.fadeMaterial)
            {
                return FeedType.Fade;
            }
            if (guid == EnvironmentGUIDs.crystallizeMaterial)
            {
                return FeedType.Crystallize;
            }
            if (guid == EnvironmentGUIDs.disolveMaterial)
            {
                return FeedType.Dissolve;
            }
            if (guid == EnvironmentGUIDs.drawMaterial)
            {
                return FeedType.Draw;
            }
            if (guid == EnvironmentGUIDs.explosionMaterial)
            {
                return FeedType.Explosion;
            }
            if (guid == EnvironmentGUIDs.geomMaterial)
            {
                return FeedType.Geom;
            }
            if (guid == EnvironmentGUIDs.mosaicMaterial)
            {
                return FeedType.Mosaic;
            }
            if (guid == EnvironmentGUIDs.polygonMaterial)
            {
                return FeedType.Polygon;
            }
            if (guid == EnvironmentGUIDs.scaleMaterial)
            {
                return FeedType.Bounce;
            }
            if (guid == EnvironmentGUIDs.leafMaterial)
            {
                return FeedType.Leaf;
            }
            if (guid == EnvironmentGUIDs.bloomMaterial)
            {
                return FeedType.Bloom;
            }

            return FeedType.None;
        }
            
    }
    
    public enum FeedType
    {
        None,
        Scale,
        Shader,
        Fade,
        Crystallize,
        Dissolve,
        Draw,
        Explosion,
        Geom,
        Mosaic,
        Polygon,
        Bounce,
        Leaf,
        Bloom,
    }

    public enum LayerGroup : int
    {
        Layer_A,
        Layer_B,
        Layer_C,
        Layer_D,
        Layer_E,
        Layer_F,
        Layer_G,
        Layer_H,
        Layer_I,
        Layer_J,
    }

    public enum ToggleGroup
    {
        IsToggle,
    }

    public class MenuTemplateTreeView : TreeView
    {
        private MagicalDresserInventorySaveData data;
        public event Action<MenuElement> OnSelect;
        public MenuTemplateTreeView(TreeViewState state) : base(state)
        {
            showAlternatingRowBackgrounds = true;
        }

        public MenuTemplateTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            showAlternatingRowBackgrounds = true;
        }
        
        protected override void RowGUI (RowGUIArgs args)
        {
            var isFolder = args.item == null ? true : args.item?.id == 0 ?
                true : String.IsNullOrWhiteSpace(MenuTemplate.FIndTemplateElement(data.menuTemplate, args.item.id)?.menuGUID ?? "");
            
            Rect rect = args.rowRect;
            rect.x += GetContentIndent(args.item);
            
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            // textStyle.alignment = TextAnchor.MiddleCenter;
            // textStyle.fontSize = fontSize;
            // if ((args.item as MenuTemplateTreeViewItem).isFolder)
            if(isFolder)
            {
                textStyle.fontStyle = FontStyle.Bold;
                /*textStyle.active = new GUIStyleState()
                {
                    textColor = Color.gray
                };*/
            }
            else
            {
                textStyle.fontStyle = FontStyle.Italic;
                /*textStyle.active = new GUIStyleState()
                {
                    textColor = Color.red
                };*/
            }
            
            EditorGUI.LabelField(rect,args.item.displayName,textStyle);
            //toggleRect.width                = 16f;
            //GUI.DrawTexture(toggleRect, texture);
            //base.RowGUI(args);
        }
        
        protected override void SelectionChanged (IList<int> selectedIds)
        {
            if (selectedIds.Count > 0)
            {
                var template = MenuTemplate.FIndTemplateElement(data.menuTemplate, selectedIds[0]);
                var menu = data.menuElements.FirstOrDefault(e => e.guid == template.menuGUID);
                OnSelect?.Invoke(menu);
            }
        }
        
        public List<MenuTemplate> GetSelectTemplates()
        {
            var templates = new List<MenuTemplate>();
            var selectedIds = GetSelection();
            if (selectedIds.Count > 0)
            {
                var template = MenuTemplate.FIndTemplateElement(data.menuTemplate, selectedIds[0]);
                if (template != null)
                {
                    templates.Add(template);
                }
            }

            return templates;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = MenuTemplateTreeViewItem.CreateFolder(0,-1,"Root");
            if (data.menuTemplate.Count == 0)
            {
                root.AddChild(MenuTemplateTreeViewItem.CreateMenu(1,0,"Null"));
            }
            else
            {
                foreach (var menu in data.menuTemplate)
                {
                    root.AddChild(GetTreeElement(menu));
                }
            }
            return root;
        }

        protected override bool CanStartDrag(TreeView.CanStartDragArgs args)
        {
            if (data.menuTemplate.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = new UnityEngine.Object[] {};
            DragAndDrop.SetGenericData("MenuTemplateTreeViewItem", new List<int>(args.draggedItemIDs));
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag("MenuTemplateTreeView");
        }
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;

            var draggedIDs = DragAndDrop.GetGenericData("MenuTemplateTreeViewItem") as List<int>;
            if (draggedIDs != null && draggedIDs.Count > 0)
            {
                visualMode = DragAndDropVisualMode.Move;
                if (args.performDrop)
                {
                    foreach (var draggedID in draggedIDs)
                    {
                        var parent = args.parentItem == null ? null : args.parentItem?.id == 0 ?
                                null :
                                MenuTemplate.FIndTemplateElement(data.menuTemplate, args.parentItem.id);
                        // メニューの子にメニューを入れない
                        if (parent != null)
                            if (!String.IsNullOrWhiteSpace(parent.menuGUID))
                                continue;
                        
                        var element = MenuTemplate.FIndTemplateElement(data.menuTemplate, draggedID, (e, p) =>
                        {
                            e.RecursionAutoCreateFalse();
                            if (p == null)
                            {
                                data.menuTemplate.Remove(e);
                            }
                            else
                            {
                                p.childs.Remove(e);
                            }
                            //args.parentItem.children.Remove(args.parentItem.children.FirstOrDefault(c => c.id == draggedID));
                        });
                        
                        if (element != null)
                        {
                            int id = args.insertAtIndex;
                            if (parent == null)
                            {
                                if (id < 0 || id > data.menuTemplate.Count)
                                {
                                    data.menuTemplate.Add(element);
                                }
                                else
                                {
                                    data.menuTemplate.Insert(id,element);
                                }
                            }
                            else
                            {
                                if (id < 0 || id > parent.childs.Count)
                                {
                                    parent.childs.Add(element);
                                }
                                else
                                {
                                    parent.childs.Insert(id,element);
                                }
                            }
                        }
                    }

                    ReloadTemplates();
                }
            }
            return visualMode;
        }
        
        protected override bool CanRename(TreeViewItem item)
        {
            return true;
            return item.displayName.Length <= 10;
        }
        
        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (args.acceptedRename)
            {
                var template = MenuTemplate.FIndTemplateElement(data.menuTemplate, args.itemID);
                if (template != null)
                {
                    template.name = args.newName;
                    if (!String.IsNullOrWhiteSpace(template.menuGUID))
                    {
                        var menu = data.menuElements.FirstOrDefault(e => e.guid == template.menuGUID);
                        if (menu != null)
                        {
                            menu.name = args.newName;
                        }
                    }
                    template.RecursionAutoCreateFalse();
                }
                ReloadTemplates();
            }
        }

        public void Setup(MagicalDresserInventorySaveData d)
        {
            data = d;
            ReloadTemplates();
        }

        public void ReloadTemplates()
        {
            data.ReloadTemplates();
            ReloadItemIcons(rootItem);
            Reload();
        }

        public void ReloadItemIcons(TreeViewItem parent)
        {
            if(parent==null) return;
            if(parent.children==null) return;
            foreach (var item in parent.children)
            {
                if (item.id != 1)
                {
                    var template = MenuTemplate.FIndTemplateElement(data.menuTemplate, item.id);
                    if (template == null)
                    {
                        // parent.children.Remove(item);
                    }
                    else
                    {
                        if (String.IsNullOrWhiteSpace(template.menuGUID))
                        {
                            item.displayName = template.name;
                            item.icon = template.icon;
                            ReloadItemIcons(item);
                        }
                        else
                        {
                            var menu = data.menuElements.FirstOrDefault(e => e.guid == template.menuGUID);
                            if (menu == null)
                            {
                                // parent.children.Remove(item);
                            }
                            else
                            {
                                item.displayName = menu.name;
                                item.icon = menu.icon;
                            }
                        }
                    }
                }
                
            }
        }

        MenuTemplateTreeViewItem GetTreeElement(MenuTemplate template,int depth = 0)
        {
            if (template.childs.Count == 0)
            {
                var root = MenuTemplateTreeViewItem.CreateMenu(template.GetGuid(),depth,template.name,template.icon);
                return root;
            }
            else
            {
                var root = MenuTemplateTreeViewItem.CreateFolder(template.GetGuid(),depth,template.name,template.icon);
                foreach (var menu in template.childs)
                {
                    root.AddChild(GetTreeElement(menu,depth+1));
                }
                return root;
            }
        }

        class MenuTemplateTreeViewItem : TreeViewItem
        {
            public bool isFolder = true;

            public static MenuTemplateTreeViewItem CreateFolder(int id, int depth, string name, Texture2D icon = null)
            {
                return new MenuTemplateTreeViewItem()
                {
                    id = id,
                    depth = depth,
                    displayName = name,
                    icon = icon,
                    isFolder = true
                };
            }
            public static MenuTemplateTreeViewItem CreateMenu(int id, int depth, string name, Texture2D icon = null)
            {
                return new MenuTemplateTreeViewItem()
                {
                    id = id,
                    depth = depth,
                    displayName = name,
                    icon = icon,
                    isFolder = false
                };
            }

        }
    }
}
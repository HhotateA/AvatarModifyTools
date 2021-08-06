using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using HhotateA.AvatarModifyTools.Core;
using UnityEngine.Serialization;

namespace HhotateA.AvatarModifyTools.MagicDresser
{
    public class MagicDresserSaveData : ScriptableObject
    {
        public string name = "MagicDresser";
        public Texture2D icon = null;
        public List<MenuElement> menuElements = new List<MenuElement>();
        public Dictionary<LayerGroup, LayerSettings> layerSettingses;

        public MagicDresserSaveData()
        {
            layerSettingses = new Dictionary<LayerGroup, LayerSettings>();
            foreach (LayerGroup layer in Enum.GetValues(typeof(LayerGroup)))
            {
                layerSettingses.Add(layer,new LayerSettings());
            }
        }

        public void ApplyRoot(GameObject root)
        {
            foreach (var menuElement in menuElements)
            {
                foreach (var item in menuElement.activeItems)
                {
                    item.GetRelativeGameobject(root.transform);
                }
                foreach (var item in menuElement.inactiveItems)
                {
                    item.GetRelativeGameobject(root.transform);
                }
            }
        }
        public void ApplyPath(GameObject root)
        {
            foreach (var menuElement in menuElements)
            {
                foreach (var item in menuElement.activeItems)
                {
                    item.GetRelativePath(root);
                }
                foreach (var item in menuElement.inactiveItems)
                {
                    item.GetRelativePath(root);
                }
            }
        }
    }

    [Serializable]
    public class LayerSettings
    {
        public bool isSaved = true;
        public DefaultValue defaultValue;
        public MenuElement defaultElement;

        public enum DefaultValue
        {
            Default,
            Element,
            AllOn,
            AllOff,
            Nothing
        }
    }
    
    [Serializable]
    public class MenuElement
    {
        public string name;
        public Texture2D icon;
        public List<ItemElement> activeItems = new List<ItemElement>();
        public List<ItemElement> inactiveItems = new List<ItemElement>();
        public bool isToggle = true;
        public LayerGroup layer = LayerGroup.A;
        public bool isSaved = true;
        public bool isDefault = true;
        public string param = "";
        public int value = 0;
    }
    
    [Serializable]
    public class ItemElement
    {
        public bool active = true;
        public GameObject obj;
        public string path;
        public FeedType type = FeedType.None;
        public float delay = 0f;
        public float duration = 1f;
        public Shader animationShader;
        public string animationParam = "_AnimationTime";

        public ItemElement(GameObject o)
        {
            obj = o;
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
                animationShader = animationShader,
                animationParam = animationParam,
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
            if (String.IsNullOrWhiteSpace(path)) return;
            var cs = path.Split('/');
            foreach (var c in cs)
            {
                root = root.FindInChildren(c);
            }

            obj = root.gameObject;
        }
    }

    public enum FeedType
    {
        None,
        Scale,
        Shader,
        Feed,
        Draw,
        Geom
    }

    public enum LayerGroup
    {
        A,
        B,
        C
    }
}
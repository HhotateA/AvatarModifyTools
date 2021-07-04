using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using JetBrains.Annotations;
using UnityEngine.Serialization;

namespace HhotateA
{
    public class LayersSaveData : ScriptableObject
    {
        [SerializeField] List<LayerData> layers = new List<LayerData>();

        public List<LayerData> Layers => layers;
        
        public class MaterialData
        {
            public RenderTexture texture;
            public Vector4 tilling;
            public BlendMode mode;
            public Color color;
            public Color comparison;
            public Vector4 setting;
            public int mask;
        }

        public void AddLayer(RenderTexture tex)
        {
            layers.Add(new LayerData(tex));
        }

        public void DeleateLayer(int index)
        {
            layers.RemoveAt(index);
        }

        public int LayerCount()
        {
            return layers.Count;
        }

        public MaterialData GetLayer(int index)
        {
            if (index < layers.Count)
            {
                var l = layers[index];
                var ld = new MaterialData()
                {
                    texture = l.Texture,
                    tilling = new Vector4(l.scale.x,l.scale.y,l.offset.x,l.offset.y),
                    mode = l.layerMode,
                    color = l.color,
                    comparison = l.comparison,
                    mask = -1,
                };
                if (l.active == false)
                {
                    ld.mode = BlendMode.Disable;
                }

                if (ld.mode == BlendMode.HSV)
                {
                    ld.setting = l.settings;
                }
                else
                if(ld.mode == BlendMode.Color)
                {
                    ld.setting = new Vector4(1f, 0f, l.settings.z, l.settings.w);
                }
                else
                if(ld.mode == BlendMode.Bloom)
                {
                    ld.setting = new Vector4(1f/(float)l.Texture.width, 1f/(float)l.Texture.height, l.settings.z, l.settings.w); 
                }

                if (l.isMask)
                {
                    ld.mask = 1;
                }
                return ld;
            }

            return null;
        }

        public string GetLayerName(int index)
        {
            return layers[index].name;
        }
        public bool GetLayerActive(int index)
        {
            return layers[index].active;
        }

        public void SetLayer(int index,string newName,bool? isActive,BlendMode? newMode,bool? isMask)
        {
            if (index < layers.Count)
            {
                layers[index].name = newName ?? layers[index].name;
                layers[index].active = isActive ?? layers[index].active;
                layers[index].layerMode = newMode ?? layers[index].layerMode;
                layers[index].isMask = isMask ?? layers[index].isMask;
            }
        }

        public void ReplaceLayer(int from,int to)
        {
            if (from < layers.Count && to < layers.Count)
            {
                var l = layers[from];
                layers[from] = layers[to];
                layers[to] = l;
            }
        }
        
        public bool LayerButton(int index,bool disableButton = false)
        {
            var l = layers[index];

            using (new EditorGUI.DisabledScope(disableButton))
            {
                if (GUILayout.Button(l.Texture, GUILayout.Width(60), GUILayout.Height(60)))
                {
                    return true;
                }
            }

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(30)))
            {
                l.layerMode = (BlendMode) EditorGUILayout.EnumPopup("", l.layerMode, GUILayout.Width(75));
                l.scale = EditorGUILayout.Vector2Field("", l.scale, GUILayout.Width(75));
                l.offset = EditorGUILayout.Vector2Field("", l.offset, GUILayout.Width(75));
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("isMask",GUILayout.Width(50));
                    l.isMask = EditorGUILayout.Toggle("", l.isMask, GUILayout.Width(25));
                }
            }

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(30)))
            {
                if (l.layerMode == BlendMode.HSV)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("H", GUILayout.Width(20));
                    var h = GUILayout.HorizontalSlider(l.settings.x, -1f, 1f, GUILayout.Width(130));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("S", GUILayout.Width(20));
                    var s = GUILayout.HorizontalSlider(l.settings.y, -1f, 1f, GUILayout.Width(130));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("V", GUILayout.Width(20));
                    var v = GUILayout.HorizontalSlider(l.settings.z, -1f, 1f, GUILayout.Width(130));
                    EditorGUILayout.EndHorizontal();
                    l.settings = new Vector4(h,s,v,l.settings.w);
                }
                else
                if(l.layerMode == BlendMode.Color)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("From", GUILayout.Width(50));
                    l.comparison = EditorGUILayout.ColorField( l.comparison, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("To", GUILayout.Width(50));
                    l.color = EditorGUILayout.ColorField( l.color, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Brightness", GUILayout.Width(30));
                    var z = GUILayout.HorizontalSlider(l.settings.z, 0f, 1f, GUILayout.Width(120));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Threshold", GUILayout.Width(30));
                    var w = GUILayout.HorizontalSlider(l.settings.w, 0f, 2f, GUILayout.Width(120));
                    EditorGUILayout.EndHorizontal();
                    l.settings = new Vector4(l.settings.x,l.settings.y,z,w);
                }
                else
                if(l.layerMode == BlendMode.Bloom)
                {
                    l.color = EditorGUILayout.ColorField(new GUIContent(), l.color, true, true, true,  GUILayout.Width(130));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Intensity", GUILayout.Width(30));
                    var z = GUILayout.HorizontalSlider(l.settings.z, 1f, 10f, GUILayout.Width(120));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Threshold", GUILayout.Width(30));
                    var w = GUILayout.HorizontalSlider(l.settings.w, 0f, 2f, GUILayout.Width(120));
                    EditorGUILayout.EndHorizontal();
                    l.settings = new Vector4(l.settings.x,l.settings.y,z,w);
                }
                else
                {
                    l.color = EditorGUILayout.ColorField("", l.color, GUILayout.Width(150));
                }
                //l.colorChange = EditorGUILayout.Slider("", l.colorChange, 0.001f, 2f);
            }
            
            return false;
        }
        
        public void SaveTextures(string path)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                layers[i].SaveTexture(path);
            }
        }
    }

    [System.SerializableAttribute]
    public class LayerData
    {
        public RenderTexture Texture
        {
            get
            {
                if (texture == null && origin != null)
                {
                    var t = TexturePainter.Bytes2Texture(origin);
                    texture = TexturePainter.GetReadableTexture(t);
                }
                return texture;
            }
        }

        public string name = "";
        public RenderTexture texture;
        private byte[] origin;
        public Vector2 scale = new Vector2(1,1);
        public Vector2 offset = new Vector2(0,0);
        public bool active = true;
        public BlendMode layerMode = BlendMode.Normal;
        public Color color = Color.white;
        public Color comparison = Color.black;
        public Vector4 settings = Vector4.zero;
        public bool isMask = false;

        public LayerData(RenderTexture rt)
        {
            texture = rt;
            name = rt.name;
        }

        public void SaveTexture(string path)
        {
            origin = TexturePainter.Texture2Bytes(texture);
        }
    }

    public enum BlendMode
    {
        Disable,
        Normal,
        Additive,
        Multiply,
        Subtraction,
        Division,

        Bloom,
        HSV,
        Color,
        AlphaMask,
        
        Override,
    };
}
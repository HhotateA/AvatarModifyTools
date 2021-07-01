using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

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
                    comparison = l.comparison
                };
                if (l.active == false)
                {
                    ld.mode = BlendMode.disable;
                }

                if (ld.mode == BlendMode.hsv)
                {
                    ld.setting = l.settings;
                }
                else
                if(ld.mode == BlendMode.color)
                {
                    ld.setting = new Vector4(1f, 0f, l.settings.z, l.settings.w);
                }
                else
                if(ld.mode == BlendMode.bloom)
                {
                    ld.setting = new Vector4(1f/(float)l.Texture.width, 1f/(float)l.Texture.height, l.settings.z, l.settings.w); 
                }
                return ld;
            }

            return null;
        }

        public void SetLayer(int index,bool? isActive,BlendMode? newMode)
        {
            if (index < layers.Count)
            {
                layers[index].active = isActive ?? layers[index].active;
                layers[index].layerMode = newMode ?? layers[index].layerMode;
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
        
        public bool LayerButton(int index,bool disableButton = false, bool deleateButton = false,Action<int> indexChenge = null)
        {
            var l = layers[index];
            using (var block = new EditorGUILayout.HorizontalScope(GUILayout.Width(30)))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(30)))
                {
                    if (GUILayout.Button("↑", GUILayout.Width(30)))
                    {
                        ReplaceLayer(index, index + 1);
                        indexChenge?.Invoke(index + 1);
                    }

                    using (new EditorGUILayout.HorizontalScope(GUILayout.Width(30)))
                    {
                        l.active = EditorGUILayout.Toggle("", l.active, GUILayout.Width(10));
                        if (deleateButton)
                        {
                            if (GUILayout.Button("×", GUILayout.Width(20)))
                            {
                                DeleateLayer(index);
                                indexChenge?.Invoke(-1);
                            }
                        }
                    }

                    if (GUILayout.Button("↓", GUILayout.Width(30)))
                    {
                        ReplaceLayer(index, index - 1);
                        indexChenge?.Invoke(index - 1);
                    }
                }

                EditorGUI.BeginDisabledGroup(disableButton);
                if (GUILayout.Button(l.Texture, GUILayout.Width(60), GUILayout.Height(60)))
                {
                    return true;
                }

                EditorGUI.EndDisabledGroup();

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(30)))
                {
                    l.layerMode = (BlendMode) EditorGUILayout.EnumPopup("", l.layerMode, GUILayout.Width(75));
                    l.scale = EditorGUILayout.Vector2Field("", l.scale, GUILayout.Width(75));
                    l.offset = EditorGUILayout.Vector2Field("", l.offset, GUILayout.Width(75));
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(30)))
                {
                    if (l.layerMode == BlendMode.hsv)
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
                    if(l.layerMode == BlendMode.color)
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
                    if(l.layerMode == BlendMode.bloom)
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
            }
            return false;
        }
        
        public void SetLayerActive(int index,bool? val = null)
        {
            layers[index].active = val ?? !layers[index].active;
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
        public RenderTexture texture;
        private byte[] origin;
        public Vector2 scale = new Vector2(1,1);
        public Vector2 offset = new Vector2(0,0);
        public bool active = true;
        public BlendMode layerMode = BlendMode.normal;
        public Color color = Color.white;
        public Color comparison = Color.black;
        public Vector4 settings = Vector4.zero;

        public LayerData(RenderTexture rt)
        {
            texture = rt;
        }

        public void SaveTexture(string path)
        {
            origin = TexturePainter.Texture2Bytes(texture);
        }
    }

    public enum BlendMode
    {
        disable,
        normal,
        additive,
        multiply,
        subtraction,
        division,

        bloom,
        hsv,
        color,
        alphaMask,
    };
}
using HhotateA.AvatarModifyTools.Core;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Linq;
using System.Runtime.InteropServices;

namespace HhotateA.AvatarModifyTools.TextureModifyTool
{
    public class TexturePainter
    {
        private LayersSaveData layerSaveData;
        private CustomRenderTexture targetTexture;
        private CustomRenderTexture previewTexture;
        private Material targetMaterial;
        private Material previewMaterial;
        private float previewScale = 1f;
        Vector2 previewPosition = new Vector2(0.5f,0.5f);
        private Rect rect;
        private int editIndex = -1;

        ComputeShader GetComputeShader()
        {
            var computePath = AssetDatabase.GUIDToAssetPath("8e33ed767aaabf04eae3c3866bece392");
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(computePath);
            return compute;
        }

        public Texture GetTexture()
        {
            return targetTexture;
        }

        // 最大キャッシュ数
        private const int maxCaches = 16;
        private List<RenderTexture> caches = new List<RenderTexture>();
        private int cashIndex = -1;
        
        public RenderTexture GetEditTexture()
        {
            return layerSaveData.GetLayer(editIndex).texture;
        }

        public RenderTexture GetTemporaryTexture()
        {
            RenderTexture maskTexture = RenderTexture .GetTemporary(GetEditTexture().width, GetEditTexture().height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            maskTexture.enableRandomWrite = true;
            maskTexture.Create();
            var currentRT = RenderTexture.active;
            Graphics.Blit(targetTexture, maskTexture);
            RenderTexture.active = currentRT;
            return maskTexture;
        }
        
        public void AddCash()
        {
            var cash = RenderTexture.Instantiate(GetEditTexture());
            var currentRT = RenderTexture.active;
            Graphics.Blit(GetEditTexture(),cash);
            RenderTexture.active = currentRT;
            while (cashIndex+1 < caches.Count)
            {
                caches[caches.Count-1].Release();
                caches[caches.Count-1].DiscardContents();
                caches.RemoveAt(caches.Count-1);
            }
            if (caches.Count > maxCaches)
            {
                caches[1].Release();
                caches[1].DiscardContents();
                caches.RemoveAt(1);
            }
            caches.Add(cash);
            cashIndex = caches.Count - 1;
        }
        

        public bool CanUndo()
        {
            return cashIndex > 0;
        }
        public void UndoEditTexture()
        {
            var currentRT = RenderTexture.active;
            Graphics.Blit(caches[cashIndex-1],GetEditTexture());
            RenderTexture.active = currentRT;
            cashIndex--;
        }

        public bool CanRedo()
        {
            return cashIndex < caches.Count-1;
        }
        public void RedoEditTexture()
        {
            var currentRT = RenderTexture.active;
            Graphics.Blit(caches[cashIndex+1],GetEditTexture());
            RenderTexture.active = currentRT;
            cashIndex++;
        }
        public void ResetCashes()
        {
            foreach (var cache in caches)
            {
                cache.Release();
            }
            caches = new List<RenderTexture>();
            cashIndex = -1;
        }

        public int LayerCount()
        {
            return layerSaveData.LayerCount();
        }
        
        public TexturePainter(Texture baselayer)
        {
            layerSaveData = ScriptableObject.CreateInstance<LayersSaveData>();
            if (baselayer != null)
            {
                ResizeTexture(baselayer.width, baselayer.height);
                AddLayer(baselayer);
            }
        }

        public void ResizeTexture(int width, int height)
        {
            if(targetTexture) targetTexture.Release();
            targetTexture = new CustomRenderTexture(width,height);
            targetMaterial = new Material(Shader.Find("HhotateA/TexturePainter"));
            targetTexture.initializationSource = CustomRenderTextureInitializationSource.Material;
            targetTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
            targetTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
            /*targetTexture.initializationMode = CustomRenderTextureUpdateMode.Realtime;
            targetTexture.updateMode = CustomRenderTextureUpdateMode.Realtime;*/
            targetTexture.initializationMaterial = targetMaterial;
            targetTexture.material = targetMaterial;
            targetTexture.Create();
            targetTexture.Initialize();
            SyncLayers();
        }

        public void AddLayer(Texture tex = null)
        {
            if(layerSaveData.LayerCount() > 16) return;
            if (tex == null)
            {
                var rt = new RenderTexture(targetTexture.width,targetTexture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                rt.enableRandomWrite = true;
                rt.Create();
                tex = rt;
                tex.name = "NewLayer" + LayerCount().ToString();
            }
            else
            {
                tex = TextureCombinater.GetReadableRenderTexture(tex);
            }

            layerSaveData.AddLayer(tex as RenderTexture);
            SyncLayers();
        }
        
        public void AddMask(Color col,Gradient gradient)
        {
            if(layerSaveData.LayerCount() > 16) return;
            var rt = new RenderTexture(targetTexture.width,targetTexture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            rt.enableRandomWrite = true;
            rt.Create();
            var tex = rt;
            
            ClearColor(tex,col,gradient);
            
            tex.name = "Mask" + LayerCount().ToString();
            layerSaveData.AddLayer(tex);
            layerSaveData.SetLayer(layerSaveData.LayerCount()-1,null,true,BlendMode.Override,true);
            SyncLayers();
        }

        public Texture SyncLayers()
        {
            for (int i = 0; i < 16; i++)
            {
                if (i < layerSaveData.LayerCount())
                {
                    var ld = layerSaveData.GetLayer(i);
                    targetMaterial.SetTexture("_Layer"+i,ld.texture);
                    targetMaterial.SetTextureScale("_Layer"+i,new Vector2(ld.tilling.x,ld.tilling.y));
                    targetMaterial.SetTextureOffset("_Layer"+i,new Vector2(ld.tilling.z,ld.tilling.w));
                    targetMaterial.SetInt("_Mode"+i, (int) ld.mode);
                    targetMaterial.SetColor("_Color"+i, ld.color);
                    targetMaterial.SetColor("_Comparison"+i, ld.comparison);
                    targetMaterial.SetVector("_Settings"+i, ld.setting);
                    targetMaterial.SetInt("_Mask"+i, ld.mask);
                }
                else
                {
                    targetMaterial.SetTexture("_Layer"+i,null);
                }
            }

            return targetTexture;
        }

        public void LayersUpdate()
        {
            SyncLayers();
            targetTexture.Initialize();
        }

        public void ChangeEditLayer(int index)
        {
            if (0 <= index && index < LayerCount())
            {
                editIndex = index;
            }
            ResetCashes();
            if (0 <= editIndex && editIndex < LayerCount())
            {
                AddCash();
            }
        }

        public void Display(int width, int height, bool moveLimit = true, int rotationDrag = 2, int positionDrag = 1)
        {
            //SyncLayers(); // じつは舞フレーム呼ぶ必要ない
            LayersUpdate();
            rect = GUILayoutUtility.GetRect(width, height, GUI.skin.box);
            EditorGUI.DrawPreviewTexture(rect, ScalePreview(width,height,moveLimit)); 
            var e = Event.current;

            if (rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag && e.button == rotationDrag)
                {
                }

                if (e.type == EventType.MouseDrag && e.button == positionDrag)
                {
                    previewPosition += new Vector2(-e.delta.x,e.delta.y) * previewScale * 0.002f;
                }


                if (e.type == EventType.ScrollWheel)
                {
                    var p = new Vector3(e.mousePosition.x - rect.x, rect.height - e.mousePosition.y + rect.y,1f);
                    var uv = new Vector2(p.x/rect.width,p.y/rect.height);
            
                    Vector2 previewUV = (uv 
                                         - new Vector2(0.5f, 0.5f))
                                        * previewScale
                                        + previewPosition;
                    if (moveLimit)
                    {
                        previewScale = Mathf.Clamp(previewScale + e.delta.y * 0.01f,0.05f,1f);
                    }
                    else
                    {
                        previewScale = Mathf.Clamp(previewScale + e.delta.y * 0.01f,0.05f,2.5f);
                    }
                    
                    previewPosition = previewUV - (uv - new Vector2(0.5f, 0.5f))*previewScale;
                }
            }
        }

        public bool IsInDisplay(Vector2 pos)
        {
            return rect.Contains(pos);
        }
        
        CustomRenderTexture ScalePreview(int width,int height,bool moveLimit = true)
        {
            if (!previewTexture)
            {
                previewTexture = new CustomRenderTexture(targetTexture.width,targetTexture.height);
                previewMaterial = new Material(Shader.Find("HhotateA/TexturePreview"));
                previewMaterial.SetTexture("_MainTex",targetTexture);
                previewTexture.initializationSource = CustomRenderTextureInitializationSource.Material;
                previewTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
                previewTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
                previewTexture.initializationMaterial = previewMaterial;
                previewTexture.material = previewMaterial;
                previewTexture.Create();
            }

            var x = 1f;
            var y = 1f;
            if (width < height)
            {
                x = (float) width / (float) height;
            }
            if (height < width)
            {
                y = (float) height / (float) width;
            }
            
            Vector4 scale = new Vector4(
                previewPosition.x - previewScale * 0.5f * x,
                previewPosition.y - previewScale * 0.5f * y,
                previewPosition.x + previewScale * 0.5f * x,
                previewPosition.y + previewScale * 0.5f * y);

            if (moveLimit)
            {
                if (scale.x < 0f) previewPosition.x -= scale.x;
                if (scale.z > 1f) previewPosition.x -= scale.z-1f;
                if (scale.y < 0f) previewPosition.y -= scale.y;
                if (scale.w > 1f) previewPosition.y -= scale.w-1f;
            }


            previewMaterial.SetVector("_Scale",scale);
            
            previewTexture.Initialize();

            return previewTexture;
        }

        public void LayerButtons(bool deleateButton = false)
        {
            for (int i = LayerCount()-1; i >= 0; i--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var block = new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("↑", GUILayout.Width(20)))
                                {
                                    layerSaveData.ReplaceLayer(i,i+1);
                                    if (editIndex == i)
                                    {
                                        ChangeEditLayer(i + 1);
                                    }
                                    else
                                    if (editIndex == i+1) 
                                    {
                                        ChangeEditLayer(i);
                                    }
                                }
                                using (new EditorGUI.DisabledScope(!deleateButton))
                                {
                                    if (GUILayout.Button("×",  GUILayout.Width(40)))
                                    {
                                        layerSaveData.DeleateLayer(i);
                                        if (i < editIndex) ChangeEditLayer(editIndex - 1);
                                        // Layer削除後Index外が発生するので早期breakaaaa
                                        break;
                                    }
                                }
                            }
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                var active = EditorGUILayout.Toggle("", layerSaveData.GetLayerActive(i), GUILayout.Width(10));
                                var name = EditorGUILayout.TextField(layerSaveData.GetLayerName(i), GUILayout.Width(50));
                                layerSaveData.SetLayer(i,name,active,null,null);
                            }
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("↓", GUILayout.Width(20)))
                                {
                                    layerSaveData.ReplaceLayer(i,i-1);
                                    if (editIndex == i)
                                    {
                                        ChangeEditLayer(i - 1);
                                    }
                                    else
                                    if (editIndex == i-1)
                                    {
                                        ChangeEditLayer(i);
                                    }
                                }
                                if (GUILayout.Button("+", GUILayout.Width(40)))
                                {
                                    AddLayer(layerSaveData.GetLayer(i).texture);
                                }
                            }
                        }

                        if (layerSaveData.LayerButton(i,i==editIndex))
                        {
                            ChangeEditLayer(i);
                        };
                    }
                }
            }
        }
        
        public void SetLayerActive(int index,bool val)
        {
            layerSaveData.SetLayer(index,null,val,null,null);
        }
        
        public Vector2 Touch(Action<Vector2,Vector2> onDrag = null)
        {
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                var p = new Vector3(e.mousePosition.x - rect.x, rect.height - e.mousePosition.y + rect.y,1f);
                var uv = new Vector2(p.x/rect.width,p.y/rect.height);
                Vector2 previewUV = (uv 
                                     - new Vector2(0.5f, 0.5f))
                                    * previewScale
                                    + previewPosition;

                if (e.type == EventType.MouseDrag)
                {
                    // drag前
                    var pd = new Vector3(e.mousePosition.x - rect.x - e.delta.x, rect.height - e.mousePosition.y + rect.y + e.delta.y,1f);
                    var uvd = new Vector2(pd.x/rect.width,pd.y/rect.height);
            
                    Vector2 previewUVd = (uvd 
                                          - new Vector2(0.5f, 0.5f))
                                         * previewScale
                                         + previewPosition;
                
                    onDrag?.Invoke(previewUVd,previewUV);
                }
                return previewUV;
            }
            return new Vector2(-1f,-1f);
        }

        public void DrawLine(Vector2 from,Vector2 to,Color brushColor,Gradient gradient,float brushWidth,float brushStrength,float brushPower = 0f)
        {
            if (editIndex == -1) return;
            if (0f < from.x && from.x < 1f &&
                0f < from.y && from.y < 1f &&
                0f < to.x && to.x < 1f &&
                0f < to.y && to.y < 1f)
            {
                var compute = GetComputeShader();
                int kernel = compute.FindKernel("DrawLine");
                compute.SetInt("_Width",GetEditTexture().width);
                compute.SetInt("_Height",GetEditTexture().height);
                compute.SetVector("_Color",brushColor);
                compute.SetVector("_FromPoint",from);
                compute.SetVector("_ToPoint",to);
                compute.SetFloat("_BrushWidth",brushWidth);
                compute.SetFloat("_BrushStrength",brushStrength);
                compute.SetFloat("_BrushPower",brushPower);
                compute.SetTexture(kernel,"_ResultTex",GetEditTexture());
                compute.Dispatch(kernel, GetEditTexture().width,GetEditTexture().height,1);
            }
        }
        
        public void FillTriangle(Mesh mesh,int index,Color color,int areaExpansion = 1)
        {
            var compute = GetComputeShader();
            int kernel = compute.FindKernel("TriangleFill");
            var uvs = new ComputeBuffer(mesh.uv.Length,Marshal.SizeOf(typeof(Vector2)));
            var tris = new ComputeBuffer(mesh.triangles.Length,sizeof(int));
            uvs.SetData(mesh.uv);
            tris.SetData(mesh.triangles);
            compute.SetBuffer(kernel,"_UVs",uvs);
            compute.SetBuffer(kernel,"_Triangles",tris);
            compute.SetTexture(kernel, "_ResultTex", GetEditTexture());
            compute.SetVector("_Color", color);
            compute.SetInt("_Width", GetEditTexture().width);
            compute.SetInt("_Height", GetEditTexture().height);
            compute.SetInt("_TriangleID", index);
            compute.SetInt("_AreaExpansion", areaExpansion);
            compute.Dispatch(kernel, GetEditTexture().width, GetEditTexture().height, 1);
            uvs.Release();
            tris.Release();
        }
        public void FillTriangles(Mesh mesh,List<int> index,Color color,Gradient gradient,Vector2 from, Vector2 to,int areaExpansion = 1)
        {
            var compute = GetComputeShader();
            int kernel = compute.FindKernel("TriangleFillGradient");
            
            var xmax = mesh.vertices.Max(v => v.x);
            var xmin = mesh.vertices.Min(v => v.x);
            var ymax = mesh.vertices.Max(v => v.y);
            var ymin = mesh.vertices.Min(v => v.y);
            var zmax = mesh.vertices.Max(v => v.z);
            var zmin = mesh.vertices.Min(v => v.z);
            var normalizedVertices = mesh.vertices.Select(v =>
                new Vector3(
                    v.x * (xmax - xmin) - xmin,
                    v.y * (ymax - ymin) - ymin,
                    v.z * (zmax - zmin) - zmin)).ToArray();

            var uvs = new ComputeBuffer(mesh.uv.Length,Marshal.SizeOf(typeof(Vector2)));
            var tris = new ComputeBuffer(mesh.triangles.Length,sizeof(int));

            uvs.SetData(mesh.uv);
            tris.SetData(mesh.triangles);
            
            compute.SetBuffer(kernel,"_UVs",uvs);
            compute.SetBuffer(kernel,"_Triangles",tris);
            
            compute.SetTexture(kernel, "_ResultTex", GetEditTexture());
            compute.SetVector("_Color", color);
            compute.SetInt("_Width", GetEditTexture().width);
            compute.SetInt("_Height", GetEditTexture().height);
            compute.SetVector("_FromPoint",from);
            compute.SetVector("_ToPoint",to);
            var gradientBuffer = new ComputeBuffer(GetEditTexture().width, Marshal.SizeOf(typeof(Vector4)));
            gradientBuffer.SetData(TextureCombinater.GetGradientBuffer(gradient,GetEditTexture().width));
            compute.SetBuffer(kernel, "_Gradient", gradientBuffer);
            compute.SetInt("_AreaExpansion", areaExpansion);
            for (int i = 0; i < index.Count; i++)
            {
                if (0 < index[i] && index[i] < mesh.triangles.Length / 3)
                {
                    compute.SetInt("_TriangleID", index[i]);
                    compute.Dispatch(kernel, GetEditTexture().width, GetEditTexture().height, 1);
                }
            }
            uvs.Release();
            tris.Release();
            gradientBuffer.Release();
        }
        
        public void ReadUVMap(Mesh mesh)
        {
            RenderTexture uvMap = new RenderTexture(targetTexture.width,targetTexture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            uvMap.enableRandomWrite = true;
            uvMap.Create();
            uvMap.name = "UVMap";
            var compute = GetComputeShader();
            int kernel = compute.FindKernel("DrawUV");
            var uvs = new ComputeBuffer(mesh.uv.Length,Marshal.SizeOf(typeof(Vector2)));
            var tris = new ComputeBuffer(mesh.triangles.Length,sizeof(int));
            uvs.SetData(mesh.uv);
            tris.SetData(mesh.triangles);
            compute.SetInt("_Width",uvMap.width);
            compute.SetInt("_Height",uvMap.height);
            compute.SetVector("_Color",Vector4.one);
            compute.SetBuffer(kernel,"_UVs",uvs);
            compute.SetBuffer(kernel,"_Triangles",tris);
            compute.SetTexture(kernel,"_ResultTex",uvMap);
            compute.Dispatch(kernel, mesh.triangles.Length/3,1,1);
            
            uvs.Release();
            tris.Release();

            AddLayer(uvMap);
        }

        public void ClearColor(Texture rt,Color color,Gradient gradient)
        {
            var compute = GetComputeShader();
            int kernel = compute.FindKernel("ClearColorGradient");
            compute.SetVector("_Color",color);
            compute.SetTexture(kernel,"_ResultTex",rt);
            var gradientBuffer = new ComputeBuffer(rt.height, Marshal.SizeOf(typeof(Vector4)));
            gradientBuffer.SetData(TextureCombinater.GetGradientBuffer(gradient,rt.height));
            compute.SetBuffer(kernel, "_Gradient", gradientBuffer);
            compute.Dispatch(kernel, rt.width,rt.height,1);
            gradientBuffer.Release();
        }
        
        public void FillColor(Vector2 uv, Color brushColor,Gradient gradient,float threshold = 0.007f,int areaExpansion = 1,bool maskAllLayers = true)
        {
            if (0f < uv.x && uv.x < 1f &&
                0f < uv.y && uv.y < 1f)
            {
                RenderTexture maskTexture = maskAllLayers ? GetTemporaryTexture() : GetEditTexture();

                var compute = GetComputeShader();
                Vector2Int pix = new Vector2Int(
                    (int) (uv.x * (float) maskTexture.width),
                    (int) (uv.y * (float) maskTexture.height));

                // 種まき
                var seedPixelsArray = Enumerable.Range(0, maskTexture.width * maskTexture.height)
                    .Select(_ => 0).ToArray();
                seedPixelsArray[maskTexture.width * pix.y + pix.x] = 2;
                var seedPixels = new ComputeBuffer(seedPixelsArray.Length, sizeof(int));
                seedPixels.SetData(seedPixelsArray);
                
                compute.SetFloat("_ColorMargin", threshold);
                compute.SetInt("_Width", maskTexture.width);
                compute.SetInt("_Height", maskTexture.height);
                compute.SetVector("_Color", brushColor);
                
                // 領域判定
                int kernel = compute.FindKernel("SeedFill");
                compute.SetTexture(kernel, "_ResultTex", maskTexture);
                compute.SetBuffer(kernel, "_SeedPixels", seedPixels);
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 50; j++)
                    {
                        compute.Dispatch(kernel, maskTexture.width, maskTexture.height, 1);
                    }
                    
                    seedPixels.GetData(seedPixelsArray);
                    int jobCount = seedPixelsArray.Where(s => s == 2).Count();
                    if (seedPixelsArray.Where(s => s == 2).Count() == 0) break;
                }
                
                // 色塗り
                kernel = compute.FindKernel("FillColorPointGradient");
                compute.SetVector("_Point",uv);
                compute.SetInt("_Width", GetEditTexture().width);
                compute.SetInt("_Height", GetEditTexture().height);
                compute.SetTexture(kernel, "_ResultTex", GetEditTexture());
                var gradientBuffer = new ComputeBuffer(GetEditTexture().width, Marshal.SizeOf(typeof(Vector4)));
                gradientBuffer.SetData(TextureCombinater.GetGradientBuffer(gradient,GetEditTexture().width));
                compute.SetBuffer(kernel, "_Gradient", gradientBuffer);
                compute.SetBuffer(kernel, "_SeedPixels", seedPixels);
                compute.SetInt("_AreaExpansion", areaExpansion);
                compute.Dispatch(kernel, GetEditTexture().width, GetEditTexture().height, 1);

                seedPixels.Release();
                gradientBuffer.Release();
                if(maskAllLayers) RenderTexture.ReleaseTemporary(maskTexture);
            }
        }
        
        public void FillColor(Vector2 from, Vector2 to, Color brushColor,Gradient gradient,float threshold = 0.007f,int areaExpansion = 1,bool maskAllLayers = true)
        {
            if (0f < to.x && to.x < 1f &&
                0f < to.y && to.y < 1f)
            {
                RenderTexture maskTexture = maskAllLayers ? GetTemporaryTexture() : GetEditTexture();

                var compute = GetComputeShader();
                Vector2Int pix = new Vector2Int(
                    (int) (to.x * (float) maskTexture.width),
                    (int) (to.y * (float) maskTexture.height));

                // 種まき
                var seedPixelsArray = Enumerable.Range(0, maskTexture.width * maskTexture.height)
                    .Select(_ => 0).ToArray();
                seedPixelsArray[maskTexture.width * pix.y + pix.x] = 2;
                var seedPixels = new ComputeBuffer(seedPixelsArray.Length, sizeof(int));
                seedPixels.SetData(seedPixelsArray);
                
                compute.SetFloat("_ColorMargin", threshold);
                compute.SetInt("_Width", maskTexture.width);
                compute.SetInt("_Height", maskTexture.height);
                compute.SetVector("_Color", brushColor);
                
                // 領域判定
                int kernel = compute.FindKernel("SeedFill");
                compute.SetTexture(kernel, "_ResultTex", maskTexture);
                compute.SetBuffer(kernel, "_SeedPixels", seedPixels);
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 50; j++)
                    {
                        compute.Dispatch(kernel, maskTexture.width, maskTexture.height, 1);
                    }
                    
                    seedPixels.GetData(seedPixelsArray);
                    int jobCount = seedPixelsArray.Where(s => s == 2).Count();
                    if (seedPixelsArray.Where(s => s == 2).Count() == 0) break;
                }
                
                // 色塗り
                kernel = compute.FindKernel("FillColorLineGradient");
                compute.SetVector("_FromPoint",from);
                compute.SetVector("_ToPoint",to);
                compute.SetInt("_Width", GetEditTexture().width);
                compute.SetInt("_Height", GetEditTexture().height);
                compute.SetTexture(kernel, "_ResultTex", GetEditTexture());
                var gradientBuffer = new ComputeBuffer(GetEditTexture().width, Marshal.SizeOf(typeof(Vector4)));
                gradientBuffer.SetData(TextureCombinater.GetGradientBuffer(gradient,GetEditTexture().width));
                compute.SetBuffer(kernel, "_Gradient", gradientBuffer);
                compute.SetBuffer(kernel, "_SeedPixels", seedPixels);
                compute.SetInt("_AreaExpansion", areaExpansion);
                compute.Dispatch(kernel, GetEditTexture().width, GetEditTexture().height, 1);

                seedPixels.Release();
                if(maskAllLayers) RenderTexture.ReleaseTemporary(maskTexture);
            }
        }

        public void DrawStamp(Texture stamp, Vector2 uv, Vector2 scale, Color col, float rot = 0f)
        {
            if (0f < uv.x && uv.x < 1f &&
                0f < uv.y && uv.y < 1f)
            {
                float w = (float)stamp.width;
                int s = 1;
                while (w > (float) GetEditTexture().width * scale.x)
                {
                    w *= 0.5f;
                    s++;
                }
                s = Mathf.Clamp(s, 1, TextureCombinater.GetMipMapCount((Texture2D)stamp));

                var compute = GetComputeShader();
                int kernel = compute.FindKernel("DrawStamp");
                compute.SetInt("_Width", GetEditTexture().width);
                compute.SetInt("_Height", GetEditTexture().height);
                compute.SetTexture(kernel, "_ResultTex", GetEditTexture());
                compute.SetInt("_StampWidth", stamp.width);
                compute.SetInt("_StampHeight", stamp.height);
                compute.SetVector("_Color", col);
                compute.SetTexture(kernel, "_Stamp", stamp,s-1);
                compute.SetVector("_StampUV", uv);
                compute.SetVector("_StampScale", scale);
                compute.SetFloat("_StampRotation", rot);
                
                compute.Dispatch(kernel, GetEditTexture().width, GetEditTexture().height, 1);
            }
        }

        public Texture GetStamp(Vector2 from,Vector2 to,bool maskAllLayers = true)
        {
            RenderTexture maskTexture = maskAllLayers ? GetTemporaryTexture() : GetEditTexture();
            var currentRT = RenderTexture.active;
            RenderTexture.active = maskTexture;
            from = new Vector2(Mathf.Clamp01(from.x),Mathf.Clamp01(from.y));
            to = new Vector2(Mathf.Clamp01(to.x),Mathf.Clamp01(to.y));
            var fromxy = new Vector2(from.x * maskTexture.width, (1.0f-from.y) * maskTexture.height);
            var toxy = new Vector2(to.x * maskTexture.width, (1.0f-to.y) * maskTexture.height);
            var min = new Vector2(Mathf.Min(fromxy.x,toxy.x),Mathf.Min(fromxy.y,toxy.y));
            var max = new Vector2(Mathf.Max(fromxy.x,toxy.x),Mathf.Max(fromxy.y,toxy.y));
            if ((int) min.x == (int) max.x) return null;
            if ((int)min.y == (int)max.y)  return null;
            var texture = new Texture2D((int)max.x-(int)min.x,(int)max.y-(int)min.y,TextureFormat.RGBAFloat,false);
            texture.ReadPixels(new Rect(min.x, min.y, max.x, max.y), 0, 0);
            texture.Apply();
            RenderTexture.active = currentRT;
            if(maskAllLayers) RenderTexture.ReleaseTemporary(maskTexture);
            return texture;
        }

        public Color SpuitColor(Vector2 uv,bool maskAllLayers = true)
        {
            RenderTexture maskTexture = maskAllLayers ? GetTemporaryTexture() : GetEditTexture();
            Vector2Int pix = new Vector2Int(
                (int) (uv.x * (float) maskTexture.width),
                (int) (uv.y * (float) maskTexture.height));
            var c = TextureCombinater.GetPixel(maskTexture, pix.x, pix.y);
            if(maskAllLayers) RenderTexture.ReleaseTemporary(maskTexture);
            return c;
        }

        public void Gaussian(Vector2 from,Vector2 to,float brushWidth,float brushStrength,float brushPower = 0f)
        {
            var compute = GetComputeShader();
            int kernel = compute.FindKernel("Gaussian");
            compute.SetFloat("_BrushWidth",brushWidth);
            compute.SetFloat("_BrushStrength",brushStrength);
            compute.SetFloat("_BrushPower",brushPower);
            compute.SetInt("_Width",GetEditTexture().width);
            compute.SetInt("_Height",GetEditTexture().height);
            compute.SetVector("_FromPoint",from);
            compute.SetVector("_ToPoint",to);
            compute.SetTexture(kernel,"_ResultTex",GetEditTexture());
            compute.Dispatch(kernel, GetEditTexture().width,GetEditTexture().height,1);
        }

        public Texture SaveTexture(string path)
        {
            byte[] bytes = TextureCombinater.Texture2Bytes((RenderTexture)GetTexture());
            //File.WriteAllBytes(path, bytes);
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                fs.Write(bytes, 0, bytes.Length);
            }
            path = FileUtil.GetProjectRelativePath(path);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture>(path);
        }

        public void SaveLayerData(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            layerSaveData.SaveTextures(path);
            AssetDatabase.CreateAsset(layerSaveData,path);
        }
        public void LoadLayerData(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            var tsd = AssetDatabase.LoadAssetAtPath<LayersSaveData>(path);
            layerSaveData = tsd;
        }
    }
}

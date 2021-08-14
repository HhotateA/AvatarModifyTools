/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

namespace HhotateA.AvatarModifyTools.Core
{
    public class TextureCreator
    {
        public string name = "";
        private CustomRenderTexture targetTexture;
        private Material targetMaterial;
        // private int editIndex = -1;
        private List<LayerData> layerDatas = new List<LayerData>();
        public List<LayerData> LayerDatas => layerDatas;
        public LayerData GetLayerData(int index) => layerDatas[index];
        private LayerData editLayer;
        
        public TextureCreator(Texture baselayer)
        {
            name = baselayer.name;
            if (baselayer != null)
            {
                ResizeTexture(baselayer.width, baselayer.height);
                AddLayer(baselayer);
            }
        }
        
        ComputeShader GetComputeShader()
        {
            return AssetUtility.LoadAssetAtGuid<ComputeShader>(EnvironmentVariable.computeShader);
        }
        
        public Texture GetTexture(int index = -1)
        {
            if (0 <= index && index < LayerCount())
            {
                return layerDatas[index].texture;
            }
            return targetTexture;
        }
        public void SetLayers(List<LayerData> layers)
        {
            foreach (var layer in layerDatas)
            {
                layer.Release();
            }
            layerDatas = layers;
        }
        public void AddLayers(List<LayerData> layers)
        {
            layers.AddRange(layerDatas);
            layerDatas = layers;
        }

        // 最大キャッシュ数
        private int maxCaches
        {
            get => EnvironmentVariable.maxCaches;
        }
        private List<RenderTexture> caches = new List<RenderTexture>();
        private int cashIndex = -1;
        
        public RenderTexture GetEditTexture()
        {
            return editLayer.texture;
        }

        public LayerData GetEditLayer()
        {
            return editLayer;
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
            return layerDatas.Count;
        }

        public void ResizeTexture(int width, int height)
        {
            if(targetTexture) targetTexture.Release();
            targetTexture = new CustomRenderTexture(width,height);
            targetMaterial = new Material(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentVariable.texturePainterShader));
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
            if(LayerCount() > 16) return;
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

            layerDatas.Insert(0,new LayerData((RenderTexture)tex));
            SyncLayers();
        }
        
        public void AddLayer(Color col,Gradient gradient = null)
        {
            if(LayerCount() > 16) return;
            if(gradient==null) gradient = new Gradient();
            var rt = new RenderTexture(targetTexture.width,targetTexture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            rt.enableRandomWrite = true;
            rt.Create();
            var tex = rt;
            
            ClearColor(tex,col,gradient);
            
            tex.name = "NewLayer" + LayerCount().ToString();
            layerDatas.Insert(0,new LayerData((RenderTexture)tex)
            {
                active = true,
            });
            SyncLayers();
        }
        public void AddMask(Color col,Gradient gradient = null)
        {
            if(LayerCount() > 16) return;
            if(gradient==null) gradient = new Gradient();
            var rt = new RenderTexture(targetTexture.width,targetTexture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            rt.enableRandomWrite = true;
            rt.Create();
            var tex = rt;
            
            ClearColor(tex,col,gradient);
            
            tex.name = "Mask" + LayerCount().ToString();
            layerDatas.Insert(0,new LayerData((RenderTexture)tex)
            {
                active = true,
                layerMode = BlendMode.Override,
                isMask = true
            });
            SyncLayers();
        }

        public Texture SyncLayers()
        {
            for (int i = 0; i < 16; i++)
            {
                if (i < LayerCount())
                {
                    var ld = layerDatas[layerDatas.Count-1-i];
                    targetMaterial.SetTexture("_Layer"+i,ld.texture);
                    targetMaterial.SetTextureScale("_Layer"+i,new Vector2(ld.scale.x,ld.scale.y));
                    targetMaterial.SetTextureOffset("_Layer"+i,new Vector2(ld.offset.x,ld.offset.y));
                    targetMaterial.SetInt("_Mode"+i, ld.active ? (int) ld.layerMode : (int) BlendMode.Disable);
                    targetMaterial.SetColor("_Color"+i, ld.color);
                    targetMaterial.SetColor("_Comparison"+i, ld.comparison);
                    if (ld.layerMode == BlendMode.HSV)
                    {
                        targetMaterial.SetVector("_Settings"+i, ld.settings);
                    }
                    else
                    if(ld.layerMode == BlendMode.Color)
                    {
                        targetMaterial.SetVector("_Settings"+i, new Vector4(1f, 0f, ld.settings.z, ld.settings.w));
                    }
                    else
                    if(ld.layerMode == BlendMode.Bloom)
                    {
                        targetMaterial.SetVector("_Settings"+i, new Vector4(1f/(float)ld.texture.width, 1f/(float)ld.texture.height, ld.settings.z, ld.settings.w));
                    }
                    targetMaterial.SetInt("_Mask"+i, ld.isMask ? 1 : -1);
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
                editLayer = layerDatas[index];
                ResetCashes();
                AddCash();
            }
        }

        public void ReplaceLayer(int from,int to)
        {
            if (from < LayerCount() && to < LayerCount())
            {
                var l = layerDatas[from];
                layerDatas[from] = layerDatas[to];
                layerDatas[to] = l;
            }
        }
        
        public void DeleateLayer(int index)
        {
            layerDatas[index].texture.Release();
            layerDatas[index].texture.DiscardContents();
            layerDatas.RemoveAt(index);
        }

        public void CopyLayer(int index)
        {
            layerDatas.Insert(0,new LayerData(TextureCombinater.GetReadableRenderTexture(layerDatas[index].texture))
            {
                active = layerDatas[index].active,
                color = layerDatas[index].color,
                comparison = layerDatas[index].comparison,
                isMask = layerDatas[index].isMask,
                layerMode = layerDatas[index].layerMode,
                name = layerDatas[index].name,
                offset = layerDatas[index].offset,
                scale = layerDatas[index].scale,
                settings = layerDatas[index].settings
            });
        }

        public void SetLayerActive(int index,bool val)
        {
            layerDatas[index].active = val;
        }

        public void DrawLine(Vector2 from,Vector2 to,Color brushColor,Gradient gradient,float brushWidth,float brushStrength,float brushPower = 0f)
        {
            if (editLayer == null) return;
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
            var verts = new ComputeBuffer(mesh.vertices.Length,Marshal.SizeOf(typeof(Vector3)));
            var tris = new ComputeBuffer(mesh.triangles.Length,sizeof(int));

            uvs.SetData(mesh.uv);
            verts.SetData(mesh.vertices);
            tris.SetData(mesh.triangles);
            
            compute.SetBuffer(kernel,"_UVs",uvs);
            compute.SetBuffer(kernel,"_Vertices",uvs);
            compute.SetBuffer(kernel,"_Triangles",tris);
            
            compute.SetTexture(kernel, "_ResultTex", GetEditTexture());
            compute.SetVector("_Color", color);
            compute.SetInt("_Width", GetEditTexture().width);
            compute.SetInt("_Height", GetEditTexture().height);
            compute.SetVector("_FromPoint",from);
            compute.SetVector("_ToPoint",to);
            var gradientBuffer = new ComputeBuffer(256, Marshal.SizeOf(typeof(Vector4)));
            gradientBuffer.SetData(TextureCombinater.GetGradientBuffer(gradient,256));
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
            verts.Release();
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
                gradientBuffer.Release();
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
            return TextureCombinater.ConvertToPngAndSave(path,(RenderTexture) GetTexture());
        }

        public void Release()
        {
            targetTexture.Release();
            foreach (var layer in layerDatas)
            {
                layer.Release();
            }
        }
    }
    
    [System.SerializableAttribute]
    public class LayerData
    {
        public string name = "";
        public RenderTexture texture;
        public byte[] origin;
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
        
        public LayerData(byte[] png)
        {
            origin = png;
        }

        public void Release()
        {
            if (texture)
            {
                texture.Release();
            }
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

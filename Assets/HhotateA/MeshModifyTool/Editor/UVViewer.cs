using HhotateA.AvatarModifyTools.Core;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Linq;
using System.Runtime.InteropServices;

namespace HhotateA.AvatarModifyTools.MeshModifyTool
{
    public class UVViewer
    {    
        private Rect rect;
        private Material baseMaterial;
        private Texture baseTexture;
        private RenderTexture uvMap;
        private CustomRenderTexture targetTexture;
        private Material targetMaterial;

        ComputeShader GetComputeShader()
        {
            var computePath = AssetDatabase.GUIDToAssetPath("9fb4250b8646a5e4d96521f4f85c7cba");
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(computePath);
            return compute;
        }

        public UVViewer(Material material)
        {
            baseMaterial = material;
            baseTexture = material.mainTexture;
            targetTexture = new CustomRenderTexture(1, 1);
        }
        
        public void Display(int width, int height)
        {
            if (width != targetTexture.width || height != targetTexture.height)
            {
                UpdatePreview(width,height);
            }
            rect = GUILayoutUtility.GetRect(width, height, GUI.skin.box);
            targetTexture.Initialize();
            EditorGUI.DrawPreviewTexture(rect, targetTexture);
            //baseMaterial.mainTexture = targetTexture;
        }

        public void ReadUVMap(Mesh mesh,List<int> verts)
        {
            uvMap = new RenderTexture(targetTexture.width,targetTexture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            uvMap.enableRandomWrite = true;
            uvMap.Create();
            uvMap.name = "UVMap";
            var compute = GetComputeShader();
            int kernel = compute.FindKernel("DrawUV");
            var triangles = new List<int>();
            for (int i = 0; i < mesh.triangles.Length / 3; i++)
            {
                if (verts.Contains(mesh.triangles[i * 3 + 0]) ||
                    verts.Contains(mesh.triangles[i * 3 + 1]) ||
                    verts.Contains(mesh.triangles[i * 3 + 2]))
                {
                    triangles.Add(mesh.triangles[i * 3 + 0]);
                    triangles.Add(mesh.triangles[i * 3 + 1]);
                    triangles.Add(mesh.triangles[i * 3 + 2]);
                }
            }
            var uvs = new ComputeBuffer(mesh.uv.Length,Marshal.SizeOf(typeof(Vector2)));
            var tris = new ComputeBuffer(triangles.Count,sizeof(int));
            uvs.SetData(mesh.uv);
            tris.SetData(triangles);
            compute.SetInt("_Width",uvMap.width);
            compute.SetInt("_Height",uvMap.height);
            compute.SetVector("_Color",Color.red);
            compute.SetBuffer(kernel,"_UVs",uvs);
            compute.SetBuffer(kernel,"_Triangles",tris);
            compute.SetTexture(kernel,"_ResultTex",uvMap);
            compute.Dispatch(kernel, mesh.triangles.Length/3,1,1);
            
            uvs.Release();
            tris.Release();

            if (targetMaterial != null)
            {
                targetMaterial.SetTexture("_UVMap", uvMap);
            }
        }

        public void UVTextureSize(Vector2 scale, Vector2 offset)
        {
            if (targetMaterial != null)
            {
                targetMaterial.SetTextureScale("_UVMap",scale);
                targetMaterial.SetTextureOffset("_UVMap",offset);
            }
        }
        
        public void BaseTextureSize(Vector2 scale, Vector2 offset)
        {
            if (targetMaterial != null)
            {
                targetMaterial.SetTextureScale("_BaseTex",scale);
                targetMaterial.SetTextureOffset("_BaseTex",offset);
            }
        }

        public void UpdatePreview(int width, int height)
        {
            targetTexture = new CustomRenderTexture(width,height);
            if (targetMaterial == null)
            {
                targetMaterial = new Material(Shader.Find("HhotateA/UVViewer"));
            }

            targetMaterial.SetTexture("_BaseTex",baseTexture);
            targetTexture.initializationSource = CustomRenderTextureInitializationSource.Material;
            targetTexture.initializationMode = CustomRenderTextureUpdateMode.Realtime;
            targetTexture.updateMode = CustomRenderTextureUpdateMode.Realtime;
            targetTexture.initializationMaterial = targetMaterial;
            targetTexture.material = targetMaterial;
            targetTexture.Create();
        }

        public void Release()
        {
            baseMaterial.mainTexture = baseTexture;
        }
    }
}

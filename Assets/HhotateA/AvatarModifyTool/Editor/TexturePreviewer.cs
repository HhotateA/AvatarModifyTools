/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using UnityEngine;
using UnityEditor;
using System;

namespace HhotateA.AvatarModifyTools.Core
{
    public class TexturePreviewer
    {
        private TextureCreator textureCreater;
        private CustomRenderTexture targetTexture;
        private Material targetMaterial;
        private CustomRenderTexture previewTexture;
        private Material previewMaterial;
        private float previewScale = 1f;
        Vector2 previewPosition = new Vector2(0.5f,0.5f);
        private Rect rect;

        private RenderTexture overlayTexture;
        
        public TexturePreviewer(TextureCreator tc)
        {
            if (tc == null)
            {
                throw new NullReferenceException("Missing Texture Creater");
            }
            textureCreater = tc;
            previewTexture = new CustomRenderTexture(textureCreater.GetTexture().width,textureCreater.GetTexture().height);
            previewMaterial = new Material(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentVariable.texturePreviewShader));
            previewMaterial.SetTexture("_MainTex",textureCreater.GetTexture());
            previewTexture.initializationSource = CustomRenderTextureInitializationSource.Material;
            previewTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
            previewTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
            previewTexture.initializationMaterial = previewMaterial;
            previewTexture.material = previewMaterial;
            previewTexture.Create();
            
            targetTexture = new CustomRenderTexture(textureCreater.GetTexture().width,textureCreater.GetTexture().height);
            targetMaterial = new Material(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentVariable.texturePreviewShader));
            targetMaterial.SetTexture("_MainTex",textureCreater.GetTexture());
            targetTexture.initializationSource = CustomRenderTextureInitializationSource.Material;
            targetTexture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
            targetTexture.updateMode = CustomRenderTextureUpdateMode.OnDemand;
            targetTexture.initializationMaterial = targetMaterial;
            targetTexture.material = targetMaterial;
            targetTexture.Create();
            
            rect = new Rect();
        }
        
        ComputeShader GetComputeShader()
        {
            return AssetUtility.LoadAssetAtGuid<ComputeShader>(EnvironmentVariable.computeShader);
        }
        
        public Texture GetTexture()
        {
            Vector4 scale = new Vector4(0f ,0f ,1f ,1f );
            targetMaterial.SetVector("_Scale",scale);
            targetMaterial.SetTexture("_Overlay",overlayTexture);
            targetTexture.Initialize();
            return targetTexture;
        }

        public void Display(int width, int height, bool moveLimit = true, int rotationDrag = 2, int positionDrag = 1,bool canTouch = true)
        {
            textureCreater.LayersUpdate();
            rect = GUILayoutUtility.GetRect(width, height, GUI.skin.box);
            EditorGUI.DrawPreviewTexture(rect, ScalePreview(width,height,moveLimit)); 
            var e = Event.current;

            if (rect.Contains(e.mousePosition) && canTouch)
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

        public void MouseOverlay(Texture tex,float scale = 1f,float rotate = 0f)
        {
            var e = Event.current;
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
        
        CustomRenderTexture ScalePreview(int width,int height,bool moveLimit = true)
        {
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
            previewMaterial.SetTexture("_Overlay",overlayTexture);
            previewTexture.Initialize();

            return previewTexture;
        }

        public void Release()
        {
            previewTexture.Release();
        }

        public void PreviewPoint(Vector2 uv, Color brushColor, float brushWidth, float brushStrength)
        {
            PreviewClear();
            if (0f < uv.x && uv.x < 1f &&
                0f < uv.y && uv.y < 1f)
            {
                var compute = GetComputeShader();
                int kernel = compute.FindKernel("ClearColor");
                compute.SetVector("_Color",Vector4.zero);
                compute.SetTexture(kernel,"_ResultTex",overlayTexture);
                compute.Dispatch(kernel, overlayTexture.width,overlayTexture.height,1);
                
                kernel = compute.FindKernel("DrawPoint");
                compute.SetTexture(kernel,"_ResultTex",overlayTexture);
                compute.SetVector("_Color", brushColor);
                compute.SetInt("_Width",overlayTexture.width);
                compute.SetInt("_Height",overlayTexture.height);
                compute.SetVector("_Point",uv);
                compute.SetFloat("_BrushWidth",brushWidth);
                compute.SetFloat("_BrushStrength",brushStrength);
                compute.SetFloat("_BrushPower",1f);
                compute.Dispatch(kernel, overlayTexture.width,overlayTexture.height,1);
            }
        }
        
        public void PreviewStamp(Texture stamp, Vector2 uv, Vector2 scale, Color col, float rot = 0f)
        {
            PreviewClear();
            if (0f < uv.x && uv.x < 1f &&
                0f < uv.y && uv.y < 1f)
            {
                var compute = GetComputeShader();
                int kernel = compute.FindKernel("ClearColor");
                compute.SetVector("_Color",Vector4.zero);
                compute.SetTexture(kernel,"_ResultTex",overlayTexture);
                compute.Dispatch(kernel, overlayTexture.width,overlayTexture.height,1);
                
                kernel = compute.FindKernel("DrawStamp");
                compute.SetInt("_Width",overlayTexture.width);
                compute.SetInt("_Height",overlayTexture.height);
                compute.SetTexture(kernel, "_ResultTex", overlayTexture);
                compute.SetInt("_StampWidth", stamp.width);
                compute.SetInt("_StampHeight", stamp.height);
                compute.SetVector("_Color", col);
                compute.SetTexture(kernel, "_Stamp", stamp);
                compute.SetVector("_StampUV", uv);
                compute.SetVector("_StampScale", scale);
                compute.SetFloat("_StampRotation", rot);
                compute.Dispatch(kernel, overlayTexture.width, overlayTexture.height, 1);
            }
        }
        
        public void PreviewLine(Vector2 from,Vector2 to,Color brushColor,float brushWidth,float brushStrength)
        {
            PreviewClear();
            if (0f < from.x && from.x < 1f &&
                0f < from.y && from.y < 1f &&
                0f < to.x && to.x < 1f &&
                0f < to.y && to.y < 1f)
            {
                var compute = GetComputeShader();
                int kernel = compute.FindKernel("ClearColor");
                compute.SetVector("_Color",Vector4.zero);
                compute.SetTexture(kernel,"_ResultTex",overlayTexture);
                compute.Dispatch(kernel, overlayTexture.width,overlayTexture.height,1);
                
                kernel = compute.FindKernel("DrawLine");
                compute.SetInt("_Width",overlayTexture.width);
                compute.SetInt("_Height",overlayTexture.height);
                compute.SetVector("_Color",brushColor);
                compute.SetVector("_FromPoint",from);
                compute.SetVector("_ToPoint",to);
                compute.SetFloat("_BrushWidth",brushWidth);
                compute.SetFloat("_BrushStrength",brushStrength);
                compute.SetFloat("_BrushPower",1f);
                compute.SetTexture(kernel,"_ResultTex",overlayTexture);
                compute.Dispatch(kernel, overlayTexture.width, overlayTexture.height, 1);
            }
        }
        
        public void PreviewBox(Vector2 from,Vector2 to,Color brushColor,float brushWidth,float brushStrength)
        {
            PreviewClear();
            if (0f < from.x && from.x < 1f &&
                0f < from.y && from.y < 1f &&
                0f < to.x && to.x < 1f &&
                0f < to.y && to.y < 1f)
            {
                var compute = GetComputeShader();
                int kernel = compute.FindKernel("ClearColor");
                compute.SetVector("_Color",Vector4.zero);
                compute.SetTexture(kernel,"_ResultTex",overlayTexture);
                compute.Dispatch(kernel, overlayTexture.width,overlayTexture.height,1);
                
                kernel = compute.FindKernel("DrawBox");
                compute.SetInt("_Width",overlayTexture.width);
                compute.SetInt("_Height",overlayTexture.height);
                compute.SetVector("_Color",brushColor);
                compute.SetVector("_FromPoint",from);
                compute.SetVector("_ToPoint",to);
                compute.SetFloat("_BrushWidth",brushWidth);
                compute.SetFloat("_BrushStrength",brushStrength);
                compute.SetFloat("_BrushPower",1f);
                compute.SetTexture(kernel,"_ResultTex",overlayTexture);
                compute.Dispatch(kernel, overlayTexture.width, overlayTexture.height, 1);
            }
        }

        public void PreviewClear()
        {
            if (overlayTexture == null)
            {
                overlayTexture = new RenderTexture( previewTexture.width, previewTexture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                overlayTexture.enableRandomWrite = true;
                overlayTexture.Create();
            }
        }
    }
}
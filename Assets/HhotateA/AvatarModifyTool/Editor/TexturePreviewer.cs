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
        private CustomRenderTexture previewTexture;
        private Material previewMaterial;
        private float previewScale = 1f;
        Vector2 previewPosition = new Vector2(0.5f,0.5f);
        private Rect rect;

        public TexturePreviewer(TextureCreator tc)
        {
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
            rect = new Rect();
        }

        public void Display(int width, int height, bool moveLimit = true, int rotationDrag = 2, int positionDrag = 1)
        {
            textureCreater.LayersUpdate();
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
            
            previewTexture.Initialize();

            return previewTexture;
        }

        public void Release()
        {
            previewTexture.Release();
        }
    }
}
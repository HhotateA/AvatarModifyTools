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
        private TextureCreator textureCreator;
        private TexturePreviewer texturePreviewer;

        public UVViewer(Material material)
        {
            textureCreator = new TextureCreator(material.mainTexture);
            texturePreviewer = new TexturePreviewer(textureCreator);
        }
        
        public void Display(int width, int height)
        {
            texturePreviewer.Display(width,height);
        }

        public void ReadUVMap(Mesh mesh)
        {
            if (textureCreator.LayerCount() > 1)
            {
                textureCreator.DeleateLayer(1);
            }
            textureCreator.ReadUVMap(mesh);
        }

        public void UVTextureSize(Vector2 scale, Vector2 offset)
        {
            textureCreator.GetLayerData(1).scale = scale;
            textureCreator.GetLayerData(1).offset = offset;
        }
        
        public void BaseTextureSize(Vector2 scale, Vector2 offset)
        {
            textureCreator.GetLayerData(0).scale = scale;
            textureCreator.GetLayerData(0).offset = offset;
        }
        public void Release()
        {
            texturePreviewer.Release();
        }
    }
}

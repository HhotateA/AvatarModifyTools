/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using HhotateA.AvatarModifyTools.Core;
using UnityEngine;

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

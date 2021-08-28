/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using HhotateA.AvatarModifyTools.Core;
using System.Collections.Generic;
using UnityEngine;

namespace HhotateA.AvatarModifyTools.TextureModifyTool
{
    public class LayersSaveData : ScriptableObject
    {
        [SerializeField] List<LayerData> layers = new List<LayerData>();

        public void SetLayersData(List<LayerData> layerList)
        {
            foreach (var layer in layerList)
            {
                layers.Add(new LayerData(TextureCombinater.Texture2Bytes(layer.texture))
                {
                    active = layer.active,
                    color = layer.color,
                    comparison = layer.comparison,
                    isMask = layer.isMask,
                    layerMode = layer.layerMode,
                    name = layer.name,
                    offset = layer.offset,
                    scale = layer.scale,
                    settings = layer.settings
                });
            }
        }

        public List<LayerData> GetLayersData()
        {
            var layerList = new List<LayerData>();
            foreach (var layer in layers)
            {
                layerList.Add(new LayerData(TextureCombinater.GetReadableRenderTexture(TextureCombinater.Bytes2Texture(layer.origin)))
                {
                    active = layer.active,
                    color = layer.color,
                    comparison = layer.comparison,
                    isMask = layer.isMask,
                    layerMode = layer.layerMode,
                    name = layer.name,
                    offset = layer.offset,
                    scale = layer.scale,
                    settings = layer.settings
                });
            }

            return layerList;
        }
    }
}
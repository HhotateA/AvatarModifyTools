/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using Color = UnityEngine.Color;

namespace HhotateA.AvatarModifyTools.Core
{
    public class TextureCombinater
    {
        const int maxResolution = 4096;

        public static Texture2D CombinateSaveTexture(Texture2D[] texs,string path = null,int tilling = 0)
        {
            var combinatedTexture = CombinateTextures(texs,tilling);
            if (!string.IsNullOrWhiteSpace(path)) combinatedTexture = ConvertToPngAndSave(path, combinatedTexture);
            return combinatedTexture;
        }

        public static Texture2D CombinateTextures(Texture2D[] texs,int tilling = 0)
        {
            if (tilling == 0)
            {
                while (texs.Length > tilling * tilling)
                {
                    tilling++;
                }
            }
            
            int resolution = Mathf.Min(texs[0].width,maxResolution/tilling);

            List<Texture2D> resizedTexs = new List<Texture2D>();
            foreach (var tex in texs)
            {
                if(tex==null) resizedTexs.Add(ResizeTexture(Texture2D.blackTexture, resolution, resolution));
                else resizedTexs.Add(ResizeTexture(tex, resolution, resolution));
            }

            Texture2D combinatedTexture = new Texture2D(resolution * tilling, resolution * tilling);
            int i = 0;
            for (int column = tilling-1; column >= 0; column--)
            {
                for (int row = 0; row < tilling; row++)
                {
                    if (i < resizedTexs.Count)
                    {
                        if (resizedTexs[i] != null)
                        {
                            combinatedTexture.SetPixels(row * resolution, column * resolution,
                                resolution, resolution,
                                GetTexturePixels(resizedTexs[i]));
                        }
                        else
                        {
                            // 透明で上書き
                            combinatedTexture.SetPixels(row * resolution, column * resolution,
                                resolution, resolution,
                                Enumerable.Repeat<Color>(Color.clear, resolution * resolution).ToArray());
                        }
                    }
                    else
                    {
                        // 透明で上書き
                        combinatedTexture.SetPixels(row * resolution, column * resolution,
                            resolution, resolution,
                            Enumerable.Repeat<Color>(Color.clear, resolution * resolution).ToArray());
                    }

                    i++;
                }
            }
            return combinatedTexture;
        }

        public static Texture2D ResizeSaveTexture(string path,Texture2D srcTexture, int newWidth, int newHeight)
        {
            return ConvertToPngAndSave(path, GetReadableTexture(srcTexture,newWidth,newHeight));
        }

        public static Texture2D ResizeTexture(Texture2D srcTexture, int newWidth, int newHeight)
        {
            var resizedTexture = new Texture2D(newWidth, newHeight);
            Graphics.ConvertTexture(srcTexture, resizedTexture);
            return resizedTexture;
        }
        public static Texture2D ConvertToPngAndSave(string path,Texture2D tex)
        {
            //Pngに変換
            byte[] bytes = tex.EncodeToPNG();
            //保存
            using (var fs = new System.IO.FileStream(Path.GetFullPath(path), System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                fs.Write(bytes, 0, bytes.Length);
            }
            AssetDatabase.ImportAsset(path);
            var a = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Debug.Log(a);
            var importer = (TextureImporter) TextureImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.streamingMipmaps = true;
            for (int texsize = 32; texsize <= tex.width; texsize = texsize * 2)
            {
                importer.maxTextureSize = texsize;
            }
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Texture2D ConvertToPngAndSave(string path,RenderTexture tex)
        {
            byte[] bytes = Texture2Bytes(tex);
            //File.WriteAllBytes(path, bytes);
            using (var fs = new System.IO.FileStream(Path.GetFullPath(path), System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                fs.Write(bytes, 0, bytes.Length);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter) TextureImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.streamingMipmaps = true;
            for (int texsize = 32; texsize <= tex.width; texsize = texsize * 2)
            {
                importer.maxTextureSize = texsize;
            }
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Color[] GetTexturePixels(Texture2D texture)
        {
            return GetReadableTexture(texture).GetPixels();
        }
        
        public static Texture2D GetReadableTexture(Texture2D srcTexture, int newWidth=0, int newHeight=0)
        {
            int width = newWidth == 0 ? srcTexture.width : newWidth;
            int height = newHeight == 0 ? srcTexture.height : newHeight;
            RenderTexture rt = RenderTexture.GetTemporary( 
                width,height,0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default);
            RenderTexture currentRT = RenderTexture.active;
            Graphics.Blit(srcTexture, rt);
            RenderTexture.active = rt;
            var t = new Texture2D(width,height);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply();
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(rt);
            return t;
        }
        
        public static RenderTexture GetReadableRenderTexture(Texture srcTexture)
        {
            RenderTexture rt = new RenderTexture(srcTexture.width,srcTexture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            rt.enableRandomWrite = true;
            rt.Create();
            RenderTexture currentRT = RenderTexture.active;
            Graphics.Blit(srcTexture, rt);
            RenderTexture.active = currentRT;
            rt.name = srcTexture.name;
            return rt;
        }
        
        public static Texture2D Bytes2Texture(byte[] bytes)
        {
            int pos = 16;
            int width = 0;
            for (int i = 0; i < 4; i++)
            {
                width = width * 256 + bytes[pos++];
            }
            int height = 0;
            for (int i = 0; i < 4; i++)
            {
                height = height * 256 + bytes[pos++];
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            texture.LoadImage(bytes);
            return texture;
        }

        public static byte[] Texture2Bytes(RenderTexture texture)
        {
            Texture2D tex = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false);
            var current = RenderTexture.active;
            RenderTexture.active = texture;
            tex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = current;
            tex.Apply();
            return tex.EncodeToPNG();
        }

        public static Color GetPixel(RenderTexture rt,int x,int y)
        {
            var currentRT = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            var colors = texture.GetPixel(x,y);
            RenderTexture.active = currentRT;
            return colors;
        }
        
        public static Vector4[] GetGradientBuffer(Gradient gradient,int step = 256)
        {
            var buffer = new Vector4[step];
            for (int i = 0;i<step;i++)
            {
                buffer[i] = gradient.Evaluate((float)i / (float)step);
            }

            return buffer;
        }

        public static int GetMipMapCount(Texture2D tex)
        {
            return tex.mipmapCount;
        }
    }
}
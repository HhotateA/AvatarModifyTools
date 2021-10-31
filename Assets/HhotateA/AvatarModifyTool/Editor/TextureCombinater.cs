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

        public static Texture2D CombinateSaveTexture(Texture2D[] texs,string path = null,int tilling = 0,int margin = 0)
        {
            var combinatedTexture = CombinateTextures(texs,tilling,margin);
            if (!string.IsNullOrWhiteSpace(path)) combinatedTexture = ConvertToPngAndSave(path, combinatedTexture);
            return combinatedTexture;
        }

        public static Texture2D CombinateTextures(Texture2D[] textures,int tilling = 0,int margin = 0)
        {
            if (tilling == 0)
            {
                while (textures.Length > tilling * tilling)
                {
                    tilling++;
                }
            }
            
            int resolution = Mathf.Min(textures[0].width,maxResolution/tilling);

            List<Texture2D> resizedTexs = new List<Texture2D>();
            foreach (var texture in textures)
            {
                if(texture==null) resizedTexs.Add(ResizeTexture(Texture2D.blackTexture, resolution, resolution, margin));
                else resizedTexs.Add(ResizeTexture(texture, resolution, resolution, margin));
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

        public static Texture2D ResizeSaveTexture(string path,Texture2D texture, int newWidth, int newHeight)
        {
            return ConvertToPngAndSave(path, GetReadableTexture(texture,newWidth,newHeight));
        }

        public static Texture2D ResizeTexture(Texture2D texture, int newWidth, int newHeight,int margin = 0)
        {
            if (!texture) return null;
            if (margin > 0)
            {
                var tempTex = new Texture2D(newWidth-2*margin, newHeight-2*margin);
                Graphics.ConvertTexture(texture, tempTex);
                var resizedTexture = new Texture2D(newWidth, newHeight);
                // 透明で上書き
                resizedTexture.SetPixels(0, 0, newWidth, newHeight, Enumerable.Repeat<Color>(Color.clear, newWidth * newHeight).ToArray());
                resizedTexture.SetPixels(margin, margin, newWidth-2*margin, newHeight-2*margin, GetTexturePixels(tempTex));
                resizedTexture.Apply();
                return resizedTexture;
            }
            else
            {
                var resizedTexture = new Texture2D(newWidth, newHeight);
                Graphics.ConvertTexture(texture, resizedTexture);
                return resizedTexture;
            }
        }
        public static Texture2D ConvertToPngAndSave(string path,Texture2D texture)
        {
            if (!texture) return null;
            byte[] bytes = texture.EncodeToPNG();
            // File.WriteAllBytes(path, bytes);
            using (var fs = new System.IO.FileStream(Path.GetFullPath(path), System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                fs.Write(bytes, 0, bytes.Length);
            }
            path = AssetUtility.GetProjectRelativePath(path);
            AssetDatabase.ImportAsset(path);
            var importer = TextureImporter.GetAtPath(path) as TextureImporter;
            if (importer)
            {
                importer.alphaIsTransparency = true;
                importer.streamingMipmaps = true;
                for (int texsize = 32; texsize <= texture.width; texsize = texsize * 2)
                {
                    importer.maxTextureSize = texsize;
                }
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Texture2D ConvertToPngAndSave(string path,RenderTexture texture)
        {
            if (!texture) return null;
            byte[] bytes = Texture2Bytes(texture);
            // File.WriteAllBytes(path, bytes);
            using (var fs = new System.IO.FileStream(Path.GetFullPath(path), System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                fs.Write(bytes, 0, bytes.Length);
            }
            path = AssetUtility.GetProjectRelativePath(path);
            AssetDatabase.ImportAsset(path);
            var importer = TextureImporter.GetAtPath(path) as TextureImporter;
            if (importer)
            {
                importer.alphaIsTransparency = true;
                importer.streamingMipmaps = true;
                for (int texsize = 32; texsize <= texture.width; texsize = texsize * 2)
                {
                    importer.maxTextureSize = texsize;
                }
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Color[] GetTexturePixels(Texture2D texture)
        {
            return GetReadableTexture(texture).GetPixels();
        }
        
        public static Texture2D GetReadableTexture(Texture2D texture, int newWidth=0, int newHeight=0)
        {
            if (!texture) return null;
            int width = newWidth == 0 ? texture.width : newWidth;
            int height = newHeight == 0 ? texture.height : newHeight;
            RenderTexture rt = RenderTexture.GetTemporary( 
                width,height,0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default);
            RenderTexture currentRT = RenderTexture.active;
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;
            var t = new Texture2D(width,height);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply();
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(rt);
            return t;
        }
        
        public static RenderTexture GetReadableRenderTexture(Texture texture)
        {
            if (!texture) return null;
            RenderTexture rt = new RenderTexture(texture.width,texture.height,0,RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            rt.enableRandomWrite = true;
            rt.Create();
            RenderTexture currentRT = RenderTexture.active;
            Graphics.Blit(texture, rt);
            RenderTexture.active = currentRT;
            rt.name = texture.name;
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
            if (!texture) return null;
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
        
        public static Texture2D Texture2Texture2D(Texture texture)
        {
            if (!texture) return null;
            var result = new Texture2D( texture.width, texture.height, TextureFormat.RGBA32, false );
            var currentRT = RenderTexture.active;
            var rt = new RenderTexture( texture.width, texture.height, 32 );
            Graphics.Blit( texture, rt );
            RenderTexture.active = rt;
            var source = new Rect( 0, 0, rt.width, rt.height );
            result.ReadPixels( source, 0, 0 );
            result.Apply();
            RenderTexture.active = currentRT;
            return result;
        }

        public static Texture2DArray CreateTexture2DArray(Texture2D[] textures)
        {
            textures = textures.Where(t => t != null).ToArray();
            int widthMax = textures.Max(t => t.width);
            int width = 1;
            while (width < widthMax)
            {
                width *= 2;
            }
            
            int heightMax = textures.Max(t => t.height);
            int height = 1;
            while (height < heightMax)
            {
                height *= 2;
            }
            
            var result = new Texture2DArray(width, height, textures.Length, TextureFormat.ARGB32, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
    
            for (var i = 0; i < textures.Length; i++)
            {
                var texture = GetReadableTexture(textures[i]);
                texture = ResizeTexture(texture, width, height);
                result.SetPixels(GetTexturePixels(texture), i, 0);
            }
            result.Apply();
            return result;
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
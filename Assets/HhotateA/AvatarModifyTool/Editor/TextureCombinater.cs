using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using Color = UnityEngine.Color;

namespace HhotateA
{
    public class TextureCombinater
    {
        const int maxResolution = 4096;

        public static Texture2D CombinateSaveTexture(Texture2D[] texs,string path = null,int tilling = 0)
        {
            var combinatedTexture = CombinateTextures(texs,tilling);
            combinatedTexture.alphaIsTransparency = true;
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
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
            var i = TextureImporter.GetAtPath(path) as TextureImporter;
            i.alphaIsTransparency = true;
            i.streamingMipmaps = true;
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
            Graphics.Blit(srcTexture, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(width,height);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return t;
        }
    }
}
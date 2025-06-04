using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Editor
{
    
    
    public static class TextureImporterFixerStrict
    {
        [MenuItem("Tools/Custom/Fix Selected Textures to RGBA32")]
        [Obsolete("Obsolete")]
        public static void FixTextures()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.textureFormat = TextureImporterFormat.RGBA32; // 强制 RGBA32
                    importer.mipmapEnabled = false;
                    importer.isReadable = true;
                    importer.sRGBTexture = true;

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();

                    Debug.Log($"✔ Fixed texture: {path}");
                }
            }
        }
    }
    public class TextureArrayCreator : ScriptableWizard
    {
        [MenuItem("Tools/Texture Array Creator")]
        public static void ShowWindow()
        {
            DisplayWizard<TextureArrayCreator>("Create Texture Array", "Build Asset");
        }

        public string path = "Assets/Misc/Export/";
        public string filename = "MyTextureArray";
        public int2 targetSize = 1024;
        public List<Texture2D> textures = new();
        public bool mipmaps = true;
        private ReorderableList _list;

        void OnWizardCreate()
        {
            CompileArray(textures, path, filename);
        }

   


        private void CompileArray(List<Texture2D> tes, string p, string f)
        {
            if (tes == null || tes.Count == 0)
            {
                Debug.LogError("No textures assigned");
                return;
            }
            Texture2D sample = tes[0];
            Texture2DArray textureArray =
                new Texture2DArray(sample.width, sample.height, tes.Count, sample.format, false);
            textureArray.filterMode = FilterMode.Trilinear;
            textureArray.wrapMode = TextureWrapMode.Repeat;

            for (int i = 0; i < tes.Count; i++)
            {
                Texture2D tex = tes[i];
                textureArray.SetPixels(tex.GetPixels(0), i, 0);
            }

            textureArray.Apply();

            string uri = p + f + ".asset";
            AssetDatabase.CreateAsset(textureArray, uri);
            Debug.Log("Saved asset to " + uri);
        }
        // private void CompileArray(List<Texture2D> tes, string p, string f)
        // {
        //     if (tes == null || tes.Count == 0)
        //     {
        //         Debug.LogError("No textures assigned");
        //         return;
        //     }
        //
        //     int width = targetSize.x; // 你可以改成任何目标统一尺寸
        //     int height = targetSize.y;
        //
        //     /*Texture2DArray textureArray = new Texture2DArray(width, height, tes.Count, TextureFormat.RGBA32, mipmaps)
        //         {
        //             filterMode = FilterMode.Trilinear,
        //             wrapMode = TextureWrapMode.Repeat
        //         };*/
        //     TextureFormat format = textures[0].format;
        //     Texture2DArray textureArray = new Texture2DArray(
        //         textures[0].width,
        //         textures[0].height,
        //         textures.Count,
        //         format,
        //         mipmaps
        //     );
        //
        //
        //     for (int i = 0; i < tes.Count; i++)
        //     {
        //         Texture2D sourceTex = tes[i];
        //         if (textures[i].format != format)
        //         {
        //             Debug.LogError($"❌ Format mismatch at index {i}: {textures[i].name} format is {textures[i].format}, expected {format}");
        //             continue;
        //         }
        //         // 强制读取和转换格式
        //         Texture2D readableTex = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, mipmaps);
        //         Graphics.CopyTexture(sourceTex, readableTex); // 尽量复制像素
        //         Color[] pixels = readableTex.GetPixels();
        //
        //         // Resize 成目标尺寸
        //         Texture2D resizedTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        //         resizedTex.SetPixels(ResizeAndCopy(resizedTex,pixels, sourceTex.width, sourceTex.height, width, height));
        //         resizedTex.Apply();
        //
        //         // textureArray.SetPixels(resizedTex.GetPixels(), i);
        //         Graphics.CopyTexture(resizedTex, 0, 0, textureArray, i, 0);
        //
        //     }
        //
        //
        //     textureArray.Apply();
        //
        //     string uri = p + f + ".asset";
        //     AssetDatabase.CreateAsset(textureArray, uri);
        //     Debug.Log("Saved asset to " + uri);
        // }
        public static Color[] ResizeAndCopy( Texture2D tex, Color[] originalPixels, int originalWidth, int originalHeight, int targetWidth, int targetHeight)
        {
            Texture2D temp = new Texture2D(originalWidth, originalHeight);
            temp.SetPixels(originalPixels);
            temp.Apply();

            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            Graphics.Blit(temp, rt);

            RenderTexture.active = rt;
            Texture2D resized = new Texture2D(targetWidth, targetHeight);
            resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            resized.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return resized.GetPixels();
        }

    }
}
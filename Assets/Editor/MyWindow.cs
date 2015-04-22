using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

class CellUnityWindow : EditorWindow
{
    // Debug function to instanciate molecules in edit mode
    public static class MyMenuCommands
    {
        [MenuItem("My Commands/Load volume texture")]
        static void FirstCommand()
        {
            int n = 128;
            int size = n * n * n;
            Color[] volumeColors = new Color[size];

            var bytes = File.ReadAllBytes(@"D:\Projects\Unity5\VolumeRayTracing\Data\field.bin");
            var fieldData = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, fieldData, 0, bytes.Length);

            for (int i = 0; i < size; i ++)
            {
                volumeColors[i].a = fieldData[i];
            }

            var texture3D = new Texture3D(n, n, n, TextureFormat.Alpha8, true);
            texture3D.SetPixels(volumeColors);
            texture3D.wrapMode = TextureWrapMode.Clamp;
            texture3D.anisoLevel = 0;
            texture3D.Apply();

            string path = "Assets/field_texture.asset";

            Texture3D tmp = (Texture3D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture3D));
            if (tmp)
            {
                AssetDatabase.DeleteAsset(path);
                tmp = null;
            }

            AssetDatabase.CreateAsset(texture3D, path);
            AssetDatabase.SaveAssets();

            // Print the path of the created asset
            Debug.Log(AssetDatabase.GetAssetPath(texture3D));
        }

        //[MenuItem("My Commands/Add volume texture")]
        //static void FirstCommand()
        //{
        //    int n = 128;
        //    int size = n * n * n;
        //    Color[] volumeColors = new Color[size];

        //    // Clear cols
        //    for (int i = 0; i < size; i++) volumeColors[i].a = 0;

        //    float scale = 1.5f;

        //    // Fill cols
        //    foreach (var atom in PdbLoader.LoadPdbFile("1w6k"))
        //    {
        //        float radius = atom.w * scale;
        //        float radiusSqr = radius * radius;
        //        Vector3 pos = (Vector3)atom * scale + new Vector3(n, n, n) * 0.5f;

        //        Vector3 pos_int = new Vector3((int)pos.x, (int)pos.y, (int)pos.z);
        //        Vector3 round_offset = pos - pos_int;

        //        int influenceRadius = (int)radius * 3;

        //        for (int x = -influenceRadius; x <= influenceRadius; x++)
        //        {
        //            for (int y = -influenceRadius; y <= influenceRadius; y++)
        //            {
        //                for (int z = -influenceRadius; z <= influenceRadius; z++)
        //                {
        //                    Vector3 local = new Vector3(x, y, z);
        //                    Vector3 global = pos_int + local;

        //                    if (global.x < 0 || global.y < 0 || global.z < 0) continue;
        //                    if (global.x >= n || global.y >= n || global.z >= n) continue;

        //                    int idx = (int)global.x + (int)global.y * n + (int)global.z * n * n;

        //                    // Gaussian surface formula from: https://bionano.cent.uw.edu.pl/Software/SurfaceDiver/UsersManual/Surface
        //                    var r = Mathf.Pow(Vector3.Distance(local, round_offset), 2);
        //                    float a = -Mathf.Log(2) * r / (radiusSqr);
        //                    float gauss_f = 2 * Mathf.Exp(a);

        //                    volumeColors[idx].a += gauss_f;
        //                }
        //            }
        //        }
        //    }

        //    var texture3D = new Texture3D(n, n, n, TextureFormat.Alpha8, true);
        //    texture3D.SetPixels(volumeColors);
        //    texture3D.wrapMode = TextureWrapMode.Clamp;
        //    texture3D.anisoLevel = 0;
        //    texture3D.Apply();

        //    string path = "Assets/volume_texture.asset";

        //    Texture3D tmp = (Texture3D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture3D));
        //    if (tmp)
        //    {
        //        AssetDatabase.DeleteAsset(path);
        //        tmp = null;
        //    }

        //    AssetDatabase.CreateAsset(texture3D, path);
        //    AssetDatabase.SaveAssets();

        //    // Print the path of the created asset
        //    Debug.Log(AssetDatabase.GetAssetPath(texture3D));
        //}
    }
}
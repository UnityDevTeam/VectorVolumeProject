using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Linq;
using System.Globalization;
using Assets.Editor.MVVReader;

class MyWindow : EditorWindow
{

    // Debug function to instanciate molecules in edit mode
    public static class MyMenuCommands
    {

        static public float myGameTime;
        static public Material currentMaterial;

        //static public MVVTimer timer;

        static void doIt()
        {
            myGameTime += 0.01f;
            Selection.activeGameObject.GetComponent<Renderer>().material.SetFloat("time", myGameTime);
            SceneView.RepaintAll();
        }

        [MenuItem("My Commands/Start Timer")]
        static void StartTime()
        {
            EditorApplication.update += doIt;
            
            
        }

        [MenuItem("My Commands/Stop Timer")]
        static void StopTime()
        {
            EditorApplication.update -= doIt;
        }

        [MenuItem("My Commands/Test")]
        static void Test()
        {
            //Debug.Log(System.Runtime.InteropServices.Marshal.SizeOf(typeof(Region)));

            Material mat = Selection.activeGameObject.GetComponent<Renderer>().sharedMaterial;
            // Creating Cubic Lookup Table (From Lvid Wang)
            Debug.Log("Creating Cubic Lookup Table");

            Color[] lookup_data = new Color[256];

            for (int i = 0; i < 256; i++)
            {
                float x = (float)i / 255.0f;
                float x_sqr = x * x;
                float x_cub = x * x_sqr;

                float w0 = (-x_cub + 3.0f * x_sqr - 3.0f * x + 1.0f) / 6.0f;
                float w1 = (3.0f * x_cub - 6.0f * x_sqr + 4.0f) / 6.0f;
                float w2 = (-3.0f * x_cub + 3.0f * x_sqr + 3.0f * x + 1.0f) / 6.0f;
                float w3 = x_cub / 6.0f;

                lookup_data[i] = new Color(1.0f - w1 / (w0 + w1) + x, // h0(x)
                                           1.0f + w3 / (w2 + w3) - x, // h1(x)
                                           w0 + w1,                   // g0(x)
                                           1);
            }

            Texture2D lookup = new Texture2D(256, 1, TextureFormat.RGBAFloat, true);
            lookup.anisoLevel = 0;
            lookup.SetPixels(lookup_data);
            lookup.filterMode = FilterMode.Trilinear;
            lookup.wrapMode = TextureWrapMode.Repeat;

            mat.SetTexture("_cubicLookup", lookup);
        }

        [MenuItem("My Commands/Load MVVObject")]
        static void LoadMVVObject()
        {
            MVVRoot obj = new MVVRoot();
            /*var path = EditorUtility.OpenFilePanel(
                    "XML file",
                    "",
                    "xml");*/
            obj.readFromFile("C:\\Users\\orakeldel\\Documents\\Uni\\Ideen\\Ivan\\orange\\orange3.xml", "ORANGE");
            //obj.readFromFile("C:\\Users\\orakeldel\\Documents\\Uni\\Ideen\\Ivan\\orange\\orange_nobitmap.xml", "ORANGE");
            //obj.readFromFile(path, "ORANGE");
            currentMaterial = obj.getMaterial("Assets/Resources/MVVShader.shader");
            Selection.activeGameObject.GetComponent<Renderer>().material = currentMaterial;
            obj.passToShader(Selection.activeGameObject.GetComponent<Renderer>().material);
            
        }

        [MenuItem("My Commands/Load volume texture")]
        static void FirstCommand()
        {
            int n = 128;
            int size = n * n * n;
            Color[] volumeColors = new Color[size];

            var bytes = File.ReadAllBytes(@"D:\Projects\Unity5\VolumeRayTracing\Data\field.bin");
            var fieldData = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, fieldData, 0, bytes.Length);

            for (int i = 0; i < size; i++)
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

        [MenuItem("My Commands/Load *.sdf file from SDFGen")]
        static void LoadSDFCommand()
        {

            var path = EditorUtility.OpenFilePanel(
                    "SDF file",
                    "",
                    "sdf");

            Debug.Log("Loading sdf file: " + path);

            StreamReader file = new StreamReader(path);

            var sizes = file.ReadLine();
            var sizeArr = sizes.Split(new char[] { ' ' });
            int[] sizeFromFile = new int[] { Convert.ToInt32(sizeArr[0]), Convert.ToInt32(sizeArr[1]), Convert.ToInt32(sizeArr[2]) };

            file.ReadLine(); //Origin ignored for now
            file.ReadLine(); //Cell spacing ignored for now

            var pow = Math.Floor(Math.Log(sizeFromFile.Max(), 2)) + 1;
            int useSize = Convert.ToInt32(Math.Pow(2, pow));

            var size = new int[] { useSize, useSize, useSize };
            //float.Parse("41.00027357629127", CultureInfo.InvariantCulture.NumberFormat);

            var linearsize = size[0] * size[1] * size[2];

            Color[] volumeColors = new Color[linearsize];
            String line;

            Debug.Log("Size:" + sizeFromFile[0] + ", " + sizeFromFile[1] + ", " + sizeFromFile[2]);

            float min = float.MaxValue;
            float max = float.MinValue;

            for (int x = 0; x < size[2]; x++)
            {
                for (int y = 0; y < size[1]; y++)
                {
                    for (int z = 0; z < size[0]; z++)
                    {
                        var idx = z + size[1] * y + size[1] * size[2] * x;
                        if (x >= sizeFromFile[2])
                        {
                            volumeColors[idx].a = float.MaxValue;
                            continue;
                        }
                        if (y >= sizeFromFile[1])
                        {
                            volumeColors[idx].a = float.MaxValue;
                            continue;
                        }
                        if (z >= sizeFromFile[0])
                        {
                            volumeColors[idx].a = float.MaxValue;
                            continue;
                        }
                        if ((line = file.ReadLine()) == null)
                        {
                            volumeColors[idx].a = float.MaxValue;
                            continue;
                        }
                        var f = float.Parse(line, CultureInfo.InvariantCulture.NumberFormat);
                        min = Math.Min(min, f);
                        max = Math.Max(max, f);
                        volumeColors[idx].a = f;
                    }
                }
            }
            //bring in range 0..1 with a 0.5 isosurface
            /*if (max < 0) { max = 0; }
            if (min > 0) { min = 0; }

            var max2 = max * 2;
            var min2 = min * 2;


            Debug.Log("Min: " + min + ", Max: " + max);

            for (int i = 0; i < linearsize; i++)
            {
                if (volumeColors[i].a == float.MaxValue) volumeColors[i].a = max;

                if (volumeColors[i].a > 0)
                {
                    volumeColors[i].a = volumeColors[i].a / max2 + 0.5f;
                }
                else
                {
                    volumeColors[i].a = 0.5f - volumeColors[i].a / min2;
                }
            }*/
            var minmax = max - min;

            for (int i = 0; i < linearsize; i++)
            {
                if (volumeColors[i].a == float.MaxValue) volumeColors[i].a = max;

                volumeColors[i].a =  (volumeColors[i].a - min) / minmax ;
            }


            var texture3D = new Texture3D(size[0], size[1], size[2], TextureFormat.Alpha8, true);
            texture3D.SetPixels(volumeColors);
            texture3D.wrapMode = TextureWrapMode.Clamp;
            texture3D.anisoLevel = 0;
            texture3D.Apply();

            string assetPath = "Assets/mesh_sdf_texture.asset";

            Texture3D tmp = (Texture3D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture3D));
            if (tmp)
            {
                AssetDatabase.DeleteAsset(assetPath);
                tmp = null;
            }

            AssetDatabase.CreateAsset(texture3D, assetPath);
            AssetDatabase.SaveAssets();

            // Print the path of the created asset
            Debug.Log("Saved to" + AssetDatabase.GetAssetPath(texture3D));
        }


        [MenuItem("My Commands/Load *.lxn file")]
        static void LoadLXNCommand()
        {

            var path = EditorUtility.OpenFilePanel(
                    "LXN file",
                    "",
                    "lxn");

            Debug.Log("Loading lxn file: " + path);

            //StreamReader file = new StreamReader(path);

            MVVSDFFile sdf = new MVVSDFFile(File.ReadAllBytes(path), false);

            var dim = new int[3];

            for (int i = 0; i < 3; i++)
            {
                dim[i] = (int)Math.Pow(2, Math.Ceiling(Math.Log(sdf.sizes[i], 2)));
            }

            Texture3D texture3D = new Texture3D(dim[0], dim[1], dim[2], TextureFormat.Alpha8, false);
            var data = new Color[dim[0] * dim[1] * dim[2]];
            for (int x = 0; x < dim[0]; x++)
            {
                for (int y = 0; y < dim[1]; y++)
                {
                    for (int z = 0; z < dim[2]; z++)
                    {
                        var idx = z + dim[1] * y + dim[1] * dim[2] * x;
                        var idx2 = z + sdf.sizes[1] * y + sdf.sizes[1] * sdf.sizes[2] * x;
                        if (idx2 < sdf.volumeColors.Length)
                        {
                            var stuff = sdf.volumeColors[idx2].a;
                            data[idx].a = stuff;
                        }
                        else
                        {
                            data[idx].a = 1.0f;
                        }
                    }
                }
            }
            texture3D.SetPixels(data);
            texture3D.filterMode = FilterMode.Bilinear;
            switch (sdf.addressMode)
            {
                case 1: texture3D.wrapMode = TextureWrapMode.Repeat; break;
                default: texture3D.wrapMode = TextureWrapMode.Clamp; break;
            }

            texture3D.anisoLevel = 0;
            texture3D.Apply();

            string assetPath = "Assets/lxn_texture.asset";

            Texture3D tmp = (Texture3D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture3D));
            if (tmp)
            {
                AssetDatabase.DeleteAsset(assetPath);
                tmp = null;
            }

            AssetDatabase.CreateAsset(texture3D, assetPath);
            AssetDatabase.SaveAssets();

            // Print the path of the created asset
            Debug.Log("Saved to" + AssetDatabase.GetAssetPath(texture3D));

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

using System;
using System.IO;
using UnityEngine;

namespace DistanceFieldBaker
{
    class Program
    {
        static void Main(string[] args)
        {
            var size = 128;
            var half_size = size/2;
            var arraySize = size*size*size;

            var field = new float[arraySize];

            for (var i = 0; i < 128; i++)
            {
                for (int j = 0; j < 128; j++)
                {
                    for (int k = 0; k < 128; k++)
                    {
                        var v = new Vector3(i, j, k) / size;
                        v -= new Vector3(0.5f, 0.5f, 0.5f);

                        var res = Vector3.Magnitude(v) - 0.45f;

                        int idx = i + j * size + k * size * size;
                        field[idx] = res;
                    }
                }
            }

            var byteArray = new byte[field.Length * sizeof(float)];
            Buffer.BlockCopy(field, 0, byteArray, 0, byteArray.Length);

            BinaryWriter writer = new BinaryWriter(File.Open(@"D:\Projects\Unity5\VolumeRayTracing\Data\field.bin", FileMode.Create));
            writer.Write(byteArray);

            //var texture3D = new Texture3D(size, size, size, TextureFormat.Alpha8, true);
            //texture3D.SetPixels(field);
            //texture3D.wrapMode = TextureWrapMode.Clamp;
            //texture3D.anisoLevel = 0;
            //texture3D.Apply();

            //string path = "Assets/test_field.asset";

            //Texture3D tmp = (Texture3D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture3D));
            //if (tmp)
            //{
            //    AssetDatabase.DeleteAsset(path);
            //    tmp = null;
            //}

            //AssetDatabase.CreateAsset(texture3D, path);
            //AssetDatabase.SaveAssets();

            int a = 0;
        }
    }
}

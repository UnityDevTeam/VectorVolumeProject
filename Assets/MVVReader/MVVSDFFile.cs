using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.MVVReader
{
    public class MVVSDFFile : IComparable
    {
        public char[] magicWord = new char[4]; // Must be 'L', 'X', 'N', '\0'
        public int channelTypeId;              // int: 0, uint: 1
        public int dimension;                  // Should be 3
        public int numChannels;                // Should be 1
        public int[] sizes = new int[10];      // Size of dimensions (should only contain 3 values)
        public int addressMode;                // Can be clamp(0), wrap(1), mirror(2)
        public int hasBBox;                    // if BoundingBox is there, better be...
        public float[] bboxMin = new float[3]; // lower corner
        public float[] bboxMax = new float[3]; // upper corner
        public int[] reserved = new int[10];   // Reserved for whatever 

        public Color[] volumeColors;           // Saves the color of these texture 

        public int[] index = new int[3];       // Stores index of texture in global 3d atlas


        private int linearSize;                 // Just for array-building

        public MVVSDFFile(String filename) : this(File.ReadAllBytes(filename))
        {
        }

        public MVVSDFFile(byte[] bytes)
        {
            magicWord[0] = BitConverter.ToChar(bytes, 0);
            magicWord[1] = BitConverter.ToChar(bytes, 1);
            magicWord[2] = BitConverter.ToChar(bytes, 2);
            magicWord[3] = BitConverter.ToChar(bytes, 3);
            channelTypeId = BitConverter.ToInt32(bytes, 4);
            dimension = BitConverter.ToInt32(bytes, 8);
            numChannels = BitConverter.ToInt32(bytes, 12);
            for (int i = 16; i < 56; i += 4)
            {
                sizes[(i - 16) / 4] = BitConverter.ToInt32(bytes, i);
            }
            addressMode = BitConverter.ToInt32(bytes, 56);
            hasBBox = BitConverter.ToInt32(bytes, 60);
            bboxMin[0] = BitConverter.ToInt32(bytes, 64);
            bboxMin[1] = BitConverter.ToInt32(bytes, 68);
            bboxMin[2] = BitConverter.ToInt32(bytes, 72);
            bboxMax[0] = BitConverter.ToInt32(bytes, 76);
            bboxMax[1] = BitConverter.ToInt32(bytes, 80);
            bboxMax[2] = BitConverter.ToInt32(bytes, 84);
            for (int i = 88; i < 128; i += 4)
            {
                reserved[(i - 88) / 4] = BitConverter.ToInt32(bytes, i);
            }

            linearSize = sizes[0] * sizes[1] * sizes[2];
            
            byte[] databytes = new byte[linearSize * 4];

            // Load data to 3D texture
            volumeColors = new Color[linearSize];

            Array.Copy(bytes, 128, databytes, 0, linearSize * 4);

            for (int i = 0; i < volumeColors.Length; i++)
            {
                volumeColors[i].a = BitConverter.ToSingle(databytes, i * 4);
            }
            //Debug.Log("Creating Texture (" + sizes[0] + ", " + sizes[1] + ", " + sizes[2]+ ")");
            //texture3D = new Texture3D(sizes[0], sizes[1], sizes[2], TextureFormat.Alpha8, false);
            //texture3D.SetPixels(volumeColors);
            //texture3D.filterMode = FilterMode.Bilinear;
            //switch (addressMode)
            //{
            //    case 1: texture3D.wrapMode = TextureWrapMode.Repeat; break;
            //    default: texture3D.wrapMode = TextureWrapMode.Clamp; break;
            //}
            
            //texture3D.anisoLevel = 0;
            //texture3D.Apply();
        }

        public int CompareTo(object obj)
        {
            return this.sizes[0] * this.sizes[1] * this.sizes[2] - ((MVVSDFFile)obj).sizes[0] * ((MVVSDFFile)obj).sizes[1] * ((MVVSDFFile)obj).sizes[2];
        }
    }
}

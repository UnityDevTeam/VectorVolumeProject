using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Assets.Editor.MVVReader
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

        // AABD: Stores the bounding box as aabb
        public MVVTransform aabb;
        public MVVVolume volume;


        private int linearSize;                 // Just for array-building

        public MVVSDFFile(String filename) : this(File.ReadAllBytes(filename), false)
        {
        }

        /// <summary>
        /// Load sdf from data
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="newFormat">true if new file format is used</param>
        public MVVSDFFile(byte[] bytes, bool newFormat)
        {
            // Loading new format:
            if (!newFormat)
            {
                loadOld(bytes);
                return;
            }

            StreamReader file = new StreamReader(new MemoryStream(bytes));

            var size = file.ReadLine();
            var sizeArr = size.Split(new char[] { ' ' });
            sizes[0] = Convert.ToInt32(sizeArr[0]);
            sizes[1] = Convert.ToInt32(sizeArr[1]);
            sizes[2] = Convert.ToInt32(sizeArr[2]);

            var linearsize = sizes[0] * sizes[1] * sizes[2];

            Color[] volumeColors = new Color[linearsize];
            String line;

            float min = float.MaxValue;
            float max = float.MinValue;

            for (int x = 0; x < sizes[0]; x++)
            {
                for (int y = 0; y < sizes[1]; y++)
                {
                    for (int z = 0; z < sizes[2]; z++)
                    {
                        var idx = z + sizes[1] * y + sizes[1] * sizes[2] * x;
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
            //bring in range -1..1 with a 0 isosurface

            Debug.Log("Min: " + min + ", Max: " + max);

            var minmax = max-min;

            for (int i = 0; i < linearsize; i++)
            {
                if (volumeColors[i].a == float.MaxValue) volumeColors[i].a = max;

                volumeColors[i].a = 2 * (volumeColors[i].a - min) / minmax - 1;
            }

            aabb = new MVVTransform();
            volume = this.getVolume();

        }

        public void loadOld(byte[] bytes)
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
            bboxMin[0] = BitConverter.ToSingle(bytes, 64);
            bboxMin[1] = BitConverter.ToSingle(bytes, 68);
            bboxMin[2] = BitConverter.ToSingle(bytes, 72);
            bboxMax[0] = BitConverter.ToSingle(bytes, 76);
            bboxMax[1] = BitConverter.ToSingle(bytes, 80);
            bboxMax[2] = BitConverter.ToSingle(bytes, 84);

            //Debug.Log("Bounding Box: " + bboxMin[0] + ", " + bboxMin[1] + ", " + bboxMin[2] + " ---> " + bboxMax[0] + ", " + bboxMax[1] + ", " + bboxMax[2]);

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
                volumeColors[i].a = BitConverter.ToSingle(databytes, i * 4)/2.0f+0.5f;
            }
            // Create AABB
            bboxMax[0] = bboxMax[0] * 2 - 1;
            bboxMax[1] = bboxMax[1] * 2 - 1;
            bboxMax[2] = bboxMax[2] * 2 - 1;
            bboxMin[0] = bboxMin[0] * 2 - 1;
            bboxMin[1] = bboxMin[1] * 2 - 1;
            bboxMin[2] = bboxMin[2] * 2 - 1;
            var diffX = (bboxMax[0] - bboxMin[0]) / 2.0f;
            var diffY = (bboxMax[1] - bboxMin[1]) / 2.0f;
            var diffZ = (bboxMax[2] - bboxMin[2]) / 2.0f;
            aabb = new MVVTransform();
            aabb.position = new Vector3(bboxMin[0] + diffX, bboxMin[1] + diffY, bboxMin[2] + diffZ);
            aabb.scale = new Vector3(diffX, diffY, diffZ);
            Debug.Log("Box: " + "(" + bboxMax[0] + ", " + bboxMax[1] + ", " + bboxMax[2] + ") - (" + bboxMin[0] + ", " + bboxMin[1] + ", " + bboxMin[2] + ")");
            Debug.Log("Box: " + "(" + aabb.position[0] + ", " + aabb.position[1] + ", " + aabb.position[2] + ") - (" + aabb.scale[0] + ", " + aabb.scale[1] + ", " + aabb.scale[2] + ")");
            volume = this.getVolume();
        }

        public int CompareTo(object obj)
        {
            return this.sizes[0] * this.sizes[1] * this.sizes[2] - ((MVVSDFFile)obj).sizes[0] * ((MVVSDFFile)obj).sizes[1] * ((MVVSDFFile)obj).sizes[2];
        }

        public MVVVolume getVolume()
        {
            MVVVolume volume = new MVVVolume();
            volume.aabb = this.aabb;
            volume.colors = this.volumeColors;
            volume.dimension = new int[] { sizes[0], sizes[1], sizes[2] };
            return volume;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Editor
{
    class SDFFile
    {
        public char[] magicWord = new char[4];
        public int channelTypeId;
        public int dimension;
        public int numChannels;
        public int[] sizes = new int[10];
        public int addressMode;
        public int hasBBox;
        public float[] bboxMin = new float[3];
        public float[] bboxMax = new float[3];
        public int[] reserved = new int[10];
        public int linearSize;

        public float[] data;

        public SDFFile(byte[] bytes)
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
                sizes[(i-16)/4] = BitConverter.ToInt32(bytes, i);
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
                reserved[(i-88)/4] = BitConverter.ToInt32(bytes, i);
            }

            linearSize = sizes[0] * sizes[1] * sizes[2];
            data = new float[linearSize];

            byte[] databytes = new byte[linearSize * 4];

            Array.Copy(bytes, 128, databytes, 0, linearSize * 4);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = BitConverter.ToSingle(databytes, i * 4);
            }

        }
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace Assets.Editor.MVVReader
{
    public class MVVVolume : IComparable
    {
        public Color[] colors;
        public int[] dimension;
        public Vector3 index;
        public Vector3 size;
        public MVVTransform aabb;

        public int CompareTo(object obj)
        {
            return this.dimension[0] * this.dimension[1] * this.dimension[2] - ((MVVVolume)obj).dimension[0] * ((MVVVolume)obj).dimension[1] * ((MVVVolume)obj).dimension[2];
        }
    }
}

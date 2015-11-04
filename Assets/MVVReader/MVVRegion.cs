using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Assets.MVVReader
{
    public enum MVVRegionType
    {
        EMPTY,  // No color (transparent)
        BITMAP, // Take color from texture
        COLOR,  // Solid Color
        RBF     // Color from RBF (how does that work?)
    }

    public class MVVRegion : Object
    {
        public int index;
        public string identifier;   // Unique Name
        public MVVRegionType type;  // Type of Region
        public Texture2D image;     // For type Bitmap
        public Color color;         // For type Color
        public float opacity = 1.0f;// Overrides color
        public MVVTransform transform; // For Bitmap transform
        public List<MVVIndex> embedded_objects = new List<MVVIndex>(); // Embedded Objects

        /// <summary>
        /// Load image from path
        /// </summary>
        /// <param name="path"></param>
        internal void loadImage(string path)
        {
            image = new Texture2D(1, 1); // image size will change on loadimage
            image.LoadImage(File.ReadAllBytes(path));
        }

    }
}

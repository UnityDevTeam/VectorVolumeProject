using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Assets.Editor.MVVReader
{
    /// <summary>
    /// Type of a region
    /// </summary>
    public enum MVVRegionType:int
    {
        EMPTY = 0,  // No color (transparent)
        BITMAP = 1, // Take color from texture
        COLOR = 2,  // Solid Color
        RBF = 3    // Color from RBF (how does that work?)
    }

    /// <summary>
    /// A region, stores all information and includes image loading
    /// </summary>
    public class MVVRegion : Object
    {
        public int index;
        public string identifier;   // Unique Name
        public MVVRegionType type;  // Type of Region
        public Color color;         // For type Color
        public float opacity = 1.0f;// Overrides color
        public MVVTransform transform; // For Bitmap transform
        public List<MVVIndex> embedded_objects = new List<MVVIndex>(); // Embedded Objects
        public MVVVolume volume = new MVVVolume(); //The volumetric image

        /// <summary>
        /// Load image from path, the image has to have the size NxN^2
        /// </summary>
        /// <param name="path"></param>
        internal void loadImage(string path)
        {
            Debug.Log("Loading image: " + path);
            Texture2D image = new Texture2D(1, 1); // image size will change on loadimage
            image.LoadImage(File.ReadAllBytes(path));
            volume.dimension = new int[] { image.width + 2, image.width + 2, image.width + 2};
            Color[] colors = image.GetPixels();

            // Put a 1px wrap border around the image, for automatic blending to work
            volume.colors = new Color[(image.width + 2) * (image.width + 2) * (image.width + 2)];
            int linearIndex = 0;
            int localIndex = 0;
            int xterm = 0;
            int yterm = 0;
            int zterm = 0;
            for (int x = 0; x < image.width + 2; x++)
            {
                for (int y = 0; y < image.width + 2; y++)
                {
                    for (int z = 0; z < image.width + 2; z++)
                    {
                        linearIndex = x + y * (image.width + 2) + z * (image.width + 2) * (image.width + 2);
                        if (x == 0) xterm = image.width - 1;
                        else if (x == image.width + 1) xterm = 0;
                        else xterm = x - 1;
                        if (y == 0) yterm = image.width - 1;
                        else if (y == image.width + 1) yterm = 0;
                        else yterm = y - 1;
                        if (z == 0) zterm = image.width - 1;
                        else if (z == image.width + 1) zterm = 0;
                        else zterm = z - 1;
                        localIndex = xterm + yterm * (image.width) + zterm * (image.width) * (image.width);


                        Color tmp = colors[localIndex];
                        volume.colors[linearIndex] = tmp;
                    }
                }
            }
        }

    }
}

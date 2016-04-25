using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Assets.Editor.MVVReader
{
    /// <summary>
    /// Type of an SDF
    /// </summary>
    public enum MVVSDFType:int
    {
        DEFAULT = 0, // Standard SDF...
        SEEDING = 1, // Multiple Instancing of SDF (using seed files)
        TILING = 2  // Tile in all directions
    }

    /// <summary>
    /// All SDF informations are stored here
    /// </summary>
    public class MVVSDF : MVVIndexedObject
    {
        public int index;                   // ID in shader
        public string identifier;           // Unique Name
        public MVVSDFFile file;             // File
        public MVVTransform transform;      // Transform of SDF   
        public MVVSDFType type = MVVSDFType.DEFAULT; // Type of SDF
        //public MVVTransform[] seedTransforms;  // Seed Transforms, only used if type=SEEDING
        public MVVIndex seedIndex;          // Index for seeded SDFs
        public float offset = 0f;           // offset iso-surface
        public int functionID;              // ID of function in the shader
        public string function;             // Function string


        /// <summary>
        /// Load SDF from File and save it to texture3d
        /// </summary>
        /// <param name="path">absoute file path</param>
        /// <returns>MVVSDFFile that has been generated</returns>
        public MVVSDFFile loadSDF(string path)
        {
            if (path.EndsWith(".obj"))
            {
                file = new MVVSDFFile(File.ReadAllBytes(path), true);
            }
            else
            {
                file = new MVVSDFFile(File.ReadAllBytes(path), false);
            }

            return file;
        }
    }
}

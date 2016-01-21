using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Assets.Editor.MVVReader
{
    public enum MVVSDFType:int
    {
        DEFAULT = 0, // Standard SDF...
        SEEDING = 1, // Multiple Instancing of SDF (using seed files)
        TILING = 2  // Tile in all directions
    }

    public class MVVSDF : MVVIndexedObject
    {
        public int index;
        public string identifier;           // Unique Name
        public MVVSDFFile file;             // File
        public MVVTransform transform;      // Transform of SDF   
        public MVVSDFType type = MVVSDFType.DEFAULT; // Type of SDF
        //public MVVTransform[] seedTransforms;  // Seed Transforms, only used if type=SEEDING
        public MVVIndex seedIndex;
        public float offset = 0f;              // offset iso-surface
        public int functionID;
        public string function;


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

        /*/// <summary>
        /// Loads a seed file with seed positions.
        /// </summary>
        /// <param name="path">absolute file path</param>
        public void loadSeedFile(string path)
        {
            var seedList = MVVSeedHelper.loadSeedFile(path);
            seedTransforms = new MVVTransform[seedList.Length];
            for (int i = 0; i < seedList.Length; i++){
                seedTransforms[i] = new MVVTransform(seedList[i]*2 + transform.position);
                seedTransforms[i].rotation = transform.rotation;
                seedTransforms[i].scale = transform.scale;
            }
        }

        /// <summary>
        /// Loads a seed file with seed locations.
        /// loadSeedFile must be called first.
        /// </summary>
        /// <param name="path">absolute file path</param>
        public void loadSeedRotationFile(string path)
        {
            var seedList = MVVSeedHelper.loadSeedFile(path);
            for (int i = 0; i < seedList.Length; i++)
            {
                seedTransforms[i].rotation += seedList[i];
            }
        }

        /// <summary>
        /// Loads a seed file with seed positions.
        /// loadSeedFile must be called first.
        /// </summary>
        /// <param name="path">absolute file path</param>
        public void loadSeedScaleFile(string path)
        {

            var seedList = MVVSeedHelper.loadSeedFile(path);
            for (int i = 0; i < seedList.Length; i++)
            {
                seedTransforms[i].scale.Scale(seedList[i]);
                
            }

        }*/
    }
}

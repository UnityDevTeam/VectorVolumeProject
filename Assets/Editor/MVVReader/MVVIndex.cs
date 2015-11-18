using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace Assets.Editor.MVVReader
{
    public class MVVIndex
    {
        public int index;
        public MVVTransform transform; // Transform of index, no rotation (for now...)
        public List<MVVEmbedded> embedded_objects = new List<MVVEmbedded>(); // All objects in this Index
        public int[] index_size = new int[3]; // number of divisions in each direction (int)
        public List<MVVEmbedded>[, ,] embedded_indexed_objects; // Objects that are contained in indices as 3d-array
        public bool use_index; // true if index is used

        public MVVIndex() { }

        /// <summary>
        /// Creates an index with only one region element
        /// </summary>
        /// <param name="mvv_object"></param>
        /// <param name="transform"></param>
        public MVVIndex(MVVObject mvv_object, MVVTransform transform)
        {
            this.transform = new MVVTransform();
            embedded_objects.Add(new MVVEmbedded(mvv_object, transform));
            use_index = false;

        }

        /// <summary>
        /// Load embedded Objects to list
        /// </summary>
        /// <param name="mvv_object">object</param>
        /// <param name="seedfile">translation file (needed)</param>
        /// <param name="rotatefile">rotate file</param>
        /// <param name="scalefile">scale file</param>
        /// <param name="globalTransform">Transfrom that is done for each seed additionally</param>
        public void addObjectsFromSeed(MVVObject mvv_object, string seedfile, string rotatefile, string scalefile, MVVTransform globalTransform)
        {
            var seedPositions = MVVSeedHelper.loadSeedFile(seedfile);
            var seedRotations = MVVSeedHelper.loadSeedFile(rotatefile);
            var seedScales = MVVSeedHelper.loadSeedFile(scalefile);
            for (int i = 0; i < seedPositions.Length; i++)
            {
                MVVTransform transform = new MVVTransform();
                transform.position = seedPositions[i] + globalTransform.position;
                if (seedRotations != null) transform.rotation = seedRotations[i] + globalTransform.rotation;
                if (seedScales != null) transform.scale = Vector3.Scale(seedScales[i], globalTransform.scale);
                var embedded = new MVVEmbedded(mvv_object, transform);
                embedded_objects.Add(embedded);
            }
            use_index = true;
        }

        /// <summary>
        /// Adds embedded_objects to embedded_indexed_objects
        /// </summary>
        public void buildIndex()
        {
            MVVOBB[,,] index_obbs = getIndexMVVOBBs(); //obb will only be traslated...
            embedded_indexed_objects = new List<MVVEmbedded>[index_size[0], index_size[1], index_size[2]];
            for (var x = 0; x < index_size[0]; x++)
            {
                for (var y = 0; y < index_size[1]; y++)
                {
                    for (var z = 0; z < index_size[2]; z++)
                    {
                        embedded_indexed_objects[x, y, z] = new List<MVVEmbedded>();
                        foreach (MVVEmbedded embedded in embedded_objects)
                        {
                            if (index_obbs[x, y, z].Intersects(MVVOBB.Transform(embedded.mvv_object.outerBounds, embedded.transform)))
                            {
                                embedded_indexed_objects[x, y, z].Add(embedded);
                            }
                        }
                    }
                }
            }
        }

        private MVVOBB[,,] getIndexMVVOBBs()
        {
            // Calc scale for one OBB
            Vector3 scale = Vector3.Scale(transform.scale, 
                                          new Vector3(1f / index_size[0], 1f / index_size[1], 1f / index_size[2]));
            MVVOBB[,,] result = new MVVOBB[index_size[0], index_size[1], index_size[2]];
            Vector3 min = transform.position - Vector3.Scale(transform.scale, new Vector3(.5f, .5f, .5f));
            for (var x = 0; x < index_size[0]; x++)
            {
                for (var y = 0; y < index_size[1]; y++)
                {
                    for (var z = 0; z < index_size[2]; z++)
                    {
                        result[x, y, z] = new MVVOBB();
                        result[x, y, z].transform.scale = scale;
                        result[x, y, z].transform.position = min + Vector3.Scale(scale, new Vector3(x, y, z));
                    }
                }
            }
            return result;
        }
    }
}

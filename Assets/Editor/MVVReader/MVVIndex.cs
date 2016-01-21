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
        public void addObjectsFromSeed(MVVIndexedObject mvv_object, string seedfile, string rotatefile, string scalefile, MVVTransform globalTransform)
        {
            globalTransform.createMatrix();
            Debug.Log("Global:" + globalTransform.matrix.ToString());
            var seedPositions = MVVSeedHelper.loadSeedFile(seedfile);
            var seedRotations = MVVSeedHelper.loadSeedFile(rotatefile);
            var seedScales = MVVSeedHelper.loadSeedFile(scalefile);
            for (int i = 0; i < seedPositions.Length; i++)
            {
                MVVTransform transform = new MVVTransform();
                transform.position = seedPositions[i]*2 + globalTransform.position;
                transform.rotation = globalTransform.rotation;
                transform.scale = globalTransform.scale;
                if (seedRotations != null) transform.rotation = seedRotations[i] + transform.rotation;
                if (seedScales != null) transform.scale = Vector3.Scale(seedScales[i], transform.scale);
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
            //MVVOBB[,,] index_obbs = getIndexMVVOBBs(); //obb will only be traslated...
            embedded_indexed_objects = new List<MVVEmbedded>[index_size[0], index_size[1], index_size[2]];
            /*for (var x = 0; x < index_size[0]; x++)
            {
                for (var y = 0; y < index_size[1]; y++)
                {
                    for (var z = 0; z < index_size[2]; z++)
                    {
                        embedded_indexed_objects[x, y, z] = new List<MVVEmbedded>();
                        foreach (MVVEmbedded embedded in embedded_objects)
                        {
                            MVVOBB bound = new MVVOBB();
                            bound.transform = new MVVTransform(embedded.transform);
                            if (index_obbs[x, y, z].Intersects(bound))
                            {
                                embedded_indexed_objects[x, y, z].Add(embedded);
                            }
                        }
                    }
                }
            }*/
            // New strategy: look at corner points and decide what index they have;
            //Build array
            for (var x = 0; x < index_size[0]; x++)
            {
                for (var y = 0; y < index_size[1]; y++)
                {
                    for (var z = 0; z < index_size[2]; z++)
                    {
                        embedded_indexed_objects[x, y, z] = new List<MVVEmbedded>();
                    }
                }
            }
            transform.createMatrix();
            //Vector3 corner0 = transform.matrix.MultiplyPoint(new Vector3(-1, -1, -1));
            //Vector3 corner1 = transform.matrix.MultiplyPoint(new Vector3(-1, -1, 1));
            //Vector3 corner2 = transform.matrix.MultiplyPoint(new Vector3(-1, 1, -1));
            //Vector3 corner3 = transform.matrix.MultiplyPoint(new Vector3(-1, 1, 1));
            //Vector3 corner4 = transform.matrix.MultiplyPoint(new Vector3(1, -1, -1));
            //Vector3 corner5 = transform.matrix.MultiplyPoint(new Vector3(1, -1, 1));
            //Vector3 corner6 = transform.matrix.MultiplyPoint(new Vector3(1, 1, -1));
            //Vector3 corner7 = transform.matrix.MultiplyPoint(new Vector3(1, 1, 1));
            Vector3 corner0 = new Vector3(-1, -1, -1);
            Vector3 corner1 = new Vector3(-1, -1, 1);
            Vector3 corner2 = new Vector3(-1, 1, -1);
            Vector3 corner3 = new Vector3(-1, 1, 1);
            Vector3 corner4 = new Vector3(1, -1, -1);
            Vector3 corner5 = new Vector3(1, -1, 1);
            Vector3 corner6 = new Vector3(1, 1, -1);
            Vector3 corner7 = new Vector3(1, 1, 1);
            int count = 0;
            foreach (MVVEmbedded embedded in embedded_objects)
            {
                
                MVVTransform trans = new MVVTransform(embedded.transform);
                trans.createMatrix();
                Vector3[] p = new Vector3[8];
                //p[0] = trans.matrix.MultiplyPoint(corner0);
                //p[1] = trans.matrix.MultiplyPoint(corner1);
                //p[2] = trans.matrix.MultiplyPoint(corner2);
                //p[3] = trans.matrix.MultiplyPoint(corner3);
                //p[4] = trans.matrix.MultiplyPoint(corner4);
                //p[5] = trans.matrix.MultiplyPoint(corner5);
                //p[6] = trans.matrix.MultiplyPoint(corner6);
                //p[7] = trans.matrix.MultiplyPoint(corner7);
                p[0] = transform.matrix.inverse.MultiplyPoint(trans.matrix.MultiplyPoint(corner0));
                p[1] = transform.matrix.inverse.MultiplyPoint(trans.matrix.MultiplyPoint(corner1));
                p[2] = transform.matrix.inverse.MultiplyPoint(trans.matrix.MultiplyPoint(corner2));
                p[3] = transform.matrix.inverse.MultiplyPoint(trans.matrix.MultiplyPoint(corner3));
                p[4] = transform.matrix.inverse.MultiplyPoint(trans.matrix.MultiplyPoint(corner4));
                p[5] = transform.matrix.inverse.MultiplyPoint(trans.matrix.MultiplyPoint(corner5));
                p[6] = transform.matrix.inverse.MultiplyPoint(trans.matrix.MultiplyPoint(corner6));
                p[7] = transform.matrix.inverse.MultiplyPoint(trans.matrix.MultiplyPoint(corner7));
                //get min/max x/y/z
                int min_x = Int32.MaxValue;
                int min_y = Int32.MaxValue;
                int min_z = Int32.MaxValue;
                int max_x = Int32.MinValue;
                int max_y = Int32.MinValue;
                int max_z = Int32.MinValue;

                // find min/max
                for (int i = 0; i < 8; i++)
                {
                    // p[i] is in is now in (-1,-1,-1)x(1,1,1) of index, time to check correct ccordinate
                    // get in range 0..0.999
                    p[i] = p[i]/2.0f + new Vector3(0.499f, 0.499f, 0.499f);
                    Vector3 tmp = Vector3.Scale(p[i], new Vector3(index_size[0], index_size[1], index_size[2]));
                    int x_cur = (int)Math.Floor(tmp.x);
                    int y_cur = (int)Math.Floor(tmp.y);
                    int z_cur = (int)Math.Floor(tmp.z);
                    if (x_cur < min_x) min_x = Math.Max(x_cur,0);
                    if (y_cur < min_y) min_y = Math.Max(y_cur,0);
                    if (z_cur < min_z) min_z = Math.Max(z_cur,0);
                    if (x_cur > max_x) max_x = Math.Min(x_cur,index_size[0]-1);
                    if (y_cur > max_y) max_y = Math.Min(y_cur,index_size[1]-1);
                    if (z_cur > max_z) max_z = Math.Min(z_cur, index_size[2] - 1);
                }
                //Debug.Log("(" + min_x + "," + min_y + "," + min_z + ") (" + max_x + "," + max_y + "," + max_z + ")");
                //Debug.Log(p[0].ToString() + "\n" + p[1].ToString() + "\n" + p[2].ToString() + "\n" + p[3].ToString() + "\n" + p[4].ToString() + "\n" + p[5].ToString() + "\n" + p[6].ToString() + "\n" + p[7].ToString());
                //Debug.Log(p[0].x + ", " + p[0].y + ", " + p[0].z);
                //Debug.Log(trans.matrix.ToString()); 
                //Debug.Log(transform.matrix.ToString());
                //Debug.Log(trans.matrix.ToString());
                //return;
                // Embedd into all cells that are in the cube created by min/max x/y/z
                for (var x = min_x; x <= max_x; x++)
                {
                    for (var y = min_y; y <= max_y; y++)
                    {
                        for (var z = min_z; z <= max_z; z++)
                        {
                            embedded_indexed_objects[x, y, z].Add(embedded);
                            count++;
                        }
                    }
                }
            }
            var counter = "";
            var num = 0;
            for (var x = 0; x < index_size[0]; x++)
            {
                for (var y = 0; y < index_size[1]; y++)
                {
                    for (var z = 0; z < index_size[2]; z++)
                    {
                        counter += ", " + embedded_indexed_objects[x, y, z].Count;
                        num += embedded_indexed_objects[x, y, z].Count;
                    }
                }
            }
            Debug.Log(counter);
            Debug.Log(num);
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

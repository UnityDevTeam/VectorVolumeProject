﻿using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System;
using UnityEditor;

namespace Assets.Editor.MVVReader
{

    public struct Node
    {
        public int positiveId;
        public int negativeId;
        public int isLeaf;
        public int sdfId;
        public int regionId;

        public Node(int positiveId, int negativeId, int isLeaf, int sdfId, int regionId)
        {
            this.positiveId = positiveId;
            this.negativeId = negativeId;
            this.isLeaf = isLeaf;
            this.sdfId = sdfId;
            this.regionId = regionId;
        }
    }

    public struct SDF
    {
        public int index_x;
        public int index_y;
        public int index_z;
        public int size_x;
        public int size_y;
        public int size_z;
        public Matrix4x4 transform;
        public int type;
        int first_transform;
        int max_transform;
        // TODO seeded SDFs

        public SDF(int[] index, int[] size, Matrix4x4 transform, int type, int first_transform, int max_transform)
        {
            this.index_x = index[0];
            this.index_y = index[1];
            this.index_z = index[2];
            this.size_x = size[0];
            this.size_y = size[1];
            this.size_z = size[2];
            this.transform = transform;
            this.type = type;
            this.first_transform = first_transform;
            this.max_transform = max_transform;
        }
    }

    public struct Region
    {
        public int type;
        public Vector3 color;
        public float opacity;
        // TODO textures
        // public Vector3 scale;

        // For now only support one index
        // if no index is used, define 1x1x1 index
        public int embedded;
        public int index_size_x;
        public int index_size_y;
        public int index_size_z;
        public Matrix4x4 index_transform;
        public int index_offset;

        public Region(int type, Color color, float opacity, int[] index_size, Matrix4x4 index_transform, int index_offset, int embedded)
        {
            this.type = type;
            this.color = new Vector3(color.r, color.g, color.b);
            this.opacity = opacity;
            this.index_size_x = index_size[0];
            this.index_size_y = index_size[1];
            this.index_size_z = index_size[2];
            this.index_transform = index_transform;
            this.index_offset = index_offset;
            this.embedded = embedded;

        }
    }

    public struct Instance
    {
        public Matrix4x4 transform;
        public int rootnode;

        public Instance(Matrix4x4 transform, int rootnode)
        {
            this.transform = transform;
            this.rootnode = rootnode;
        }
    }

    public struct Indexcell
    {
        public int instance;
        public int max_instance;

        public Indexcell(int instance, int max_instance)
        {
            this.instance = instance;
            this.max_instance = max_instance;
        }
    }

    public class MVVRoot
    {
        public Dictionary<string, MVVObject> objects = new Dictionary<string,MVVObject>();  // Available Objects
        public Dictionary<string, MVVSDF> sdfs = new Dictionary<string,MVVSDF>();           // Available SDFs
        public Dictionary<string, MVVRegion> regions = new Dictionary<string,MVVRegion>();  // Available Regions
        public MVVObject rootObject;

        public Dictionary<string, MVVSDFFile> sdfTextures = 
            new Dictionary<string,MVVSDFFile>(); // Use same 3DTexture for mulitple SDFs if possible -> Speed
        public Dictionary<string, Texture2D> regionTextures =
            new Dictionary<string,Texture2D>();   // Use same Bitmap textures for mulitple regions if possible -> Speed

        public Texture3D globalTexture; // 3D texture atlas of all SDF Textures

        private int currentSDFIndex = 0;
        private int currentRegionIndex = 0;
        private int currentObjectIndex = 0;
        private int currentNodeIndex = 0;
        private int currentIndexOffset = 0;
        private int currentTransformIndex = 0;


        public void passToShader(Material mat)
        {
            Node[] nodes_for_shader = new Node[128];
            SDF[] sdfs_for_shader = new SDF[32];
            Region[] regions_for_shader = new Region[32];
            Instance[] instances_for_shader = new Instance[16384];
            Indexcell[] indexcells_for_shader = new Indexcell[16384];
            Matrix4x4[] transforms_for_shader = new Matrix4x4[16384];
            Debug.Log("Passing MVV to shader...");
            mat.SetTexture("_VolumeAtlas", globalTexture);
            int max_dim = Math.Max(globalTexture.depth, Math.Max(globalTexture.width, globalTexture.height));
            //mat.SetVector("_VolumeAtlasSize", new Vector3(globalTexture.height, globalTexture.depth, globalTexture.width));
            mat.SetVector("_VolumeAtlasSize", new Vector3(max_dim, max_dim, max_dim));
            mat.SetInt("_rootInstance", rootObject.index);


            Debug.Log("...creating Buffers");
            foreach (MVVObject obj in objects.Values)
            {
                instances_for_shader[obj.index] = new Instance(Matrix4x4.identity, obj.tree.root.index);

                //obj.tree.debug();

                var nodeList = obj.tree.asList();

                foreach (MVVTreeNode node in nodeList)
                {
                    if (node.isLeaf)
                    {
                        nodes_for_shader[node.index] = new Node(-1, -1, 1, -1, node.region.index);
                    }
                    else
                    {
                        nodes_for_shader[node.index] = new Node(node.positive.index, node.negative.index, 0, node.sdf.index, -1);
                    }
                }
            }
            

            foreach (MVVSDF sdf in sdfs.Values)
            {
                sdf.transform.createMatrix();
                int start_transform = currentTransformIndex;
                int max_transform = 0;
                if (sdf.seedTransforms != null && sdf.seedTransforms.Length > 0)
                {
                    max_transform = sdf.seedTransforms.Length;
                    for (; currentTransformIndex < start_transform + sdf.seedTransforms.Length; currentTransformIndex++)
                    {
                        sdf.seedTransforms[currentTransformIndex - start_transform].createMatrix();
                        transforms_for_shader[currentTransformIndex] = sdf.seedTransforms[currentTransformIndex - start_transform].matrix.inverse;
                    }
                }
                sdfs_for_shader[sdf.index] = new SDF(sdf.file.index, sdf.file.sizes, sdf.transform.matrix.inverse, (int)sdf.type, start_transform, max_transform);
            }

            foreach (MVVRegion region in regions.Values)
            {
                MVVIndex regionindex = new MVVIndex();
                regionindex.index_size = new int[] { 1, 1, 1 };
                regionindex.transform = new MVVTransform();
                regionindex.embedded_indexed_objects = new List<MVVEmbedded>[1, 1, 1];
                regionindex.embedded_indexed_objects[0, 0, 0] = new List<MVVEmbedded>();

                // Now we create exaclty one index, for now...
                foreach (MVVIndex ind in region.embedded_objects)
                {
                    if (ind.use_index)
                    {
                        // if we use index, assume this is the only object, for now...
                        regionindex = ind;
                        break;
                    }
                    else
                    {
                        regionindex.embedded_indexed_objects[0, 0, 0].Add(ind.embedded_objects[0]);
                    }
                }

                regionindex.transform.createMatrix();

                if (region.embedded_objects.Count > 0)
                {
                    regions_for_shader[region.index] = new Region((int)region.type, region.color, region.opacity, regionindex.index_size, regionindex.transform.matrix.inverse, currentIndexOffset, 1);
                } else
                {
                    regions_for_shader[region.index] = new Region((int)region.type, region.color, region.opacity, regionindex.index_size, regionindex.transform.matrix.inverse, currentIndexOffset, 1);
                }


                // Go through each index_cell
                for (int x = 0; x < regionindex.embedded_indexed_objects.GetLength(0); x++)
                {
                    for (int y = 0; y < regionindex.embedded_indexed_objects.GetLength(1); y++)
                    {
                        for (int z = 0; z < regionindex.embedded_indexed_objects.GetLength(2); z++)
                        {
                            // calculate linear index
                            int linear_index = x * regionindex.index_size[1] * regionindex.index_size[2] +
                              				   y * regionindex.index_size[2] +
                                               z;
                            if (regionindex.embedded_indexed_objects[x,y,z].Count > 0)
                            {
                                // Add all instances
                                indexcells_for_shader[linear_index + currentIndexOffset] = new Indexcell(currentObjectIndex, regionindex.embedded_indexed_objects[x,y,z].Count);

                                foreach (MVVEmbedded emb in regionindex.embedded_indexed_objects[x, y, z])
                                {
                                    emb.transform.createMatrix();
                                    instances_for_shader[currentObjectIndex] = new Instance(emb.transform.matrix.inverse, emb.mvv_object.tree.root.index);
                                    
                                    currentObjectIndex++;
                                }
                            } else
                            {
                                indexcells_for_shader[linear_index + currentIndexOffset] = new Indexcell(-1, -1);
                            }
                        }
                    }
                }
                currentIndexOffset += regionindex.embedded_indexed_objects.GetLength(0) * regionindex.embedded_indexed_objects.GetLength(1) * regionindex.embedded_indexed_objects.GetLength(2);
            }


            Debug.Log("Index:" + sdfs["SKIN"].file.index[0] + "," + sdfs["SKIN"].file.index[1] + "," + sdfs["SKIN"].file.index[2]);
            Debug.Log("Size:" + sdfs["SKIN"].file.sizes[0] + "," + sdfs["SKIN"].file.sizes[1] + "," + sdfs["SKIN"].file.sizes[2]);

            // Fill buffers
            GPUBuffer.Instance.NodeBuffer.SetData(nodes_for_shader);
            GPUBuffer.Instance.SDFBuffer.SetData(sdfs_for_shader);
            GPUBuffer.Instance.RegionBuffer.SetData(regions_for_shader);
            GPUBuffer.Instance.IndexcellBuffer.SetData(indexcells_for_shader);
            GPUBuffer.Instance.InstanceBuffer.SetData(instances_for_shader);
            GPUBuffer.Instance.TransformBuffer.SetData(transforms_for_shader);

            Debug.Log("...uploading");
            // Load buffers to shader
            mat.SetBuffer("nodeBuffer", GPUBuffer.Instance.NodeBuffer);
            mat.SetBuffer("sdfBuffer", GPUBuffer.Instance.SDFBuffer);
            mat.SetBuffer("regionBuffer", GPUBuffer.Instance.RegionBuffer);
            mat.SetBuffer("indexcellBuffer", GPUBuffer.Instance.IndexcellBuffer);
            mat.SetBuffer("instanceBuffer", GPUBuffer.Instance.InstanceBuffer);

            Debug.Log("...done!");

            // Creating Cubic Lookup Table (From Lvid Wang)
            Debug.Log("Creating Cubic Lookup Table");

            Color[] lookup_data = new Color[256];

            for (int i = 0; i < 256; i++)
            {
                float x = (float)i / 255.0f;
                float x_sqr = x * x;
                float x_cub = x * x_sqr;

                float w0 = (-x_cub + 3.0f * x_sqr - 3.0f * x + 1.0f) / 6.0f;
                float w1 = (3.0f * x_cub - 6.0f * x_sqr + 4.0f) / 6.0f;
                float w2 = (-3.0f * x_cub + 3.0f * x_sqr + 3.0f * x + 1.0f) / 6.0f;
                float w3 = x_cub / 6.0f;

                lookup_data[i] = new Color(1.0f - w1 / (w0 + w1) + x, // h0(x)
                                           1.0f + w3 / (w2 + w3) - x, // h1(x)
                                           w0 + w1,                   // g0(x)
                                           1);
            }

            Texture2D lookup = new Texture2D(256, 1, TextureFormat.RGBAFloat, true);
            lookup.anisoLevel = 0;
            lookup.SetPixels(lookup_data);
            lookup.filterMode = FilterMode.Trilinear;

            mat.SetTexture("_cubicLookup", lookup);

            foreach (MVVSDF sdf in sdfs.Values)
            {
                Debug.Log(sdf.identifier + ":" + sdf.file.sizes[0] + "," + sdf.file.sizes[1] + "," + sdf.file.sizes[2]);
            }

        }

        /// <summary>
        /// Reads an MVVObject from XML. Root Object must be last Object-element.
        /// Currently unsupported features:
        ///     importing of other XML files
        ///     Regions as attributes in Positive/Negative Tags
        ///     inline sdf (defining sdf in mapping)
        ///     inline region (defining region in mapping)
        ///     minimal file without mapping
        /// </summary>
        /// <param name="filename">Filename of XML MVV File</param>
        /// <returns>True if successfull, false otherwise</returns>
        public bool readFromFile(string filename, string rootName)
        {
            Debug.Log("Loading file " + filename);
            XmlDocument document = new XmlDocument();
            document.Load(filename);
            Debug.Log("Reading MVVObjects");

            var baseDir = new FileInfo(filename).Directory.FullName;

            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                if (node.Name != "Object") continue;
                if (node.Attributes["name"] == null) 
                    throw new IllegalMVVFileException("Object must have a name attribute");
                var nameObject = node.Attributes["name"].InnerText;
                Debug.Log("Reading stuff from " + nameObject);
                this.objects[nameObject] = new MVVObject(nameObject, this);
                this.objects[nameObject].index = currentObjectIndex;
                currentObjectIndex++;
                this.rootObject = this.objects[nameObject]; //Last child is root object...
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Name == "SDF")
                    {
                        if (childNode.Attributes["name"] == null)
                            throw new IllegalMVVFileException("SDF (of Object " + nameObject + ") must have a name attribute");
                        addSDFFromNode(childNode, baseDir);
                        Debug.Log(childNode.Attributes["name"].InnerText+" Scale: "+sdfs[childNode.Attributes["name"].InnerText].transform.scale.ToString());
                    }
                    else if (childNode.Name == "Region")
                    {
                        if (childNode.Attributes["name"] == null)
                            throw new IllegalMVVFileException("Region (of Object " + nameObject + ") must have a name attribute");
                        addRegionFromNode(childNode, baseDir);
                    }
                    else if (childNode.Name == "Mappings")
                    {
                        if (childNode.Attributes["name"] == null)
                            throw new IllegalMVVFileException("Mappings (of Object " + nameObject + ") must have a name attribute");
                        string name = childNode.Attributes["name"].InnerText;
                        if (!objects.ContainsKey(name))
                            throw new IllegalMVVFileException("Mappings (of Object " + nameObject + ") must have the same name as object");
                        objects[name].tree = new MVVTree();
                        objects[name].tree.root = new MVVTreeNode();
                        objects[name].tree.root.tree = objects[name].tree;
                        addMappings(childNode.FirstChild, objects[name].tree.root);
                    }
                }

                // Finally calculate object bounds
                this.objects[nameObject].calcBounds();
                Debug.Log("Finished Loading stuff from " + nameObject);
            }

            // Loaded everything, bin packing texture
            polulateTexture();
            Debug.Log("Finished loading. Set root object to " + rootObject.identifier);
            

            return true;
        }

        private void polulateTexture()
        {
            // go through all Textures and pack them to globalTexture
            // Using some kind of first fit (Reference: A genetic algorithm for packing in three dimensions, Corcoran, Wainwright)
            
            //Sort by volume && biggest dimension
            List<MVVSDFFile> sortedSDF = new List<MVVSDFFile>();
            int[] biggest = { 0, 0, 0 };
            foreach (KeyValuePair<string, MVVSDFFile> sdf in sdfTextures)
            {
                sortedSDF.Add(sdf.Value);
                if (sdf.Value.sizes[0] > biggest[0]) biggest[0] = sdf.Value.sizes[0];
                if (sdf.Value.sizes[1] > biggest[1]) biggest[1] = sdf.Value.sizes[1];
                if (sdf.Value.sizes[2] > biggest[2]) biggest[2] = sdf.Value.sizes[2];
            }
            sortedSDF.Sort();
            sortedSDF.Reverse();

            int smallestOfBiggest;

            if (biggest[0] < biggest[1])
                if (biggest[0] < biggest[2])
                    smallestOfBiggest = 0;
                else
                    smallestOfBiggest = 2;
            else
                if (biggest[1] < biggest[2])
                    smallestOfBiggest = 1;
                else
                    smallestOfBiggest = 2;
            
            var indexpos = new int[] { 0, 0, 0 };

            for (int i = 0; i < sortedSDF.Count; i++)
            {
                sortedSDF[i].index[0] = indexpos[0];
                sortedSDF[i].index[1] = indexpos[1];
                sortedSDF[i].index[2] = indexpos[2];

                indexpos[smallestOfBiggest] += sortedSDF[i].sizes[smallestOfBiggest];

            }

            var dimension = biggest;
            dimension[smallestOfBiggest] = indexpos[smallestOfBiggest];

            for (int i = 0; i<dimension.Length; i++)
            {
                dimension[i] = (int)Math.Pow(2, Math.Ceiling(Math.Log(dimension[i], 2)));
            }

            Debug.Log("3D-Atlas size: " + dimension[0] + "," + dimension[1] + "," + dimension[2]);

            Color[] colors = new Color[dimension[0] * dimension[1] * dimension[2]];
            int currentSDF = 0;

            int linearIndex = 0;
            int localLinearIndex = 0;

            for (int x = 0; x < dimension[0]; x++)
            {
                if (smallestOfBiggest == 0)
                {
                    if (currentSDF < sortedSDF.Count && x > sortedSDF[currentSDF].index[0] + sortedSDF[currentSDF].sizes[0])
                    {
                        currentSDF++;
                    }
                }
                if (smallestOfBiggest == 1) currentSDF = 0; // Reset current for new run
                for (int y = 0; y < dimension[1]; y++)
                {
                    if (smallestOfBiggest == 1)
                    {
                        if (currentSDF < sortedSDF.Count && y > sortedSDF[currentSDF].index[1] + sortedSDF[currentSDF].sizes[1])
                        {
                            currentSDF++;
                        }
                    }
                    if (smallestOfBiggest == 2) currentSDF = 0; // Reset current for new run
                    for (int z = 0; z < dimension[2]; z++)
                    {
                        if (currentSDF > sortedSDF.Count - 1)
                        {
                            // Okay, no more sdfs...
                            colors[linearIndex].a = 1f;
                            continue;

                        }
                        if (smallestOfBiggest == 2)
                        {
                            //Check if we are in correct sdf...
                            if (currentSDF < sortedSDF.Count && z > sortedSDF[currentSDF].index[2] + sortedSDF[currentSDF].sizes[2])
                            {
                                currentSDF++; // go to the next one...
                            }
                        }

                        if (z < sortedSDF[currentSDF].index[2] + sortedSDF[currentSDF].sizes[2] &&
                            y < sortedSDF[currentSDF].index[1] + sortedSDF[currentSDF].sizes[1] &&
                            x < sortedSDF[currentSDF].index[0] + sortedSDF[currentSDF].sizes[0] )
                        {
                            // We are in current sdf
                            // Calculate local index;
                            localLinearIndex = (z - sortedSDF[currentSDF].index[2]) +
                                               (y - sortedSDF[currentSDF].index[1]) * sortedSDF[currentSDF].sizes[2] +
                                               (x - sortedSDF[currentSDF].index[0]) * sortedSDF[currentSDF].sizes[1] * sortedSDF[currentSDF].sizes[2];
                            try
                            {
                                colors[linearIndex] = sortedSDF[currentSDF].volumeColors[localLinearIndex];
                            }
                            catch (IndexOutOfRangeException)
                            {
                                Debug.Log("ID: " + currentSDF);
                                Debug.Log("(x,y,z) = " + x + "," + y + "," + z);
                                Debug.Log("index = " + sortedSDF[currentSDF].index[0] + "," + sortedSDF[currentSDF].index[1] + "," + sortedSDF[currentSDF].index[2]);
                                Debug.Log("Sizes = " + sortedSDF[currentSDF].sizes[0] + "," + sortedSDF[currentSDF].sizes[1] + "," + sortedSDF[currentSDF].sizes[2]);
                                return;
                            }
                            
                        } else
                        {
                            colors[linearIndex].a = 1f;
                        }
                        linearIndex++;
                    }
                }
            }

            globalTexture = new Texture3D(dimension[0], dimension[1], dimension[2], TextureFormat.Alpha8, true);
            globalTexture.SetPixels(colors);
            globalTexture.filterMode = FilterMode.Point;
            globalTexture.anisoLevel = 0;
            globalTexture.Apply();

            //Temp add to assets...
            string assetPath = "Assets/mvv_test.asset";

            Texture3D tmp = (Texture3D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture3D));
            if (tmp)
            {
                AssetDatabase.DeleteAsset(assetPath);
                tmp = null;
            }

            AssetDatabase.CreateAsset(globalTexture, assetPath);
            AssetDatabase.SaveAssets();

        }

        private void addMappings(XmlNode node, MVVTreeNode tree_node)
        {
            tree_node.index = currentNodeIndex;
            currentNodeIndex++;
            if (node.Name == "SDF")
            {
                // We are not in a leaf...
                tree_node.isLeaf = false;

                if (node.Attributes["name"] == null)
                    throw new IllegalMVVFileException("SDF in Mapping has no name");
                var name = node.Attributes["name"].InnerText;
                if (!sdfs.ContainsKey(name))
                    throw new IllegalMVVFileException("SDF Reference (" + name + ") does not exist");
                tree_node.sdf = sdfs[name];
                tree_node.positive = new MVVTreeNode();
                tree_node.positive.tree = tree_node.tree;
                addMappings(node["Positive"].FirstChild, tree_node.positive);
                tree_node.negative = new MVVTreeNode();
                tree_node.negative.tree = tree_node.tree;
                addMappings(node["Negative"].FirstChild, tree_node.negative);
            } else if(node.Name == "Region")
            {
                // Now we have a leaf
                tree_node.isLeaf = true;
                if (node.Attributes["name"] == null)
                    throw new IllegalMVVFileException("Region in Mapping has no name");
                var name = node.Attributes["name"].InnerText;
                if (!regions.ContainsKey(name))
                    throw new IllegalMVVFileException("Region Reference (" + name + ") does not exist");
                tree_node.region = regions[name];
            } else
            {
                throw new IllegalMVVFileException("Unknown Tag in Tree (" + node.Name + ")");
            }
        }

        private void addRegionFromNode(XmlNode childNode, string baseDir)
        {
            var nameRegion = childNode.Attributes["name"].InnerText;
            Debug.Log("Creating Region: " + nameRegion);
            var region = new MVVRegion();
            region.index = currentRegionIndex;
            currentRegionIndex++;
            region.identifier = nameRegion;

            if (childNode.Attributes["type"] == null)
                throw new IllegalMVVFileException("Region (" + nameRegion + ") must have a type attribute");

            switch (childNode.Attributes["type"].InnerText)
            {
                case "bitmap":
                    region.type = MVVRegionType.BITMAP;
                    break;
                case "color":
                    region.type = MVVRegionType.COLOR;
                    break;
                case "rbf":
                    region.type = MVVRegionType.RBF;
                    break;
                default:
                    region.type = MVVRegionType.EMPTY;
                    break;
            }

            if (childNode.Attributes["rgb"] != null)
            {
                var colorArray = childNode.Attributes["rgb"].InnerText.Split(new char[] { ' ' });
                if (colorArray.Length != 3)
                    throw new IllegalMVVFileException("Incorrect Color format (Region: " + nameRegion + ")");
                region.color = new Color(float.Parse(colorArray[0]),
                                         float.Parse(colorArray[1]),
                                         float.Parse(colorArray[2]));
            }

            if (childNode.Attributes["opacity"] != null)
            {
                region.opacity = float.Parse(childNode.Attributes["opacity"].InnerText);
            }
            // Loading texture START

            if (childNode.Attributes["file"] != null)
            {
                var pathTex = childNode.Attributes["file"].InnerText;
                var rootPath = "";
                if (Path.IsPathRooted(pathTex))
                {
                    rootPath = pathTex;
                }
                else
                {
                    rootPath = baseDir + "\\" + pathTex;
                }
                if (regionTextures.ContainsKey(rootPath))
                {
                    region.image = regionTextures[rootPath];
                }
                else
                {
                    region.loadImage(rootPath);
                    regionTextures.Add(rootPath, region.image);
                }
            }
            // Loading texture END

            // Loading Transformations BEGIN
            region.transform = getMVVTransform(childNode);
            // Loading Transformations END

            processEmbeddedObjects(childNode, region, baseDir);

            regions[region.identifier] = region;
        }

        private void processEmbeddedObjects(XmlNode regionXML, MVVRegion region, string baseDir)
        {
            foreach (XmlNode embeddedXML in regionXML.ChildNodes)
            {
                if (embeddedXML.Name == "Embedded")
                {
                    // That's a simple embedded object without index and seeding...
                    
                    if (embeddedXML.Attributes["name"] == null)
                        throw new IllegalMVVFileException("Embedded (in Region " + region.identifier  + ") must have a name attribute.");
                    string key = embeddedXML.Attributes["name"].InnerText;
                    if (!objects.ContainsKey(key)){
                        throw new IllegalMVVFileException("Embedded (" + key  + ") points to unknown object.");
                    }
                    region.embedded_objects.Add(new MVVIndex(objects[key], new MVVTransform(getMVVTransform(embeddedXML))));
                }
                else if (embeddedXML.Name == "Index")
                {
                    if (embeddedXML.Attributes["size"] == null)
                        throw new IllegalMVVFileException("Index (in Region " + region.identifier + ") must have a name attribute.");
                    var size = embeddedXML.Attributes["size"].InnerText.Split(new char[] { ' ' });
                    var index = new MVVIndex();
                    index.index_size[0] = int.Parse(size[0]);
                    index.index_size[1] = int.Parse(size[1]);
                    index.index_size[2] = int.Parse(size[2]);
                    index.use_index = true;
                    // Load Index Transforms
                    index.transform = getMVVTransform(embeddedXML);
                    // Load regions
                    foreach (XmlNode embeddedIndex in embeddedXML.ChildNodes)
                    {
                        if (embeddedIndex.Name == "Embedded")
                        {
                            if (embeddedIndex.Attributes["name"] == null)
                                throw new IllegalMVVFileException("Embedded (in Region " + region.identifier + ") must have a name attribute.");
                            string key = embeddedIndex.Attributes["name"].InnerText;
                            if (!objects.ContainsKey(key))
                            {
                                throw new IllegalMVVFileException("Embedded (" + key + ") points to unknown object.");
                            }
                            if (embeddedIndex.Attributes["seedFile"] == null)
                            {
                                index.embedded_objects.Add(new MVVEmbedded(objects[key], getMVVTransform(embeddedIndex)));
                            }
                            else
                            {
                                string seedPath = null;
                                string rotationPath = null;
                                string scalePath = null;
                                var pathSeed = embeddedIndex.Attributes["seedFile"].InnerText;
                                if (Path.IsPathRooted(pathSeed))
                                {
                                    seedPath = pathSeed;
                                }
                                else
                                {
                                    seedPath = baseDir + "\\" + pathSeed;
                                }
                                if (embeddedIndex.Attributes["rotateFile"] != null)
                                {
                                    pathSeed = embeddedIndex.Attributes["rotateFile"].InnerText;
                                    if (Path.IsPathRooted(pathSeed))
                                    {
                                        rotationPath = pathSeed;
                                    }
                                    else
                                    {
                                        rotationPath = baseDir + "\\" + pathSeed;
                                    }
                                }
                                if (embeddedIndex.Attributes["scaleFile"] != null)
                                {
                                    pathSeed = embeddedIndex.Attributes["scaleFile"].InnerText;
                                    if (Path.IsPathRooted(pathSeed))
                                    {
                                        scalePath = pathSeed;
                                    }
                                    else
                                    {
                                        scalePath = baseDir + "\\" + pathSeed;
                                    }
                                }
                                index.addObjectsFromSeed(objects[key], seedPath, rotationPath, scalePath, getMVVTransform(embeddedIndex));
                                
                            }
                        }
                    }
                    Debug.Log("Building Index for " + region.identifier);
                    // TODO: Reactivate!
                    //index.buildIndex();
                    Debug.Log("Done.");
                }
            }
        }

        private void addSDFFromNode(XmlNode childNode, string baseDir)
        {
            var nameSDF = childNode.Attributes["name"].InnerText;
            Debug.Log("Creating SDF: " + nameSDF);
            var sdf = new MVVSDF();
            sdf.index = currentSDFIndex;
            currentSDFIndex++;
            sdf.identifier = nameSDF;
            if (childNode.Attributes["file"] == null)
                throw new IllegalMVVFileException("SDF (" + nameSDF + ") must have a file attribute");

            // Loading texture START
            var pathSDF = childNode.Attributes["file"].InnerText;
            var rootPath = "";
            if (Path.IsPathRooted(pathSDF))
            {
                rootPath = pathSDF;
            }
            else
            {
                rootPath = baseDir + "\\" + pathSDF;
            }
            if (sdfTextures.ContainsKey(rootPath))
            {
                sdf.file = sdfTextures[rootPath];
            }
            else
            {
                sdf.loadSDF(rootPath);
                sdfTextures.Add(rootPath, sdf.file);
            }
            // Loading texture END

            // Loading Seed Stuff BEGIN
            if (childNode.Attributes["seedFile"] != null)
            {
                var pathSeed = childNode.Attributes["seedFile"].InnerText;
                rootPath = "";
                if (Path.IsPathRooted(pathSeed))
                {
                    rootPath = pathSeed;
                }
                else
                {
                    rootPath = baseDir + "\\" + pathSeed;
                }
                sdf.loadSeedFile(rootPath);
                if (childNode.Attributes["rotateFile"] != null)
                {
                    pathSeed = childNode.Attributes["rotateFile"].InnerText;
                    rootPath = "";
                    if (Path.IsPathRooted(pathSeed))
                    {
                        rootPath = pathSeed;
                    }
                    else
                    {
                        rootPath = baseDir + "\\" + pathSeed;
                    }
                    sdf.loadSeedRotationFile(rootPath);
                }
                if (childNode.Attributes["scaleFile"] != null)
                {
                    pathSeed = childNode.Attributes["scaleFile"].InnerText;
                    rootPath = "";
                    if (Path.IsPathRooted(pathSeed))
                    {
                        rootPath = pathSeed;
                    }
                    else
                    {
                        rootPath = baseDir + "\\" + pathSeed;
                    }
                    sdf.loadSeedScaleFile(rootPath);
                }
            }
            // Loading Seed Stuff END

            // Loading Transformations BEGIN
            sdf.transform = getMVVTransform(childNode);
            // Loading Transformations END

            sdf.type = MVVSDFType.DEFAULT;
            if (childNode.Attributes["type"] != null)
            {
                switch (childNode.Attributes["type"].InnerText)
                {
                    case "seeding":
                        sdf.type = MVVSDFType.SEEDING;
                        break;
                    case "tiling":
                        sdf.type = MVVSDFType.TILING;
                        break;
                    default:
                        break;
                }
            }

            if (childNode.Attributes["offset"] != null)
            {
                sdf.offset = float.Parse(childNode.Attributes["offset"].InnerText);
            }

            sdfs[nameSDF] = sdf;

        }

        /// <summary>
        /// Get tranform from node with transpose, scale and rotate attributes
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private MVVTransform getMVVTransform(XmlNode node)
        {
            string translate = null;
            string rotate = null;
            string scale = null;
            if (node.Attributes["translate"] != null)
            {
                translate = node.Attributes["translate"].InnerText;
            }
            if (node.Attributes["scale"] != null)
            {
                scale = node.Attributes["scale"].InnerText;
            }
            if (node.Attributes["rotate"] != null)
            {
                rotate = node.Attributes["rotate"].InnerText;
            }
            return getMVVTransform(translate, scale, rotate);
        }

        /// <summary>
        /// Add Transform from String, format "X Y Z" with X,Y,Z floats.
        /// </summary>
        /// <param name="translate">translate</param>
        /// <param name="scale">scale</param>
        /// <param name="rotate">rotate</param>
        private MVVTransform getMVVTransform(string translate, string scale, string rotate)
        {
            MVVTransform transform = new MVVTransform();
            if (translate != null)
            {
                var translateArray = translate.Split(new char[] { ' ' });
                if (translateArray.Length != 3) throw new IllegalMVVFileException("Illegal Translate Attribute");
                transform.position = new Vector3(float.Parse(translateArray[0]),
                                                 float.Parse(translateArray[1]),
                                                 float.Parse(translateArray[2]));
            }
            if (scale != null)
            {
                var scaleArray = scale.Split(new char[] { ' ' });
                if (scaleArray.Length != 3) throw new IllegalMVVFileException("Illegal Scale Attribute");
                transform.scale = new Vector3(float.Parse(scaleArray[0]),
                                                 float.Parse(scaleArray[1]),
                                                 float.Parse(scaleArray[2]));
            }
            if (rotate != null)
            {
                var rotateArray = rotate.Split(new char[] { ' ' });
                if (rotateArray.Length != 3) throw new IllegalMVVFileException("Illegal Rotate Attribute");
                transform.rotation = new Vector3(float.Parse(rotateArray[0]),
                                                 float.Parse(rotateArray[1]),
                                                 float.Parse(rotateArray[2]));
            }
            return transform;
        }
    }


}
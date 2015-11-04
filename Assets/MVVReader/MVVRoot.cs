using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System;
using UnityEditor;

namespace Assets.MVVReader
{
    

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
        private int currentIndexIndex = 0;

        public void passToShader(Material mat)
        {

            // Stuff to set
            //sampler3D _VolumeAtlas; // Atlas of all SDFs...
            //int3 _VolumeAtlasSize; // Size of Atlas for calculation...
            //int _rootSDF; // ID of First sdf
            //              // SDF stuff
            //int3 _sdfIndices[MAX_SDFS]; // Indices of sdfs in atlas
            //int3 _sdfDimensions[MAX_SDFS]; // Dimensions of sdfs in atlas
            //int _sdfPositiveId[MAX_SDFS]; // Stores region/sdf id of positive branch
            //int _sdfPositiveRegion[MAX_SDFS]; // True if this branch is a region
            //int _sdfNegativeId[MAX_SDFS]; // Stores region/sdf id of negative branch
            //int _sdfNegativeRegion[MAX_SDFS]; // True if this branch is a region
            //float4x4 _sdfTransforms[MAX_SDFS]; // Stores Transform of SDF (inverse)
            //int _sdfType[MAX_SDFS]; // Type of sdf: 0: default, 1: seed, 2: tiling
            //float4x4 _sdfSeedTransforms[MAX_SDFSEEDS]; // Transform of seeds (inverse)
            //int _sdfSeedTransformIndices[MAX_SDFS]; // Starting index of seedTransforms
            //int _sdfSeedTransformLenght[MAX_SDFS]; // Length of indices
            //float _sdfOffset[MAX_SDFS]; // iso-surface offset
            //                            // Region stuff
            //int _regionType[MAX_REGIONS]; // Region type: 0: empty, 1: bitmap, 2: color, 3: rbf
            //sampler2D _regionTextures[MAX_REGIONS]; // Textures of array
            //float3 _regionColor[MAX_REGIONS]; // Color of region
            //float _regionOpacity[MAX_REGIONS]; // Opacity of region
            //float3 _regionScales[MAX_REGIONS]; // For Bitmap transform
            //int _regionHasEmbedd[MAX_REGIONS]; // Does region have embedds
            //                                    //There are two kinds of embedds: with and without index
            //int _regionIndices[MAX_REGIONS]; // IDs of Index for Embedded Objects (-1 no index)
            //int _regionEmbeddedIndices[MAX_REGIONS]; // Where embedded objects start (-1 no element in index)
            //int _regionEmbeddedLength[MAX_REGIONS]; // how many objects are there in this part?
            //                                        // Indices
            //float4x4 _indexTransform[MAX_INDICES]; // Transform of index (inverse)
            //int3 _indexSizes[MAX_INDICES]; // Size in each dimenstion
            //int _indexOffset[MAX_INDICES]; // Offset in indexEmbeddedIndices/Length
            //int _embeddedSDFs[MAX_EMBEDDED]; // index of sdfs that are embedded
            //float4x4 _embeddedTransforms[MAX_EMBEDDED]; // transform for embedded objects (inverse)
            //                                            //float4x4 _embeddedBoundingBox[MAX_EMBEDDED]; // boundingbox for embedded objects (inverse)
            //                                            //The following will also include direct embedds in region without index...
            //int _indexEmbeddedIndices[MAX_INDICESPARTS]; // Where embedded objects start (-1 no element in index)
            //int _indexEmbeddedLength[MAX_INDICESPARTS]; // how many objects are there in this part?

            mat.SetTexture("_VolumeAtlas", globalTexture);
            int max_dim = Math.Max(globalTexture.depth, Math.Max(globalTexture.width, globalTexture.height));
            mat.SetInt("_VolumeAtlasSize0", max_dim);
            mat.SetInt("_VolumeAtlasSize1", max_dim);
            mat.SetInt("_VolumeAtlasSize2", max_dim);
            mat.SetInt("_rootSDF", rootObject.tree.root.sdf.index);

            var nodeList = rootObject.tree.asList();
            int highest_seeding_id = 0;

            foreach(MVVTreeNode node in nodeList)
            {
                if (!node.isLeaf)
                {
                    // Hey thats a sdf
                    mat.SetVector("_sdfIndices" + node.sdf.index, new Vector4(node.sdf.file.index[0], node.sdf.file.index[1], node.sdf.file.index[2]));
                    mat.SetVector("_sdfDimensions" + node.sdf.index, new Vector4(node.sdf.file.sizes[0], node.sdf.file.sizes[1], node.sdf.file.sizes[2]));
                    if (node.positive.isLeaf)
                    {
                        // Positive Node is a region
                        mat.SetInt("_sdfPositiveId" + node.sdf.index, node.positive.region.index);
                        mat.SetInt("_sdfPositiveRegion" + node.sdf.index, 1);
                    }
                    else
                    {
                        mat.SetInt("_sdfPositiveId" + node.sdf.index, node.positive.sdf.index);
                        mat.SetInt("_sdfPositiveRegion" + node.sdf.index, 0);
                    }
                    if (node.negative.isLeaf)
                    {
                        // Negative Node is a region
                        mat.SetInt("_sdfNegativeId" + node.sdf.index, node.negative.region.index);
                        mat.SetInt("_sdfNegativeRegion" + node.sdf.index, 1);
                    }
                    else
                    {
                        mat.SetInt("_sdfNegativeId" + node.sdf.index, node.negative.sdf.index);
                        mat.SetInt("_sdfNegativeRegion" + node.sdf.index, 0);
                    }
                    node.sdf.transform.createMatrix();
                    mat.SetMatrix("_sdfTransforms" + node.sdf.index, node.sdf.transform.matrix.inverse);
                    int sdf_type = 0;
                    if (node.sdf.type == MVVSDFType.SEEDING) sdf_type = 1;
                    if (node.sdf.type == MVVSDFType.TILING) sdf_type = 2;
                    mat.SetInt("_sdfType" + node.sdf.index, sdf_type);
                    if (sdf_type == 1)
                    {
                        //Seeding stuff
                        mat.SetInt("_sdfSeedTransformIndices" + node.sdf.index, highest_seeding_id);
                        mat.SetInt("_sdfSeedTransformLength" + node.sdf.index, node.sdf.seedTransforms.Length);
                        foreach (MVVTransform trans in node.sdf.seedTransforms)
                        {
                            trans.createMatrix();
                            mat.SetMatrix("_sdfSeedTransforms" + highest_seeding_id, trans.matrix.inverse);
                            highest_seeding_id++;
                        }
                    }
                    mat.SetFloat("_sdfOffset" + node.sdf.index, node.sdf.offset);
                } else
                {
                    //node region
                    int region_type = 0;
                    if (node.region.type == MVVRegionType.COLOR)
                    {
                        mat.SetVector("_regionColor" + node.region.index, node.region.color);
                        region_type = 1;
                    }
                    if (node.region.type == MVVRegionType.BITMAP)
                    {
                        mat.SetTexture("_regionTextures" + node.region.index, node.region.image);
                        mat.SetVector("_regionScales" + node.region.index, node.region.transform.scale);

                        region_type = 2;
                    }
                    if (node.region.type == MVVRegionType.RBF)
                    {
                        region_type = 3;
                    }
                    mat.SetInt("_regionType" + node.region.index, region_type);
                    mat.SetFloat("_regionOpacity" + node.region.index, node.region.opacity);

                    if (node.region.embedded_objects.Count > 0)
                    {
                        // Embedded objects
                        mat.SetInt("_regionHasEmbedd" + node.region.index, 1);
                    }
                    else
                    {
                        mat.SetInt("_regionHasEmbedd" + node.region.index, 0);
                    }

                }

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
                this.rootObject = this.objects[nameObject]; //Last child is root object...
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Name == "SDF")
                    {
                        if (childNode.Attributes["name"] == null)
                            throw new IllegalMVVFileException("SDF (of Object " + nameObject + ") must have a name attribute");
                        addSDFFromNode(childNode, baseDir);
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

            globalTexture = new Texture3D(dimension[0], dimension[1], dimension[2], TextureFormat.Alpha8, false);
            globalTexture.SetPixels(colors);
            globalTexture.filterMode = FilterMode.Bilinear;
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

            if (childNode.Attributes["color"] != null)
            {
                var colorArray = childNode.Attributes["color"].InnerText.Split(new char[] { ' ' });
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
                    region.embedded_objects.Add(new MVVIndex(objects[key], getMVVTransform(embeddedXML)));
                }
                else if (embeddedXML.Name == "Index")
                {
                    if (embeddedXML.Attributes["size"] == null)
                        throw new IllegalMVVFileException("Index (in Region " + region.identifier + ") must have a name attribute.");
                    var size = embeddedXML.Attributes["size"].InnerText.Split(new char[] { ' ' });
                    var index = new MVVIndex();
                    index.index = currentIndexIndex;
                    currentIndexIndex++;
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
                transform.position = new Vector3(float.Parse(scaleArray[0]),
                                                 float.Parse(scaleArray[1]),
                                                 float.Parse(scaleArray[2]));
            }
            if (rotate != null)
            {
                var rotateArray = rotate.Split(new char[] { ' ' });
                if (rotateArray.Length != 3) throw new IllegalMVVFileException("Illegal Rotate Attribute");
                transform.position = new Vector3(float.Parse(rotateArray[0]),
                                                 float.Parse(rotateArray[1]),
                                                 float.Parse(rotateArray[2]));
            }
            return transform;
        }
    }


}

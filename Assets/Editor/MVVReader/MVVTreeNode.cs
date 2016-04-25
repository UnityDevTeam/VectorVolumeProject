using UnityEngine;
using System.Collections;

namespace Assets.Editor.MVVReader
{
    /// <summary>
    /// Single TreeNode of MVVTree
    /// </summary>
    public class MVVTreeNode : Object
    {
        public int index;           // ID in shader
        public MVVTree tree;        // Tree this node belongs to
        public MVVSDF sdf;          // SDF (or null if leaf)
        public MVVTreeNode positive;// positive Child TreeNode (or null if leaf)
        public MVVTreeNode negative;// negative Child TreeNode (or null if leaf)
        public MVVRegion region;    // Region (or null if not a leaf)
        public bool isLeaf = false; // Is this a leaf node
    }
}

using UnityEngine;
using System.Collections;

namespace Assets.Editor.MVVReader
{
    public class MVVTreeNode : Object
    {
        public int index;
        public MVVTree tree;
        public MVVSDF sdf;
        public MVVTreeNode positive;
        public MVVTreeNode negative;
        public MVVRegion region;
        public bool isLeaf = false;
    }
}

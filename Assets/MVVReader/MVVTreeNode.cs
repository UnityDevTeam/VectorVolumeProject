using UnityEngine;
using System.Collections;

namespace Assets.MVVReader
{
    public class MVVTreeNode : Object
    {
        public MVVTree tree;
        public MVVSDF sdf;
        public MVVTreeNode positive;
        public MVVTreeNode negative;
        public MVVRegion region;
        public bool isLeaf = false;
    }
}

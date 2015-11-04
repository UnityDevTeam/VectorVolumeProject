using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.MVVReader
{
    public class MVVTree : Object
    {
        public string identifier;
        public MVVTreeNode root;

        public List<MVVTreeNode> asList()
        {
            List<MVVTreeNode> result = new List<MVVTreeNode>();

            addToList(root, result);

            return result;
        } 

        private void addToList(MVVTreeNode node, List<MVVTreeNode> list)
        {
            list.Add(node);
            if (!node.isLeaf)
            {
                addToList(node.positive, list);
                addToList(node.negative, list);
            }
        }
    }
}

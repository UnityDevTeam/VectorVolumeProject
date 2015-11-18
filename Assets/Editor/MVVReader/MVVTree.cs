using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Assets.Editor.MVVReader
{
    public class MVVTree
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

        public void debug()
        {
            debugHelp(root, 0);
        }

        private void debugHelp(MVVTreeNode node, int space)
        {
            string spacer = new String(' ', space);


            if (!node.isLeaf)
            {
                Debug.Log(spacer + node.index + ": SDF: " + node.sdf.index + " ( " + node.sdf.identifier + ")");
                debugHelp(node.positive, space + 2);
                debugHelp(node.negative, space + 2);
            } else
            {
                Debug.Log(spacer + node.index + ": Region: " + node.region.index + " ( " + node.region.identifier + ")");
            }
        }
    }
}

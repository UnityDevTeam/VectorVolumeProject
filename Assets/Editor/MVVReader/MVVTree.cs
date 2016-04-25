using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Assets.Editor.MVVReader
{
    /// <summary>
    /// Stores the MVV Tree data structure
    /// </summary>
    public class MVVTree
    {
        public string identifier;
        public MVVTreeNode root;

        /// <summary>
        /// Returns all nodes of the tree as a depth-first list
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Debug output the tree
        /// </summary>
        public void debug()
        {
            debugHelp(root, 0);
        }

        /// <summary>
        /// Prints out Subtree beginning with node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="space"></param>
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

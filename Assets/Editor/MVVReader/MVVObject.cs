using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Assets.Editor.MVVReader
{
    public class MVVObject : MVVIndexedObject
    {
        public int index;
        public string identifier;
        public MVVTree tree;
        public MVVRoot root;
        public MVVOBB outerBounds;

        public MVVObject(string nameObject, MVVRoot root)
        {
            this.identifier = nameObject;
            this.root = root;
        }

        public void calcBounds()
        {
            //var bbmin = new Vector3(tree.root.sdf.file.bboxMin[0], tree.root.sdf.file.bboxMin[1], tree.root.sdf.file.bboxMin[2]);
            //var bbmax = new Vector3(tree.root.sdf.file.bboxMax[0], tree.root.sdf.file.bboxMax[1], tree.root.sdf.file.bboxMax[2]);
            //outerBounds = MVVOBB.Transform(new MVVOBB(bbmin, bbmax), new MVVTransform(tree.root.sdf.transform));
        }
    }
}

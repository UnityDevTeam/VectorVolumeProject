using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Assets.Editor.MVVReader
{
    /// <summary>
    /// An object, either the root object or an embedded object
    /// </summary>
    public class MVVObject : MVVIndexedObject
    {
        public int index;
        public string identifier;
        public MVVTree tree; //The tree of this object
        public MVVRoot root; //The root this object belongs to

        public MVVObject(string nameObject, MVVRoot root)
        {
            this.identifier = nameObject;
            this.root = root;
        }
    }
}

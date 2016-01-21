﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Assets.Editor.MVVReader
{
    /// <summary>
    /// MVVEmbedded manages a transformation for the indexed Object mvv_object
    /// mvv_object must be a subclass from MVVIndexedObject
    /// </summary>
    public class MVVEmbedded : Object
    {
        public MVVIndexedObject mvv_object; // Embedded Object
        public MVVTransform transform; // Transform of object

        public MVVEmbedded(MVVIndexedObject mvv_object, MVVTransform transform)
        {
            this.mvv_object = mvv_object;
            this.transform = transform;
        }


        
    }
}

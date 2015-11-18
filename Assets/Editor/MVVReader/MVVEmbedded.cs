using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Assets.Editor.MVVReader
{
    public class MVVEmbedded : Object
    {
        public MVVObject mvv_object; // Embedded Object
        public MVVTransform transform; // Transform of object

        public MVVEmbedded(MVVObject mvv_object, MVVTransform transform)
        {
            this.mvv_object = mvv_object;
            this.transform = transform;
        }

        
    }
}

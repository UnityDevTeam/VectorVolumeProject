using UnityEngine;

namespace Assets.Editor.MVVReader
{
    public class MVVTransform : Object
    {
        public Vector3 position = new Vector3(0, 0, 0);
        public Vector3 scale = new Vector3(1, 1, 1);
        public Vector3 rotation = new Vector3(0, 0, 0); //Rotation: rotate z, rotate y, rotate x in that order...

        public Matrix4x4 matrix = Matrix4x4.identity;

        public MVVTransform() { }

        public MVVTransform(Vector3 position)
        {
            this.position = position;
        }

        public MVVTransform(MVVTransform transform)
        {
            this.position = transform.position;
            this.scale = transform.scale;
            this.rotation = transform.rotation;
        }

        /// <summary>
        /// Populates matrix
        /// </summary>
        public void createMatrix()
        {
            matrix = Matrix4x4.identity;
            matrix.SetTRS(position, Quaternion.Euler(rotation), scale);
        }

    }
}

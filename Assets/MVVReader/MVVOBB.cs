using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Assets.MVVReader
{
    /// <summary>
    /// Orientaded Bounding Box
    /// http://www.3dkingdoms.com/weekly/weekly.php?a=21
    /// </summary>
    public class MVVOBB
    {
        public MVVTransform transform = new MVVTransform(); // Stores transformation and center

        public MVVOBB() { }

        public MVVOBB(MVVOBB obb)
        {
            transform.position = obb.transform.position;
            transform.rotation = obb.transform.rotation;
            transform.scale = obb.transform.scale;
            transform.createMatrix();
        }

        public MVVOBB(Vector3 min, Vector3 max)
        {
            transform.scale = (max - min) / 2;
            transform.position = min + (max - min) / 2;
            transform.createMatrix();
        }

        public void Rotate(Vector3 rotation)
        {
            transform.rotation += rotation;
            transform.createMatrix();
        }

        public static MVVOBB Transform(MVVOBB obb, MVVTransform transform)
        {
            var newObb = new MVVOBB(obb);
            newObb.transform.position += transform.position;
            newObb.transform.rotation += transform.rotation;
            newObb.transform.position += transform.scale;
            transform.scale = Vector3.Scale(newObb.transform.scale, transform.scale);
            return newObb;
        }

        /// <summary>
        /// Checks if a point is contained in this bounding Box
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Vector3 point)
        {
            var newPoint = transform.matrix.inverse.MultiplyPoint(point);
            return (new Bounds(Vector3.zero, Vector3.one)).Contains(newPoint);
        }

        /// <summary>
        /// Checks if OBB intersects with this bounding Box
        /// Method from reimplemented from http://www.3dkingdoms.com/weekly/weekly.php?a=21
        /// </summary>
        /// <param name="obb"></param>
        /// <returns></returns>
        public bool Intersects(MVVOBB obb)
        {
            Vector3 sizeA = this.transform.scale;
            Vector3 sizeB = obb.transform.scale;
            Vector3[] rotationA = this.GetInvertedRotations();
            Vector3[] rotationB = obb.GetInvertedRotations();

            float[,] rotation = new float[3,3];
            float[,] rotationAbs = new float[3,3];

            for (var i = 0; i < 3; i++)
            {
                for (var k = 0; k < 3; k++)
                {
                    rotation[i,k] = Vector3.Dot(rotationA[i], rotationB[k]);
                    rotationAbs[i,k] = Math.Abs(rotation[i,k]);
                }
            }

            // Vector separating the centers of Box B and of Box A	
            Vector3 vSepWS = obb.transform.position - this.transform.position;
            // Rotated into Box A's coordinates
            Vector3 vSepA = new Vector3(Vector3.Dot(vSepWS, rotationA[0]), Vector3.Dot(vSepWS, rotationA[1]), Vector3.Dot(vSepWS, rotationA[2]));

            float ExtentA, ExtentB, Separation;

            // Test if any of A's basis vectors separate the box
            for (var i = 0; i < 3; i++)
            {
                ExtentA = sizeA[i];
                ExtentB = Vector3.Dot(sizeB, new Vector3(rotationAbs[i,0], rotationAbs[i,1], rotationAbs[i,2]));
                Separation = Math.Abs(vSepA[i]);

                if (Separation > ExtentA + ExtentB) return false;
            }

            // Test if any of B's basis vectors separate the box
            for (var k = 0; k < 3; k++)
            {
                ExtentA = Vector3.Dot(sizeA, new Vector3(rotationAbs[0,k], rotationAbs[1,k], rotationAbs[2,k]));
                ExtentB = sizeB[k];
                Separation = Math.Abs(Vector3.Dot(vSepA, new Vector3(rotation[0,k], rotation[1,k], rotation[2,k])));

                if (Separation > ExtentA + ExtentB) return false;
            }

            // Now test Cross Products of each basis vector combination ( A[i], B[k] )
            for (var i = 0; i < 3; i++)
            {
                for (var k = 0; k < 3; k++)
                {
                    int i1 = (i + 1) % 3, i2 = (i + 2) % 3;
                    int k1 = (k + 1) % 3, k2 = (k + 2) % 3;
                    ExtentA = sizeA[i1] * rotationAbs[i2,k] + sizeA[i2] * rotationAbs[i1,k];
                    ExtentB = sizeB[k1] * rotationAbs[i,k2] + sizeB[k2] * rotationAbs[i,k1];
                    Separation = Math.Abs(vSepA[i2] * rotation[i1,k] - vSepA[i1] * rotation[i2,k]);
                    if (Separation > ExtentA + ExtentB) return false;
                }
            }

            // No separating axis found, the boxes overlap	
            return true;
        }

        public Vector3[] GetInvertedRotations()
        {
            var result = new Vector3[3];
            result[0] = new Vector3(transform.matrix[0], transform.matrix[1], transform.matrix[2]);
            result[1] = new Vector3(transform.matrix[4], transform.matrix[5], transform.matrix[6]);
            result[2] = new Vector3(transform.matrix[8], transform.matrix[9], transform.matrix[10]);
            return result;
        }
    }
}

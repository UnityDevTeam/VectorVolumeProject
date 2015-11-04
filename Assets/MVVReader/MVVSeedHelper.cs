using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.MVVReader
{
    public class MVVSeedHelper
    {
        /// <summary>
        /// Loads a seed file with seed positions.
        /// </summary>
        /// <param name="path">absolute file path</param>
        public static Vector3[] loadSeedFile(string path)
        {
            if (path == null) return null;
            int counter = 1;
            string line;
            string[] seedArray;

            List<Vector3> seedList = new List<Vector3>();

            // Read the file and display it line by line.
            System.IO.StreamReader file =
               new System.IO.StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                seedArray = line.Split(new char[] { ' ' });
                if (seedArray.Length != 3)
                    throw new IllegalMVVFileException("Illegal Seedfile (line " + counter + ") of file " + path);
                seedList.Add(new Vector3(float.Parse(seedArray[0]),
                                         float.Parse(seedArray[1]),
                                         float.Parse(seedArray[2])));
                counter++;
            }

            file.Close();

            return seedList.ToArray();
        }
    }
}

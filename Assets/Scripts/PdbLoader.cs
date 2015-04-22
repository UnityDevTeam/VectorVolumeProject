using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Bond
{
    public int atom1;
    public int atom2;
}

public static class PdbLoader
{
    public static string[] AtomSymbols = { "C", "H", "N", "O", "P", "S" };
    public static float[] AtomRadii = { 1.548f, 1.100f, 1.400f, 1.348f, 1.880f, 1.808f };

    public static void CreatePdbPrefab(string pdbName)
    {
        var atoms = LoadPdbFile(pdbName);
        var prefabPath = Application.dataPath + "/Prefabs/" + pdbName + ".pdb";
        var prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
    }

    public static Vector4[] LoadPdbFile(string pdbName)
    {
        Debug.Log("hello");
        var path = Application.dataPath + "/Molecules/" + pdbName + ".pdb";
        if (!File.Exists(path))
        {
            Debug.Log("hello");
            DownloadPdbFile(pdbName);
        }
        //// Gaussian surface formula goes from 2 to 0
        //float r = pow(distance(local, rounding_offset), 2);	
        //float a = -log(2) * r /(atomRadiusSquare);
        //float gauss_f = 2*exp((a));
		
        return ReadPdbFile(path);
    }

    private static void DownloadPdbFile(string pdbName)
    {
        Debug.Log("hello");
        var www = new WWW("http://www.rcsb.org/pdb/download/downloadFile.do?fileFormat=pdb&compression=NO&structureId=" + WWW.EscapeURL(pdbName));

        while (!www.isDone)
        {
            EditorUtility.DisplayProgressBar("Download", "Downloading...", www.progress);
        }
        EditorUtility.ClearProgressBar();

        if (!string.IsNullOrEmpty(www.error)) throw new Exception(www.error);
        File.WriteAllText(Application.dataPath + "/Molecules/" + pdbName + ".pdb", www.text);
    }

    private static Vector4[] ReadPdbFile(string path)
    {
        if(!File.Exists(path)) throw new Exception("File not found at: " + path);
        
        var atoms = new List<Vector4>();

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("ATOM"))
            {
                var split = line.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
                var position = split.Where(s => s.Contains(".")).ToList();
                var id = Array.IndexOf(AtomSymbols, split[2][0].ToString());
                if (id < 0) throw new Exception("Atom symbol not found");

                var atom = new Vector4(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]), AtomRadii[id]);
                atoms.Add(atom);
            }            
        }

        // Find the bounding box of the molecule and center atoms with the origin 
        var bbMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var bbMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        
        foreach (var atom in atoms)
        {
            bbMin = Vector3.Min(bbMin, new Vector3(atom.x, atom.y, atom.z));
            bbMax = Vector3.Max(bbMax, new Vector3(atom.x, atom.y, atom.z));
        }

        var bbCenter = bbMin + (bbMax - bbMin) * 0.5f;

        for (var i = 0; i < atoms.Count; i++)
        {
            atoms[i] -= new Vector4(bbCenter.x, bbCenter.y, bbCenter.z, 0);
        }

        Debug.Log("Loaded " + atoms.Count + " atoms.");

        return atoms.ToArray();
    }
}


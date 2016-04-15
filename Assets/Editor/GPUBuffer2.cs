using System;
using Assets.Editor.MVVReader;
using UnityEngine;

[ExecuteInEditMode]
public class GPUBuffer : MonoBehaviour
{
    //cutaways
    public static int NumNodes = 128;
    public static int NumSDFs = 64;
    public static int NumRegions = 64;
    public static int NumInstances = 262144;
    public static int NumIndices = 262144;
    //public static int NumTransforms = 262144;

    //MVV Buffers //

    public ComputeBuffer NodeBuffer;
    public ComputeBuffer SDFBuffer;
    public ComputeBuffer RegionBuffer;
    public ComputeBuffer InstanceBuffer;
    public ComputeBuffer IndexcellBuffer;
    //public ComputeBuffer TransformBuffer;

    //*****//

    // Declare the buffer manager as a singleton
    private static GPUBuffer _instance = null;
    public static GPUBuffer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GPUBuffer>();
                if (_instance == null)
                {
                    var go = GameObject.Find("_ComputeBufferManager");
                    if (go != null)
                        DestroyImmediate(go);

                    go = new GameObject("_ComputeBufferManager") { hideFlags = HideFlags.HideInInspector };
                    _instance = go.AddComponent<GPUBuffer>();
                }
            }
            return _instance;
        }
    }

    void OnEnable()
    {
        InitBuffers();
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    public void InitBuffers()
    {
        if (NodeBuffer == null) NodeBuffer = new ComputeBuffer(NumNodes, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Node)));
        if (SDFBuffer == null) SDFBuffer = new ComputeBuffer(NumSDFs, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SDF)));
        if (RegionBuffer == null) RegionBuffer = new ComputeBuffer(NumRegions, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Region)));
        if (InstanceBuffer == null) InstanceBuffer = new ComputeBuffer(NumInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Instance)));
        if (IndexcellBuffer == null) IndexcellBuffer = new ComputeBuffer(NumIndices, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Indexcell)));
        //if (TransformBuffer == null) TransformBuffer = new ComputeBuffer(NumTransforms, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Matrix4x4)));

    }

    // Flush buffers on exit
    void ReleaseBuffers()
    {
        if (NodeBuffer != null) { NodeBuffer.Release(); NodeBuffer = null; }
        if (SDFBuffer != null) { SDFBuffer.Release(); SDFBuffer = null; }
        if (RegionBuffer != null) { RegionBuffer.Release(); RegionBuffer = null; }
        if (InstanceBuffer != null) { InstanceBuffer.Release(); InstanceBuffer = null; }
        if (IndexcellBuffer != null) { IndexcellBuffer.Release(); IndexcellBuffer = null; }
        //if (TransformBuffer != null) { TransformBuffer.Release(); TransformBuffer = null; }
    }
}
/**
 * @file      ROSPointCloudVisualiser.cs
 * @author    Benjamin Williams <bwilliams@lincoln.ac.uk>
 * @copyright Copyright (c) University of Lincoln 2020
 *
 * @brief     A script which dispatches the PointCloudMeshVertices.compute compute shader to
 *            transform PointCloud2 points to mesh vertices. Some snippets of code throughout
 *            are borrowed from inmo-jang's repo.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("UoL/ROS/PointCloud2 Visualiser (mesh-based)")]
[RequireComponent(typeof(ROSPointCloud2Subscriber))]
public class ROSPointCloudVisualiser : MonoBehaviour
{
    #region Fields 

    /// <summary>
    /// The subscriber this visualiser should listen to
    /// </summary>
    [HideInInspector]
    public ROSPointCloud2Subscriber subscriber;

    [Header("[compute shader params]")]

    /// <summary>
    /// The compute shader for the transformation
    /// </summary>
    public ComputeShader computeShader;

    //The buffer for points (input)
    private ComputeBuffer pointBuffer;

    //The buffer for vertices (output)
    private ComputeBuffer vertexBuffer;

    //The buffer for indices (output)
    private ComputeBuffer indicesBuffer;

    //The buffer for vertex colours (output)
    private ComputeBuffer colorBuffer;





    /// <summary>
    /// The kernel ID of CSMain, the main kernel of the compute shader
    /// </summary>
    [HideInInspector]
    public int kernelID;

    /// <summary>
    /// The kernel id of ComputeIndices
    /// </summary>
    [HideInInspector]
    public int indicesKernelID;
    
    //The maximum number of points in the visualisation. This is used for pre-allocation of memory.
    public const int MAX_NUM_POINTS = 512 * 512;

    //The stride of one point in the data (20 bytes)
    public const int POINT_STRUCT_STRIDE = 20;

    //Number of threads run on each kernel (our wavefront is 64x1x1 = 64)
    public const int NUM_THREADS = 64;




    /// <summary>
    /// The mesh vertices
    /// </summary>
    [HideInInspector]
    public Vector3[] vertices = new Vector3[MAX_NUM_POINTS];

    /// <summary>
    /// The mesh indices
    /// </summary>
    [HideInInspector]
    public int[] indices = new int[MAX_NUM_POINTS];

    /// <summary>
    /// The mesh colours
    /// </summary>
    [HideInInspector]
    public Color[] colours = new Color[MAX_NUM_POINTS];

    /// <summary>
    /// The actual mesh
    /// </summary>
    private Mesh mesh;

    /// <summary>
    /// The shader to render the points with
    /// </summary>
    [Header("[rendering]")]
    public Shader renderShader;

    /// <summary>
    /// The vector by which each point is hadamarded against.
    /// </summary>
    public Vector3 renderArea = new Vector3(1, 0.2f, 1);

    /// <summary>
    /// The last sequence value of the data
    /// </summary>
    private uint lastSeq = default(uint);

    #endregion

    #region Unity callbacks

    /// <summary>
    /// Called on start-up of the visualiser
    /// </summary>
    private void Start()
    {
        //Get the subscriber
        this.subscriber = GetComponent<ROSPointCloud2Subscriber>();

        //Get the kernel id
        kernelID = computeShader.FindKernel("CSMain");
        indicesKernelID = computeShader.FindKernel("ComputeIndices");

        //Create the compute buffers
        this.CreateComputeBuffers();

        //Create mesh with 32 bit vertex index, to allow for a looot of vertices (thanks inmo-jang)
        this.mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        //Set mesh
        GetComponent<MeshFilter>().mesh = this.mesh;
        GetComponent<MeshRenderer>().material = new Material(this.renderShader);

        //Set up indices initially
        this.SetupIndices();
    }


    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        //Last seq is not the same as the subscriber's seq?
        //Something has updated:
        if (lastSeq != subscriber.seqValue)
            this.OnMessageReceived();

        //Set shader params
        GetComponent<Renderer>().material.SetVector("_PointMultMagnitude", renderArea);

        //Set last sequence value to this seq value
        lastSeq = subscriber.seqValue;
    }


    /// <summary>
    /// Called when the application is exited (just dispose of compute buffers)
    /// </summary>
    public void OnApplicationQuit()
    {
        //Dispose of all buffers
        vertexBuffer.Dispose();
        pointBuffer.Dispose();
        indicesBuffer.Dispose();
        colorBuffer.Dispose();
    }

    #endregion

    #region Compute shader funcs

    /// <summary>
    /// Set up the indices of the mesh initially -- they don't need to change as
    /// this is a bunch of points and it doesnt matter in what order the vertices appear.
    /// </summary>
    private void SetupIndices()
    {
        //Set indices buffer, run it
        indicesBuffer.SetData(indices);
        computeShader.SetBuffer(indicesKernelID, "indices", indicesBuffer);
        computeShader.Dispatch(indicesKernelID, MAX_NUM_POINTS / NUM_THREADS, 1, 1);

        //And get the data
        indicesBuffer.GetData(indices);

        //Set mesh indices
        mesh.SetIndices(indices, MeshTopology.Points, 0);
    }

    /// <summary>
    /// Create the compute buffers used throughout this visualisation.
    /// </summary>
    private void CreateComputeBuffers()
    {
        //Make a point buffer
        pointBuffer = new ComputeBuffer(MAX_NUM_POINTS, POINT_STRUCT_STRIDE, ComputeBufferType.Structured);

        //Make a vertex buffer (which is just a vec3 buffer)
        vertexBuffer = new ComputeBuffer(MAX_NUM_POINTS, sizeof(float) * 3);

        //Make an indices buffer
        indicesBuffer = new ComputeBuffer(MAX_NUM_POINTS, sizeof(int));

        //Create colour buffer (rgba)
        colorBuffer = new ComputeBuffer(MAX_NUM_POINTS, sizeof(float) * 4);
    }

    /// <summary>
    /// Dispatches the compute shader but sets the compute buffers first.
    /// </summary>
    private void DispatchComputeShader()
    {
        //Set point buffer (input)
        pointBuffer.SetData(subscriber.data);

        //Tell the GPU to run, but set up buffers for communication first
        computeShader.SetBuffer(kernelID, "points", pointBuffer);
        computeShader.SetBuffer(kernelID, "vertices", vertexBuffer);
        computeShader.SetBuffer(kernelID, "colors", colorBuffer);
        computeShader.Dispatch(kernelID, MAX_NUM_POINTS / NUM_THREADS, 1, 1);
    }

    #endregion

    #region Visualisation / OnMessageReceived(...)

    /// <summary>
    /// Updates the point cloud mesh in the visualisation by
    /// firstly getting data from the VB and setting internal mesh data, then 
    /// actually applying it to the mesh
    /// </summary>
    private void UpdateMesh()
    {
        //We don't really need to clear the mesh, but:
        //--
        //this.vertices = new Vector3[MAX_NUM_POINTS];
        //mesh.Clear();

        //Set mesh vertices
        vertexBuffer.GetData(this.vertices);

        //Get mesh colours
        colorBuffer.GetData(this.colours);

        //Set mesh vertices, colours & indices
        mesh.vertices = this.vertices;
        mesh.colors = this.colours;
        mesh.SetIndices(this.indices, MeshTopology.Points, 0);
    }


    /// <summary>
    /// Called when a message has been received from the
    /// subscriber (indirectly)
    /// </summary>
    private void OnMessageReceived()
    {
        //Run compute shader
        this.DispatchComputeShader();

        //Set mesh
        this.UpdateMesh();
    }

    #endregion
}

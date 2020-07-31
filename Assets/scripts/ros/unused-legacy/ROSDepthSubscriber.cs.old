/**
 * @file       ROSDepthSubscriber.cs
 * @brief      A basic ROS bridge subscriber for PointCloud2 topic types
 *
 * @author     Benjamin Williams <trewelb@gmail.com>
 * @copyright  Copyright (c) University of Lincoln 2020
*/

using System.Collections;

using System.Collections.Generic;
using UnityEngine;

using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Protocols;

using stdMsgs = RosSharp.RosBridgeClient.MessageTypes.Std;
using sensorMsgs = RosSharp.RosBridgeClient.MessageTypes.Sensor;

[System.Serializable]
public class ROSConnectionInfo
{
    [Header("[uri info]")]

    /// <summary>
    /// The IP to connect to 
    /// </summary>
    public string ip = "127.0.0.1";

    /// <summary>
    /// The protocol (web socket by default)
    /// </summary>
    public string protocol = "ws";

    /// <summary>
    /// The port (9090 by default)
    /// </summary>
    public int port = 9090;

    /// <summary>
    /// The URI to connect to 
    /// </summary>
    /// <value></value>
    public string uri
    {
        get { return $"{protocol}://{ip}:{port}"; }
    }

    [Header("[internal ros info]")]

    /// <summary>
    /// The serialiser
    /// </summary>
    public RosSocket.SerializerEnum serializer;

    /// <summary>
    /// The ros protocol 
    /// </summary>
    public Protocol rosProtocol;
}

public class ROSDepthSubscriber : MonoBehaviour
{
    /// <summary>
    /// Connection info for ROS bridge
    /// </summary>
    [Header("[connection]")]
    public ROSConnectionInfo connectionInfo;

    /// <summary>
    /// The connector
    /// </summary>
    public ROSBridgeConnector connector;

    public ComputeShader computeShader;
    public RenderTexture texture;
    private ComputeBuffer buffer;
    private int kernelID;

    public UnityEngine.VFX.VisualEffect visualEffect;

    private int texSize = 512;
    private int numThreads = 64;

    public void Start()
    {
        //Make a new connect and connect
        connector = new ROSBridgeConnector(connectionInfo);
        connector.Connect(this.onRosBridgeConnect);

        //Make buffer
        texture = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.ARGBFloat);
        texture.enableRandomWrite = true;
        texture.Create();

        buffer = new ComputeBuffer(texSize * texSize, 20, ComputeBufferType.Structured);
        this.kernelID = computeShader.FindKernel("CSMain");
    }

    private void onRosBridgeConnect()
    {
        //At this point, the connection is 100% established so lets subscribe to something
        //without fear of the RosSocket being null
        connector.SubscribeTo<sensorMsgs.PointCloud2>("/camera/depth/color/points", onPointCloudMessage);
    }

    private byte[] lastBytes;

    private int dispatchSeq = 0;
    private int oldDispatchSeq = 0;

    private void onPointCloudMessage(sensorMsgs.PointCloud2 message)
    {
        /*
         * fields:
            x 0 32
            y 4 32
            z 8 64?
            rgb 16 

            20 bytes:
            ----------
            x is bytes   [0 to 4]
            y is bytes   [4 to 8]
            z is bytes   [8 to 16]
            rgb is bytes [16 to 20]
        */

        //var fields = message.header.seq;

        //Debug.Log(message.data.Length / message.point_step);


        //Get the first element
        lastBytes = message.data;
        dispatchSeq++;

        //this.PrintFrameData(ref message);
        //this.PrintFieldData(ref message);
    }

    private void Update()
    {
        //For some reason unity doesnt run the compute shader stuff
        //if its in the onPointCloudMessage callback.. so this is a work around.

        if(oldDispatchSeq != dispatchSeq)
        {
            //And do compute shader stuff
            //Run compute shader
            buffer.SetData(lastBytes);
            computeShader.SetTexture(kernelID, "outTex", texture);
            computeShader.SetBuffer(kernelID, "points", buffer);
            //computeShader.Dispatch(kernelID, texture.width / numThreads, texture.height / numThreads, 1);
            computeShader.Dispatch(kernelID, buffer.count / numThreads, 1, 1);

            //Debug.Log("dispatch!");

            //Set dispatch seq
            oldDispatchSeq = dispatchSeq;
        }

        int texID = Shader.PropertyToID("renderTexture");
        visualEffect.SetTexture(texID, texture);
    }

    private void PrintFieldData(ref sensorMsgs.PointCloud2 message)
    {
        foreach(var field in message.fields)
        {
            Debug.Log($"Start at byte {field.offset}, read {field.count}, name = {field.name}, datatype = {field.datatype}");
        }
    }

    private void PrintFrameData(ref sensorMsgs.PointCloud2 message)
    {
        var seq = message.header.seq;
        var timeSecs = message.header.stamp.nsecs;

        var bigendian = message.is_bigendian;
        var dense = message.is_dense;

        var pt_step = message.point_step;
        var row_step = message.row_step;

        Debug.Log($"[message {seq} time {timeSecs}]: big endian [{bigendian}], dense [{dense}] => step (pt) is {pt_step}, step (row) is {row_step}");
    }


    public void OnApplicationQuit()
    {
        //Unsubscribe
        connector.UnsubscribeFromAll();

        //Close the connection on exit
        connector.Close();

        buffer.Dispose();
    }
}

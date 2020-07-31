/**
 * @file      ROSPointCloud2Subscriber.cs
 * @author    Benjamin Williams <bwilliams@lincoln.ac.uk>
 * @copyright Copyright (c) University of Lincoln 2020
 *
 * @brief     A script which subscribes to a PointCloud2 topic, using ros-sharp's UnitySubscriber
 *            class. 
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using RosSharp;
using RosSharp.RosBridgeClient;
using stdMessages = RosSharp.RosBridgeClient.MessageTypes.Std;
using sensorMessages = RosSharp.RosBridgeClient.MessageTypes.Sensor;

[AddComponentMenu("UoL/ROS/PointCloud2 Subscriber")]
[RequireComponent(typeof(RosConnector))]
public class ROSPointCloud2Subscriber : UnitySubscriber<sensorMessages.PointCloud2>
{
    #region Fields

    /// <summary>
    /// The last time a message was received
    /// </summary>
    [HideInInspector]
    protected float lastTime = -1f;

    /// <summary>
    /// Data sent in last message
    /// </summary>
    [HideInInspector]
    public byte[] data;

    /// <summary>
    /// The sequence value for the last message
    /// </summary>
    [HideInInspector]
    public uint seqValue = default(uint);

    #endregion

    #region UnitySubscriber overrides 

    /// <summary>
    /// Called when a message is received
    /// </summary>
    /// <param name="message"></param>
    protected override void ReceiveMessage(sensorMessages.PointCloud2 message)
    {
        //Copy the ref over
        this.data = message.data;

        //Set last seq value
        this.seqValue = message.header.seq;
    }

    #endregion
}

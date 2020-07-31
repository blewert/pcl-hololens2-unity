using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PointCloudComputeTest : MonoBehaviour
{
    public ComputeShader compute;
    public RenderTexture renderTex;
    private int kernelID;

    private Mesh mesh;

    public int renderTexSize = 512;

    public ComputeBuffer buffer;

    // Start is called before the first frame update
    void Start()
    {
        //byte array -> shader -> vertex data?
        //byte array -> shader -> tex -> gpu particles?
        //byte array -> marshal -> vertex data?
        //byte array -> shader -> onrenderobject -> gfx shader 

        //Find the kernel id
        kernelID = compute.FindKernel("CSMain");

        //And make a render texture
        renderTex = new RenderTexture(renderTexSize, renderTexSize, 24, RenderTextureFormat.Default);
        renderTex.enableRandomWrite = true;
        renderTex.Create();

        //Set the texture
        //compute.SetBuffer()
        buffer = new ComputeBuffer(renderTexSize * renderTexSize, 20, ComputeBufferType.Structured);

        //Set to render tex
        GetComponent<Renderer>().material.SetTexture("_MainTex", renderTex);
    }

    public void SetPoints()
    {
        //Set the data
        //buffer.SetData(data);

        compute.SetTexture(kernelID, "outTex", renderTex);

        ////Dispatch
        compute.Dispatch(kernelID, renderTex.width / 8, renderTex.height / 8, 1);
    }

    public void Update()
    {
        SetPoints();
    }

    void OnDestroy()
    {
        buffer.Dispose();
    }
}

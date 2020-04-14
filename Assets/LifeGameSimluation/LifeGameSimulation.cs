using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LifeGameSimulation : MonoBehaviour
{
    public Shader showBufferShader;
    public ComputeShader computeShader;
    [Range(10, 100)]
    public float zoomScale = 50.0f;
    [Range(50, 250)]
    public float zoomRange = 100.0f;
    [Range(0,1)]
    public float initLifeProb = 0.5f;
    public float updateTime = 1.0f;
    RenderTexture[] renderTextures = new RenderTexture[2];
    CommandBuffer commandBuffer;
    ComputeBuffer PRNGStates;
    bool isFirstFrame = true;
    private Material showBufferMaterial;
    float dt = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        for (var i = 0; i < 2; i++)
        {
            var renderTexture = new RenderTexture(4096, 4096, 0, RenderTextureFormat.RFloat, 0);
            if (renderTexture)
            {
                renderTexture.filterMode = FilterMode.Point;
                renderTexture.enableRandomWrite = true;
                renderTexture.useMipMap = false;
                renderTexture.wrapMode = TextureWrapMode.Clamp;

                renderTexture.Create();
            }
            renderTextures[i] = renderTexture;
        }

        CreatePRNGStates();

        showBufferMaterial = new Material(showBufferShader);

        commandBuffer = new CommandBuffer();
        commandBuffer.name = "Show Life";
        Camera.main.AddCommandBuffer(CameraEvent.AfterEverything, commandBuffer);

        dt = updateTime;
    }

    void ComputeLife()
    {
        int kernalID = -1;
        commandBuffer.Clear();
        kernalID = computeShader.FindKernel("CSSimulation");

        commandBuffer.SetComputeIntParam(computeShader, "_IsFistFrame", isFirstFrame ? 1 : 0);
        commandBuffer.SetComputeFloatParam(computeShader, "_InitLifeProb", initLifeProb); 
        commandBuffer.SetComputeIntParam(computeShader, "_OutputTargetSize", renderTextures[0].width);
        commandBuffer.SetComputeBufferParam(computeShader, kernalID, "_PRNGStates", PRNGStates);
        commandBuffer.SetComputeTextureParam(computeShader, kernalID, "_Buffer0", renderTextures[0]);
        commandBuffer.SetComputeTextureParam(computeShader, kernalID, "_Buffer1", renderTextures[1]);
        commandBuffer.DispatchCompute(computeShader, kernalID, renderTextures[0].width / 32, renderTextures[0].height / 32, 1);

        Graphics.ExecuteCommandBuffer(commandBuffer);
        isFirstFrame = false;
    }

    void CreatePRNGStates()
    {
        var mapSize = renderTextures[0].width;

        PRNGStates = new ComputeBuffer(mapSize * mapSize, 4 * 4, ComputeBufferType.Structured, ComputeBufferMode.Immutable);

        var _mt19937 = new MersenneTwister.MT.mt19937ar_cok_opt_t();
        _mt19937.init_genrand((uint)System.DateTime.Now.Ticks);

        var data = new uint[mapSize * mapSize * 4];
        for (var i = 0; i < mapSize * mapSize * 4; ++i)
        {
            data[i] = _mt19937.genrand_int32();
        }

        PRNGStates.SetData(data);
    }

    void OnDestroy()
    {
        if (commandBuffer != null)
        {
            commandBuffer.Release();
            commandBuffer = null;
        }

        if (PRNGStates != null)
        {
            PRNGStates.Release();
            PRNGStates = null;
        }
    }


    
    // Update is called once per frame
    void Update()
    {
        dt += Time.deltaTime;
        if (dt > updateTime)
        {
            ComputeLife();
            dt = 0.0f;
        }
        commandBuffer.Clear();

        showBufferMaterial.SetFloat("_ZoomScale", zoomScale);
        showBufferMaterial.SetFloat("_ZoomRange", zoomRange);
        showBufferMaterial.SetVector("_ResolutionSize", new Vector4(Camera.main.pixelWidth, Camera.main.pixelHeight, 0,0));
        showBufferMaterial.SetVector("_Mouse", new Vector4(Input.mousePosition.x, Input.mousePosition.y, Input.GetMouseButton(0) ? 1.0f : 0.0f, Input.GetMouseButton(1) ? 1.0f : 0.0f));
        commandBuffer.Blit(renderTextures[0], BuiltinRenderTextureType.CameraTarget, showBufferMaterial);
    }
}

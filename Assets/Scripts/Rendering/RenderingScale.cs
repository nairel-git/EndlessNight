using System.ComponentModel;
using UnityEngine;

public class RenderingScale : MonoBehaviour
{
    private RenderTexture rt;

    [Header("Creates and sets up lowres cam on start (Just enable it on a empty object)")]

    [Range(0.01f, 1f)]
    public float scale = 0.25f;
    public FilterMode filter = FilterMode.Point;

    void Start()
    {

        var renderCam = gameObject.AddComponent<Camera>();
        // --- Camera setup ---
        renderCam.orthographic = true;
        renderCam.cullingMask = LayerMask.GetMask("Nothing");
        renderCam.clearFlags = CameraClearFlags.Nothing;
        renderCam.nearClipPlane = 0.01f;
        renderCam.farClipPlane = 0.02f;
        renderCam.allowHDR = false;
        renderCam.allowMSAA = false;
        renderCam.useOcclusionCulling = false;
        renderCam.depth = Camera.main.depth + 1;

        // --- Create RenderTexture ---
        int width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * scale));

        rt = new RenderTexture(width, height, 24, RenderTextureFormat.Default);
        rt.filterMode = filter;
        rt.Create();

        Camera.main.targetTexture = rt;
    }

    

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(rt, dest);
    }

}
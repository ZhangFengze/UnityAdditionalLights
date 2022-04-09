using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;

[ExecuteAlways]
public class BakeLightmap : MonoBehaviour
{
    private class BakeObject
    {
        public MeshFilter meshFilter;
        public Renderer renderer;
        public GameObject go;
    }

    private static BakeLightmap instance;
    public static BakeLightmap Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new BakeLightmap();
            }
            return instance;
        }
    }

    private static RenderTexture texture;

    [MenuItem("ZFZ/bake")]
    public static void Bake()
    {
        RenderPipelineManager.beginFrameRendering += RenderPipelineManager_beginFrameRendering;
    }

    private static void RenderPipelineManager_beginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        Debug.Log("beginFrameRendering");
        RenderPipelineManager.beginFrameRendering -= RenderPipelineManager_beginFrameRendering;
        Bake(context);
    }

    private static void Bake(ScriptableRenderContext context)
    {
        //Debug.Assert(texture == null);

        if (texture == null)
            texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);

        var cmd = new CommandBuffer();
        CoreUtils.SetRenderTarget(cmd, texture);
        CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.black);

        var mat = new Material(Shader.Find("Flatten To Lightmap"));
        foreach (var o in CollectBakeObjects())
        {
            cmd.SetGlobalVector("_LightmapScaleOffset", o.renderer.lightmapScaleOffset);
            for(int subMeshIndex = 0; subMeshIndex < o.meshFilter.sharedMesh.subMeshCount; subMeshIndex++)
                cmd.DrawMesh(o.meshFilter.sharedMesh, Matrix4x4.identity, mat, subMeshIndex, 0);
        }
        context.ExecuteCommandBuffer(cmd);

        RenderPipelineManager.endFrameRendering += RenderPipelineManager_endFrameRendering1;
    }

    private static void RenderPipelineManager_endFrameRendering1(ScriptableRenderContext arg1, Camera[] arg2)
    {
        Debug.Log("endFrameRendering");
        RenderPipelineManager.endFrameRendering -= RenderPipelineManager_endFrameRendering1;

        Debug.Assert(texture != null);
        SaveTGA(texture, TextureFormat.RGBA32, $@"D:\Projects\BakeLightmap\Assets\output.tga");
        AssetDatabase.Refresh();

        texture.Release();
        texture = null;
    }

    private static BakeObject[] CollectBakeObjects()
    {
        var activeScene = SceneManager.GetActiveScene();
        var gameObjects = Object.FindObjectsOfType<GameObject>().Where(go => go.scene == activeScene);
        var needBakeObjects = gameObjects.Select(CollectBakeObject).Where(x => x != null);
        return needBakeObjects.ToArray();
    }

    private static BakeObject CollectBakeObject(GameObject go)
    {
        if (!go.activeInHierarchy)
            return null;

        if ((GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.ContributeGI) == 0)
            return null;

        var renderer = GetRenderer(go);
        if (renderer == null)
            return null;

        var mesh = go.GetComponent<MeshFilter>();
        if (mesh == null || mesh.sharedMesh == null)
            return null;

        return new BakeObject { meshFilter = mesh, renderer = renderer, go = go };
    }

    private static Renderer GetRenderer(GameObject go)
    {
        Renderer r = go.GetComponent<MeshRenderer>();
        if (r != null) return r;
        return go.GetComponent<SkinnedMeshRenderer>();
    }

    private static void SaveTGA(RenderTexture rt, TextureFormat format, string path)
    {
        var texture = new Texture2D(rt.width, rt.height, format, false);

        var oldRT = RenderTexture.active;
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = oldRT;

        File.WriteAllBytes(path, texture.EncodeToTGA());
    }
}

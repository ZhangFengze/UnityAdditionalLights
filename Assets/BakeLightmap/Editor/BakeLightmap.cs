using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;

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

    [MenuItem("ZFZ/bake")]
    public static void Bake()
    {
        var texture = new RenderTexture(512, 512, 0, RenderTextureFormat.R8);

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
        Graphics.ExecuteCommandBuffer(cmd);

        SaveTGA(texture, TextureFormat.R8, "Assets/output.tga");

        var settings = new TextureImporterSettings();
        settings.textureType=TextureImporterType.SingleChannel;
        settings.textureShape=TextureImporterShape.Texture2D;
        settings.mipmapEnabled=false;
        settings.singleChannelComponent=TextureImporterSingleChannelComponent.Red;

        AssetDatabase.ImportAsset("Assets/output.tga", ImportAssetOptions.ForceSynchronousImport);
        var importer=TextureImporter.GetAtPath("Assets/output.tga") as TextureImporter;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

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

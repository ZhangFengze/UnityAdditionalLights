using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using static SetupSceneAdditionalLights;

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

    public static Texture2D Bake(AdditionalLightingMode mode)
    {
        var rt = new RenderTexture(Lightmapping.lightingSettings.lightmapMaxSize, Lightmapping.lightingSettings.lightmapMaxSize, 0, GetRenderTextureFormat(mode), RenderTextureReadWrite.Linear);

        var cmd = new CommandBuffer();
        CoreUtils.SetRenderTarget(cmd, rt);
        CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.black);

        var mat = new Material(Shader.Find("Flatten To Lightmap"));
        int pass = mat.FindPass(GetShaderPass(mode));
        foreach (var o in CollectBakeObjects())
        {
            cmd.SetGlobalVector("_LightmapScaleOffset", o.renderer.lightmapScaleOffset);
            for (int subMeshIndex = 0; subMeshIndex < o.meshFilter.sharedMesh.subMeshCount; subMeshIndex++)
                cmd.DrawMesh(o.meshFilter.sharedMesh, o.go.transform.localToWorldMatrix, mat, subMeshIndex, pass);
        }
        Graphics.ExecuteCommandBuffer(cmd);

        var texture = SaveTexture(rt, GetTextureFormat(mode));
        rt.Release();
        return texture;
    }

    public static Texture2D BakeAndSave(AdditionalLightingMode mode, string path)
    {
        var lightmap = Bake(mode);

        File.WriteAllBytes(path, lightmap.EncodeToTGA());

        var settings = new TextureImporterSettings();
        ConfigureImportSettings(mode, settings);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = TextureImporter.GetAtPath(path) as TextureImporter;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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

    private static Texture2D SaveTexture(RenderTexture rt, TextureFormat format)
    {
        var texture = new Texture2D(rt.width, rt.height, format, false, true);

        var oldRT = RenderTexture.active;
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = oldRT;

        return texture;
    }

    private static RenderTextureFormat GetRenderTextureFormat(AdditionalLightingMode mode)
    {
        switch (mode)
        {
            case AdditionalLightingMode.OneLight:
                return RenderTextureFormat.R8;
            case AdditionalLightingMode.TwoLight:
                return RenderTextureFormat.RG16;
            default:
                Debug.Assert(false);
                return RenderTextureFormat.Default;
        }
    }

    private static TextureFormat GetTextureFormat(AdditionalLightingMode mode)
    {
        switch (mode)
        {
            case AdditionalLightingMode.OneLight:
                return TextureFormat.R8;
            case AdditionalLightingMode.TwoLight:
                return TextureFormat.RG16;
            default:
                Debug.Assert(false);
                return TextureFormat.ARGB32;
        }
    }

    private static string GetShaderPass(AdditionalLightingMode mode)
    {
        switch (mode)
        {
            case AdditionalLightingMode.OneLight:
                return "OneLight";
            case AdditionalLightingMode.TwoLight:
                return "TwoLight";
            default:
                Debug.Assert(false);
                return "";
        }
    }

    private static void ConfigureImportSettings(AdditionalLightingMode mode, TextureImporterSettings settings)
    {
        switch (mode)
        {
            case AdditionalLightingMode.OneLight:
                settings.textureType = TextureImporterType.SingleChannel;
                settings.textureShape = TextureImporterShape.Texture2D;
                settings.mipmapEnabled = false;
                settings.sRGBTexture = false;
                settings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                break;
            case AdditionalLightingMode.TwoLight:
                settings.textureType = TextureImporterType.Default;
                settings.textureShape = TextureImporterShape.Texture2D;
                settings.mipmapEnabled = false;
                settings.sRGBTexture = false;
                break;
        }
    }
}

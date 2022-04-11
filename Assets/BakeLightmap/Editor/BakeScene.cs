using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using static SetupSceneAdditionalLights;

public static class BakeScene
{
    private static string MagicGameObjectName = "SetupSceneAdditionalLights";
    private const int MaxLights = 16;

    [MenuItem("ZFZ/BakeNone")]
    public static void BakeNone()
    {
        Bake(AdditionalLightingMode.None);
    }

    [MenuItem("ZFZ/BakeOneLight")]
    public static void BakeOneLight()
    {
        Bake(AdditionalLightingMode.OneLight);
    }

    [MenuItem("ZFZ/BakeTwoLights")]
    public static void BakeTwoLights()
    {
        Bake(AdditionalLightingMode.TwoLight);
    }

    public static void Bake(AdditionalLightingMode mode)
    {
        var data = GetOrCreateData();
        data.mode = mode;
        if (mode == AdditionalLightingMode.None)
        {
            data.AdditionalLightsCount = 0;
            return;
        }

        var rawLights = Object.FindObjectsOfType<Light>().Where(FilterLight).Take(MaxLights);
        NativeArray<VisibleLight> lights = new NativeArray<VisibleLight>(rawLights.Select(ToVisibleLight).ToArray(), Allocator.Temp);
        data.AdditionalLightsCount = lights.Length;
        if (data.AdditionalLightsCount == 0)
            return;

        data.AdditionalLightPositions = new Vector4[data.AdditionalLightsCount];
        data.AdditionalLightColors = new Vector4[data.AdditionalLightsCount];
        data.AdditionalLightAttenuations = new Vector4[data.AdditionalLightsCount];
        data.AdditionalLightSpotDirections = new Vector4[data.AdditionalLightsCount];

        for (int i = 0; i < lights.Length; ++i)
        {
            UniversalRenderPipeline.InitializeLightConstants_Common(lights, i, out data.AdditionalLightPositions[i],
                out data.AdditionalLightColors[i],
                out data.AdditionalLightAttenuations[i],
                out data.AdditionalLightSpotDirections[i],
                out Vector4 _);
        }

        data.Refresh();
        data.AdditionalLightsLightmap = BakeLightmap.BakeAndSave(mode, "Assets/output.tga");
        data.Refresh();
    }

    private static SetupSceneAdditionalLights GetOrCreateData()
    {
        var obj = GameObject.Find(MagicGameObjectName);
        if (obj == null)
            obj = new GameObject(MagicGameObjectName);

        var data = obj.GetComponent<SetupSceneAdditionalLights>();
        if (data != null)
            Object.DestroyImmediate(data);

        return obj.AddComponent<SetupSceneAdditionalLights>();
    }

    private static bool FilterLight(Light light)
    {
        if (light.type != LightType.Point)
            return false;
        if (light.lightmapBakeType == LightmapBakeType.Baked)
            return false;
        return true;
    }

    private static VisibleLight ToVisibleLight(Light raw)
    {
        VisibleLight light = new VisibleLight();
        var lightField = typeof(VisibleLight).GetField("m_InstanceId", BindingFlags.NonPublic | BindingFlags.Instance);
        lightField.SetValue(light, raw.GetInstanceID());
        light.lightType = raw.type;
        light.finalColor = raw.color * raw.intensity;
        light.localToWorldMatrix = raw.transform.localToWorldMatrix;
        light.range = raw.range;
        light.spotAngle = raw.spotAngle;
        return light;
    }
}

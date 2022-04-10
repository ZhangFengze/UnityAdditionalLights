using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SetupSceneAdditionalLights : MonoBehaviour
{
    static private class LightConstantBuffer
    {
        public static int _AdditionalLightsCount;
        public static int _AdditionalLightsPosition;
        public static int _AdditionalLightsColor;
        public static int _AdditionalLightsAttenuation;
        public static int _AdditionalLightsSpotDir;
        public static int _AdditionalLightsLightmap;
    }

    public int AdditionalLightsCount;
    public Vector4[] AdditionalLightPositions;
    public Vector4[] AdditionalLightColors;
    public Vector4[] AdditionalLightAttenuations;
    public Vector4[] AdditionalLightSpotDirections;
    public Texture2D AdditionalLightsLightmap;

    static SetupSceneAdditionalLights()
    {
        LightConstantBuffer._AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
        LightConstantBuffer._AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
        LightConstantBuffer._AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
        LightConstantBuffer._AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
        LightConstantBuffer._AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
        LightConstantBuffer._AdditionalLightsLightmap = Shader.PropertyToID("_AdditionalLightsLightmap");
    }

    public void Refresh()
    {
        Shader.SetGlobalVector(LightConstantBuffer._AdditionalLightsCount, new Vector4(AdditionalLightsCount, 0.0f, 0.0f, 0.0f));
        if (AdditionalLightsCount > 0)
        {
            Shader.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsPosition, AdditionalLightPositions);
            Shader.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsColor, AdditionalLightColors);
            Shader.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsAttenuation, AdditionalLightAttenuations);
            Shader.SetGlobalVectorArray(LightConstantBuffer._AdditionalLightsSpotDir, AdditionalLightSpotDirections);
            Shader.SetGlobalTexture(LightConstantBuffer._AdditionalLightsLightmap, AdditionalLightsLightmap);
        }
    }

    private void OnEnable()
    {
        Refresh();
    }
}

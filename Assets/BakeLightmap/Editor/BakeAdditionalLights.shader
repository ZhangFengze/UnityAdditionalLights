Shader "Bake Additional Lights"
{
    Properties { }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Cull off

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        float4 _LightmapScaleOffset;

        struct appdata
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 lightmapUV : TEXCOORD1;
        };

        struct v2f
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 lightmapUV : TEXCOORD1;
            float3 positionWS : TEXCOORD2;
            float3 normalWS : TEXCOORD3;
            float4 tangentWS : TEXCOORD4;    // xyz: tangent, w: sign
            float3 viewDirWS : TEXCOORD5;
        };

        bool Brighter(Light a, Light b)
        {
            return a.distanceAttenuation > b.distanceAttenuation;
        }

        v2f vert(appdata v)
        {
            v2f o;

            VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
            VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);

            o.uv = v.uv;
            o.lightmapUV = v.lightmapUV * _LightmapScaleOffset.xy + _LightmapScaleOffset.zw;
            o.positionCS = float4(o.lightmapUV * float2(2, -2) + float2(-1, 1), 0.5, 1);
            // o.positionCS = vertexInput.positionCS;
            o.positionWS = vertexInput.positionWS;
            o.normalWS = normalInput.normalWS;
            o.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
            real sign = v.tangentOS.w * GetOddNegativeScale();
            o.tangentWS = half4(normalInput.tangentWS.xyz, sign);
            o.uv = v.uv;
            o.lightmapUV = v.lightmapUV;
            return o;
        }

        float frag1light(v2f i) : SV_Target
        {
            int mostImportantLightIndex = -1;
            Light mostImportantLight = (Light)0;

            UNITY_LOOP
            for (int lightIndex = 0u; lightIndex < _AdditionalLightsCount.x; ++lightIndex)
            {
                Light light = GetAdditionalPerObjectLight(lightIndex, i.positionWS);
                if (Brighter(light, mostImportantLight))
                {
                    mostImportantLightIndex = lightIndex;
                    mostImportantLight = light;
                }
            }

            return (mostImportantLightIndex + 1) / 16.f;
        }

        float frag2lights(v2f i) : SV_Target
        {
            int lightIndex1 = -1;
            int lightIndex2 = -1;
            Light light1 = (Light)0;
            Light light2 = (Light)0;

            UNITY_LOOP
            for (int lightIndex = 0u; lightIndex < _AdditionalLightsCount.x; ++lightIndex)
            {
                Light light = GetAdditionalPerObjectLight(lightIndex, i.positionWS);
                if (Brighter(light, light1))
                {
                    lightIndex2 = lightIndex1;
                    light2 = light1;
                    lightIndex1 = lightIndex;
                    light1 = light;
                }
                else if (Brighter(light, light2))
                {
                    lightIndex2 = lightIndex;
                    light2 = light;
                }
            }

            int combinedIndex = (lightIndex1 + 1) * 16 + (lightIndex2 + 1);
            return combinedIndex / 256.f;
        }
        ENDHLSL

        Pass
        {
            Name "OneLight"

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag1light
            ENDHLSL
        }

        Pass
        {
            Name "TwoLights"

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag2lights
            ENDHLSL
        }
    }
}

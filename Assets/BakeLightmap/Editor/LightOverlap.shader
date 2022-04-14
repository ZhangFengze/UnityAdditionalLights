Shader "Light Overlap"
{
    Properties { }

    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            static half4 _NoLightColor = half4(0.56471, 0.65837, 0.57758, 1);
            static half4 _OneLightColor = half4(0.25818, 0.7454, 0.30054, 1);
            static half4 _TwoLightColor = half4(1, 0.42327, 0.12214, 1);
            static half4 _OtherLightColor = half4(0.54572, 0.0319, 0.05448, 1);

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD2;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                int cnt = 0;
                UNITY_LOOP
                for (int lightIndex = 0u; lightIndex < _AdditionalLightsCount.x; ++lightIndex)
                {
                    Light light = GetAdditionalPerObjectLight(lightIndex, input.positionWS);
                    if (light.distanceAttenuation > HALF_MIN)
                        ++cnt;
                }

                if (cnt == 0)
                    return _NoLightColor;
                if (cnt == 1)
                    return _OneLightColor;
                if (cnt == 2)
                    return _TwoLightColor;
                return _OtherLightColor;
            }

            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
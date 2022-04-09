Shader "Flatten To Lightmap"
{
    Properties
    {
    }

    SubShader
    {
		Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Cull off

        Pass
        {
			Name "ForwardLit"
			Tags{"LightMode" = "UniversalForward"}


		    HLSLPROGRAM 

            #pragma vertex vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4 _LightmapScaleOffset;

			struct appdata
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float4 tangentOS    : TANGENT;
				float2 uv           : TEXCOORD0;
				float2 lightmapUV   : TEXCOORD1;
			};

            struct v2f
            {
                float4 positionCS               : SV_POSITION;
                float2 uv                       : TEXCOORD0;
                float2 lightmapUV               : TEXCOORD1;
                float3 positionWS               : TEXCOORD2;
                float3 normalWS                 : TEXCOORD3;
                float4 tangentWS                : TEXCOORD4;    // xyz: tangent, w: sign
                float3 viewDirWS                : TEXCOORD5;
            };

            v2f vert (appdata v)
            {
                v2f o;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);

                o.uv = v.uv;
                o.lightmapUV = v.lightmapUV * _LightmapScaleOffset.xy + _LightmapScaleOffset.zw;
                //o.lightmapUV = v.uv* _LightmapScaleOffset.xy + _LightmapScaleOffset.zw;
                o.positionCS = float4(o.lightmapUV * float2(2,-2) + float2(-1, 1), 0.5, 1);
                //o.positionCS = vertexInput.positionCS;
                o.positionWS = vertexInput.positionWS;
                o.normalWS = normalInput.normalWS;
                o.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS); 
                real sign = v.tangentOS.w * GetOddNegativeScale();
                o.tangentWS = half4(normalInput.tangentWS.xyz, sign);

                //o.positionCS = float4(v.lightmapUV * 2 - 1, 0.5, 1);

                //o.vertex.x = o.vertex.x * 0.001 + -0.99;
                //o.vertex.x = v.uv.x;
                //o.vertex.y = -v.uv.y;// o.vertex.y - 0.2;
                //o.vertex.x += _ZFZ_FLOAT0;
                //o.vertex.y = _ZFZ_FLOAT1;
                //o.vertex.z = 0.5;
                //o.vertex.w = 1;
                o.uv = v.uv;
                o.lightmapUV= v.lightmapUV;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(0,1,0,1);
            }
            ENDHLSL
        }
    }
}

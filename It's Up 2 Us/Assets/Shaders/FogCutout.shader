Shader "Hidden/FogCutout"
{
    Properties { _Color("Fog Color", Color) = (0,0,0,0.85) }

    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref 1
            Comp NotEqual     // draw fog where stencil != 1
            Pass Keep
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS: SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 _Color;
            half4 frag (Varyings i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}

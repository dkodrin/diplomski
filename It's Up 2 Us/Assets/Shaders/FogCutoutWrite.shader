Shader "Hidden/FogCutoutWrite"
{
    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent-10"
            "RenderType"="Transparent"
        }
        ZWrite Off
        ColorMask 0        // don't draw any color, only stencil

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace     // write 1 where the FOV mesh renders
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

            half4 frag (Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

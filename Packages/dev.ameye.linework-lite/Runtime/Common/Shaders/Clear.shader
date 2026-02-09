Shader "Hidden/Clear Stencil"
{
    Properties { }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        ColorMask 0
        
        Stencil
        {
            Ref 0
            Comp Always
            Pass Replace
            Fail Replace
            ZFail Replace
        }

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            #pragma vertex Vert
            #pragma fragment frag

            half4 frag(Varyings IN) : SV_TARGET
            {
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
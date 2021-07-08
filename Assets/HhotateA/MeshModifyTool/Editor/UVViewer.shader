Shader "HhotateA/UVViewer"
{
    Properties
    {
        _BaseTex ("_BaseTex",2D) = "black" {}
        _UVMap ("_UVMap",2D) = "black" {}
        _Color ("Color",color) = (1,1,1,1)
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"

            #pragma vertex InitCustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            sampler2D _BaseTex;
            float4 _BaseTex_ST;
            sampler2D _UVMap;
            float4 _UVMap_ST;
            float4 _Color;

            float4 frag(v2f_customrendertexture  IN) : COLOR
            {
                float4 col = tex2D(_BaseTex,TRANSFORM_TEX(IN.localTexcoord.xy,_BaseTex));
                float4 uvmap = tex2D(_UVMap,TRANSFORM_TEX(IN.localTexcoord.xy,_UVMap));
                return lerp(col,_Color,uvmap.a);
            }
            ENDCG
        }
    }
}
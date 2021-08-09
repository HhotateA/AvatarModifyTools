/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/TexturePreview"
{
    Properties
    {
        _MainTex ("_MainTex",2D) = "black" {}
        _Scale ("_Scale",vector) = (0,0,0,0)
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

            sampler2D _MainTex;
            float4 _Scale;

            float4 frag(v2f_customrendertexture  IN) : COLOR
            {
                uint2 check = IN.localTexcoord.xy*50.0;
                float4 base = check.x%2==check.y%2 ? float4(0.8,0.8,0.8,1.0) : float4(1,1,1,1);
                float2 uv = _Scale.xy;
                uv += (_Scale.zw - _Scale.xy) * IN.localTexcoord.xy;
                float4 col = tex2D(_MainTex, uv);
		        if(uv.x<0.0 || 1.0<uv.x || uv.y<0.0 || 1.0<uv.y ) col.a = 0.0;
                return lerp(base,col,col.a);
            }
            ENDCG
        }
    }
}
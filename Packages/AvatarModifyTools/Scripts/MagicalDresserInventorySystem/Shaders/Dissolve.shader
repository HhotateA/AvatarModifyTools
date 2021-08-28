/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
// made by ku(su)+
Shader "HhotateA/DimensionalStorage/Dissolve"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1.,1.,1.,1.)
		_ColorCull("ColorCull", Color) = (0.,0.,0.,1.)
        [HDR] _SingleColor("Single Color", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        
        _AnimationTime ("AnimationTime",Range(0,1))=0
        _DisolveTex ("Disolve", 2D) = "white" {}
        _SingleWidth ("SingleWidth",Range(0,1))=0.3
        _EmissionWidth ("EmissionWidth",Range(0,1))=0.4
        _ClipWidth ("ClipWidth",Range(0,1))=0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque"}
        LOD 100
        cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex,_DisolveTex;
            fixed4 _Color, _ColorCull;
            float4 _MainTex_ST,_DisolveTex_ST,_EmissionColor,_SingleColor;
            float _AnimationTime;
            float _SingleWidth,_EmissionWidth,_ClipWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i,fixed facing : VFACE) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float disolve = tex2D(_DisolveTex,i.uv).r;

                float th = 1.0-_AnimationTime;
                col = disolve+_SingleWidth >= th ? col : _SingleColor;
                col = disolve+_EmissionWidth >= th ? col : _EmissionColor;
                
                clip(disolve + _ClipWidth - th);

                return  facing > 0 ? col * _Color : col * _ColorCull;
            }
            ENDCG
        }
    }
}

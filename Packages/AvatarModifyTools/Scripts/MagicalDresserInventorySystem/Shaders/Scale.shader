/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/DimensionalStorage/Scale"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WeightPoint ("_WeightPoint",vector) = (0,0,0,0)
        _ScaleBounce ("_ScaleBounce",vector) = (0.6,1.2,0.8,0.9)
    	_AnimationTime ("_AnimationTime",range(0.,1.)) = 0.
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _WeightPoint;
            float4 _ScaleBounce;
            float _AnimationTime;

            float lerp2(float a,float b,float x)
            {
                return lerp(a,b,x);
            }
            float y (float x)
            {
                float v1 = lerp2(0.0,_ScaleBounce.y,x/_ScaleBounce.x);
                float v2 = lerp2(_ScaleBounce.y,_ScaleBounce.w,(x-_ScaleBounce.x)/(_ScaleBounce.z-_ScaleBounce.x));
                float v3 = lerp2(_ScaleBounce.w,1.0,(x-_ScaleBounce.z)/(1.0-_ScaleBounce.z));
                return x < _ScaleBounce.x ? v1 :
                       x < _ScaleBounce.z ? v2 : v3;
            }

            v2f vert (appdata v)
            {
                v2f o;
                float4 pos = v.vertex;
                pos -= _WeightPoint;
                pos *= y(_AnimationTime);
                pos += _WeightPoint;
                o.vertex = UnityObjectToClipPos(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}

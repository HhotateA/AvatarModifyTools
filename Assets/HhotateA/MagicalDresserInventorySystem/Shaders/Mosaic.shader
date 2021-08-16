/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/DimensionalStorage/Mosaic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1.,1.,1.,1.)
		_ColorCull("ColorCull", Color) = (0.2,0.2,0.2,1.)
    	_AnimationTime ("_AnimationTime",range(0.,1.)) = 0.
    	
		_ST("BaseST",vector) = (1.0,1.0,-0.5,-0.5)
		_Mosaic ("BaseMosaic",vector) = (5.0,5.0,0.25,0.0)
		_FPS("fps",float) = 10.0
		_Shift("Shift",float) = 1.0
		_MaxPhase("MaxPhase",float) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+50"}

		GrabPass{ "_BackgroundTexture"}
        Pass
        {
            CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            	float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            	float2 viewuv : TEXCOORD1;
            	float3 normal : NORMAL;
				float4 screenuv : TEXCOORD2;
            };

			#define stepscore 5.0
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color, _ColorCull;
            float _AnimationTime;
			uniform float4 _Mosaic;
			uniform float4 _ST;
			uniform float _FPS;
			uniform float _Shift;
			uniform float _MaxPhase;
			uniform sampler2D _BackgroundTexture;

            appdata vert (appdata v)
            {
                return v;
            }
            
			[maxvertexcount(3)]
			void geom(triangle appdata v[3], inout TriangleStream<v2f> tristream)
			{
                v2f o;
            	for(int i = 0; i < 3; i++)
            	{
	                o.vertex = UnityObjectToClipPos(v[i].vertex);
	                o.uv = TRANSFORM_TEX(v[i].uv, _MainTex);
					o.viewuv = calccamera(v[i].vertex) - calccamera(float4(0.0,0.0,0.0,1.0)) + float4(0.5,0.5,0.0,0.0);
					o.screenuv = ComputeGrabScreenPos(o.vertex);
            		o.normal = UnityObjectToWorldNormal(v[i].normal);
					tristream.Append(o);
            	}
				tristream.RestartStrip();
			}

            fixed4 frag (v2f i,fixed facing : VFACE) : SV_Target
            {
				float4 vec = mosaic(i.viewuv,_ST,_Mosaic.xy,_FPS,_AnimationTime*_MaxPhase);
				float4 col = tex2D(_MainTex,i.uv);
            	float4 loc = tex2Dproj(_BackgroundTexture,i.screenuv+vec*_Shift*_AnimationTime);
				float4 finalRGBA = lerp(
					lerp(vec,col,_AnimationTime),
					lerp(vec,loc,1.0-_AnimationTime),
					step(_AnimationTime-pow(vec.a,_Mosaic.z),_Mosaic.w));
                return  facing > 0 ? finalRGBA * _Color : finalRGBA * _ColorCull;
            }
            ENDCG
			CGINCLUDE
				#include "UnityCG.cginc"
				//base on "https://qiita.com/_nabe/items/c8ba019f26d644db34a8"
				float3 rgb2hsv(float3 c) {
					float4 k = float4( 0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0 );
					float e = 1.0e-10;
					float4 p = lerp( float4(c.bg, k.wz), float4(c.gb, k.xy), step(c.b, c.g) );
					float4 q = lerp( float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r) );
					float d = q.x - min(q.w, q.y);
					return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x );
				}
				float3 hsv2rgb(float3 c) {
					float4 k = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
					float3 p = abs( frac(c.xxx + k.xyz) * 6.0 - k.www );
					return c.z * lerp( k.xxx, saturate(p - k.xxx), c.y );
				}
				float3 stereocamerapos(){
				                float3 cameraPos = _WorldSpaceCameraPos;
				                #if defined(USING_STEREO_MATRICES)
				                // cameraPos = (unity_STereoWorldSpaceCameraPos[0] + unity_STereoWorldSpaceCameraPos[1]) * .5;
				                #endif
				                return cameraPos;
				}
				float4 calccamera(float4 input) {
					float4 vpos = mul(UNITY_MATRIX_MV,input);
					vpos.xyz -= _WorldSpaceCameraPos;
					vpos.xyz += stereocamerapos();
					return vpos;
				}
				float getGray(float4 c)
	            {
            		return (c.r+c.g+c.b)*c.a*0.3333333333;
	            }
				float random(float3 seed) {
				    return frac(sin(dot(seed+float3(500,500,500), float3(871.1, 510.92, 127.5))) * 7275.1)-1.0;
				}
				float4 noise(float3 uv) {
		            return float4(	frac(sin(dot(uv, float3(4028.01, 5103.91, 2145.23))) * 775.125),
									frac(sin(dot(uv, float3(1648.42, 7032.91, 151.23))) * 501.325),
									frac(sin(dot(uv, float3(2872.14, 2247.91, 356.23))) * 964.532),
									frac(sin(dot(uv, float3(1016.14, 4824.41, 12.23))) * 163.436));
		        }
				float3 randomvec(float3 seed) {
					return float3(	frac(sin(dot(seed, float3(428.01, 300.91, 2145.23))) * 7250.1)-0.5,
									frac(sin(dot(seed, float3(1648.42, 372.91, 1421.23))) * 8731.2)-0.5,
									frac(sin(dot(seed, float3(2872.14, 22.91, 35.23))) * 966.0)-0.5);
				}
				float4 mosaic(float2 uv,float4 st,float2 mos,float fps,float phase) {
					uv = uv*st.xy + st.zw;
					return noise(float3(floor(uv*mos*(1.0+phase))*mos,floor(_Time.y*fps)))/(1.0+phase);
				}
			ENDCG
        }
    }
}

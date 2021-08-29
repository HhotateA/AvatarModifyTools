/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/DimensionalStorage/pARTICLE"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1.,1.,1.,1.)
		_ColorCull("ColorCull", Color) = (0.2,0.2,0.2,1.)
    	_ParticleTex("ParticleTex", 2D) = "white" {}
    	_ParticleSize("_ParticleSize",float)=0.1
    	_AnimationTime ("_AnimationTime",range(0.,1.)) = 0.
    	_Distance ("_Distance",float) = 15
    	_Center ("_Center",vector) = (0.0,0.0,0.0,0.0)
    	_Extent ("_Center",vector) = (0.5,0.5,0.5,0.5)
    	_Factor ("_Factor",float) = 0.5
    	_ScaleNoise ("_ScaleNoise",float) = 1.0
    	_RotationNoise ("_RotationNoise",float) = 1.0
    	_MoveNoise ("_MoveNoise",float) = 1.0
    	_Scale ("_Scale",float) = 1.0
    	_Rotation ("_Rotation",float) = 1.0
    	_Move ("_Move",float) = 1.0
    	_Grabity ("_Grabity",vector) = (0.0,-1.0,0.0,0.0)
    	_AnimationVec ("_AnimationVec",vector) = (0.0,-1.0,0.0,0.0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+50"}
    	Cull Off
    	Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
            	float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 uv2 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

			#define stepscore 5.0
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color, _ColorCull;
            sampler2D _ParticleTex;
            float4 _ParticleTex_ST;
            float _ParticleSize;
            float _AnimationTime;
            float _Distance;
            float4 _Center,_Extent;
            float _Factor;
            float _ScaleNoise, _RotationNoise, _MoveNoise;
            float _Scale, _Rotation, _Move;
            float4 _Grabity;
            float4 _AnimationVec;
            
			static float2 particleOffset[4] = {
				float2(-1.0,-1.0),
				float2(-1.0, 1.0),
				float2( 1.0,-1.0),
				float2( 1.0, 1.0),
			};

            appdata vert (appdata v)
            {
                return v;
            }
            
			[maxvertexcount(4)]
			void geom(triangle appdata v[3], inout TriangleStream<v2f> tristream)
			{
                v2f o;
				float4 vec = (v[0].vertex+v[1].vertex+v[2].vertex)/3.0;
				float3 height = saturate(((vec - _Center) / _Extent) * 0.5 + 0.5);
            	float animationTime = saturate((1.0-_AnimationTime)*(1.0+_Factor) - dot( _AnimationVec.xyz, height.xyz) * _Factor - _Factor);
            	float3 noise = randomvec(vec);
            	float3 phase = float3(
            		(animationTime+animationTime*noise.x*_ScaleNoise)*_Scale,
            		(animationTime+animationTime*noise.y*_RotationNoise)*_Rotation,
            		(animationTime+animationTime*noise.z*_MoveNoise)*_Move
            		);
            	
	            float4 wwp = mul(UNITY_MATRIX_M,vec);
            	wwp.xyz = norqrot(float3(0,1,0),phase.x,wwp);
            	wwp.xyz += noise*phase.z + _Grabity.xyz*animationTime;
	            float4 vwp = mul(UNITY_MATRIX_V,wwp);
            	for(int i = 0; i < 3; i++)
            	{
            		v[i].vertex = mul(UNITY_MATRIX_M,v[i].vertex);
            		v[i].vertex.xyz = norqrot(float3(0,1,0),phase.x,v[i].vertex);
            		v[i].vertex = mul(UNITY_MATRIX_V,v[i].vertex);
            	}

            	for(int i = 0; i < 4; i++)
            	{
	                // o.vertex = UnityObjectToClipPos(v[i].vertex);
            		float4 sqr = float4(particleOffset[i] * _ParticleSize,0.,1.);
            		sqr.xyz = norqrot(float3(0,0,1),phase.y,sqr);
	                float4 ppos = mul(UNITY_MATRIX_P,vwp + float4(sqr.xyz,1.));
	                float4 opos = mul(UNITY_MATRIX_P,v[min(i,2)].vertex);
            		o.vertex = lerp(opos,ppos,saturate((phase.x-0.1)*2.));
	                o.uv = TRANSFORM_TEX(v[min(i,2)].uv, _MainTex);
            		o.uv2 = float3(particleOffset[i]*0.5+0.5,animationTime);
					tristream.Append(o);
            	}
				tristream.RestartStrip();
			}

            fixed4 frag (v2f i,fixed facing : VFACE) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 pcol = tex2D(_ParticleTex, i.uv2);
            	col = lerp(facing > 0 ? col * _Color : col * _ColorCull,col * pcol,i.uv2.z);
                return  col;
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
	                cameraPos = (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]) * .5;
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
				float4 qmat(float4 q1, float4 q2) {
							return float4(cross(q1.xyz, q2.xyz) + q2.w*q1.xyz + q1.w*q2.xyz, q1.w*q2.w - dot(q1.xyz, q2.xyz));
						}
				float4 qrot(float4 qwe, float4 pos) {
							return float4(qmat(qmat(qwe, pos), float4(-qwe.xyz, qwe.w)));
						}
				float3 norqrot(float3 bec, float rot, float4 pos) {
							float4 qwe = float4(normalize(bec)*sin(rot*UNITY_TWO_PI), cos(rot*UNITY_TWO_PI));
							return qrot(qwe, pos);
				}
				float random(float3 seed) {
				    return frac(sin(dot(seed+float3(500,500,500), float3(871.1, 510.92, 127.5))) * 7275.1)-1.0;
				}
				float3 randomvec(float3 seed) {
					return float3(	frac(sin(dot(seed, float3(428.01, 300.91, 2145.23))) * 7250.1)-0.5,
									frac(sin(dot(seed, float3(1648.42, 372.91, 1421.23))) * 8731.2)-0.5,
									frac(sin(dot(seed, float3(2872.14, 22.91, 35.23))) * 966.0)-0.5);
				}
				// taken from http://answers.unity.com/answers/641391/view.html
				float4x4 inverse(float4x4 input)
				{
					#define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
					float4x4 cofactors = float4x4(
						minor(_22_23_24, _32_33_34, _42_43_44), 
						-minor(_21_23_24, _31_33_34, _41_43_44),
						minor(_21_22_24, _31_32_34, _41_42_44),
						-minor(_21_22_23, _31_32_33, _41_42_43),

						-minor(_12_13_14, _32_33_34, _42_43_44),
						minor(_11_13_14, _31_33_34, _41_43_44),
						-minor(_11_12_14, _31_32_34, _41_42_44),
						minor(_11_12_13, _31_32_33, _41_42_43),

						minor(_12_13_14, _22_23_24, _42_43_44),
						-minor(_11_13_14, _21_23_24, _41_43_44),
						minor(_11_12_14, _21_22_24, _41_42_44),
						-minor(_11_12_13, _21_22_23, _41_42_43),

						-minor(_12_13_14, _22_23_24, _32_33_34),
						minor(_11_13_14, _21_23_24, _31_33_34),
						-minor(_11_12_14, _21_22_24, _31_32_34),
						minor(_11_12_13, _21_22_23, _31_32_33)
					);
					#undef minor
					return transpose(cofactors) / determinant(input);
				}
			ENDCG
        }
    }
}

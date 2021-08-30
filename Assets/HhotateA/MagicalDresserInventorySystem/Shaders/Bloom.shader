/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/DimensionalStorage/Bloom"
{
	Properties{
		_MainTex ("MainTexture", 2D) = "white" {}
		_col ("Color",color) = (1.0,1.0,1.0,1.0)
		[Space(100)]
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", int) = 2
		[Enum(ScaleMode,0,NormalMode,1)] _Base("BaseMode", int) = 0
		_BloomTex ("BloomTexture", 2D) = "white" {}
		[HDR]_bloomcol ("BloomColor",color) = (1.0,1.0,1.0,1.0)
		_range ("Range",range(0.0,5.0)) = 1.0
		_brightness ("Brightness",range(0.0,10.0)) = 0.2
		_knee ("Knee",range(0.0,10.0)) = 2.0
		_softknee ("SoftKnee",range(0.0,3.0)) = 0.6
		_threshold ("Threshold",range(0.0,2.0)) = 1.2
    	_AnimationTime ("_AnimationTime",range(0.,1.)) = 0.
	}
	SubShader{
		Tags { "RenderType"="Transparent" "Queue"="Transparent+1500"}
		LOD 100

		Pass{ //元オブジェクトの描画
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			Cull Back
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			float _AnimationTime;

			struct appdata{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex; float4 _MainTex_ST;
			float4 _col;
			
			v2f vert (appdata v){
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target{
				fixed4 col = tex2D(_MainTex, i.uv);
				clip(_AnimationTime-0.5);
				return col*_col;
			}
			ENDCG
		}

		Pass{ //Bloom部分の描画
			Blend SrcAlpha One
			ZWrite off
			Cull [_CullMode]
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			float _AnimationTime;

			#define roop 48

			struct appdata{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct g2f{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float alpha : TEXCOORD1;
			};

			sampler2D _BloomTex; float4 _BloomTex_ST;
			float _Base;
			float4 _bloomcol;
			float _range;
			float _knee;
			float _softknee;
			float _brightness;
			float _threshold;
			
           	float gammacorrect(float gamma, float input) {
				float output = saturate(pow( input, 1.0/gamma));
				output = lerp(output,1.0,step(9.9,gamma));
           		return output;
            }
			
			appdata vert (appdata v){
				appdata o;
				o.vertex = v.vertex;
				o.normal = normalize(v.normal);
				o.uv = TRANSFORM_TEX(v.uv, _BloomTex);
				return o;
			}

			[maxvertexcount(roop*3)]
			void geom(triangle appdata input[3], inout TriangleStream<g2f> outStream){
				g2f output;
				for(int scale=1;scale<roop+1;scale++){
					for(int index=0;index<3;index++){
						if(_Base==0){
							output.vertex = UnityObjectToClipPos(input[index].vertex+input[index].vertex*scale*_range/float(roop)); //ループごとにスケールを大きくしていく(ノーマル方向に広げるとポリゴンがうくョ)
						}else{
							output.vertex = UnityObjectToClipPos(input[index].vertex+input[index].normal*scale*_range/float(roop)); //ループごとにスケールを大きくしていく(ノーマル方向に広げるとポリゴンがうくョ)
						}
						
						output.uv = input[index].uv;
						output.alpha = float(roop-scale)/float(roop);
						outStream.Append(output);
					}
					outStream.RestartStrip();
				}
			}
			
			fixed4 frag (g2f i) : SV_Target{
				// sample the texture
				fixed4 col = tex2D(_BloomTex, i.uv);
				col *= lerp(0.0,_brightness,saturate((length(col.rgb)-_threshold)/_softknee)); //しきい値以下の色なら光らせない(干渉区間あり)
				col *= gammacorrect( _knee, i.alpha); //なんかいい感じに(悲しいことに_kee=0はバグる)
				col *= _bloomcol;
				clip(_AnimationTime-i.alpha*0.5);
				clip(i.alpha*0.5-_AnimationTime+0.5);
				return col;
			}
			ENDCG
		}
	}
}

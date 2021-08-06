Shader "HhotateA/MagicDresser/Draw"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1.,1.,1.,1.)
    	_AnimationTex ("_AnimationTex", 2D) = "white" {}
    	_AnimationTime ("_AnimationTime",range(0.,1.)) = 0.
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            	float2 viewuv : TEXCOORD1;
            };

			#define stepscore 5.0
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _AnimationTex;
            float4 _AnimationTex_ST;
            float _AnimationTime;

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
					o.viewuv = TRANSFORM_TEX(
						calccamera(v[i].vertex) - calccamera(float4(0.0,0.0,0.0,1.0)) + float4(0.5,0.5,0.0,0.0) ,
						_AnimationTex);
					tristream.Append(o);
            	}
				tristream.RestartStrip();
			}

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

            	float alpha = getGray(tex2D(_AnimationTex, i.viewuv));

				float4 whitecol = float4(1.0,1.0,1.0,col.a);
				float4 linecol = float4(pow(saturate(1.0-length(fwidth(col).rgb)),16.0).xxx,1.0);
				float4 drawcol = col;
					drawcol.xyz = rgb2hsv(col.rgb);
					drawcol.z = 1.0;
					drawcol.rgb = hsv2rgb(drawcol.xyz);
					drawcol *= linecol;
				float4 tooncol = float4(ceil(col.r*stepscore)/stepscore,
										ceil(col.g*stepscore)/stepscore,
										ceil(col.b*stepscore)/stepscore,
										col.a);
				tooncol *= linecol;

            	if(_AnimationTime<0.2)
            	{
            		float phase = (_AnimationTime*5.) - 0.;
            		phase = step(alpha,phase);
					clip(phase-0.5);
            		col = whitecol;
            	}
            	else if(_AnimationTime<0.4)
            	{
            		float phase = (_AnimationTime*5.) - 1.;
            		phase = step(alpha,phase);
            		col = lerp(whitecol,linecol,phase);            		
            	}
            	else if(_AnimationTime<0.6)
            	{
            		float phase = (_AnimationTime*5.) - 2.;
            		phase = step(alpha,phase);
            		col = lerp(linecol,drawcol,phase);            		
            	}
            	else if(_AnimationTime<0.8)
            	{
            		float phase = (_AnimationTime*5.) - 3.;
            		phase = step(alpha,phase);
            		col = lerp(drawcol,tooncol,phase);            		
            	}
            	else
            	{
            		float phase = (_AnimationTime*5.) - 4.;
            		phase = step(alpha,phase);
            		col = lerp(tooncol,col,phase);            		
            	}
                return col;
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
			ENDCG
        }
    }
}

/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
// made by ku(su)+
Shader "HhotateA/DimensionalStorage/Crystallize"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1.,1.,1.,1.)
		_ColorCull("ColorCull", Color) = (0.2,0.2,0.2,1.)
        [HDR] _SingleColor("Single Color", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _DisolveTex ("Disolve", 2D) = "white" {}
        _AnimationTime ("AnimationTime",Range(0,1))=0

        [HideInInspector] _EmissionWidth ("EmissionWidth",Range(0,0.1))=0.5
        [HideInInspector] _normalDist("NormalDist",Range(0,0.1))=1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Cull off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint id : SV_VertexID;
				float4 normal :NORMAL;
			};

			struct v2g
			{
				float2 uv : TEXCOORD0;
				float4 vertex :POSITION;
				uint id : TEXCOORD1;
				float4 normal :TEXCOORD2;
			};

			struct g2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

            float random(float2 p)
			{
			    return frac(sin(dot(p,fixed2(12.9898,78.233)))*43758.5453);
			}
			float3 random3(float2 p)
			{
			    return float3(random(p*2),random(p*4),random(p*6));
			}

            sampler2D _MainTex,_DisolveTex;
            float4 _MainTex_ST,_DisolveTex_ST,_EmissionColor,_SingleColor;
            float _AnimationTime,_EmissionWidth,_normalDist;
            fixed4 _Color, _ColorCull;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                o.id = v.id;
                return o;
            }
            
            [maxvertexcount(16)]
			void geom (triangle  v2g input[3],
				inout TriangleStream< g2f > SpriteStream)
			{
				g2f o;
                float3 center = (input[0].vertex+input[1].vertex+input[2].vertex)/3;
                
				for(uint i=0;i<3;i++)
				{   
                    float3 yup = (random3(input[0].uv)*float3(2,1,2)-float3(1,0,1))*float3(0.5,1,0.5);
                    float r = random(input[i].uv)*0.5+0.5;
                    float3 up = (1.-_AnimationTime)* ( yup*r*5*_AnimationTime + yup)*_normalDist ;
                    float3 vert = (input[i].vertex-center)*_AnimationTime*float3(1,2.-_AnimationTime,1)+center+up;
                    
					o.vertex = UnityObjectToClipPos(vert + up);
                    o.uv     = input[i].uv;
					SpriteStream.Append(o);	
				}
				SpriteStream.RestartStrip();
            }

            fixed4 frag (g2f i,fixed facing : VFACE) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float disolve = tex2D(_DisolveTex,i.uv).r;

                float th =((1.-_AnimationTime)*1.2-0.1);
                col=col*saturate(_AnimationTime*2-1) + _SingleColor * saturate((1.-_AnimationTime)*4);

                th=saturate(1.-_AnimationTime*2)*1.2-0.1;
                col.a*=(disolve>=th);

                float d= abs(disolve-th)+(disolve>=th)<_EmissionWidth;
                col = col*(1-d*0.9) + _EmissionColor*d;

                return  facing > 0 ? col * _Color : col * _ColorCull;
            }
            ENDCG
        }
    }
}

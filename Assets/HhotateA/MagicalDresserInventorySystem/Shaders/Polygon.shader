/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
// made by ku(su)+
Shader "HhotateA/DimensionalStorage/Polygon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1.,1.,1.,1.)
		_ColorCull("ColorCull", Color) = (0.2,0.2,0.2,1.)
        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _AnimationTime ("AnimationTime",Range(0,1))=0

        [HideInInspector] _EmissionWidth ("EmissionWidth",Range(0,0.1))=0.5
        [HideInInspector] _BlockNum("BlockNum",Range(0,50))=10
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+50" }
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
                float4 color : COLOR;
			};

            float random(float2 p)
			{
			    return frac(sin(dot(p,fixed2(12.9898,78.233)))*43758.5453);
			}
			float3 random3(float2 p)
			{
			    return float3(random(p*2),random(p*4),random(p*6));
			}

            sampler2D _MainTex;
            float4 _MainTex_ST,_EmissionColor;
            fixed4 _Color, _ColorCull;
            float _AnimationTime,_EmissionWidth,_BlockNum;

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
                float3 block = float3(floor(center.x*_BlockNum),floor(center.y*_BlockNum),floor(center.z*_BlockNum));

                
				for(uint i=0;i<3;i++)
				{   
                    float4 vert = (abs(block.y) < _AnimationTime*0.5* (_BlockNum+1) ) * input[i].vertex;
                    o.color = _EmissionColor* (abs(block.y) > _AnimationTime*0.5* (_BlockNum) ) ;
					o.vertex = UnityObjectToClipPos(vert);
                    o.uv     = input[i].uv;
					SpriteStream.Append(o);	
				}
				SpriteStream.RestartStrip();
            }

            fixed4 frag (g2f i,fixed facing : VFACE) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) + i.color;

                return  facing > 0 ? col * _Color : col * _ColorCull;
            }
            ENDCG
        }
    }
}

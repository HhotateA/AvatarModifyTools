// CC0
// 2021/03/19 by @HhotateA_xR
Shader "Fee/AvatarPenTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", color) = (1.,1.,1.,1.)
        _ClipRange("ClipRange", float) = 1.
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _ClipRange;

            appdata vert (appdata v)
            {
                return v;
            }
            
            [maxvertexcount(3)]
            void geom(triangle appdata IN[3], inout TriangleStream<v2f> triStream)
            {
                float4 wpos0 = mul(unity_ObjectToWorld,IN[0].vertex);
                float4 wpos1 = mul(unity_ObjectToWorld,IN[1].vertex);
                float4 wpos2 = mul(unity_ObjectToWorld,IN[2].vertex);

                float range = max(distance(wpos0,wpos1)
                             ,max(distance(wpos1,wpos2)
                                 ,distance(wpos2,wpos0)));
                if(range<_ClipRange)
                {
                    v2f o;
                    
                    for(int i=0; i<3; i++)
                    {
                        o.vertex = UnityObjectToClipPos(IN[i].vertex);
                        o.uv = IN[i].uv;
                        o.color = IN[i].color;
                        UNITY_TRANSFER_FOG(o, o.vertex);
                        triStream.Append(o);
                    }
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv)*_Color*i.color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

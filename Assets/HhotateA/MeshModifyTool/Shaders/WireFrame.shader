/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/MeshModifyTool/OverlayWireFrame"
{
    Properties
    {
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest",float) = 4
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        //Offset 0, -1
        Tags {"Queue"="Overlay"}
        ZTest [_ZTest]

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            appdata vert (appdata v)
            {
                return v;
            }

            fixed4 _Color;
            float _NormalAlpha;

            [maxvertexcount(6)]
            void geom(triangle appdata input[3], inout LineStream<v2f> outStream)
            {
                v2f output[3];
                for(int i=0;i<3;i++)
                {
                    output[i].vertex = UnityObjectToClipPos(input[i].vertex);
                    output[i].color = input[i].color;
                }
                outStream.Append(output[0]);
                outStream.Append(output[1]);
                outStream.RestartStrip();
                
                outStream.Append(output[1]);
                outStream.Append(output[2]);
                outStream.RestartStrip();
                
                outStream.Append(output[2]);
                outStream.Append(output[0]);
                outStream.RestartStrip();
            }

            fixed4 frag (v2f i) : SV_Target
            {
                clip(0.99-_NormalAlpha);
                return _Color * i.color;
            }
            ENDCG
        }
    }
}

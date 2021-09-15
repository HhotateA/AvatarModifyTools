/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
Shader "HhotateA/TexturePainter"
{
    Properties
    {
        _Overlay ("_Overlay",2D) = "black" {}
        _Color0 ("Color", Color) = (1,1,1,1)
        _Color1 ("Color", Color) = (1,1,1,1)
        _Color2 ("Color", Color) = (1,1,1,1)
        _Color3 ("Color", Color) = (1,1,1,1)
        _Color4 ("Color", Color) = (1,1,1,1)
        _Color5 ("Color", Color) = (1,1,1,1)
        _Color6 ("Color", Color) = (1,1,1,1)
        _Color7 ("Color", Color) = (1,1,1,1)
        _Color8 ("Color", Color) = (1,1,1,1)
        _Color9 ("Color", Color) = (1,1,1,1)
        _Color10 ("Color", Color) = (1,1,1,1)
        _Color11 ("Color", Color) = (1,1,1,1)
        _Color12 ("Color", Color) = (1,1,1,1)
        _Color13 ("Color", Color) = (1,1,1,1)
        _Color14 ("Color", Color) = (1,1,1,1)
        //_Color15 ("Color", Color) = (1,1,1,1)
        _Comparison0 ("Color", Color) = (1,1,1,1)
        _Comparison1 ("Color", Color) = (1,1,1,1)
        _Comparison2 ("Color", Color) = (1,1,1,1)
        _Comparison3 ("Color", Color) = (1,1,1,1)
        _Comparison4 ("Color", Color) = (1,1,1,1)
        _Comparison5 ("Color", Color) = (1,1,1,1)
        _Comparison6 ("Color", Color) = (1,1,1,1)
        _Comparison7 ("Color", Color) = (1,1,1,1)
        _Comparison8 ("Color", Color) = (1,1,1,1)
        _Comparison9 ("Color", Color) = (1,1,1,1)
        _Comparison10 ("Color", Color) = (1,1,1,1)
        _Comparison11 ("Color", Color) = (1,1,1,1)
        _Comparison12 ("Color", Color) = (1,1,1,1)
        _Comparison13 ("Color", Color) = (1,1,1,1)
        _Comparison14 ("Color", Color) = (1,1,1,1)
        //_Comparison15 ("Color", Color) = (1,1,1,1)
        _Layer0 ("",2D) = "black" {}
        _Layer1 ("",2D) = "black" {}
        _Layer2 ("",2D) = "black" {}
        _Layer3 ("",2D) = "black" {}
        _Layer4 ("",2D) = "black" {}
        _Layer5 ("",2D) = "black" {}
        _Layer6 ("",2D) = "black" {}
        _Layer7 ("",2D) = "black" {}
        _Layer8 ("",2D) = "black" {}
        _Layer9 ("",2D) = "black" {}
        _Layer10 ("",2D) = "black" {}
        _Layer11 ("",2D) = "black" {}
        _Layer12 ("",2D) = "black" {}
        _Layer13 ("",2D) = "black" {}
        _Layer14 ("",2D) = "black" {}
        //_Layer15 ("",2D) = "black" {}
        _Mode0 ("",int) = 1
        _Mode1 ("",int) = 1
        _Mode2 ("",int) = 1
        _Mode3 ("",int) = 1
        _Mode4 ("",int) = 1
        _Mode5 ("",int) = 1
        _Mode6 ("",int) = 1
        _Mode7 ("",int) = 1
        _Mode8 ("",int) = 1
        _Mode9 ("",int) = 1
        _Mode10 ("",int) = 1
        _Mode11 ("",int) = 1
        _Mode12 ("",int) = 1
        _Mode13 ("",int) = 1
        _Mode14 ("",int) = 1
        _Mode15 ("",int) = 1
        _Alpha0 ("",int) = 1
        _Alpha1 ("",int) = 1
        _Alpha2 ("",int) = 1
        _Alpha3 ("",int) = 1
        _Alpha4 ("",int) = 1
        _Alpha5 ("",int) = 1
        _Alpha6 ("",int) = 1
        _Alpha7 ("",int) = 1
        _Alpha8 ("",int) = 1
        _Alpha9 ("",int) = 1
        _Alpha10 ("",int) = 1
        _Alpha11 ("",int) = 1
        _Alpha12 ("",int) = 1
        _Alpha13 ("",int) = 1
        _Alpha14 ("",int) = 1
        //_Alpha15 ("",int) = 1
        _Settings0 ("Settings", vector) = (0,0,0,0)
        _Settings1 ("Settings", vector) = (0,0,0,0)
        _Settings2 ("Settings", vector) = (0,0,0,0)
        _Settings3 ("Settings", vector) = (0,0,0,0)
        _Settings4 ("Settings", vector) = (0,0,0,0)
        _Settings5 ("Settings", vector) = (0,0,0,0)
        _Settings6 ("Settings", vector) = (0,0,0,0)
        _Settings7 ("Settings", vector) = (0,0,0,0)
        _Settings8 ("Settings", vector) = (0,0,0,0)
        _Settings9 ("Settings", vector) = (0,0,0,0)
        _Settings10 ("Settings", vector) = (0,0,0,0)
        _Settings11 ("Settings", vector) = (0,0,0,0)
        _Settings12 ("Settings", vector) = (0,0,0,0)
        _Settings13 ("Settings", vector) = (0,0,0,0)
        _Settings14 ("Settings", vector) = (0,0,0,0)
        //_Settings15 ("Settings", vector) = (0,0,0,0)
        _Mask0 ("",int) = -1
        _Mask1 ("",int) = -1
        _Mask2 ("",int) = -1
        _Mask3 ("",int) = -1
        _Mask4 ("",int) = -1
        _Mask5 ("",int) = -1
        _Mask6 ("",int) = -1
        _Mask7 ("",int) = -1
        _Mask8 ("",int) = -1
        _Mask9 ("",int) = -1
        _Mask10 ("",int) = -1
        _Mask11 ("",int) = -1
        _Mask12 ("",int) = -1
        _Mask13 ("",int) = -1
        _Mask14 ("",int) = -1
        //_Mask15 ("",int) = -1
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            Name "Update"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"

            #pragma vertex InitCustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            /*enum BlendMode
            {
                disable,
                normal,
                additive,
                multiply,
                subtraction,
                division,
                bloom,
                hsv,
                color,
                alphaMask
            };*/

            #define Layer(num) \
                uniform float4 _Color##num;\
                uniform float4 _Comparison##num;\
                uniform sampler2D _Layer##num;\
                uniform float4 _Layer##num##_ST;\
                uniform int _Mode##num;\
                uniform int _Alpha##num;\
                uniform float4 _Settings##num;\
                uniform int _Mask##num;\

            #define LayerCol(origin,num,mask) \
                if(_Mask##num == -1)\
                {\
                    ##origin = lerp(\
                                ##origin,\
                                OverrideColor(##origin,_Layer##num,TRANSFORM_TEX(IN.localTexcoord.xy,_Layer##num),_Color##num,_Comparison##num,_Settings##num,_Mode##num,_Alpha##num),\
                                float4(mask.rgb*mask.a,mask.a));\
                }\
                else\
                {\
                    ##mask = OverrideColor(##mask,_Layer##num,TRANSFORM_TEX(IN.localTexcoord.xy,_Layer##num),_Color##num,_Comparison##num,_Settings##num,_Mode##num,_Alpha##num);\
                }\

            /*float4 _Color0,_Color1,_Color2,_Color3,_Color4,_Color5,_Color6,_Color7,_Color8,_Color9,_Color10,_Color11,_Color12,_Color13,_Color14,_Color15;
            sampler2D _Layer0,_Layer1,_Layer2,_Layer3,_Layer4,_Layer5,_Layer6,_Layer7,_Layer8,_Layer9,_Layer10,_Layer11,_Layer12,_Layer13,_Layer14,_Layer15;
            float4 _Layer0_ST,_Layer1_ST,_Layer2_ST,_Layer3_ST,_Layer4_ST,_Layer5_ST,_Layer6_ST,_Layer7_ST,_Layer8_ST,_Layer9_ST,_Layer10_ST,_Layer11_ST,_Layer12_ST,_Layer13_ST,_Layer14_ST,_Layer15_ST;
            int _Mode0,_Mode1,_Mode2,_Mode3,_Mode4,_Mode5,_Mode6,_Mode7,_Mode8,_Mode9,_Mode10,_Mode11,_Mode12,_Mode13,_Mode14,_Mode15;*/

            sampler2D _Overlay;
            float4 _Overlay_ST;
            Layer(0)
            Layer(1)
            Layer(2)
            Layer(3)
            Layer(4)
            Layer(5)
            Layer(6)
            Layer(7)
            Layer(8)
            Layer(9)
            Layer(10)
            Layer(11)
            Layer(12)
            Layer(13)
            Layer(14)
            //Layer(15)

            static float3 linecolumn[41]={
                                                                                                                float3(-4.0, 0.0, 1.0),
                                                                                        float3(-3.0,-1.0, 4.0), float3(-3.0, 0.0, 1.0), float3(-3.0, 1.0, 4.0),
                                                                float3(-2.0,-2.0, 6.0), float3(-2.0,-1.0, 3.0), float3(-2.0, 0.0,17.0), float3(-2.0, 1.0, 3.0), float3(-2.0, 2.0, 6.0),
                                        float3(-1.0,-3.0, 4.0), float3(-1.0,-2.0, 3.0), float3(-1.0,-1.0,26.0), float3(-1.0, 0.0,10.0), float3(-1.0, 1.0,26.0), float3(-1.0, 2.0, 3.0), float3(-1.0, 3.0, 4.0),
                float3( 0.0,-4.0, 1.0), float3( 0.0,-3.0, 1.0), float3( 0.0,-2.0,17.0), float3( 0.0,-1.0,10.0), float3( 0.0, 0.0,31.0), float3( 0.0, 1.0,10.0), float3( 0.0, 2.0,17.0), float3( 0.0, 3.0, 1.0), float3( 0.0, 4.0, 1.0),
                                        float3( 1.0,-3.0, 4.0), float3( 1.0,-2.0, 3.0), float3( 1.0,-1.0,26.0), float3( 1.0, 0.0,10.0), float3( 1.0, 1.0,26.0), float3( 1.0, 2.0, 3.0), float3( 1.0, 3.0, 4.0),
                                                                float3( 2.0,-2.0, 6.0), float3( 2.0,-1.0, 3.0), float3( 2.0, 0.0,17.0), float3( 2.0, 1.0, 3.0), float3( 2.0, 2.0, 6.0),
                                                                                        float3( 3.0,-1.0, 4.0), float3( 3.0, 0.0, 1.0), float3( 3.0, 1.0, 4.0),
                                                                                                                float3( 4.0, 0.0, 1.0),

            };

		    inline float3 rgb2hsv(float3 c) {
			    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
			    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

			    float d = q.x - min(q.w, q.y);
			    float e = 1.0e-10;
			    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		    }
		    
		    inline fixed3 hsv2rgb(float3 hsv){
			    fixed4 t = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			    fixed3 p = abs(frac(hsv.xxx + t.xyz) * 6.0 - t.www);
			    return hsv.z * lerp(t.xxx, clamp(p - t.xxx, 0.0, 1.0), hsv.y);
		    }

            float getGray(float4 rgb)
            {
                return (rgb.x+rgb.y+rgb.z)*rgb.w*0.3333333333333333;
            }
            
            float getGray(float3 rgb)
            {
                return (rgb.x+rgb.y+rgb.z)*0.3333333333333333;
            }
            
            float4 OverrideColor(float4 col,sampler2D tex,float2 uv,float4 color,float4 comparison,float4 settings,int mode,int alpha)
            {
		        if(uv.x<0.0 || 1.0<uv.x || uv.y<0.0 || 1.0<uv.y ) return col;
                float4 l = tex2D(tex,uv);
                float4 lc = l*color;
                if(mode == 1) // normal
                {
                    col.rgb = lerp(
                        lerp(col.rgb,lc.rgb,lc.a),
                        lc.rgb,
                        lc.a);
                    col.a = saturate(col.a+(1.0-col.a)*lc.a);
                }
                else if (mode == 2) // additive
                {
                    col.rgb = lerp(col.rgb,col.rgb+lc.rgb,lc.a);
                }
                else if (mode == 3) // multiply
                {
                    col.rgb = lerp(col.rgb,col.rgb*lc.rgb,lc.a);
                }
                else if (mode == 4) // subtraction
                {
                    col.rgb = lerp(col.rgb,col.rgb-lc.rgb,lc.a);
                }
                else if (mode == 5) //division
                {
                    if(l.a>0.0)
                    {
                        col.rgb = lerp(col.rgb,saturate(col.rgb/max(lc.rgb,0.001)),lc.a);
                    }
                }
                else if(mode == 6) // bloom
                {
                    float4 sumcolor = (float4)0.0;
#ifdef RichBloom
                    [unroll] for(int index=0;index<41;index++) {
                        float2 offsetUV = uv + float2(settings.x*linecolumn[index].x,settings.y*linecolumn[index].y) * settings.z;
                        float4 sc = tex2D(tex,offsetUV)*color;
                        float sv = pow(saturate(linecolumn[index].z/31.0) , 1.0-saturate(getGray(sc)/settings.w));
                        sumcolor += sc * sv;
                    }
                    sumcolor = sumcolor*31.0/331.0;
#else
                    sumcolor += tex2D(tex,uv+float2(-settings.x,-settings.y) * settings.z)*color;
                    sumcolor += tex2D(tex,uv+float2(-settings.x, settings.y) * settings.z)*color;
                    sumcolor += tex2D(tex,uv+float2( settings.x,-settings.y) * settings.z)*color;
                    sumcolor += tex2D(tex,uv+float2( settings.x, settings.y) * settings.z)*color;
                    sumcolor *= 0.25;
#endif
                    col.rgb = lerp(
                        lerp(col.rgb,sumcolor.rgb,sumcolor.a),
                        sumcolor.rgb,
                        lc.a);
                }
                else if(mode == 7) // HSV
                {
                    float3 hsv = rgb2hsv(col.rgb);
                    hsv = float3(
                        settings.x <= 0.0 ? hsv.x + settings.x : settings.x,
                        settings.y <= 0.0 ? lerp(0.0,hsv.y,settings.y+1.0) : lerp(hsv.y,1.0,settings.y),
                        settings.z <= 0.0 ? lerp(0.0,hsv.z,settings.z+1.0) : lerp(hsv.z,1.0,settings.z));
                    float3 rgb = hsv2rgb(float3(hsv.x,hsv.y,hsv.z));
                    col.rgb = lerp(col.rgb,rgb,getGray(lc));
                }
                else if(mode == 8) // color
                {
                    float a = 1.0/((2.0-settings.x)*settings.w);
                    float d = -a*distance(col,comparison) + a * settings.x*settings.w;
                    float4 c = lerp(
                        0,
                        color - lerp(comparison,col,settings.z),
                        pow(saturate(d),1.0-settings.y)
                        );
                    col += c * getGray(l);
                }
                else if(mode == 9) // Alpha Mask
                {
                    col.a = lerp(col.a,getGray(lc.rgb),lc.a);
                }
		        else if(mode == 10) // override
		        {
		            col = l;		            
		        }
                
                return col; 
            }

            float4 frag(v2f_customrendertexture  IN) : COLOR
            {
                float4 col = float4(0,0,0,0);
                float4 mask = float4(1,1,1,1);
                /*col = OverrideColor(col,_Layer0,TRANSFORM_TEX(IN.localTexcoord.xy,_Layer0),_Color0,_Settings0,_Mode0);
                col = tex2D(_Layer0,IN.localTexcoord.xy);*/
                //col = tex2D(_Layer0,IN.localTexcoord.xy);
                LayerCol(col,0,mask);
                LayerCol(col,1,mask);
                LayerCol(col,2,mask);
                LayerCol(col,3,mask);
                LayerCol(col,4,mask);
                LayerCol(col,5,mask);
                LayerCol(col,6,mask);
                LayerCol(col,7,mask);
                LayerCol(col,8,mask);
                LayerCol(col,9,mask);
                LayerCol(col,10,mask);
                LayerCol(col,11,mask);
                LayerCol(col,12,mask);
                LayerCol(col,13,mask);
                LayerCol(col,14,mask);
                //LayerCol(col,15,mask);
                float4 overlay = tex2D(_Overlay, TRANSFORM_TEX(IN.localTexcoord.xy,_Overlay));
                //return overlay;
                return lerp(col,overlay,overlay.a);
            }
            ENDCG
        }
    }
}
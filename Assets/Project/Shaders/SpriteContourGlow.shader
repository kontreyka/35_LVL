Shader "Sprites/SoftContourGlow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1,0.86,0.48,0.62)
        _GlowIntensity ("Glow Intensity", Range(0,2)) = 0.6
        _GlowWidth ("Glow Width", Range(1,32)) = 14
        _LuminanceThreshold ("Luminance Edge Threshold", Range(0,1)) = 0.16
        _AlphaThreshold ("Alpha Threshold", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowWidth;
            float _LuminanceThreshold;
            float _AlphaThreshold;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float Luminance(fixed3 color)
            {
                return dot(color, fixed3(0.2126, 0.7152, 0.0722));
            }

            void AccumulateEdge(
                float2 uv,
                float2 offset,
                float centerAlpha,
                float centerLuminance,
                inout float alphaEdge,
                inout float luminanceEdge
            )
            {
                fixed4 sampleColor = tex2D(_MainTex, uv + offset);
                alphaEdge = max(alphaEdge, abs(sampleColor.a - centerAlpha));
                luminanceEdge = max(luminanceEdge, abs(Luminance(sampleColor.rgb) - centerLuminance));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 center = tex2D(_MainTex, i.uv);
                float centerLuminance = Luminance(center.rgb);
                float2 texel = _MainTex_TexelSize.xy * _GlowWidth;
                float2 diagonal = texel * 0.7071;
                float alphaEdge = 0;
                float luminanceEdge = 0;

                AccumulateEdge(i.uv, float2(texel.x, 0), center.a, centerLuminance, alphaEdge, luminanceEdge);
                AccumulateEdge(i.uv, float2(-texel.x, 0), center.a, centerLuminance, alphaEdge, luminanceEdge);
                AccumulateEdge(i.uv, float2(0, texel.y), center.a, centerLuminance, alphaEdge, luminanceEdge);
                AccumulateEdge(i.uv, float2(0, -texel.y), center.a, centerLuminance, alphaEdge, luminanceEdge);
                AccumulateEdge(i.uv, float2(diagonal.x, diagonal.y), center.a, centerLuminance, alphaEdge, luminanceEdge);
                AccumulateEdge(i.uv, float2(-diagonal.x, diagonal.y), center.a, centerLuminance, alphaEdge, luminanceEdge);
                AccumulateEdge(i.uv, float2(diagonal.x, -diagonal.y), center.a, centerLuminance, alphaEdge, luminanceEdge);
                AccumulateEdge(i.uv, float2(-diagonal.x, -diagonal.y), center.a, centerLuminance, alphaEdge, luminanceEdge);

                float alphaMask = smoothstep(_AlphaThreshold * 0.35, _AlphaThreshold, alphaEdge);
                float luminanceMask = smoothstep(_LuminanceThreshold, _LuminanceThreshold + 0.2, luminanceEdge);
                float edgeMask = max(alphaMask, luminanceMask) * _GlowIntensity;

                fixed4 color = _GlowColor * i.color;
                color.a *= saturate(edgeMask);
                return color;
            }

            ENDCG
        }
    }
}

Shader "UI/AdvancedSoftness"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // Advanced softness properties
        _EdgeSoftness ("Edge Softness (L,B,R,T)", Vector) = (0,0,0,0)
        _UseAdvancedSoftness ("Use Advanced Softness", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_CLIP_ROUNDED_RECT

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 objectPosition : TEXCOORD1;
                float4 clipRect : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _ClipSoftness;

            // Advanced softness uniforms
            float4 _EdgeSoftness;
            float _UseAdvancedSoftness;

            v2f vert (appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Store object space position for clipping
                OUT.objectPosition = v.vertex.xy;

                // Transform to clip space
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                OUT.clipRect = _ClipRect;

                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #if UNITY_UI_CLIP_RECT
                    float2 position = IN.objectPosition;

                    // Always apply Unity's standard hard clipping first
                    float hardClipAlpha = UnityGet2DClipping(position, _ClipRect);

                    // Check if advanced softness is enabled and has non-zero values (convert vector comparison to scalar)
                    bool useAdvancedSoftness = _UseAdvancedSoftness > 0.5 && dot(_EdgeSoftness, _EdgeSoftness) > 0.0;

                    if (useAdvancedSoftness)
                    {
                        // Advanced per-edge softness
                        float softnessAlpha = 1.0;
                        float4 softness = _EdgeSoftness;
                        float4 clipRect = _ClipRect;

                        // Left edge softness
                        if (softness.x > 0 && position.x < clipRect.x + softness.x)
                        {
                            softnessAlpha = min(softnessAlpha, (position.x - clipRect.x) / softness.x);
                        }

                        // Right edge softness
                        if (softness.z > 0 && position.x > clipRect.z - softness.z)
                        {
                            softnessAlpha = min(softnessAlpha, (clipRect.z - position.x) / softness.z);
                        }

                        // Bottom edge softness
                        if (softness.y > 0 && position.y < clipRect.y + softness.y)
                        {
                            softnessAlpha = min(softnessAlpha, (position.y - clipRect.y) / softness.y);
                        }

                        // Top edge softness
                        if (softness.w > 0 && position.y > clipRect.w - softness.w)
                        {
                            softnessAlpha = min(softnessAlpha, (clipRect.w - position.y) / softness.w);
                        }

                        softnessAlpha = saturate(softnessAlpha);

                        // Combine hard clipping with softness
                        color.a *= hardClipAlpha * softnessAlpha;
                    }
                    else
                    {
                        // Standard Unity clipping only
                        color.a *= hardClipAlpha;
                    }
                #endif

                #if UNITY_UI_ALPHACLIP
                    clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}

Shader "Custom/HUDShader"
{
    Properties
    {
        [Header(Glass Properties)]
        _Color ("Tint Color", Color) = (1,1,1,0.1) // Cam rengi ve saydaml���
        _MainTex ("Albedo (RGB)", 2D) = "white" {} // �ste�e ba�l� doku

        [Header(Stencil Settings)]
        [IntRange] _StencilRef ("Stencil Reference Value", Range(0, 255)) = 1 // Stencil de�eri (Script ile senkronize olmal�)

        // Di�er iste�e ba�l� �zellikler eklenebilir (�rn. parlakl�k, metaliklik vs.)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        // Stencil Buffer Ayarlar�
        Stencil
        {
            Ref [_StencilRef]   // Bu shader'�n yazaca�� referans de�eri (Properties'den gelir)
            Comp Always         // Her zaman stencil testini ge� (yani her zaman �iz)
            Pass Replace        // Stencil testi ge�ilirse (ki her zaman ge�ecek), buffer'daki de�eri Ref de�eri ile de�i�tir
            // Fail Keep        // Stencil testi ba�ar�s�z olursa buffer'� de�i�tirme (Comp Always oldu�u i�in bu sat�r etkisiz)
            // ZFail Keep       // Derinlik testi ba�ar�s�z olursa buffer'� de�i�tirme
        }

        Pass
        {
            ZWrite Off // Saydam objeler genellikle derinlik buffer'�na yazmaz
            Blend SrcAlpha OneMinusSrcAlpha // Standart saydaml�k harmanlamas�

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit" // Eski donan�mlar i�in fallback
}
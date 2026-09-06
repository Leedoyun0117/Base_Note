Shader "Hidden/BaseNote/DotTexture"
{
    // 라이팅/스프라이트 렌더 결과 위에 도트 질감을 입히는 풀스크린 후처리.
    // URP FullScreenPassRendererFeature가 이 셰이더를 머티리얼로 물려 그린다.
    //
    // 효과 순서(프래그먼트 안): 디더 → 양자화(포스터라이즈) → 팔레트 램프.
    //   · 디더가 양자화보다 먼저여야 밴딩이 체커로 부서진다.
    //   · 양자화가 팔레트보다 먼저여야 팔레트 색이 다시 뭉개지지 않는다.
    // 각 효과는 개별 토글(0/1)과 강도 파라미터로 끄고 켜며 비교한다.
    //
    // 디더 패턴은 _RefResolution(기본 320x180) 격자에 고정한다 — 이 패스가
    // 저해상도 RT에서 돌든 풀 해상도에서 돌든, 화면(=아트 픽셀) 격자에 붙어
    // 카메라가 움직여도 흐르지 않는다.
    //
    // 팔레트는 교체 가능한 Texture2D(_PaletteTex)로 주입한다. 게임 로직
    // (MemoryColor)과의 배선은 하지 않는다 — 이 슬롯이 나중에 붙일 진입점이다.
    //
    // 효과는 화면 전체가 아니라 신뢰 마스크가 덮는 좌우 띠에만 걸린다.
    // MemoryRoomBootstrap이 Shader.SetGlobalFloat("_MaskCover", 1 - 가시비율)로
    // 넘기고, UI의 mask-left/right 패널과 같은 화면 X 비율을 쓴다. 신뢰가
    // 가득이면 _MaskCover 0 → 효과 없음(피처를 상시 켜둬도 무해).
    Properties
    {
        [Header(Posterize)]
        [ToggleUI] _Posterize ("Enable", Float) = 1
        _PosterizeSteps ("Steps", Range(2, 24)) = 10

        [Header(Dither)]
        [ToggleUI] _Dither ("Enable", Float) = 1
        _DitherStrength ("Strength", Range(0, 1)) = 0.4

        [Header(Palette Ramp)]
        [ToggleUI] _Palette ("Enable", Float) = 0
        [NoScaleOffset] _PaletteTex ("Palette LUT (Nx1)", 2D) = "white" {}
        _PaletteBlend ("Blend", Range(0, 1)) = 1

        [Header(Grid)]
        _RefResolution ("Ref Resolution (art px)", Vector) = (320, 180, 0, 0)

        [Header(Mask Region)]
        // 찢긴 경계는 일정 간격마다 잡은 정점(peak)을 직선으로 이어 만든다 —
        // 뾰족한 꼭짓점 → 비스듬한 사선 → 다음 꼭짓점. 유리 깨진 듯한 삐죽함.
        _TearChunkHeight ("Avg Spike Spacing (art px)", Range(3, 48)) = 12
        // 정점 좌우 진폭(아트 픽셀). 클수록 더 깊게 삐죽거린다.
        _EdgeRoughness ("Spike Amplitude (art px)", Range(0, 20)) = 6
        // 정점 간격 불규칙 정도. 0이면 규칙적 지그재그, 크면 예측 불가.
        _TearSpikeVariance ("Spike Spacing Variance", Range(0, 0.9)) = 0.6
        // 찢긴 윤곽을 따라 그리는 밝은 경계선 두께(아트 픽셀). 0이면 선 없음.
        _EdgeLineWidth ("Edge Line Width (art px)", Range(0, 4)) = 2
        _EdgeLineColor ("Edge Line Color", Color) = (0.7, 1.0, 0.92, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "DotTexture"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Vert / Attributes / Varyings / _BlitTexture / 전역 샘플러를 제공한다.
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Posterize;
            float _PosterizeSteps;
            float _Dither;
            float _DitherStrength;
            float _Palette;
            float _PaletteBlend;
            float4 _RefResolution;
            float _TearChunkHeight;
            float _EdgeRoughness;
            float _TearSpikeVariance;
            float _EdgeLineWidth;
            float4 _EdgeLineColor;

            // 코드가 Shader.SetGlobalFloat("_MaskCover", 1 - visibleRatio)로 넘긴다.
            // Properties에 두지 않는 이유: 머티리얼 값이 전역 값을 덮어쓴다. 선언만
            // 두면 전역이 그대로 들어오고, 아무도 안 넣으면 0(= 효과 없음)이 기본이다.
            float _MaskCover;

            TEXTURE2D(_PaletteTex);
            SAMPLER(sampler_PaletteTex);

            // 4x4 Bayer 행렬(0..15). 정렬된 디더 임계값.
            static const float kBayer4x4[16] =
            {
                 0.0,  8.0,  2.0, 10.0,
                12.0,  4.0, 14.0,  6.0,
                 3.0, 11.0,  1.0,  9.0,
                15.0,  7.0, 13.0,  5.0
            };

            float BayerAt(int2 cell)
            {
                int x = cell.x - 4 * (cell.x / 4);
                int y = cell.y - 4 * (cell.y / 4);
                return (kBayer4x4[y * 4 + x] + 0.5) / 16.0; // 0..1
            }

            // 시간 항 없는 1D 해시 — 같은 입력이면 항상 같은 값(0..1).
            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            // 정점 i의 세로 위치. i * 간격을 기준으로 Hash만큼 흔들어 불규칙하게.
            // 흔들림은 ±0.45칸으로 묶어 정점 순서가 뒤집히지 않게 한다.
            float SpikeY(float i, float seed, float spacing)
            {
                float jitter = _TearSpikeVariance * 0.45 * (Hash11(i * 2.13 + seed + 7.0) - 0.5) * 2.0;
                return (i + jitter) * spacing;
            }

            // 정점 i의 좌우 X 오프셋(± _EdgeRoughness).
            float SpikeX(float i, float seed)
            {
                return (Hash11(i * 1.7 + seed) - 0.5) * 2.0 * _EdgeRoughness;
            }

            // row에서의 찢긴 경계 X 오프셋 — 이웃한 두 정점 사이를 직선 보간.
            // 정점 간격이 불규칙하므로 후보 세 구간(base-1..base+1)에서 row를
            // 품는 구간을 찾아 보간한다. 결과는 float이지만, 아래 픽셀 판정이
            // floor로 스냅하므로 사선이 아트 격자 계단으로 렌더된다.
            float TearOffsetX(float row, float seed)
            {
                float spacing = max(_TearChunkHeight, 3.0);
                float base = floor(row / spacing);
                float result = SpikeX(base, seed); // 안전망

                [unroll]
                for (int k = -1; k <= 1; k++)
                {
                    float i0 = base + float(k);
                    float y0 = SpikeY(i0, seed, spacing);
                    float y1 = SpikeY(i0 + 1.0, seed, spacing);
                    float inSeg = step(y0, row) * step(row, y1 - 1e-4);
                    float t = saturate((row - y0) / max(y1 - y0, 1e-3));
                    float segX = lerp(SpikeX(i0, seed), SpikeX(i0 + 1.0, seed), t);
                    result = lerp(result, segX, inSeg);
                }
                return result;
            }

            // 한 행에서 띠 안쪽 경계까지의 거리(아트px). >0 이면 그 픽셀은 띠 안.
            float TearEdge(float row, float seed, float coverPx)
            {
                return coverPx + TearOffsetX(row, seed);
            }

            // pxFromEdge(왼쪽이면 pxX, 오른쪽이면 ref.x - pxX)가 찢긴 경계선 위인가.
            // 세로 구간 + 덩어리 사이 가로 단차(한 행 위/아래 덩어리 edge까지 X 구간)를
            // 함께 그려, 계단이 끊기지 않고 이어진 윤곽이 된다.
            float OnTearLine(float pxFromEdge, float row, float seed, float coverPx, float lineW)
            {
                float e0 = TearEdge(row, seed, coverPx);
                float d = e0 - pxFromEdge;
                float onV = step(0.0, d) * step(d, lineW);

                float eU = TearEdge(row - 1.0, seed, coverPx);
                float eD = TearEdge(row + 1.0, seed, coverPx);
                float onHU = step(min(e0, eU), pxFromEdge) * step(pxFromEdge, max(e0, eU));
                float onHD = step(min(e0, eD), pxFromEdge) * step(pxFromEdge, max(e0, eD));
                return max(onV, max(onHU, onHD));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                half4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                float3 srcRgb = saturate(src.rgb);

                // ── 효과 영역: 마스크가 덮는 좌우 띠 (종이 찢은 경계) ─────────
                // 전부 아트 픽셀 좌표(0..320, 0..180)로 계산해 격자에 스냅한다 —
                // 부드러운 곡선이 아니라 픽셀 계단으로 찢겨야 도트 스타일과 맞는다.
                // uv는 화면 좌표라 방이 흔들려도 찢긴 윤곽은 화면에 고정된다.
                float2 ref = max(_RefResolution.xy, float2(1.0, 1.0));
                int2 artPx = int2(floor(uv * ref));
                float pxX = float(artPx.x) + 0.5;
                float rowArt = float(artPx.y);

                float coverPx = saturate(_MaskCover) * 0.5 * ref.x; // 한쪽 띠의 기준 폭(아트px)

                // 신뢰가 가득이면(_MaskCover 0) 찢김 계산 전체를 건너뛴다 — 전역
                // 값이라 모든 픽셀이 같은 분기를 타므로 사실상 공짜다.
                UNITY_BRANCH
                if (coverPx <= 1e-5)
                    return half4(srcRgb, src.a);
                float active = 1.0;

                // 덩어리 단위로 흔들리는 경계. 좌/우 결이 다르도록 seed 분리.
                float pxFromLeft = pxX;
                float pxFromRight = ref.x - pxX;
                float distLeft = TearEdge(rowArt, 0.0, coverPx) - pxFromLeft;    // >0 이면 왼쪽 띠 안
                float distRight = TearEdge(rowArt, 111.0, coverPx) - pxFromRight; // >0 이면 오른쪽 띠 안
                float insideStrip = active * step(0.0, max(distLeft, distRight));

                // 찢긴 윤곽을 따라가는 밝은 경계선(세로 + 덩어리 사이 가로 단차).
                float lineW = max(_EdgeLineWidth, 0.0);
                float onLine = active * max(
                    OnTearLine(pxFromLeft, rowArt, 0.0, coverPx, lineW),
                    OnTearLine(pxFromRight, rowArt, 111.0, coverPx, lineW));

                // 효과도 선도 없는 픽셀은 원본 그대로 반환.
                UNITY_BRANCH
                if (insideStrip <= 0.0 && onLine <= 0.0)
                    return half4(srcRgb, src.a);

                float3 c = srcRgb;

                // Bayer 셀 — 패스 해상도와 무관하게 화면(아트 격자)에 고정.
                float threshold = BayerAt(artPx) - 0.5; // -0.5..~0.47

                // 명도(최대 채널)만 양자화하고 RGB는 그 비율로 함께 스케일한다 —
                // 채널별로 따로 양자화하면 근-검정 영역에서 채널이 제각기 반올림돼
                // 마젠타·보라 색틀어짐이 생긴다. 이 방식은 색상·채도를 보존하고
                // 밝기 계단만 만든다.
                float mx = max(c.r, max(c.g, c.b));
                float q = max(_PosterizeSteps, 2.0) - 1.0;
                float v = mx * q;

                // (B) 디더 — 양자화 직전, 명도 신호를 한 칸 폭만큼 흔들어 밴딩을 부순다.
                //     픽셀당 한 번(채널별 아님)이라 색 노이즈 없이 밝기 체커만 생긴다.
                if (_Dither > 0.5)
                    v += threshold * _DitherStrength;

                // (A) 포스터라이즈 — 명도를 steps 단계로 양자화.
                if (_Posterize > 0.5)
                    v = floor(v + 0.5);

                float qmx = saturate(v / q);
                float scale = mx > 1e-4 ? qmx / mx : 0.0;
                c = saturate(c * scale);

                // (C) 팔레트 램프 — 휘도를 색 계단(LUT)에 매핑. 진입점만 열어 둔다.
                if (_Palette > 0.5)
                {
                    float luma = dot(c, float3(0.299, 0.587, 0.114));
                    float3 ramp = SAMPLE_TEXTURE2D(_PaletteTex, sampler_PaletteTex, float2(luma, 0.5)).rgb;
                    c = lerp(c, ramp, saturate(_PaletteBlend));
                }

                // 띠 안쪽만 열화 결과, 밖은 원본. 그 위에 찢긴 경계선을 얹는다.
                c = (insideStrip > 0.0) ? saturate(c) : srcRgb;
                c = (onLine > 0.0) ? _EdgeLineColor.rgb : c;
                return half4(c, src.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}

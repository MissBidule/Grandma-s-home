using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using Unity.Collections;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace LineworkLite.Common.Utils
{
    public enum InjectionPoint
    {
        [InspectorName("Before Post Processing")]
        BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
        [InspectorName("After Post Processing")]
        AfterRenderingPostProcessing = RenderPassEvent.AfterRenderingPostProcessing,
        [InspectorName("Before Transparents")]
        BeforeRenderingTransparents = RenderPassEvent.BeforeRenderingTransparents,
    }

    public enum Occlusion
    {
        Always,
        WhenOccluded,
        WhenNotOccluded
    }

    public enum CullingMode
    {
        Back,
        Off
    }

    public enum BlendingMode
    {
        Alpha,
        Premultiply,
        Additive,
        SoftAdditive
    }

    public enum Channel
    {
        R,
        G,
        B,
        A
    }

    public enum Resolution
    {
        [InspectorName("480px")]
        _480,
        [InspectorName("720px")]
        _720,
        [InspectorName("1080px")]
        _1080,
        [InspectorName("Custom")]
        Custom
    }

    public enum MaterialType
    {
        Basic,
        [InspectorName("Custom (experimental)")]
        Custom
    }

    public enum WidthControl
    {
        Shared,
        PerOutline
    }

    public enum OutlineRenderQueue
    {
        Opaque,
        Transparent,
        [InspectorName("Opaque + Transparent")]
        OpaqueAndTransparent
    }

    public static class CommonShaderPropertyId
    {
        public static readonly int ZTest = Shader.PropertyToID("_ZTest");
        public static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        public static readonly int CullMode = Shader.PropertyToID("_Cull");
        public static readonly int BlendModeSource = Shader.PropertyToID("_SrcBlend");
        public static readonly int BlendModeDestination = Shader.PropertyToID("_DstBlend");
        public static readonly int FullScreenColorBlendModeSource = Shader.PropertyToID("_Fullscreen_SrcColorBlend");
        public static readonly int FullScreenColorBlendModeDestination = Shader.PropertyToID("_Fullscreen_DstColorBlend");

        public static readonly int FullScreenStencilReference = Shader.PropertyToID("_Fullscreen_StencilReference");
        public static readonly int FullScreenStencilComparison = Shader.PropertyToID("_Fullscreen_StencilComparison");
        public static readonly int FullScreenStencilReadMask = Shader.PropertyToID("_Fullscreen_StencilReadMask");

        public static readonly int FullScreenStencilPass = Shader.PropertyToID("_Fullscreen_StencilPass");
        public static readonly int FullScreenStencilFail = Shader.PropertyToID("_Fullscreen_StencilFail");

        public static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        public static readonly int AlphaCutoutTexture = Shader.PropertyToID("_AlphaCutoutTexture");
        public static readonly int AlphaCutoutThreshold = Shader.PropertyToID("_AlphaCutoutThreshold");
        public static readonly int AlphaCutoutUVTransform = Shader.PropertyToID("_AlphaCutoutUVTransform");
        public static readonly int ReferenceResolution = Shader.PropertyToID("_ReferenceResolution");
        public static readonly int Information = Shader.PropertyToID("_Information");
    }

    public static class RenderUtils
    {
        // Shader properties.
        public static readonly int BlendModeSourceProperty = Shader.PropertyToID("_SrcBlend");
        public static readonly int BlendModeDestinationProperty = Shader.PropertyToID("_DstBlend");

        // Shader tags.
        private static readonly ShaderTagId UniversalForward = new("UniversalForward");
        private static readonly ShaderTagId UniversalForwardOnly = new("UniversalForwardOnly");
        private static readonly ShaderTagId SRPDefaultUnlit = new("SRPDefaultUnlit");
        public static readonly List<ShaderTagId> DefaultShaderTagIds = new()
            {UniversalForward, UniversalForwardOnly, SRPDefaultUnlit};

#if UNITY_6000_0_OR_NEWER
        private static readonly ShaderTagId[] ShaderTagValues = new ShaderTagId[1];
        private static readonly RenderStateBlock[] RenderStateBlocks = new RenderStateBlock[1];

        public static void CreateRendererListWithRenderStateBlock(RenderGraph renderGraph, ref CullingResults cullingResults, DrawingSettings drawingSettings,
            FilteringSettings filteringSettings, RenderStateBlock renderStateBlock, ref RendererListHandle rendererListHandle)
        {
            ShaderTagValues[0] = ShaderTagId.none;
            RenderStateBlocks[0] = renderStateBlock;

            var tagValues = new NativeArray<ShaderTagId>(ShaderTagValues, Allocator.Temp);
            var stateBlocks = new NativeArray<RenderStateBlock>(RenderStateBlocks, Allocator.Temp);
            var param = new RendererListParams(cullingResults, drawingSettings, filteringSettings)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks,
                isPassTagName = false
            };
            rendererListHandle = renderGraph.CreateRendererList(param);
        }

        private class PassData
        {
        }

        public static void ClearStencil(RenderGraph renderGraph, UniversalResourceData resourceData, Material clear)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>("Clear Stencil (Free Outline)", out _);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0); // TODO: SHOULD NOT BE NEEDED, UNITY BUG!
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData _, RasterGraphContext context) => { context.cmd.DrawProcedural(Matrix4x4.identity, clear, 0, MeshTopology.Triangles, 3, 1); });
        }
#endif

        public static (int, int) GetSrcDstBlend(BlendingMode blendMode)
        {
            var blending = (0, 0);

            switch (blendMode)
            {
                case BlendingMode.Alpha: // traditional transparency
                    blending.Item1 = (int) BlendMode.SrcAlpha;
                    blending.Item2 = (int) BlendMode.OneMinusSrcAlpha;
                    break;
                case BlendingMode.Premultiply: // premultiplied transparency
                    blending.Item1 = (int) BlendMode.One;
                    blending.Item2 = (int) BlendMode.OneMinusSrcAlpha;
                    break;
                case BlendingMode.Additive: // additive
                    blending.Item1 = (int) BlendMode.One;
                    blending.Item2 = (int) BlendMode.One;
                    break;
                case BlendingMode.SoftAdditive: // soft additive
                    blending.Item1 = (int) BlendMode.OneMinusDstColor;
                    blending.Item2 = (int) BlendMode.One;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(blendMode), blendMode, null);
            }

            return blending;
        }
    }
}

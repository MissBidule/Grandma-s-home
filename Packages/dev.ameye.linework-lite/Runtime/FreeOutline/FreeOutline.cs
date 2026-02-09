using System;
using System.Linq;
using LineworkLite.Common.Utils;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace LineworkLite.FreeOutline
{
    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Free Outline")]
#if UNITY_6000_0_OR_NEWER
    [SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
    [Tooltip("Free Outline renders outlines by rendering an extruded version of an object behind the original object.")]
    [HelpURL("https://linework.ameye.dev/free-outline")]
    public class FreeOutline : ScriptableRendererFeature
    {
        private class FreeOutlinePass : ScriptableRenderPass
        {
            private FreeOutlineSettings settings;
            private Material mask, outlineBase, clear;
            private readonly ProfilingSampler maskSampler, outlineSampler;

            public FreeOutlinePass()
            {
                profilingSampler = new ProfilingSampler(nameof(FreeOutlinePass));
                maskSampler = new ProfilingSampler(ShaderPassName.Mask);
                outlineSampler = new ProfilingSampler(ShaderPassName.Outline);
            }
            
            public bool Setup(ref FreeOutlineSettings freeOutlineSettings, ref Material maskMaterial, ref Material outlineMaterial, ref Material clearMaterial)
            {
                settings = freeOutlineSettings;
                mask = maskMaterial;
                outlineBase = outlineMaterial;
                clear = clearMaterial;
                renderPassEvent = (RenderPassEvent) freeOutlineSettings.InjectionPoint;

                foreach (var outline in settings.Outlines)
                {
                    if (outline.material == null)
                    {
                        outline.AssignMaterials(outlineBase);
                    }
                }

                foreach (var outline in settings.Outlines)
                {
                    if (!outline.IsActive())
                    {
                        continue;
                    }
                    
                    var material = outline.material;

                    var (srcBlend, dstBlend) = RenderUtils.GetSrcDstBlend(outline.blendMode);
                    material.SetInt(CommonShaderPropertyId.BlendModeSource, srcBlend);
                    material.SetInt(CommonShaderPropertyId.BlendModeDestination, dstBlend);
                    switch (outline.maskingStrategy)
                    {
                        case MaskingStrategy.Stencil:
                            material.SetFloat(CommonShaderPropertyId.CullMode, (float) CullMode.Off);
                            break;
                        case MaskingStrategy.CullFrontFaces:
                            material.SetFloat(CommonShaderPropertyId.CullMode, (float) CullMode.Front);
                            break;
                    }
                    material.SetColor(CommonShaderPropertyId.OutlineColor, outline.color);
                    material.SetColor(ShaderPropertyId.OutlineOccludedColor, outline.occlusion == Occlusion.WhenOccluded ? outline.color : outline.occludedColor);
                    material.SetFloat(ShaderPropertyId.OutlineWidth, outline.width);
                    
                    // Scale with resolution.
                    if (outline.scaleWithResolution) material.EnableKeyword(ShaderFeature.ScaleWithResolution);
                    else material.DisableKeyword(ShaderFeature.ScaleWithResolution);
                    switch (outline.referenceResolution)
                    {
                        case Resolution._480:
                            material.SetFloat(ShaderPropertyId.ReferenceResolution, 480.0f);
                            break;
                        case Resolution._720:
                            material.SetFloat(ShaderPropertyId.ReferenceResolution, 720.0f);
                            break;
                        case Resolution._1080:
                            material.SetFloat(ShaderPropertyId.ReferenceResolution, 1080.0f);
                            break;
                        case Resolution.Custom:
                            material.SetFloat(ShaderPropertyId.ReferenceResolution, outline.customResolution);
                            break;
                    }
                    
                    if (outline.extrusionMethod == ExtrusionMethod.ClipSpaceNormalVector)
                    {
                        material.SetFloat(ShaderPropertyId.OutlineWidth, outline.width);
                        material.SetFloat(ShaderPropertyId.MinOutlineWidth, outline.minWidth);
                    }
                    else
                    {
                        material.SetFloat(ShaderPropertyId.OutlineWidth, outline.width * 0.015f);
                        material.SetFloat(ShaderPropertyId.MinOutlineWidth, outline.minWidth * 0.015f);
                    }
                    if (outline.enableOcclusion) material.EnableKeyword(ShaderFeature.Occlusion);
                    else material.DisableKeyword(ShaderFeature.Occlusion);
                    if (outline.scaling == Scaling.ScaleWithDistance) material.EnableKeyword(ShaderFeature.ScaleWithDistance);
                    else material.DisableKeyword(ShaderFeature.ScaleWithDistance);
                    switch (outline.occlusion)
                    {
                        case Occlusion.Always:
                            material.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.Always);
                            break;
                        case Occlusion.WhenOccluded:
                            material.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.GreaterEqual);
                            break;
                        case Occlusion.WhenNotOccluded:
                            material.SetFloat(CommonShaderPropertyId.ZTest, (float) CompareFunction.LessEqual);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return settings.Outlines.Any(ShouldRenderOutline);
            }
            
            private static bool ShouldRenderStencilMask(Outline outline)
            {
                return outline.IsActive() && (outline.maskingStrategy == MaskingStrategy.Stencil || outline.occlusion != Occlusion.WhenNotOccluded);
            }

            private static bool ShouldRenderOutline(Outline outline)
            {
                return outline.IsActive();
            }
            
#if UNITY_6000_0_OR_NEWER
            private class PassData
            {
                internal List<RendererListHandle> MaskRendererListHandles = new();
                internal readonly List<RendererListHandle> OutlineRendererListHandles = new();
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                // 1. Mask.
                // -> Render a mask to the stencil buffer.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Mask, out var passData))
                {
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                
                    InitMaskRendererList(renderGraph, frameData, ref passData);
                    foreach (var rendererListHandle in passData.MaskRendererListHandles)
                    {
                        builder.UseRendererList(rendererListHandle);
                    }
                    
                    builder.AllowPassCulling(false);
                    
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        foreach (var handle in data.MaskRendererListHandles)
                        {
                            context.cmd.DrawRendererList(handle);
                        }
                    });
                }

                // 2. Outline.
                // -> Render an outline.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Outline, out var passData))
                {
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                    InitOutlineRendererLists(renderGraph, frameData, ref passData);
                    foreach (var rendererListHandle in passData.OutlineRendererListHandles)
                    {
                        builder.UseRendererList(rendererListHandle);
                    }

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        foreach (var handle in data.OutlineRendererListHandles)
                        {
                            context.cmd.DrawRendererList(handle);
                        }
                    });
                }
                
                // 3. Clear stencil.
                // -> Clear the stencil buffer.
                RenderUtils.ClearStencil(renderGraph, resourceData, clear);
            }

            private void InitMaskRendererList(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
            {
                passData.MaskRendererListHandles.Clear();
                
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();
                
                var sortingCriteria = cameraData.defaultOpaqueSortFlags;

                foreach (var outline in settings.Outlines)
                {
                    var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, renderingData, cameraData, lightData, sortingCriteria);
                    drawingSettings.overrideMaterial = mask;
                    
                    var renderQueueRange = outline.renderQueue switch
                    {
                        OutlineRenderQueue.Opaque => RenderQueueRange.opaque,
                        OutlineRenderQueue.Transparent => RenderQueueRange.transparent,
                        OutlineRenderQueue.OpaqueAndTransparent => RenderQueueRange.all,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    
                    var filteringSettings = new FilteringSettings(renderQueueRange, outline.layerMask, outline.RenderingLayer);
                    var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                    var blendState = BlendState.defaultValue;
                    blendState.blendState0 = new RenderTargetBlendState(0);
                    renderStateBlock.blendState = blendState;
                    
                    // Set stencil state.
                    var stencilState = StencilState.defaultValue;
                    stencilState.enabled = true;
                    stencilState.SetCompareFunction(CompareFunction.Always);
                    stencilState.SetPassOperation(StencilOp.Replace);
                    stencilState.SetFailOperation(StencilOp.Replace);
                    stencilState.SetZFailOperation(StencilOp.Replace);
                    renderStateBlock.mask |= RenderStateMask.Stencil;
                    renderStateBlock.stencilReference = 1;
                    renderStateBlock.stencilState = stencilState;
                    
                    var handle = new RendererListHandle();
                    RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock,
                        ref handle);
                    passData.MaskRendererListHandles.Add(handle);
                }
            }

            private void InitOutlineRendererLists(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
            {
                passData.OutlineRendererListHandles.Clear();

                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                var sortingCriteria = cameraData.defaultOpaqueSortFlags;

                foreach (var outline in settings.Outlines)
                {
                    if (!ShouldRenderOutline(outline))
                    {
                        continue;
                    }
                    
                    var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, renderingData, cameraData, lightData, sortingCriteria);
                    switch (outline.materialType)
                    {
                        case MaterialType.Basic:
                            drawingSettings.overrideMaterial = outline.material;
                            drawingSettings.overrideMaterialPassIndex = (int) outline.extrusionMethod;
                            drawingSettings.enableInstancing = false;
                            break;
                        case MaterialType.Custom when outline.customMaterial != null:
                            drawingSettings.overrideMaterial = outline.customMaterial;
                            break;
                    }
                    
                    var renderQueueRange = outline.renderQueue switch
                    {
                        OutlineRenderQueue.Opaque => RenderQueueRange.opaque,
                        OutlineRenderQueue.Transparent => RenderQueueRange.transparent,
                        OutlineRenderQueue.OpaqueAndTransparent => RenderQueueRange.all,
                        _ => throw new ArgumentOutOfRangeException()
                    };
         
                    var filteringSettings = new FilteringSettings(renderQueueRange, outline.layerMask, outline.RenderingLayer);

                    // Override stencil state.
                    var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
                    if (ShouldRenderStencilMask(outline))
                    {
                        var stencilState = StencilState.defaultValue;
                        stencilState.enabled = true;
                        stencilState.SetCompareFunction(CompareFunction.NotEqual);
                        stencilState.SetPassOperation(StencilOp.Zero);
                        stencilState.SetFailOperation(StencilOp.Keep); // Why is Zero not possible here?
                        renderStateBlock.mask |= RenderStateMask.Stencil;
                        renderStateBlock.stencilReference = 1;
                        renderStateBlock.stencilState = stencilState;
                    }

                    var handle = new RendererListHandle();
                    RenderUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawingSettings, filteringSettings, renderStateBlock,
                        ref handle);
                    passData.OutlineRendererListHandles.Add(handle);

                }
            }
#endif
            private RTHandle cameraDepthRTHandle;
            
            #pragma warning disable 618, 672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ConfigureTarget(cameraDepthRTHandle);
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // 1. Mask.
                // -> Render a mask to the stencil buffer.
                var maskCmd = CommandBufferPool.Get();

                using (new ProfilingScope(maskCmd, maskSampler))
                {
                    context.ExecuteCommandBuffer(maskCmd);
                    maskCmd.Clear();

                    var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

                    foreach (var outline in settings.Outlines)
                    {
                        var renderQueueRange = outline.renderQueue switch
                        {
                            OutlineRenderQueue.Opaque => RenderQueueRange.opaque,
                            OutlineRenderQueue.Transparent => RenderQueueRange.transparent,
                            OutlineRenderQueue.OpaqueAndTransparent => RenderQueueRange.all,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, ref renderingData, sortingCriteria);
                        drawingSettings.overrideMaterial = mask;

                        var filteringSettings = new FilteringSettings(renderQueueRange, outline.layerMask, outline.RenderingLayer);
                        var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                        var blendState = BlendState.defaultValue;
                        blendState.blendState0 = new RenderTargetBlendState(0);
                        renderStateBlock.blendState = blendState;

                        var stencilState = StencilState.defaultValue;
                        stencilState.enabled = true;
                        stencilState.SetCompareFunction(CompareFunction.Always);
                        stencilState.SetPassOperation(StencilOp.Replace);
                        stencilState.SetFailOperation(StencilOp.Replace);
                        stencilState.SetZFailOperation(StencilOp.Replace);
                        renderStateBlock.mask |= RenderStateMask.Stencil;
                        renderStateBlock.stencilReference = 1;
                        renderStateBlock.stencilState = stencilState;

                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                    }
                }

                context.ExecuteCommandBuffer(maskCmd);
                CommandBufferPool.Release(maskCmd);

                // 2. Outline.
                // -> Render an outline.
                var outlineCmd = CommandBufferPool.Get();

                using (new ProfilingScope(outlineCmd, outlineSampler))
                {
                    CoreUtils.SetRenderTarget(outlineCmd, renderingData.cameraData.renderer.cameraColorTargetHandle, cameraDepthRTHandle); // if using cameraColorRTHandle this does not render in scene view when rendering after post processing with post processing enabled
                    context.ExecuteCommandBuffer(outlineCmd);
                    outlineCmd.Clear();
                    
                    var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                    var renderQueueRange = RenderQueueRange.opaque;

                    foreach (var outline in settings.Outlines)
                    {
                        if (!ShouldRenderOutline(outline))
                        {
                            continue;
                        }
                        
                        renderQueueRange = outline.renderQueue switch
                        {
                            OutlineRenderQueue.Opaque => RenderQueueRange.opaque,
                            OutlineRenderQueue.Transparent => RenderQueueRange.transparent,
                            OutlineRenderQueue.OpaqueAndTransparent => RenderQueueRange.all,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        var drawingSettings = RenderingUtils.CreateDrawingSettings(RenderUtils.DefaultShaderTagIds, ref renderingData, sortingCriteria);
                        drawingSettings.overrideMaterial = outline.material;
                        drawingSettings.overrideMaterialPassIndex = (int) outline.extrusionMethod;
                        drawingSettings.perObjectData = PerObjectData.None;
                        drawingSettings.enableInstancing = false;

                        var filteringSettings = new FilteringSettings(renderQueueRange, outline.layerMask, outline.RenderingLayer);
                        
                        var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
                        if (ShouldRenderStencilMask(outline))
                        {
                            var stencilState = StencilState.defaultValue;
                            stencilState.enabled = true;
                            stencilState.SetCompareFunction(CompareFunction.NotEqual);
                            stencilState.SetPassOperation(StencilOp.Zero);
                            stencilState.SetFailOperation(StencilOp.Keep);
                            renderStateBlock.mask |= RenderStateMask.Stencil;
                            renderStateBlock.stencilReference = 1;
                            renderStateBlock.stencilState = stencilState;
                        }
                        
                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
                    }
                }

                context.ExecuteCommandBuffer(outlineCmd);
                CommandBufferPool.Release(outlineCmd);
                
                // 3. Clear stencil.
                // -> Clear the stencil buffer.
                var clearStencilCmd = CommandBufferPool.Get();
                
                using (new ProfilingScope(clearStencilCmd, outlineSampler))
                {
                    context.ExecuteCommandBuffer(clearStencilCmd);
                    clearStencilCmd.Clear();
                
                    CoreUtils.SetRenderTarget(clearStencilCmd, renderingData.cameraData.renderer.cameraColorTargetHandle, cameraDepthRTHandle); // if using cameraColorRTHandle this does not render in scene view when rendering after post processing with post processing enabled
                    clearStencilCmd.DrawProcedural(Matrix4x4.identity, clear, 0, MeshTopology.Triangles, 3, 1); 
                }
                
                context.ExecuteCommandBuffer(clearStencilCmd);
                CommandBufferPool.Release(clearStencilCmd);
            }
            #pragma warning restore 618, 672
            
            public void SetTarget(RTHandle depth)
            {
                cameraDepthRTHandle = depth;
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException(nameof(cmd));
                }
                
                cameraDepthRTHandle = null;
            }

            public void Dispose()
            {
                settings = null; // de-reference settings to allow them to be freed from memory
            }
        }

        [SerializeField] private FreeOutlineSettings settings;
        [SerializeField] private ShaderResources shaders;
        private Material maskMaterial, outlineMaterial, clearMaterial;
        private FreeOutlinePass freeOutlinePass;

        /// <summary>
        /// Called
        /// - When the Scriptable Renderer Feature loads the first time.
        /// - When you enable or disable the Scriptable Renderer Feature.
        /// - When you change a property in the Inspector window of the Renderer Feature.
        /// </summary>
        public override void Create()
        {
            if (settings == null) return;
            settings.OnSettingsChanged = null;
            settings.OnSettingsChanged += Create;

            shaders = new ShaderResources().Load();
            freeOutlinePass ??= new FreeOutlinePass();
        }

        /// <summary>
        /// Called
        /// - Every frame, once for each camera.
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings == null || freeOutlinePass == null) return;

            // Don't render for some views.
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || renderingData.cameraData.cameraType == CameraType.Reflection
                || renderingData.cameraData.cameraType == CameraType.SceneView && !settings.ShowInSceneView
#if UNITY_6000_0_OR_NEWER
                || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
#else
                )
#endif
                return;

            if (!CreateMaterials())
            {
                Debug.LogWarning("Not all required materials could be created. Free Outline will not render.");
                return;
            }
            
            var render = freeOutlinePass.Setup(ref settings, ref maskMaterial, ref outlineMaterial, ref clearMaterial);
            if (render) renderer.EnqueuePass(freeOutlinePass);
        }
        
        #pragma warning disable 618, 672
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (settings == null || freeOutlinePass == null || renderingData.cameraData.cameraType == CameraType.SceneView && !settings.ShowInSceneView) return;
            if (renderingData.cameraData.cameraType is CameraType.Preview or CameraType.Reflection) return;

            freeOutlinePass.SetTarget(renderer.cameraDepthTargetHandle);
        }
        #pragma warning restore 618, 672
        
        /// <summary>
        /// Clean up resources allocated to the Scriptable Renderer Feature such as materials.
        /// </summary>
        override protected void Dispose(bool disposing)
        {
            freeOutlinePass?.Dispose();
            freeOutlinePass = null;
            DestroyMaterials();
        }
        
        private void OnDestroy()
        {
            settings = null; // de-reference settings to allow them to be freed from memory
            freeOutlinePass?.Dispose();
        }

        private void DestroyMaterials()
        {
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(outlineMaterial);
            CoreUtils.Destroy(clearMaterial);
        }

        private bool CreateMaterials()
        {
            if (maskMaterial == null)
            {
                maskMaterial = CoreUtils.CreateEngineMaterial(shaders.mask);
            }
            
            if (outlineMaterial == null)
            {
                outlineMaterial = CoreUtils.CreateEngineMaterial(shaders.outline);
            }
            
            if (clearMaterial == null)
            {
                clearMaterial = CoreUtils.CreateEngineMaterial(shaders.clear);
            }

            return maskMaterial != null && outlineMaterial != null && clearMaterial != null;
        }
    }
}
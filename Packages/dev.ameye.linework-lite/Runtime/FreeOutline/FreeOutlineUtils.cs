using System;
using UnityEngine;

namespace LineworkLite.FreeOutline
{
    public static class FreeOutlineUtils
    {
        public static readonly string SmoothNormalsLabel = "SmoothNormalsLineworkLite";
    }

    [Serializable]
    public sealed class ShaderResources
    {
        public Shader mask;
        public Shader outline;
        public Shader clear;

        public ShaderResources Load()
        {
            mask = Shader.Find(ShaderPath.Mask);
            outline = Shader.Find(ShaderPath.Outline);
            clear = Shader.Find(ShaderPath.Clear);
            return this;
        }
    }

    static class ShaderPath
    {
        public const string Mask = "Hidden/Outlines/Free Outline/Mask";
        public const string Outline = "Hidden/Outlines/Free Outline/Outline";
        public const string Clear = "Hidden/Clear Stencil";
    }

    static class ShaderPassName
    {
        public const string Mask = "Mask (Free Outline)";
        public const string Outline = "Outline (Free Outline)";
    }

    static class ShaderPropertyId
    {
        public static readonly int OutlineOccludedColor = Shader.PropertyToID("_OutlineOccludedColor");
        public static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        public static readonly int MinOutlineWidth = Shader.PropertyToID("_MinimumOutlineWidth");
        public static readonly int ReferenceResolution = Shader.PropertyToID("_ReferenceResolution");
    }

    static class ShaderFeature
    {
        public const string ScaleWithDistance = "SCALE_WITH_DISTANCE";
        public const string ScaleWithResolution = "SCALE_WITH_RESOLUTION";
        public const string Occlusion = "OCCLUSION";
    }

    public enum MaskingStrategy
    {
        Stencil,
        CullFrontFaces
    }

    public enum ExtrusionMethod
    {
        [InspectorName("Vertex Position (OS)")]
        ObjectSpaceVertexPosition = 0,
        [InspectorName("Normalized Vertex Position (OS)")]
        ObjectSpaceNormalizedVertexPosition = 1,
        [InspectorName("Normal Vector (OS)")]
        ObjectSpaceNormalVector = 2,
        [InspectorName("Vertex Color (OS)")]
        ObjectSpaceVertexColor = 3,
        [InspectorName("Normal Vector (CS)")]
        ClipSpaceNormalVector = 4,
        [InspectorName("Normal Vector (SS)")]
        ScreenSpaceNormalVector = 5,
        [InspectorName("Normal Vector (WS)")]
        WorldSpaceNormalVector = 6,
        [InspectorName("Smoothed Normals")]
        SmoothedNormals = 7,
    }

    public enum Scaling
    {
        ConstantScreenSize,
        ScaleWithDistance
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
}

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace uLipSync
{

public static class AudioUtil
{
    static readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

    static MethodInfo GetMethod(string methodName, Type[] argTypes)
    {
        if (methods.TryGetValue(methodName, out var method)) return method;

        var asm = typeof(AudioImporter).Assembly;
        var audioUtil = asm.GetType("UnityEditor.AudioUtil");
        method = audioUtil.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.Public,
            null,
            argTypes,
            null);

        if (method != null)
        {
            methods.Add(methodName, method);
        }

        return method;
    }

    public static float[] GetMinMaxData(AudioClip clip)
    {
        if (!clip) return null;

        var method = GetMethod("GetMinMaxData", new[] { typeof(AudioImporter) });
        var path = AssetDatabase.GetAssetPath(clip);
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        return (float[])method.Invoke(null, new object[] { importer });
    }

    public static void PlayClip([UsedImplicitly] AudioClip clip)
    {
        if (!clip) return;
#if UNITY_2020_1_OR_NEWER
        var method = GetMethod("PlayPreviewClip", new[] { typeof(AudioClip), typeof(int), typeof(bool) });
        method.Invoke(null, new object[] { clip, 0, false });
#else
        var method = GetMethod("PlayClip", new Type[] { typeof(AudioClip) });
        method.Invoke(null, new object[] { clip });
#endif
    }

    public static void StopClip([UsedImplicitly] AudioClip clip)
    {
#if UNITY_2020_1_OR_NEWER
        var method = GetMethod("StopAllPreviewClips", new Type[] {});
        method.Invoke(null, new object[] {});
#else
        if (!clip) return;
        var method = GetMethod("StopClip", new Type[] { typeof(AudioClip) });
        method.Invoke(null, new object[] { clip });
#endif
    }
}

}

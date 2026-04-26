using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/*
 * @brief Contains class declaration for OutlineSuppressor
 * @details Static helper that disables the X-ray outline of sabotable objects via a Handle pattern.
 */
public static class OutlineSuppressor
{
    /*
     * @brief Opaque token returned by Acquire and required by Release to restore previous state
     */
    public class Handle
    {
        internal List<(OutlineVolumeComponent comp, bool wasActive)> savedComponents;
    }

    private static int s_refCount;

    /*
     * @brief Disables every active OutlineVolumeComponent and forces the global volume stack to a no-op state
     * @params Handle existing if non-null, returns it as-is to avoid double acquiring
     * @return Handle that the caller must pass back to Release()
    */
    public static Handle Acquire(Handle existing)
    {
        if (existing != null) return existing;
        var handle = new Handle { savedComponents = new List<(OutlineVolumeComponent, bool)>() };

        foreach (var v in GameObject.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (v == null) continue;
            var profile = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;
            if (profile == null) continue;
            if (!profile.TryGet<OutlineVolumeComponent>(out var outline) || outline == null) continue;
            handle.savedComponents.Add((outline, outline.active));
            outline.active = false;
        }

        s_refCount++;
        ApplyStackOverride();
        return handle;
    }

    /*
     * @brief  Restores the previous active state of every OutlineVolumeComponent saved in the handle
     * @params ref Handle handle the handle returned by Acquire; set to null on exit
     * @return void
    */
    public static void Release(ref Handle handle)
    {
        if (handle == null) return;
        foreach (var (comp, wasActive) in handle.savedComponents) if (comp != null) comp.active = wasActive;
        handle = null;
        s_refCount = Mathf.Max(0, s_refCount - 1);
    }

    /*
     * @brief  Forces the global volume stack OutlineVolumeComponent to render nothing
     * @details Sets every color alpha and fill alpha to 0 and the border size to 0 so IsActive() returns false which works around cameras using ViaScripting volume update mode
     * @return void
    */
    private static void ApplyStackOverride()
    {
        var stackOutline = VolumeManager.instance.stack?.GetComponent<OutlineVolumeComponent>();
        if (stackOutline == null) return;
        stackOutline.color1.Override(new Color(stackOutline.color1.value.r, stackOutline.color1.value.g, stackOutline.color1.value.b, 0f));
        stackOutline.color2.Override(new Color(stackOutline.color2.value.r, stackOutline.color2.value.g, stackOutline.color2.value.b, 0f));
        stackOutline.color3.Override(new Color(stackOutline.color3.value.r, stackOutline.color3.value.g, stackOutline.color3.value.b, 0f));
        stackOutline.color4.Override(new Color(stackOutline.color4.value.r, stackOutline.color4.value.g, stackOutline.color4.value.b, 0f));
        stackOutline.fillAlpha1.Override(0f);
        stackOutline.fillAlpha2.Override(0f);
        stackOutline.fillAlpha3.Override(0f);
        stackOutline.fillAlpha4.Override(0f);
        stackOutline.borderSize.Override(0);
    }
}

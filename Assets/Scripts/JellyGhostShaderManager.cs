using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class JellyGhostShaderManager : MonoBehaviour
{
    private static readonly int s_refractionTextureID = Shader.PropertyToID("_JellyGhost_RefractionTexture");
    private static readonly int s_refractionStrengthID = Shader.PropertyToID("_JellyGhost_RefractionStrength");

    [Tooltip("The Material which name is \"JellyGhost_ObjectColour\"\n(the only one that has the UseCustomBaseColour property to true (when the box is ticked on))")]
    [SerializeField] private Material jellyGhostObjColMaterial = null;

    [Tooltip("GameObjects that you want to use their already defined colour in the Playdough shader\n(the colour given before exporting to Unity, in Blender for example)\n/!\\The GameObjects must the Material you want to use the colour of be the first in the list,\nand the Material with the Playdough shader must be the last in the list/!\\")]
    [SerializeField] public MeshRenderer[] renderersToModify = null;

    [Tooltip("The texture used for refraction, when light is distorted, affecting the view through the object\n(for example take cracked glasses => you may see things in multiples)")]
    [SerializeField] private Texture2D refractionTexture = null;

    [Min(0f)] [SerializeField]
    [Tooltip("The strength of the light distortion effect from the above texture")]
    private float refractionStrength = 0f;

    private MeshRenderer[] renderersToModifyCpy = null;

    private void OnValidate()
    {
        Shader.SetGlobalTexture(s_refractionTextureID, refractionTexture);
        Shader.SetGlobalFloat(s_refractionStrengthID, refractionStrength);

        if (renderersToModifyCpy != renderersToModify)
        {
            UpdateRenderersToModify();

            renderersToModifyCpy = renderersToModify;
        }
    }


    public void UpdateRenderersToModify()
    {
#if UNITY_EDITOR
        if (EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }
#endif

        if (renderersToModify == null || renderersToModify.Length == 0)
        {
            return;
        }

        if (jellyGhostObjColMaterial == null ||
            jellyGhostObjColMaterial.shader == null ||
            !jellyGhostObjColMaterial.HasProperty("_BaseColour"))
        {
            return;
        }

        foreach (MeshRenderer mr in renderersToModify)
        {
            if (mr == null)
            {
                continue;
            }

            Material currentMat = mr.sharedMaterial;
            if (currentMat == null)
            {
                continue;
            }

            if (currentMat.shader == jellyGhostObjColMaterial.shader)
            {
                continue;
            }

            Color originalColor = currentMat.color;
            mr.sharedMaterial = new Material(jellyGhostObjColMaterial);
            mr.sharedMaterial.SetColor("_BaseColour", originalColor);
        }
    }
}

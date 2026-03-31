using UnityEngine;

public class SkinItem : MonoBehaviour
{
    public enum Camp { Kids, Demons }

    [Header("Configuration")]
    public Camp monCamp;

    [Header("Le Matériau Fantôme")]
    public Material materialFantome;

    private Renderer[] tousLesRenderers;
    private Material[] materiauxOriginaux;

    private void Awake()
    {
        tousLesRenderers = GetComponentsInChildren<Renderer>(true);
        materiauxOriginaux = new Material[tousLesRenderers.Length];

        for (int i = 0; i < tousLesRenderers.Length; i++)
        {
            if (tousLesRenderers[i] != null)
            {
                materiauxOriginaux[i] = tousLesRenderers[i].material;
            }
        }
    }

    public void MettreAJourSelection(Camp campSelectionne)
    {
        bool estMonCamp = (monCamp == campSelectionne);

        // ==========================================
        // 🎯 LA SOLUTION DÉFINITIVE : LE LAYER
        // 0 = Default (La souris peut cliquer dessus)
        // 2 = Ignore Raycast (La souris passe au travers !)
        // ==========================================
        int layerCible = estMonCamp ? 0 : 2;

        // On applique cette règle anti-clic à l'objet et à toutes ses parties
        Collider[] tousLesColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in tousLesColliders)
        {
            if (col != null)
            {
                col.gameObject.layer = layerCible;

                // On essaie quand même de l'éteindre au cas où
                col.enabled = estMonCamp;
            }
        }

        // ==========================================
        // LE CHANGEMENT DE VÊTEMENTS
        // ==========================================
        for (int i = 0; i < tousLesRenderers.Length; i++)
        {
            if (tousLesRenderers[i] != null)
            {
                if (estMonCamp)
                {
                    tousLesRenderers[i].material = materiauxOriginaux[i];
                }
                else if (materialFantome != null)
                {
                    tousLesRenderers[i].material = materialFantome;
                }
            }
        }
    }
}
using UnityEngine;

public class MaterialInstance : MonoBehaviour
{
    public GameObject go;
    public Material material;
    // Offset x/y à appliquer à la texture (surface input offset)
    public Vector2 surfaceOffset = Vector2.zero;
    // Nom de la propriété de texture principale (URP utilise souvent _BaseMap)
    public string textureProperty = "_BaseMap";

    void Start()
    {
        go = this.gameObject;
        var rend = go.GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning($"No Renderer found on '{go.name}'. Material instance not created.");
            return;
        }

        // Renderer.material crée une instance du matériau pour cet objet
        material = rend.material;
    }

    void Update()
    {
        if (material == null)
            return;

        // Applique l'offset x/y à la propriété principale de texture.
        // On écrit sur _BaseMap (URP) et _MainTex (Standard) pour couvrir les deux cas.
        material.SetTextureOffset(textureProperty, surfaceOffset);
        material.SetTextureOffset("_MainTex", surfaceOffset);
    }
}

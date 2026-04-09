using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class TextureTilingController : MonoBehaviour {

	// Give us the texture so that we can scale proportionally the width according to the height variable below
	// We will grab it from the meshRenderer
	public Texture texture;
	public float textureToMeshZ = 2f; // Use this to constrain texture to a certain size
	public float offsetX = 0f;
	private float offsetXCpy;
	public float offsetY = 0f;
	private float offsetYCpy;
    public Material originalMaterial;
    private Material originalMaterialCpy = null;
	public int materialIndex = 0;

	Vector3 prevScale = Vector3.one;
	float prevTextureToMeshZ = -1f;

	// Use this for initialization
	void Start () {
		prevScale = gameObject.transform.lossyScale;
		prevTextureToMeshZ = textureToMeshZ;
        
        RefreshMaterial();
	}

	void RefreshMaterial()
	{
		MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        var tempMaterial = new Material(originalMaterial);
		tempMaterial.name = "tempMaterial";
		//replace the whole array because otherwse it doesn't work
		Material[] materials = renderer.sharedMaterials;
		materials[materialIndex] = tempMaterial;
		renderer.sharedMaterials=materials;

		originalMaterialCpy = originalMaterial;
		UpdateTiling();
	}

	// Update is called once per frame
	void Update () {
		if (originalMaterial != originalMaterialCpy)
		{
			RefreshMaterial();
		}
		// If something has changed
		if(gameObject.transform.lossyScale != prevScale || !Mathf.Approximately(this.textureToMeshZ, prevTextureToMeshZ) || offsetX != offsetXCpy || offsetY != offsetYCpy)
			UpdateTiling();

		// Maintain previous state variables
		prevScale = gameObject.transform.lossyScale;
		prevTextureToMeshZ = textureToMeshZ;
	}

	[ContextMenu("UpdateTiling")]
	void UpdateTiling()
	{
		// A Unity plane is 10 units x 10 units
		float planeSizeX = 10f;
		float planeSizeZ = 10f;

		// Figure out texture-to-mesh width based on user set texture-to-mesh height
		float textureToMeshX = ((float)this.texture.width/this.texture.height)*this.textureToMeshZ;

		MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterials[materialIndex].mainTextureScale = new Vector2(planeSizeX*gameObject.transform.lossyScale.x/textureToMeshX, planeSizeZ*gameObject.transform.lossyScale.z/textureToMeshZ);
		meshRenderer.sharedMaterials[materialIndex].mainTextureOffset = new Vector2(offsetX, offsetY);
		offsetXCpy = offsetX;
		offsetYCpy = offsetY;
	}
}
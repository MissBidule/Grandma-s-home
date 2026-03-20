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

	Vector3 prevScale = Vector3.one;
	float prevTextureToMeshZ = -1f;

	// Use this for initialization
	void Start () {
		this.prevScale = gameObject.transform.lossyScale;
		this.prevTextureToMeshZ = this.textureToMeshZ;
        
        RefreshMaterial();

		this.UpdateTiling();
	}

	void RefreshMaterial()
	{
		MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        var tempMaterial = new Material(originalMaterial);
        renderer.sharedMaterial = tempMaterial;
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
			this.UpdateTiling();

		// Maintain previous state variables
		this.prevScale = gameObject.transform.lossyScale;
		this.prevTextureToMeshZ = this.textureToMeshZ;
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
		meshRenderer.sharedMaterial.mainTextureScale = new Vector2(planeSizeX*gameObject.transform.lossyScale.x/textureToMeshX, planeSizeZ*gameObject.transform.lossyScale.z/textureToMeshZ);
		meshRenderer.sharedMaterial.mainTextureOffset = new Vector2(offsetX, offsetY);
		offsetXCpy = offsetX;
		offsetYCpy = offsetY;
	}
}
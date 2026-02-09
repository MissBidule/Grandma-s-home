using System.Collections.Generic;
using System.Linq;
using LineworkLite.FreeOutline;
using UnityEditor;
using UnityEngine;

namespace LineworkLite.Editor.FreeOutline
{
    public class SmoothNormalsMeshImporter : AssetPostprocessor
    {
        public void OnPostprocessModel(GameObject gameObject)
        {
            var smoothNormals = AssetDatabase.GetLabels(assetImporter).Any(label => label.Contains(FreeOutlineUtils.SmoothNormalsLabel));

            var meshes = GetMeshesForGameobject(gameObject);

            foreach (var mesh in meshes)
            {
                if (smoothNormals)
                {
                    var uvs = SmoothNormalsBaker.ComputeSmoothedNormals(mesh);
                    if (uvs != null && uvs.Length > 0)
                    {
                        mesh.SetUVs(7, uvs);
                        Debug.Log($"Set UV8 for mesh {mesh.name} with {uvs.Length} entries.");
                        foreach (var uv in uvs)
                        {
                            Debug.Log(uv);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Smoothed normals not computed for mesh {mesh.name}.");
                    }
                }
                else
                {
                    mesh.uv8 = null;
                }
            }
        }

        private static List<Mesh> GetMeshesForGameobject(GameObject gameObject)
        {
            var meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            var meshes = meshFilters.Select(item => item.sharedMesh).ToList();
            var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            meshes.AddRange(skinnedMeshRenderers.Select(item => item.sharedMesh));
            return meshes;
        }
    }
}

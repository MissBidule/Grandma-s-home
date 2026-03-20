using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Antony
{
    [ExecuteAlways]
    public class PlaydoughShaderManager : MonoBehaviour
    {
        private static readonly int s_normalID = Shader.PropertyToID("_Playdough_CurrentNormalMap");
        private static readonly int s_heightID = Shader.PropertyToID("_Playdough_CurrentHeightMap");
        private static readonly int s_textureID = Shader.PropertyToID("_Playdough_CurrentTexture");
        private static readonly int s_roughnessID = Shader.PropertyToID("_Playdough_Roughness");
        private static readonly int s_metalnessID = Shader.PropertyToID("_Playdough_Metallic");
        private static readonly int s_normalStrengthID = Shader.PropertyToID("_Playdough_NormalStrength");
        private static readonly int s_normalTilingID = Shader.PropertyToID("_Playdough_NormalTiling");
        private static readonly int s_normalOffsetID = Shader.PropertyToID("_Playdough_NormalOffset");
        private static readonly int s_maxLODDistanceID = Shader.PropertyToID("_Playdough_MaxLODDistance");

        [Tooltip("Height maps used for vertex displacement\nDirectly changes the mesh shape")]
        [SerializeField] private Texture2D[] heightMaps = null;

        [Tooltip("Normal maps used to simulate surface detail\nAdds visual depth without increasing geometry")]
        [SerializeField] private Texture2D[] normalMaps = null;

        [Tooltip("Albedo (color) textures that define the surface appearance\nPurely visual, no physical deformation")]
        [SerializeField] private Texture2D[] textures = null;

        [Tooltip("Controls how often the normal map repeats across the surface\nHigher values = more repetition")]
        [SerializeField] private Vector2 normalTiling = Vector2.one;

        [Tooltip("Offsets the starting position of the normal map texture")]
        [SerializeField] private Vector2 normalOffset = Vector2.zero;

        [SerializeField] [Min(0f)]
        [Tooltip("Time (in seconds) before switching to the next texture\nAffects height maps, normal maps, and textures")]
        private float cycleInterval = 0.333f;

        [SerializeField] [Range(0f, 1f)]
        [Tooltip("Surface roughness\nLower values look smoother and shinier\nHigher values look drier and more matte")]
        private float roughness = 0.6f;

        [SerializeField] [Range(0f, 1f)]
        [Tooltip("Surface metalness\nLower values look dull or matte\nHigher values appear more metallic and reflective")]
        private float metalness = 0f;

        [SerializeField] [Min(1f)]
        [Tooltip("Maximum distance between the objects and the camera for the object to be rendered at full resolution")]
        private float maxLODDistance = 20f;

        [SerializeField] [Range(-10f, 10f)]
        [Tooltip("Strength of the normal map effect\nNegative values invert the normals")]
        private float normalStrength = 1f;

        [Tooltip("If enabled, height maps are chosen randomly\nIf disabled, they cycle in array order")]
        [SerializeField] private bool cycleHeightMapsRandomly = true;

        [Tooltip("If enabled, normal maps are chosen randomly\nIf disabled, they cycle in array order")]
        [SerializeField] private bool cycleNormalMapsRandomly = true;

        [Tooltip("If enabled, textures are chosen randomly\nIf disabled, they cycle in array order")]
        [SerializeField] private bool cycleTexturesRandomly = true;

        private Texture2D currentHeightMap = null;
        private Texture2D currentNormalMap = null;
        private Texture2D currentTexture = null;
        private List<Color> renderersColours = new List<Color>();
        private Vector2 normalTilingCpy = Vector2.one;
        private Vector2 normalOffsetCpy = Vector2.zero;
        private double lastTime = 0.0;
        private float deltaTime = 0f;
        private float changeTimer = 0f;
        private float roughnessCpy = 0f;
        private float metalnessCpy = 0f;
        private float normalStrengthCpy = 0f;
        private float maxLODDistanceCpy = 0f;
        private int heightMapIndex = 0;
        private int normalMapIndex = 0;
        private int textureIndex = 0;

        private void Awake()
        {
            ApplyNormalMap(0);
            ApplyHeightMap(0);
            ApplyTexture(0);

            // This is to prevent a bug where normals don't work correctly
            // Changing this value specifically resoves the bug (6000.3.0f1 LTS)
            Shader.SetGlobalVector(s_normalTilingID, normalTiling);
        }

        private void Update()
        {
            DetectCorrectTime();
            changeTimer += deltaTime;

            if (changeTimer < cycleInterval)
                return;

            if (normalMaps != null && normalMaps.Length > 0)
            {
                normalMapIndex = GetNextIndex(
                    normalMapIndex,
                    normalMaps.Length,
                    cycleNormalMapsRandomly
                );
                ApplyNormalMap(normalMapIndex);
            }

            if (heightMaps != null && heightMaps.Length > 0)
            {
                heightMapIndex = GetNextIndex(
                    heightMapIndex,
                    heightMaps.Length,
                    cycleHeightMapsRandomly
                );
                ApplyHeightMap(heightMapIndex);
            }

            if (textures != null && textures.Length > 0)
            {
                textureIndex = GetNextIndex(
                    textureIndex,
                    textures.Length,
                    cycleTexturesRandomly
                );
                ApplyTexture(textureIndex);
            }

            changeTimer = 0f;

            if (roughnessCpy != roughness)
            {
                Shader.SetGlobalFloat(s_roughnessID, roughness);
                roughnessCpy = roughness;
            }

            if (metalnessCpy != metalness)
            {
                Shader.SetGlobalFloat(s_metalnessID, metalness);
                metalnessCpy = metalness;
            }

            if (normalTilingCpy != normalTiling)
            {
                Shader.SetGlobalVector(s_normalTilingID, normalTiling);
                normalTilingCpy = normalTiling;
            }

            if (normalStrengthCpy != normalStrength)
            {
                Shader.SetGlobalFloat(s_normalStrengthID, normalStrength);
                normalStrengthCpy = normalStrength;
            }

            if (maxLODDistanceCpy != maxLODDistance)
            {
                Shader.SetGlobalFloat(s_maxLODDistanceID, maxLODDistance);
                maxLODDistanceCpy = maxLODDistance;
            }

            if (normalOffsetCpy != normalOffset)
            {
                Shader.SetGlobalVector(s_normalOffsetID, normalOffset);
                normalOffsetCpy = normalOffset;
            }
        }


        private int GetNextIndex(in int _currentIndex, in int _arraySize, in bool _random)
        {
            if (_arraySize <= 1)
                return _currentIndex;

            int increment = _random
                ? Random.Range(1, _arraySize)
                : 1;

            return (_currentIndex + increment) % _arraySize;
        }


        private void ApplyNormalMap(in int _index)
        {
            if (normalMaps == null || normalMaps.Length == 0)
                return;

            Texture2D texture = normalMaps[_index];
            if (texture == currentNormalMap)
                return;

            currentNormalMap = texture;
            Shader.SetGlobalTexture(s_normalID, texture);
        }
        private void ApplyHeightMap(in int _index)
        {
            if (heightMaps == null || heightMaps.Length == 0)
                return;

            Texture2D texture = heightMaps[_index];
            if (texture == currentHeightMap)
                return;

            currentHeightMap = texture;
            Shader.SetGlobalTexture(s_heightID, texture);
        }
        private void ApplyTexture(in int _index)
        {
            if (textures == null || textures.Length == 0)
                return;

            Texture2D texture = textures[_index];
            if (texture == currentTexture)
                return;

            currentTexture = texture;
            Shader.SetGlobalTexture(s_textureID, texture);
        }

        private void DetectCorrectTime()
        {
            if (Application.isPlaying)
            {
                deltaTime = Time.deltaTime;
                return;
            }

#if UNITY_EDITOR
            double now = EditorApplication.timeSinceStartup;
            deltaTime = (float)(now - lastTime);
            lastTime = now;
#endif
        }
    }
}

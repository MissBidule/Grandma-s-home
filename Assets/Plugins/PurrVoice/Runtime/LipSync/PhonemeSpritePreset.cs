using System.Collections.Generic;
using UnityEngine;

namespace PurrNet.Voice.LipSync
{
    [CreateAssetMenu(menuName = "PurrNet/Voice/Phoneme Sprite Preset")]
    public class PhonemeSpritePreset : ScriptableObject
    {
        [SerializeField] private Sprite _neutralSprite;
        [SerializeField] private PhonemeSprite[] _phonemeSprites;

        public Sprite neutralSprite => _neutralSprite;
        public IReadOnlyList<PhonemeSprite> sprites => _phonemeSprites;
    }
}

using UnityEngine;

namespace PurrNet.Voice.LipSync
{
    public class PurrLipSyncSprite : PurrLipSync
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private PhonemeSpritePreset _spritePreset;

        protected override void OnPhonemeChanged(string phoneme)
        {
            _spriteRenderer.sprite = GetSprite(phoneme);
        }

        private Sprite GetSprite(string phoneme)
        {
            var srites = _spritePreset.sprites;
            var c = srites.Count;

            for (var i = 0; i < c; i++)
            {
                if (srites[i].phoneme == phoneme)
                    return srites[i].sprite;
            }

            return _spritePreset.neutralSprite;
        }
    }
}

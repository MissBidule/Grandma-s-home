using System;
using PurrNet.Voice;
using UnityEngine;

public class NoiseGateFilterInstance : FilterInstance
{
    private readonly NoiseGateFilter _def;
    private float _gate = 0f;

    public NoiseGateFilterInstance(NoiseGateFilter def) => _def = def;

    public override void Process(ArraySegment<float> inputSamples, int frequency, float strength)
    {
        float rms = 0f;
        for (int i = 0; i < inputSamples.Count; i++)
        {
            float s = inputSamples.Array[inputSamples.Offset + i];
            rms += s * s;
        }
        rms = Mathf.Sqrt(rms / inputSamples.Count);
        float db = 20f * Mathf.Log10(Mathf.Max(rms, 1e-10f));
        bool gateOpen = db > _def.noiseGateThreshold;

        float deltaTime = inputSamples.Count / (float)frequency;

        if (gateOpen)
        {
            float attackTime = Mathf.Max(_def.gateAttackTime, 0.0001f);
            _gate = Mathf.Clamp01(_gate + (deltaTime / attackTime));
        }
        else
        {
            float releaseTime = Mathf.Max(_def.gateReleaseTime, 0.0001f);
            _gate = Mathf.Clamp01(_gate - (deltaTime / releaseTime));
        }

        float gain = _gate * strength;

        for (int i = 0; i < inputSamples.Count; i++)
            inputSamples.Array[inputSamples.Offset + i] *= gain;
    }
}



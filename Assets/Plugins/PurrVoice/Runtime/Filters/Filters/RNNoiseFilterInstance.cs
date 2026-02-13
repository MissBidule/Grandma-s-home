using System;
using System.Buffers;
using UnityEngine;

namespace PurrNet.Voice
{
    public class RNNoiseFilterInstance : FilterInstance
    {
        private readonly RNNoiseFilter _def;
        private readonly IntPtr _state;

        // Pre-allocated frame buffers for RNNoise (480 samples = 10ms at 48kHz)
        private readonly float[] _inFrame = new float[RNNoiseNative.FRAME_SIZE];
        private readonly float[] _outFrame = new float[RNNoiseNative.FRAME_SIZE];

        // Accumulation position in _inFrame for cross-call frame buffering
        private int _inPos;

        // Voice activity probability from the most recent processed frame
        private float _lastVad;

        private const float SCALE_UP = 32768f;
        private const float SCALE_DOWN = 1f / 32768f;

        /// <summary>
        /// Voice activity probability from the last processed frame (0.0 to 1.0).
        /// Useful for UI indicators or adaptive behavior.
        /// </summary>
        public float lastVad => _lastVad;

        public RNNoiseFilterInstance(RNNoiseFilter def)
        {
            _def = def;
            _state = RNNoiseNative.Create();

            if (_state == IntPtr.Zero)
                Debug.LogWarning("[PurrVoice] Failed to create RNNoise state. Noise suppression will be disabled.");
        }

        ~RNNoiseFilterInstance()
        {
            RNNoiseNative.Destroy(_state);
        }

        public override void Process(ArraySegment<float> inputSamples, int frequency, float strength)
        {
            if (_state == IntPtr.Zero || strength <= 0f)
                return;

            if (frequency == RNNoiseNative.SAMPLE_RATE)
                ProcessDirect(inputSamples, strength);
            else
                ProcessResampled(inputSamples, frequency, strength);
        }

        /// <summary>
        /// Processes audio already at 48kHz. Maintains frame buffering across calls
        /// so RNNoise always gets complete 480-sample frames.
        /// </summary>
        private void ProcessDirect(ArraySegment<float> samples, float strength)
        {
            float[] arr = samples.Array;
            int off = samples.Offset;
            int count = samples.Count;
            int i = 0;

            // Tracks how many samples in the current frame came from THIS call
            // (as opposed to leftover from a previous call). We can only write back
            // processed results for samples from the current ArraySegment.
            int currentChunkSamples = 0;

            while (i < count)
            {
                int needed = RNNoiseNative.FRAME_SIZE - _inPos;
                int available = count - i;
                int toCopy = Math.Min(needed, available);

                // Buffer input samples scaled to 16-bit range for RNNoise
                for (int j = 0; j < toCopy; j++)
                    _inFrame[_inPos + j] = arr[off + i + j] * SCALE_UP;

                _inPos += toCopy;
                currentChunkSamples += toCopy;
                i += toCopy;

                if (_inPos >= RNNoiseNative.FRAME_SIZE)
                {
                    _lastVad = RNNoiseNative.ProcessFrame(_state, _outFrame, _inFrame);

                    bool voiced = _lastVad >= _def.vadThreshold;

                    // Write back only the portion that belongs to the current ArraySegment
                    int writeStart = off + i - currentChunkSamples;
                    int frameOffset = RNNoiseNative.FRAME_SIZE - currentChunkSamples;

                    for (int j = 0; j < currentChunkSamples; j++)
                    {
                        float clean = voiced ? _outFrame[frameOffset + j] * SCALE_DOWN : 0f;
                        float original = arr[writeStart + j];
                        arr[writeStart + j] = Mathf.Lerp(original, clean, strength);
                    }

                    _inPos = 0;
                    currentChunkSamples = 0;
                }
            }
        }

        /// <summary>
        /// Processes audio at a non-48kHz sample rate by resampling to 48kHz,
        /// running through RNNoise, and resampling back. Uses ArrayPool to avoid GC.
        /// </summary>
        private void ProcessResampled(ArraySegment<float> inputSamples, int frequency, float strength)
        {
            float[] arr = inputSamples.Array;
            int off = inputSamples.Offset;
            int inCount = inputSamples.Count;

            // Calculate resampled length at 48kHz
            int resampledCount = (int)Math.Ceiling(inCount * (double)RNNoiseNative.SAMPLE_RATE / frequency);

            // Rent a temporary buffer from the shared pool (no GC allocation)
            var resampled = ArrayPool<float>.Shared.Rent(resampledCount);

            try
            {
                // Upsample to 48kHz via linear interpolation
                float srcRatio = frequency / (float)RNNoiseNative.SAMPLE_RATE;
                for (int i = 0; i < resampledCount; i++)
                {
                    float srcPos = i * srcRatio;
                    int s0 = (int)srcPos;
                    int s1 = Math.Min(s0 + 1, inCount - 1);
                    float frac = srcPos - s0;
                    resampled[i] = arr[off + s0] * (1f - frac) + arr[off + s1] * frac;
                }

                // Process complete 480-sample frames through RNNoise
                int pos = 0;
                while (pos + RNNoiseNative.FRAME_SIZE <= resampledCount)
                {
                    for (int j = 0; j < RNNoiseNative.FRAME_SIZE; j++)
                        _inFrame[j] = resampled[pos + j] * SCALE_UP;

                    _lastVad = RNNoiseNative.ProcessFrame(_state, _outFrame, _inFrame);

                    bool voiced = _lastVad >= _def.vadThreshold;

                    for (int j = 0; j < RNNoiseNative.FRAME_SIZE; j++)
                        resampled[pos + j] = voiced ? _outFrame[j] * SCALE_DOWN : 0f;

                    pos += RNNoiseNative.FRAME_SIZE;
                }
                // Any tail samples (< 480) remain as original values in the resampled buffer

                // Downsample back to original frequency and blend with original signal
                float dstRatio = RNNoiseNative.SAMPLE_RATE / (float)frequency;
                for (int i = 0; i < inCount; i++)
                {
                    float srcPos = i * dstRatio;
                    int s0 = (int)srcPos;
                    int s1 = Math.Min(s0 + 1, resampledCount - 1);
                    float frac = srcPos - s0;
                    float clean = resampled[s0] * (1f - frac) + resampled[s1] * frac;
                    arr[off + i] = Mathf.Lerp(arr[off + i], clean, strength);
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(resampled);
            }
        }
    }
}

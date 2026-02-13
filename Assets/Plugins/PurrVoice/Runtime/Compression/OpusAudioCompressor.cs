using System;
using System.Collections.Generic;

namespace PurrNet.Voice
{
    public static class OpusAudioCompressor
    {
        private static readonly Dictionary<int, OpusCodec> _codecs = new();

        public static byte[] Encode(float[] input, int sampleRate, int frameSize)
        {
            if (!_codecs.TryGetValue(sampleRate, out var codec))
            {
                codec = new OpusCodec(sampleRate, 1, frameSize);
                _codecs[sampleRate] = codec;
            }

            return codec.Encode(input);
        }

        public static float[] Decode(byte[] data, int sampleRate, int frameSize)
        {
            if (!_codecs.TryGetValue(sampleRate, out var codec))
            {
                codec = new OpusCodec(sampleRate, 1, frameSize);
                _codecs[sampleRate] = codec;
            }

            return codec.Decode(data);
        }
    }
}
using Concentus.Enums;
using System;
using Concentus;

namespace PurrNet.Voice
{
    public class OpusCodec
    {
        private readonly IOpusEncoder _encoder;
        private readonly IOpusDecoder _decoder;
        private readonly int _sampleRate;
        private readonly int _channels;
        private readonly int _frameSize;
        public int SampleRate => _sampleRate;

        public OpusCodec(int sampleRate, int channels, int frameSize)
        {
            _sampleRate = sampleRate;
            _channels = channels;
            _frameSize = frameSize;
            _encoder = OpusCodecFactory.CreateEncoder(sampleRate, channels, OpusApplication.OPUS_APPLICATION_VOIP);
            _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);
        }

        public byte[] Encode(float[] input)
        {
            short[] shortInput = new short[input.Length];
            for (int i = 0; i < input.Length; i++)
                shortInput[i] = (short)(Math.Clamp(input[i], -1f, 1f) * short.MaxValue);

            byte[] output = new byte[4000];
            int len = _encoder.Encode(shortInput.AsSpan(), _frameSize, output.AsSpan(), output.Length);
            Array.Resize(ref output, len);
            return output;
        }

        public float[] Decode(byte[] data)
        {
            short[] shortOutput = new short[_frameSize * _channels];
            int len = _decoder.Decode(data.AsSpan(), shortOutput.AsSpan(), _frameSize, false);
    
            float[] output = new float[len];
            for (int i = 0; i < len; i++)
                output[i] = shortOutput[i] / (float)short.MaxValue;

            return output;
        }
    }
}
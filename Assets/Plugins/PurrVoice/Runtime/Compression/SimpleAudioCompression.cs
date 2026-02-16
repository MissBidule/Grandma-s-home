using System;
using PurrNet.Packing;
using UnityEngine;

namespace PurrNet.Voice
{
    public static class SimpleAudioCompression
    {
        public static BitPacker Compress(float[] input)
        {
            var packer = BitPackerPool.Get();
            Packer<int>.Write(packer, (int)input.Length);
            short last = 0;

            for (int i = 0; i < input.Length; i++)
            {
                short current = Quantize(input[i]);
                DeltaPacker<PackedShort>.Write(packer, last, current);
                last = current;
            }

            return packer;
        }

        public static float[] Decompress(BitPacker packer)
        {
            var length = Packer<int>.Read(packer);
            var output = new float[length];
            short last = 0;

            for (int i = 0; i < length; i++)
            {
                PackedShort current = default;
                DeltaPacker<PackedShort>.Read(packer, last, ref current);
                output[i] = Dequantize(current);
                last = current;
            }

            return output;
        }

        static short Quantize(float f) => (short)Math.Clamp(MathF.Round(f * 32767f), short.MinValue, short.MaxValue);
        static float Dequantize(short s) => s / 32767f;
    }
}

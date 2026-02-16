using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PurrNet.Voice
{
    /// <summary>
    /// P/Invoke wrapper for the RNNoise native library.
    /// RNNoise processes audio exclusively at 48kHz in 480-sample frames (10ms).
    /// Samples must be in [-32768, 32768] range (16-bit PCM as float).
    /// 
    /// The native library must be built from https://github.com/xiph/rnnoise
    /// and placed in the appropriate Plugins folder for your target platform.
    /// </summary>
    public static class RNNoiseNative
    {
        private const string LIB_NAME = "rnnoise";

        /// <summary>
        /// RNNoise operates at 48000 Hz exclusively.
        /// </summary>
        public const int SAMPLE_RATE = 48000;

        /// <summary>
        /// RNNoise frame size in samples (10ms at 48kHz).
        /// </summary>
        public const int FRAME_SIZE = 480;

        private static bool? _isAvailable;

        /// <summary>
        /// Whether the native RNNoise library is loaded and available.
        /// Checked once on first access, then cached.
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                if (_isAvailable.HasValue)
                    return _isAvailable.Value;

                try
                {
                    rnnoise_get_size();
                    _isAvailable = true;
                }
                catch (DllNotFoundException)
                {
                    _isAvailable = false;
                    Debug.LogWarning(
                        "[PurrVoice] RNNoise native library not found. " +
                        "Noise suppression filter will pass audio through unchanged. " +
                        "Build the rnnoise native plugin from: https://github.com/xiph/rnnoise");
                }

                return _isAvailable.Value;
            }
        }

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr rnnoise_create(IntPtr model);

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rnnoise_destroy(IntPtr state);

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern float rnnoise_process_frame(
            IntPtr state,
            [Out] float[] output,
            [In] float[] input);

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rnnoise_get_size();

        /// <summary>
        /// Creates a new RNNoise denoiser state using the built-in model.
        /// Returns IntPtr.Zero if the native library is unavailable.
        /// </summary>
        public static IntPtr Create()
        {
            if (!IsAvailable) return IntPtr.Zero;
            return rnnoise_create(IntPtr.Zero);
        }

        /// <summary>
        /// Destroys an RNNoise denoiser state and frees native memory.
        /// Safe to call with IntPtr.Zero.
        /// </summary>
        public static void Destroy(IntPtr state)
        {
            if (state != IntPtr.Zero && IsAvailable)
                rnnoise_destroy(state);
        }

        /// <summary>
        /// Processes a single frame of 480 audio samples through the denoiser.
        /// Input/output samples must be in [-32768, 32768] range.
        /// Returns the voice activity probability (0.0 = noise, 1.0 = voice).
        /// </summary>
        public static float ProcessFrame(IntPtr state, float[] output, float[] input)
        {
            if (state == IntPtr.Zero) return 0f;
            return rnnoise_process_frame(state, output, input);
        }
    }
}

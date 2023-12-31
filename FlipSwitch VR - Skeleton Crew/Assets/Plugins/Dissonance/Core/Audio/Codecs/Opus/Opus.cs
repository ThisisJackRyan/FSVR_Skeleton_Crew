﻿using System;
using System.Runtime.InteropServices;
using Dissonance.Extensions;
using Dissonance.Threading;

namespace Dissonance.Audio.Codecs.Opus
{
    internal static class BandwidthExtensions
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(BandwidthExtensions).Name);

        public static int SampleRate(this OpusNative.Bandwidth bandwidth)
        {
            switch (bandwidth)
            {
                case OpusNative.Bandwidth.Narrowband:
                    return 8000;
                case OpusNative.Bandwidth.Mediumband:
                    return 12000;
                case OpusNative.Bandwidth.Wideband:
                    return 16000;
                case OpusNative.Bandwidth.SuperWideband:
                    return 24000;
                case OpusNative.Bandwidth.Fullband:
                    return 48000;
                default:
                    throw new ArgumentOutOfRangeException("bandwidth", Log.PossibleBugMessage(string.Format("{0} is not a valid value", bandwidth), "B534C9B2-6A9B-455E-875E-A01D93B278C8"));
            }
        }
    }

    internal class OpusNative
    {
        /// <summary>
        /// Wraps the Opus API.
        /// </summary>
        private static class OpusNativeMethods
        {
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern IntPtr opus_encoder_create(int samplingRate, int channels, int application, out int error);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern void opus_encoder_destroy(IntPtr encoder);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern int opus_encode_float(IntPtr encoder, IntPtr floatPcm, int frameSize, IntPtr byteEncoded, int maxEncodedLength);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern IntPtr opus_decoder_create(int samplingRate, int channels, out int error);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern IntPtr opus_decoder_destroy(IntPtr decoder);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern int opus_decode_float(IntPtr decoder, IntPtr byteData, int dataLength, IntPtr floatPcm, int frameSize, bool decodeFEC);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int dissonance_opus_decoder_ctl_out(IntPtr st, Ctl request, out int value);

            internal static int opus_decoder_ctl(IntPtr st, Ctl request, out int value)
            {
                return dissonance_opus_decoder_ctl_out(st, request, out value);
            }
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int opus_decoder_ctl(IntPtr st, Ctl request, out int value);
#endif

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int dissonance_opus_encoder_ctl_out(IntPtr st, Ctl request, out int value);

            internal static int opus_encoder_ctl(IntPtr st, Ctl request, out int value)
            {
                return dissonance_opus_encoder_ctl_out(st, request, out value);
            }
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int opus_encoder_ctl(IntPtr st, Ctl request, out int value);
#endif

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int dissonance_opus_encoder_ctl_in(IntPtr st, Ctl request, int value);

            internal static int opus_encoder_ctl(IntPtr st, Ctl request, int value)
            {
                return dissonance_opus_encoder_ctl_in(st, request, value);
            }
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int opus_encoder_ctl(IntPtr st, Ctl request, int value);
#endif

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern void opus_pcm_soft_clip(IntPtr pcm, int frameSize, int channels, float[] softClipMem);
        }

        private enum Ctl
        {
            SetBitrateRequest = 4002,
            GetBitrateRequest = 4003,

            SetInbandFECRequest = 4012,
            GetInbandFECRequest = 4013,

            SetPacketLossPercRequest = 4014,
            GetPacketLossPercRequest = 4015,

            ResetState = 4028,
        }

        public enum Bandwidth
        {
            /// <summary>
            /// 4KHz bandwidth (8KHz sample rate)
            /// </summary>
            Narrowband = 1101,

            /// <summary>
            /// 6Khz bandwidth (12KHz sample rate)
            /// </summary>
            Mediumband = 1102,

            /// <summary>
            /// 8Khz bandwidth (16KHz sample rate)
            /// </summary>
            Wideband = 1103,

            /// <summary>
            /// 12Khz (24KHz sample rate)
            /// </summary>
            SuperWideband = 1104,

            /// <summary>
            /// 20Khz (48KHz sample rate)
            /// </summary>
            Fullband = 1105
        }

        /// <summary>
        /// Supported coding modes.
        /// </summary>
        private enum Application
        {
            // ReSharper disable UnusedMember.Local (Justification passed in and out of opus)

            /// <summary>
            /// Best for most VoIP/videoconference applications where listening quality and intelligibility matter most.
            /// </summary>
            Voip = 2048,

            /// <summary>
            /// Best for broadcast/high-fidelity application where the decoded audio should be as close as possible to input.
            /// </summary>
            Audio = 2049,

            /// <summary>
            /// Only use when lowest-achievable latency is what matters most. Voice-optimized modes cannot be used.
            /// </summary>
            RestrictedLowLatency = 2051

            // ReSharper restore UnusedMember.Local
        }

        private enum OpusErrors
        {
            // ReSharper disable UnusedMember.Local (Justification: Cast from an int returned from opus)

            /// <summary>
            /// No error.
            /// </summary>
            Ok = 0,

            /// <summary>
            /// One or more invalid/out of range arguments.
            /// </summary>
            BadArg = -1,

            /// <summary>
            /// The mode struct passed is invalid.
            /// </summary>
            BufferToSmall = -2,

            /// <summary>
            /// An internal error was detected.
            /// </summary>
            InternalError = -3,

            /// <summary>
            /// The compressed data passed is corrupted.
            /// </summary>
            InvalidPacket = -4,

            /// <summary>
            /// Invalid/unsupported request number.
            /// </summary>
            Unimplemented = -5,

            /// <summary>
            /// An encoder or decoder structure is invalid or already freed.
            /// </summary>
            InvalidState = -6,

            /// <summary>
            /// Memory allocation has failed.
            /// </summary>
            AllocFail = -7

            // ReSharper restore UnusedMember.Local
        }

        public class OpusException : Exception
        {
            public OpusException(string message)
                : base(message)
            {
            }
        }

        /// <summary>
        /// Opus encoder.
        /// </summary>
        public sealed class OpusEncoder : IDisposable
        {
            private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof(OpusEncoder).Name);

            #region fields and properties
            private readonly LockedValue<IntPtr> _encoder;

            /// <summary>
            /// Permitted frame sizes in ms.
            /// </summary>
            private static readonly float[] PermittedFrameSizesMs = {
                2.5f, 5, 10, 20, 40, 60
            };

            /// <summary>
            /// Permitted frame sizes in samples per channel.
            /// </summary>
            public int[] PermittedFrameSizes { get; private set; }

            /// <summary>
            /// Gets or sets the bitrate setting of the encoding.
            /// </summary>
            public int Bitrate
            {
                get
                {
                    int bitrate;
                    OpusCtlOut(Ctl.GetBitrateRequest, out bitrate);
                    return bitrate;
                }
                set
                {
                    OpusCtlIn(Ctl.SetBitrateRequest, value);
                }
            }

            /// <summary>
            /// Gets or sets if Forward Error Correction encoding is enabled.
            /// </summary>
            public bool EnableForwardErrorCorrection
            {
                get
                {
                    int fec;
                    OpusCtlOut(Ctl.GetInbandFECRequest, out fec);
                    return fec > 0;
                }
                set
                {
                    OpusCtlIn(Ctl.SetInbandFECRequest, Convert.ToInt32(value));
                }
            }

            private int _packetLoss;

            /// <summary>
            /// Get or set expected packet loss percentage (0 to 1)
            /// </summary>
            public float PacketLoss
            {
                get
                {
                    int lossrate;
                    OpusCtlOut(Ctl.GetPacketLossPercRequest, out lossrate);
                    return lossrate / 100f;
                }
                set
                {
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException("value", Log.PossibleBugMessage(string.Format("Packet loss percentage must be 0 <= {0} <= 1", value), "CFDF590D-C61A-4BB4-BB2D-1FAC1E59C114"));

                    //Don't do anything if the value hasn't changed
                    var newValue = (int)(value * 100);
                    if (_packetLoss == newValue)
                        return;
                    _packetLoss = newValue;

                    //Set the value into the encoder
                    OpusCtlIn(Ctl.SetPacketLossPercRequest, _packetLoss);
                }
            }
            #endregion

            #region constructors
            /// <summary>
            /// Creates a new Opus encoder.
            /// </summary>
            /// <param name="srcSamplingRate">The sampling rate of the input stream.</param>
            /// <param name="srcChannelCount">The number of channels in the input stream.</param>
            public OpusEncoder(int srcSamplingRate, int srcChannelCount)
            {
                if (srcSamplingRate != 8000 && srcSamplingRate != 12000 && srcSamplingRate != 16000 && srcSamplingRate != 24000 && srcSamplingRate != 48000)
                    throw new ArgumentOutOfRangeException("srcSamplingRate", Log.PossibleBugMessage("sample rate must be one of the valid values", "3F2C6D2D-338E-495E-8970-42A3C98243A5"));
                if (srcChannelCount != 1 && srcChannelCount != 2)
                    throw new ArgumentOutOfRangeException("srcChannelCount", Log.PossibleBugMessage("channel count must be 1 or 2", "8FE1EC0F-09E0-4CE6-AFD7-04199202D45D"));

                int error;
                var encoder = OpusNativeMethods.opus_encoder_create(srcSamplingRate, srcChannelCount, (int)Application.Voip, out error);
                if ((OpusErrors)error != OpusErrors.Ok)
                    throw new OpusException(Log.PossibleBugMessage(string.Format("Exception occured while creating encoder: {0}", (OpusErrors)error), "D77ECA73-413F-40D1-8427-CFD8A59CD5F6"));
                _encoder = new LockedValue<IntPtr>(encoder);

                PermittedFrameSizes = new int[PermittedFrameSizesMs.Length];
                for (var i = 0; i < PermittedFrameSizesMs.Length; i++)
                    PermittedFrameSizes[i] = (int)(srcSamplingRate / 1000f * PermittedFrameSizesMs[i]);
            }
            #endregion

            /// <summary>
            /// Encode audio samples.
            /// </summary>
            /// <returns>The total number of bytes written to dstOutputBuffer.</returns>
            public int EncodeFloats(ArraySegment<float> sourcePcm, ArraySegment<byte> dstEncoded)
            {
                if (sourcePcm.Array == null)
                    throw new ArgumentNullException("sourcePcm", Log.PossibleBugMessage("source pcm must not be null", "58AE3110-8F9A-4C36-9520-B7F3383096EC"));

                if (dstEncoded.Array == null)
                    throw new ArgumentNullException("dstEncoded", Log.PossibleBugMessage("destination must not be null", "36C327BB-A128-400D-AFB3-FF760A1562C1"));

                if (Array.IndexOf(PermittedFrameSizes, sourcePcm.Count) == -1)
                    throw new ArgumentException(Log.PossibleBugMessage(string.Format("Incorrect frame size '{0}'", sourcePcm.Count), "6AFD9ADF-1D15-4197-99E9-5A19ECB8CD20"), "sourcePcm");

                int encodedLen;
                using (var encoder = _encoder.Lock())
                {
                    if (encoder.Value == IntPtr.Zero)
                        throw new DissonanceException(Log.PossibleBugMessage("Attempted to access a null Opus encoder", "647001C3-39BB-418D-99EF-1D66B8EA633C"));

                    using (var srcHandle = sourcePcm.Pin())
                    using (var dstHandle = dstEncoded.Pin())
                    {
                        encodedLen = OpusNativeMethods.opus_encode_float(encoder.Value, srcHandle.Ptr, sourcePcm.Count, dstHandle.Ptr, dstEncoded.Count);
                    }
                }

                if (encodedLen < 0)
                    throw new OpusException(Log.PossibleBugMessage(string.Format("Encoding failed: {0}", (OpusErrors)encodedLen), "9C923F57-146B-47CB-8EEE-5BF129FA3124"));

                return encodedLen;
            }

            public void Reset()
            {
                using (var encoder = _encoder.Lock())
                {
                    if (encoder.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted to access a null Opus encoder", "A86D13A5-FC58-446C-9522-DDD9D199DFA6");

                    //Wtf is going on here?
                    //Reset takes no args, returns no values, and cannot fail. So we're passing in nothing, outputting nothing, and ignoring the return code
                    int _;
                    OpusNativeMethods.opus_encoder_ctl(encoder.Value, Ctl.ResetState, out _);
                }
            }

            private int OpusCtlIn(Ctl ctl, int value)
            {
                int ret;
                using (var encoder = _encoder.Lock())
                {
                    if (encoder.Value == IntPtr.Zero)
                        throw new ObjectDisposedException("OpusEncoder", Log.PossibleBugMessage("trying to use decoder after is has been disposed", "10A3BFFB-EC3B-4664-B06C-D5D42F75FE42"));

                    ret = OpusNativeMethods.opus_encoder_ctl(encoder.Value, ctl, value);
                }

                if (ret < 0)
                    throw new Exception(Log.PossibleBugMessage(string.Format("Encoder error (Ctl {0}): {1}", ctl, (OpusErrors)ret), "4AAA9AA6-8429-4346-B939-D113206FFBA8"));

                return ret;
            }

            private int OpusCtlOut(Ctl ctl, out int value)
            {
                int ret;
                using (var encoder = _encoder.Lock())
                {
                    if (encoder.Value == IntPtr.Zero)
                        throw new ObjectDisposedException("OpusEncoder", Log.PossibleBugMessage("trying to use decoder after is has been disposed", "10A3BFFB-EC3B-4664-B06C-D5D42F75FE42"));

                    ret = OpusNativeMethods.opus_encoder_ctl(encoder.Value, ctl, out value);
                }

                if (ret < 0)
                    throw new Exception(Log.PossibleBugMessage(string.Format("Encoder error (Ctl {0}): {1}", ctl, (OpusErrors)ret), "4AAA9AA6-8429-4346-B939-D113206FFBA8"));

                return ret;
            }

            #region disposal
            ~OpusEncoder()
            {
                Dispose();
            }

            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                GC.SuppressFinalize(this);

                using (var encoder = _encoder.Lock())
                {
                    if (encoder.Value != IntPtr.Zero)
                    {
                        OpusNativeMethods.opus_encoder_destroy(encoder.Value);
                        encoder.Value = IntPtr.Zero;
                    }
                }

                _disposed = true;
            }
            #endregion
        }

        /// <summary>
        /// Opus decoder.
        /// </summary>
        public sealed class OpusDecoder : IDisposable
        {
            private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(OpusDecoder).Name);

            #region field and properties
            private readonly LockedValue<IntPtr> _decoder;

            /// <summary>
            /// Gets or sets if Forward Error Correction decoding is enabled.
            /// </summary>
            public bool EnableForwardErrorCorrection { get; set; }
            #endregion

            #region constructors
            public OpusDecoder(int outputSampleRate, int outputChannelCount)
            {
                if (outputSampleRate != 8000 && outputSampleRate != 12000 && outputSampleRate != 16000 && outputSampleRate != 24000 && outputSampleRate != 48000)
                    throw new ArgumentOutOfRangeException("outputSampleRate", Log.PossibleBugMessage("sample rate must be one of the valid values", "548757DF-DC64-40C9-BEAD-9826B8245A7D"));
                if (outputChannelCount != 1 && outputChannelCount != 2)
                    throw new ArgumentOutOfRangeException("outputChannelCount", Log.PossibleBugMessage("channel count must be 1 or 2", "BA56610F-1FA3-4D68-9507-7B0DFA0E28AB"));

                int error;
                _decoder = new LockedValue<IntPtr>(OpusNativeMethods.opus_decoder_create(outputSampleRate, outputChannelCount, out error));
                if ((OpusErrors)error != OpusErrors.Ok)
                    throw new OpusException(Log.PossibleBugMessage(string.Format("Exception occured while creating decoder: {0}", (OpusErrors)error), "6E09F275-99A1-4CD6-A36A-FA093B146B29"));
            }
            #endregion

            #region disposal
            ~OpusDecoder()
            {
                Dispose();
            }

            private bool _disposed;
            public void Dispose()
            {
                if (_disposed)
                    return;

                GC.SuppressFinalize(this);

                using (var decoder = _decoder.Lock())
                {
                    if (decoder.Value != IntPtr.Zero)
                    {
                        OpusNativeMethods.opus_decoder_destroy(decoder.Value);
                        decoder.Value = IntPtr.Zero;
                    }
                }

                _disposed = true;
            }
            #endregion

            /// <summary>
            /// Decodes audio samples.
            /// </summary>
            /// <param name="srcEncodedBuffer">Encoded data (or null, to reconstruct a missing frame)</param>
            /// <param name="dstBuffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values starting at offset replaced with audio samples.</param>
            /// <returns>The number of floats decoded and written to dstBuffer.</returns>
            /// <remarks>Set srcEncodedBuffer to null to instruct the decoder that a packet was dropped.</remarks>
            public int DecodeFloats(ArraySegment<byte>? srcEncodedBuffer, ArraySegment<float> dstBuffer)
            {
                int length;
                using (var decoder = _decoder.Lock())
                {
                    if (decoder.Value == IntPtr.Zero)
                        throw new DissonanceException(Log.PossibleBugMessage("Attempted to access a null Opus decoder", "16261551-968B-44A8-80A1-E8DFB0109469"));

                    using (var dstHandle = dstBuffer.Pin())
                    {
                        if (srcEncodedBuffer.HasValue)
                        {
                            using (var srcHandle = srcEncodedBuffer.Value.Pin())
                            {
                                length = OpusNativeMethods.opus_decode_float(
                                    decoder.Value,
                                    srcHandle.Ptr,
                                    srcEncodedBuffer.Value.Count,
                                    dstHandle.Ptr,
                                    dstBuffer.Count,
                                    false
                                );
                            }
                        }
                        else
                        {
                            //Call decoder with null pointer and zero length to inform it of packet loss
                            length = OpusNativeMethods.opus_decode_float(
                                decoder.Value,
                                IntPtr.Zero,
                                0,
                                dstHandle.Ptr,
                                dstBuffer.Count,
                                EnableForwardErrorCorrection
                            );
                        }
                    }
                }

                if (length < 0)
                {
                    if (length == (int)OpusErrors.InvalidPacket)
                    {
                        if (!srcEncodedBuffer.HasValue)
                        {
                            throw new OpusException(Log.PossibleBugMessage("Decoding failed: InvalidPacket. 'null' ", "03BE7561-3BCC-4F41-A7CB-C80F03981267"));
                        }
                        else
                        {
                            // ReSharper disable once AssignNullToNotNullAttribute (Justification Array of the segment isn't null here)
                            throw new OpusException(Log.PossibleBugMessage(string.Format("Decoding failed: InvalidPacket. '{0}'", Convert.ToBase64String(srcEncodedBuffer.Value.Array, srcEncodedBuffer.Value.Offset, srcEncodedBuffer.Value.Count)), "EF4BC24C-491E-45D9-974C-FE5CB61BD54E"));
                        }
                    }
                    else
                    {
                        throw new OpusException(Log.PossibleBugMessage(string.Format("Decoding failed: {0} ", (OpusErrors)length), "A9C8EF2C-7830-4D8E-9D6E-EF0B9827E0A8"));
                    }
                }
                return length;
            }

            public void Reset()
            {
                using (var decoder = _decoder.Lock())
                {
                    //Reset takes no args, returns no values, and cannot fail. So we're passing in nothing, outputting nothing, and ignoring the return code
                    int _;
                    OpusNativeMethods.opus_decoder_ctl(decoder.Value, Ctl.ResetState, out _);
                }
            }
        }

        public sealed class OpusSoftClip
        {
            private readonly float[] _memory;

            public OpusSoftClip(int channels = 1)
            {
                if (channels < 0)
                    throw new ArgumentOutOfRangeException("channels", "Channels must be > 0");

                _memory = new float[channels];
            }

            public void Clip(ArraySegment<float> samples)
            {
#if !NCRUNCH
                using (var handle = samples.Pin())
                {

                    OpusNativeMethods.opus_pcm_soft_clip(
                        handle.Ptr,
                        samples.Count / _memory.Length,
                        _memory.Length,
                        _memory
                    );
                }
#endif
            }

            public void Reset()
            {
                Array.Clear(_memory, 0, _memory.Length);
            }
        }
    }
}

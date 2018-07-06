using System;

namespace Dissonance.Audio
{
    /// <summary>
    /// Helper methods to convert between various representations of an audio signal
    /// </summary>
    internal static class FormatConverter
    {
        #region 16 bit
        //These methods provide conversion from the -1 to 1 float range to the -32768 to 32767 int range
        //Note the positive and negative ranges are different sizes! We take the slightly smaller range to prevent clipping. This particular method
        //is also used by Apple, ALSA2, MatLab2, sndlib2.
        //When dividing that range by our conversation scale we convert into a range of [ -1, 0.999 ]. When converting back we get the original range.

        private const float ConversionScale = 0x8000;

        public static void ConvertInt16ToFloat32(ArraySegment<short> input, ArraySegment<float> output)
        {
            if (input.Count != output.Count)
                throw new InvalidOperationException("input and output do not have equal lengths");
            var count = input.Count;

            for (var i = 0; i < count; i++)
                output.Array[i + output.Offset] = input.Array[i + input.Offset] / ConversionScale;
        }

        public static void ConvertFloat32ToInt16(ArraySegment<float> input, ArraySegment<short> output)
        {
            if (input.Count != output.Count)
                throw new InvalidOperationException("input and output do not have equal lengths");
            var count = input.Count;

            for (var i = 0; i < count; i++)
            {
                var sample = input.Array[i + input.Offset];

                //Clip the sample into the allowable range
                short converted;
                if (sample >= 1.0f)
                    converted = short.MaxValue;
                else if (sample <= -1.0f)
                    converted = short.MinValue;
                else
                    converted = (short)(sample * ConversionScale);

                output.Array[i + output.Offset] = converted;
            }
        }
        #endregion
    }
}

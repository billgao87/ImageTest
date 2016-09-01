using System;
using System.IO;
using System.Linq;
using Common.Tracers;

namespace Common.Ultities
{
    /// <summary>
    /// 使用前请确保数据长度符合要求
    /// </summary>
    public static class BitConvertor
    {
        public static Tracer Tracer = new Tracer();

        public static ushort[] BytesToUshorts(byte[] bytes, int count)
        {
            return BytesToTs<ushort>(bytes, count, sizeof(ushort));
        }

        public static short[] BytesToShorts(byte[] bytes, int count)
        {
            return BytesToTs<short>(bytes, count, sizeof (short));
        }

        public static int[] BytesToInts(byte[] bytes, int count)
        {
            return BytesToTs<int>(bytes, count, sizeof(int));
        }

        public static uint[] BytesToUInts(byte[] bytes, int count)
        {
            return BytesToTs<uint>(bytes, count, sizeof(uint));
        }

        public static T[] BytesToTs<T>(byte[] bytes, int count, int byteSizeT)
        {
            try
            {
                T[] ts = new T[count];
                Buffer.BlockCopy(bytes, 0, ts, 0, count*byteSizeT);
                return ts;
            }
            catch (Exception e)
            {
                Tracer.Exception(e);
            }
            return null;
        }

        public static float GetFloat(ushort highOrderValue, ushort lowOrderValue)
        {
            byte[] value = BitConverter.GetBytes(lowOrderValue)
                .Concat(BitConverter.GetBytes(highOrderValue))
                .ToArray();

            return BitConverter.ToSingle(value, 0);
        }

        public static double GetDouble(ushort b3, ushort b2, ushort b1, ushort b0)
        {
            byte[] value = BitConverter.GetBytes(b0)
                .Concat(BitConverter.GetBytes(b1))
                .Concat(BitConverter.GetBytes(b2))
                .Concat(BitConverter.GetBytes(b3))
                .ToArray();

            return BitConverter.ToDouble(value, 0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace random_number_test
{
    public class RandomNumberGenerator
    {
        private const int StateLength = 74207281;
        private const int Lag = 9999621;

        private ulong[] State = new ulong[StateLength];
        private int StateIndex = -1;

        public RandomNumberGenerator() { }

        public void LoadState(Stream stream)
        {
            if (stream.Read(MemoryMarshal.Cast<ulong, byte>(State)) < StateLength * sizeof(ulong))
                throw new ArgumentException("stream is too short");
        }

        public ulong Next()
        {
            if (++StateIndex >= StateLength)
                StateIndex = 0;
            int laggedIndex = StateIndex - Lag;
            if (laggedIndex < 0)
                laggedIndex += StateLength;
            return State[StateIndex] = State[StateIndex] + State[laggedIndex];
        }

        public IEnumerable<char> NextChars()
        {
            while (true)
            {
                ulong r = Next();
                for (int i = 0; i < 11; i++)
                    yield return (char)('0' + Math.BigMul(r, 43, out r));
            }
        }
    }
}

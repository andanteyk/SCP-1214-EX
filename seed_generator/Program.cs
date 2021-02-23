using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace seed_generator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("generating state...");
            var state = GenerateState();

            Console.WriteLine("writing seed.dat...");
            using (var fileStream = new FileStream("seed.dat", FileMode.Create, FileAccess.Write, FileShare.Write))
            using (var compressedStream = new DeflateStream(fileStream, CompressionLevel.Optimal))
            {
                compressedStream.Write(MemoryMarshal.Cast<ulong, byte>(state));
            }
        }


        static ulong[] GenerateState()
        {
            const int StateLength = 74207281;
            const int Lag = 9999621;
            const ulong Max = 43ul * 43 * 43 * 43 * 43 * 43 * 43 * 43 * 43 * 43 * 43;

            var output = new ulong[StateLength];

            var rng = new Seiran128();

            int currentIndex = 0;
            int currentCacheIndex = 0;
            ulong cache = 0;
            void AddInt(int character) => Add((char)('0' + character));
            void Add(char character)
            {
                if ((uint)(character - '0') >= 43)
                    throw new ArgumentException("character out of range");
                if (currentIndex >= StateLength)
                    throw new ArgumentOutOfRangeException("state out of range");

                cache = cache * 43 + ((ulong)character - '0');
                if (++currentCacheIndex < 11)
                    return;
                // constants from 2^128 / pow(43,11)
                output[currentIndex] = 0x13 * (cache + 1) + Math.BigMul(0xd9ac2bf6_6cfed94a, cache + 1, out var _);

                // debug
                var reversed = Math.BigMul(output[currentIndex], Max, out var _);
                System.Diagnostics.Debug.Assert(reversed == cache);

                cache = 0;
                currentIndex++;
                currentCacheIndex = 0;
            }
            void AddString(string str)
            {
                for (int i = 0; i < str.Length; i++)
                    Add(str[i]);
            }
            void AddRepeatProgress(int character, double progress) => AddRepeat(character, 3 + (int)Math.Sqrt(rng.NextDouble() * 50 * 50 * progress));
            void AddRepeat(int character, int length)
            {
                for (int i = 0; i < length; i++)
                    AddInt(character);
            }
            void Flush()
            {
                for (; currentCacheIndex != 0;)
                    Add(';');
            }
            static double Lerp(double a, double b, double t) => a * t + b * (1 - t);
            static double LerpClamped(double a, double b, double t) { t = t < 0 ? 0 : t > 1 ? 1 : t; return a * t + b * (1 - t); }


            Console.WriteLine("Creating output data...");

            Console.WriteLine("  Phase 0...");
            for (int i = 0; i < 5000; i++)
            {
                output[currentIndex] = rng.Next(Max);
                currentIndex++;
            }

            Console.WriteLine("  Phase 1...");
            {
                int Phase1Length = 20000 + rng.NextInt(4000);

                int character = rng.NextCharacter();

                for (int phase1 = 0; phase1 < Phase1Length; phase1++)
                {
                    if (rng.NextDouble() < 0.001)
                        character = rng.NextCharacter();

                    if (rng.NextDouble() < 0.01 * phase1 / Phase1Length)
                    {
                        AddRepeatProgress(character, (double)phase1 / Phase1Length);
                    }
                    else if (rng.NextDouble() < 0.05)
                    {
                        AddInt(character);
                    }
                    else
                    {
                        AddInt(rng.NextCharacter());
                    }
                }
            }

            Console.WriteLine("  Phase 2...");
            {
                int Phase2Length = 50000 + rng.NextInt(50000);
                var words = new[] {
                    "PLEASE",
                    "MAKE",
                    "IT",
                    "STOP",
                    "NO",
                    "MORE",
                    "HURTS",
                };

                for (int phase2 = 0; phase2 < Phase2Length; phase2++)
                {
                    if (rng.NextDouble() < LerpClamped(0.02, 0.08, Math.Sin((double)phase2 / Phase2Length * Math.PI)))
                    {
                        AddString(words[rng.NextInt(words.Length)]);
                    }
                    else if (rng.NextDouble() < Lerp(0.001, 0.01, (double)phase2 / Phase2Length))
                    {
                        AddRepeatProgress(rng.NextCharacter(), rng.NextDouble());
                    }
                    else
                    {
                        AddInt(rng.NextCharacter());
                    }
                }
            }


            Console.WriteLine("  Phase 3...");
            {
                var removal = new HashSet<int>();

                while (currentIndex < StateLength - 50)
                {
                    if ((currentIndex & 0xff) == 0 && removal.Count < 43 * 0.5 && rng.NextDouble() < 0.001)
                    {
                        int removedCharacter = rng.NextCharacter();
                        removal.Add(removedCharacter);

                        Console.WriteLine($"    {currentIndex} {(char)('0' + removedCharacter)} was removed");
                    }

                    int character;
                    do
                    {
                        character = rng.NextCharacter();
                    } while (removal.Contains(character));

                    if (rng.NextDouble() < 0.05)
                    {
                        AddRepeatProgress(character, rng.NextDouble());
                    }
                    else
                    {
                        AddInt(character);
                    }

                    if ((currentIndex & 0xff) == 0 && rng.NextDouble() < 1e-6)
                    {
                        AddString("PLEASE:STOP=:::::::::::::::::::SIGNAL:TRACE::COMPLETE::WE::RESPOND::IN::KIND::::::::::::::::::::::::::::PLEASEPPLEASE");
                    }

                    if ((currentIndex & 0xfffff) == 0)
                        Console.Write($"    {(double)currentIndex / StateLength:p2} completed...\r");
                }
            }


            AddString(":::END:OF:STREAM:::END:OF:STREAM:::THIS:PROGRAM:IS:<SCP=1214=EX>;;;BASED:ON:<SCP=1214>:BY:<DRCARNAGE>:::THIS:PROGRAM:WAS:CREATED:BY:<ANDANTE>:::SEE:@ANDANTEYK:::END:OF:STREAM:::END:OF:STREAM:::");
            Flush();



            Console.WriteLine("Finalizing output data...");
            for (int index = StateLength - 1, lagged = index - Lag; index >= Lag; index--, lagged--)
                output[index] -= output[lagged];
            for (int index = Lag - 1, lagged = StateLength - 1; index >= 0; index--, lagged--)
                output[index] -= output[lagged];

            return output;
        }




        public class Seiran128
        {
            private ulong State0;
            private ulong State1;

            public Seiran128()
            {
                Span<ulong> state = stackalloc ulong[2];
                var stateBytes = MemoryMarshal.Cast<ulong, byte>(state);

                using (var csprng = new RNGCryptoServiceProvider())
                {
                    do
                    {
                        csprng.GetBytes(stateBytes);
                    } while (state[0] == 0 && state[1] == 0);
                }

                State0 = state[0];
                State1 = state[1];
            }

            public ulong Next()
            {
                static ulong rotl(ulong x, int k) => x << k | x >> -k;

                ulong s0 = State0, s1 = State1;
                ulong result = rotl((s0 + s1) * 9, 29) + s0;

                State0 = s0 ^ rotl(s1, 29);
                State1 = s0 ^ s1 << 9;

                return result;
            }

            public ulong Next(ulong max) => Math.BigMul(Next(), max, out var _);
            public int NextInt(int max) => max > 0 ? (int)Next((ulong)max) : throw new ArgumentOutOfRangeException(nameof(max));

            public int NextInt(int min, int max) => min < max ? (int)Next((ulong)(max - min)) + min : throw new ArgumentOutOfRangeException(nameof(min));

            public int NextCharacter() => NextInt(43);

            public double NextDouble() => (Next() >> 11) / (double)(1ul << 53);
        }
    }
}

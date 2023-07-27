﻿using System;


namespace Program
{

    class Program
    {
        // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
        public static int[] MiddleGamePawn = {
              0,   0,   0,   0,   0,   0,  0,   0,
            -35,  -1, -20, -23, -15,  24, 38, -22,
            -26,  -4,  -4, -10,   3,   3, 33, -12,
            -27,  -2,  -5,  12,  17,   6, 10, -25,
            -14,  13,   6,  21,  23,  12, 17, -23,
             -6,   7,  26,  31,  65,  56, 25, -20,
             98, 134,  61,  95,  68, 126, 34, -11,
              0,   0,   0,   0,   0,   0,  0,   0,
        };
        public static int[] EndGamePawn = {
              0,   0,   0,   0,   0,   0,   0,   0,
             13,   8,   8,  10,  13,   0,   2,  -7,
              4,   7,  -6,   1,   0,  -5,  -1,  -8,
             13,   9,  -3,  -7,  -7,  -8,   3,  -1,
             32,  24,  13,   5,  -2,   4,  17,  17,
             94, 100,  85,  67,  56,  53,  82,  84,
            178, 173, 158, 134, 147, 132, 165, 187,
              0,   0,   0,   0,   0,   0,   0,   0,

        };
        public static int[] MiddleGameKnight = {
            -105, -21, -58, -33, -17, -28, -19,  -23,
             -29, -53, -12,  -3,  -1,  18, -14,  -19,
             -23,  -9,  12,  10,  19,  17,  25,  -16,
             -13,   4,  16,  13,  28,  19,  21,   -8,
              -9,  17,  19,  53,  37,  69,  18,   22,
             -47,  60,  37,  65,  84, 129,  73,   44,
             -73, -41,  72,  36,  23,  62,   7,  -17,
            -167, -89, -34, -49,  61, -97, -15, -107,
        };

        public static int[] EndGameKnight = {
            -58, -38, -13, -28, -31, -27, -63, -99,
            -25,  -8, -25,  -2,  -9, -25, -24, -52,
            -24, -20,  10,   9,  -1,  -9, -19, -41,
            -17,   3,  22,  22,  22,  11,   8, -18,
            -18,  -6,  16,  25,  16,  17,   4, -18,
            -23,  -3,  -1,  15,  10,  -3, -20, -22,
            -42, -20, -10,  -5,  -2, -20, -23, -44,
            -29, -51, -23, -15, -22, -18, -50, -64,
        };

        public static int[] MiddleGameBishop = {
            -33,  -3, -14, -21, -13, -12, -39, -21,
              4,  15,  16,   0,   7,  21,  33,   1,
              0,  15,  15,  15,  14,  27,  18,  10,
             -6,  13,  13,  26,  34,  12,  10,   4,
             -4,   5,  19,  50,  37,  37,   7,  -2,
            -16,  37,  43,  40,  35,  50,  37,  -2,
            -26,  16, -18, -13,  30,  59,  18, -47,
            -29,   4, -82, -37, -25, -42,   7,  -8,
        };

        public static int[] EndGameBishop = {
            -23,  -9, -23,  -5, -9, -16,  -5, -17,
            -14, -18,  -7,  -1,  4,  -9, -15, -27,
            -12,  -3,   8,  10, 13,   3,  -7, -15,
             -6,   3,  13,  19,  7,  10,  -3,  -9,
             -3,   9,  12,   9, 14,  10,   3,   2,
              2,  -8,   0,  -1, -2,   6,   0,   4,
             -8,  -4,   7, -12, -3, -13,  -4, -14,
            -14, -21, -11,  -8, -7,  -9, -17, -24,
        };

        public static int[] MiddleGameRook = {
            //-19, -13,   1,  17, 16,  7, -37, -26,
            -19, -13, -17,  17, 16,  7, -37, -26,
            -44, -16, -20,  -9, -1, 11,  -6, -71,
            -45, -25, -16, -17,  3,  0,  -5, -33,
            -36, -26, -12,  -1,  9, -7,   6, -23,
            -24, -11,   7,  26, 24, 35,  -8, -20,
             -5,  19,  26,  36, 17, 45,  61,  16,
             27,  32,  58,  62, 80, 67,  26,  44,
             32,  42,  32,  51, 63,  9,  31,  43,
        };

        public static int[] EndGameRook = {
            -9,  2,  3, -1, -5, -13,   4, -20,
            -6, -6,  0,  2, -9,  -9, -11,  -3,
            -4,  0, -5, -1, -7, -12,  -8, -16,
             3,  5,  8,  4, -5,  -6,  -8, -11,
             4,  3, 13,  1,  2,   1,  -1,   2,
             7,  7,  7,  5,  4,  -3,  -5,  -3,
            11, 13, 13, 11, -3,   3,   8,   3,
            13, 10, 18, 15, 12,  12,   8,   5,
        };

        public static int[] MiddleGameQueen = {
             -1, -18,  -9,  10, -15, -25, -31, -50,
            -35,  -8,  11,   2,   8,  15,  -3,   1,
            -14,   2, -11,  -2,  -5,   2,  14,   5,
             -9, -26,  -9, -10,  -2,  -4,   3,  -3,
            -27, -27, -16, -16,  -1,  17,  -2,   1,
            -13, -17,   7,   8,  29,  56,  47,  57,
            -24, -39,  -5,   1, -16,  57,  28,  54,
            -28,   0,  29,  12,  59,  44,  43,  45,
        };

        public static int[] EndGameQueen = {
            -33, -28, -22, -43,  -5, -32, -20, -41,
            -22, -23, -30, -16, -16, -23, -36, -32,
            -16, -27,  15,   6,   9,  17,  10,   5,
            -18,  28,  19,  47,  31,  34,  39,  23,
              3,  22,  24,  45,  57,  40,  57,  36,
            -20,   6,   9,  49,  47,  35,  19,   9,
            -17,  20,  32,  41,  58,  25,  30,   0,
             -9,  22,  22,  27,  27,  19,  10,  20,
        };

        public static int[] MiddleGameKing = {
            -15,  36,  12, -54,   8, -28,  24,  14,
              1,   7,  -8, -64, -43, -16,   9,   8,
            -14, -14, -22, -46, -44, -30, -15, -27,
            -49,  -1, -27, -39, -46, -44, -33, -51,
            -17, -20, -12, -27, -30, -25, -14, -36,
             -9,  24,   2, -16, -20,   6,  22, -22,
             29,  -1, -20,  -7,  -8,  -4, -38, -29,
            -65,  23,  16, -15, -56, -34,   2,  13,
        };

        public static int[] EndGameKing = {
            -53, -34, -21, -11, -28, -14, -24, -43,
            -27, -11,   4,  13,  14,   4,  -5, -17,
            -19,  -3,  11,  21,  23,  16,   7,  -9,
            -18,  -4,  21,  24,  27,  23,   9, -11,
             -8,  22,  24,  27,  26,  33,  26,   3,
             10,  17,  23,  15,  20,  45,  44,  13,
            -12,  17,  14,  17,  17,  38,  23,  11,
            -74, -35, -18, -18, -11,  15,   4, -17,
        };

        public static int[] MiddleGameValues = new int[8] { 0, 82, 337, 365, 477, 1023, 0, 0 };
        public static int[] EndGameValues =    new int[8] { 0, 94, 281, 297, 512,  936, 0, 0 };
        public static int[][] PieceValues = {
            MiddleGameValues,
            EndGameValues,
        };


        public static int[][] PieceSquareTables = { 
            MiddleGamePawn,
            MiddleGameKnight,
            MiddleGameBishop,
            MiddleGameRook,
            MiddleGameQueen,
            MiddleGameKing,
            EndGamePawn,
            EndGameKnight,
            EndGameBishop,
            EndGameRook,
            EndGameQueen,
            EndGameKing,
        };

        // https://discord.com/channels/1132289356011405342/1132768358350200982/1132768358350200982

        public static decimal[] CompressDecimal(byte[] data)
        {
            var result = new decimal[data.Length / 12];
            for (int idx = 0; idx < result.Length; idx++)
            {
                result[idx] = new Decimal(
                    BitConverter.ToInt32(data, idx * 12),
                    BitConverter.ToInt32(data, idx * 12 + 4),
                    BitConverter.ToInt32(data, idx * 12 + 8),
                    false,
                    0);
            }
            return result;
        }

        public static void PrintDecimalList(decimal[] data)
        {
            Array.ForEach(data, x => Console.WriteLine("" + x + "m,"));
        }

        public static ulong[] CompressLong(byte[] data)
        {
            var result = new ulong[data.Length / 8];
            for (int idx = 0; idx < result.Length; idx++)
            {
                result[idx] = BitConverter.ToUInt64(data, idx * 8);
            }
            return result;
        }

        public static void PrintLongList(ulong[] data)
        {
            Array.ForEach(data, x => Console.WriteLine("" + x + ","));
        }

        public static ulong[] CompressPVs()
        {
            byte[] bytes = new byte[MiddleGameValues.Length * 2];

            for (int i = 0; i < MiddleGameValues.Length; ++i)
            {
                bytes[i] = (byte)(MiddleGameValues[i] / 4);
                bytes[i + MiddleGameValues.Length] = (byte)(EndGameValues[i] / 4);
            }

            return CompressLong(bytes);
        }

        public static int[] DecompressPVs(ulong[] compressed)
        {
            int[] decompressed = compressed.SelectMany(BitConverter.GetBytes).Select(x => x * 4).ToArray();
            return decompressed;
        }

        public static decimal[] CompressPSTs()
        {
            byte[] bytes = new byte[PieceSquareTables.Length * 64];
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (var table in PieceSquareTables)
            {
                foreach (int value in table)
                {
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);

                }
            }
            int i = 0;
            foreach (var table in PieceSquareTables)
            {
                foreach (int value in table)
                {
                    int range = max - min;
                    int mapped = (value - min) * 255 / range;
                    bytes[i++] = (byte)mapped;
                }
            }
            Console.WriteLine("Min: " + min + ", Max: " + max);
            // -167 187 = 375
            // -9 = bias

            return CompressDecimal(bytes);
        }

        public static int[] DecompressPSTs(decimal[] compressed)
        {
            return compressed.SelectMany(decimal.GetBits).Where((x, i) => i % 4 != 3).SelectMany(BitConverter.GetBytes).Select((byte x) => x * 375 / 255 - 167 - 9).ToArray();
        }

        static void Main()
        {
            decimal[] compressed = CompressPSTs();
            int[] decompressed = DecompressPSTs(compressed);
            int[] original = PieceSquareTables.SelectMany((int[] x) => x).ToArray();

            int maxDiff = 0;
            for (int i = 0; i < original.Length; ++i)
            {
                int diff = Math.Abs(original[i] - decompressed[i]);
                maxDiff = Math.Max(diff, maxDiff);
            }

            ulong[] compressedPVs = CompressPVs();
            int[] decompressedPVs = DecompressPVs(compressedPVs);

            for (int i = 0; i < decompressedPVs.Length; ++i)
            {
                Console.Write(decompressedPVs[i] + " ");
            }

            Console.WriteLine("Max difference: " + maxDiff);


            PrintDecimalList(compressed);
            Console.WriteLine("##############");
            PrintLongList(compressedPVs);
        }

    }

}
/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Project.Models
{
    internal sealed class RandomGenerator : Random, IDisposable
    {
        private readonly RandomNumberGenerator randomProvider = RandomNumberGenerator.Create();
        public void Dispose() => randomProvider.Dispose();

        public override int Next() => Next(0, int.MaxValue);
        public override int Next(int maxValue) => Next(0, maxValue);

        #region Next based off GetInt32 source Copyright (c) .NET Foundation and Contributers
        //
        // The MIT License(MIT)
        //
        // Copyright(c) .NET Foundation and Contributors
        // 
        // All rights reserved.
        // 
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files(the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        // 
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        // 
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.
        //
        // Bibliography reference 3 in Index.razor (main page). Adapted from .NET Core source code at:
        // https://github.com/dotnet/corefx/blob/master/src/System.Security.Cryptography.Algorithms/src/System/Security/Cryptography/RandomNumberGenerator.cs
        // Adapted for Project
        public override int Next(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
            {
                throw new ArgumentException("Range of random number does not contain at least one possibility.");
            }

            // The total possible range is [0, 4,294,967,295).
            // Subtract one to account for zero being an actual possibility.
            uint range = (uint)maxValue - (uint)minValue - 1;

            // If there is only one possible choice, nothing random will actually happen, so return
            // the only possibility.
            if (range == 0)
            {
                return minValue;
            }

            // Create a mask for the bits that we care about for the range. The other bits will be
            // masked away.
            uint mask = range;
            mask |= mask >> 1;
            mask |= mask >> 2;
            mask |= mask >> 4;
            mask |= mask >> 8;
            mask |= mask >> 16;

            var resultSpan = new Span<uint>(new uint[1]);
            uint result;

            do
            {
                var data = MemoryMarshal.AsBytes(resultSpan);
                randomProvider.GetBytes(data);
                result = mask & resultSpan[0];
            }
            while (result > range);

            return (int)result + minValue;
        }
        #endregion Next based off GetInt32 source Copyright (c) .NET Foundation and Contributers

        public override void NextBytes(byte[] buffer) => randomProvider.GetBytes(buffer);
        public override void NextBytes(Span<byte> buffer) => randomProvider.GetBytes(buffer);

        // Bibliography reference 4 in Index.razor (main page). Adapted from answer by Conrad Albrecht and edit by Portman Wills to:
        // https://stackoverflow.com/questions/2854438/how-to-generate-a-cryptographically-secure-double-between-0-and-1#answer-2854635
        // Adapted for Project
        public override double NextDouble()
        {
            // Step 1: fill an array with 8 random bytes
            var bytes = new byte[8];
            randomProvider.GetBytes(bytes);
            // Step 2: bit-shift 11 and 53 based on double's mantissa bits
            var ul = BitConverter.ToUInt64(bytes, 0) / (1 << 11);
            return ul / (double)(1UL << 53);
        }
    }
}

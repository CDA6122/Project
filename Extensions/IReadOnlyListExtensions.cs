/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Extensions
{
    internal static class IReadOnlyListExtensions
    {
        // Bibliography reference 2 in Index.razor (main page). Adapted from comment by Joseph Musser (jnm2) to:
        // https://github.com/dotnet/corefx/issues/4697#issuecomment-317605600
        // Adapted for Project
        internal static int BinarySearch<TElem, TVal>(this IReadOnlyList<TElem> list, Func<TElem, TVal> getValue, TVal value)
            where TVal : IComparable<TVal>
        {
            var high = list.Count - 1;
            var low = 0;

            while (low <= high)
            {
                var mid = (high + low) >> 1;
                var comparison = value.CompareTo(getValue(list[mid]));
                if (comparison == 0)
                {
                    return mid;
                }
                if (comparison > 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return ~low;
        }
    }
}

/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Models
{
    internal sealed class SimulationResult
    {
        internal readonly IReadOnlyList<double> Files;
        internal readonly IReadOnlyList<SimulationEventBase> Events;

        internal SimulationResult(IReadOnlyList<double> files, IReadOnlyList<SimulationEventBase> events)
        {
            Files = files;
            Events = events;
        }

        internal void Deconstruct(out IReadOnlyList<double> files, out IReadOnlyList<SimulationEventBase> events)
        {
            files = Files;
            events = Events;
        }
    }
}

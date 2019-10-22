/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Models
{
    internal static class Simulation
    {
        internal static IReadOnlyList<SimulationEvent> Run()
        {
            // TODO: create actual random events; for now, this will just return some fake events
            return new ReadOnlyCollection<SimulationEvent>(new[]
            {
                new SimulationEvent()
            });
        }
    }
}

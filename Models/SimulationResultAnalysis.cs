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
    internal static class SimulationResultAnalysis
    {
        internal static (TimeSpan totalTimeBuffering, TimeSpan meanBufferingTimePerNode)?
            Analyse(SimulationParameters parameters, IReadOnlyList<SimulationEventBase> events)
        {
            var totalTimeBuffering = TimeSpan.Zero;
            var requestedFiles = new TimeSpan?[parameters.Nodes];
            foreach (var @event in events)
            {
                switch (@event)
                {
                    case SimulationEventFileRequested requested:
                        requestedFiles[requested.RequestingNode] = requested.EventTime;
                        break;

                    case SimulationEventFullyBuffered buffered:
                        var beforeBuffered = requestedFiles[buffered.Node];
                        if (beforeBuffered != null)
                        {
                            totalTimeBuffering += buffered.EventTime - ((TimeSpan)beforeBuffered);
                            requestedFiles[buffered.Node] = null;
                        }
                        break;

                    case SimulationEventStreamingAborted aborted:
                        var beforeAborted = requestedFiles[aborted.Node];
                        if (beforeAborted != null)
                        {
                            totalTimeBuffering += aborted.EventTime - ((TimeSpan)beforeAborted);
                            requestedFiles[aborted.Node] = null;
                        }
                        break;

                    case SimulationEventSimulationEnd ended:
                        foreach (var endingBuffer in requestedFiles)
                        {
                            if (endingBuffer != null)
                            {
                                totalTimeBuffering += ended.EventTime - ((TimeSpan)endingBuffer);
                            }
                        }
                        break;
                }
            }

            return (totalTimeBuffering, totalTimeBuffering / parameters.Nodes);
        }
    }
}

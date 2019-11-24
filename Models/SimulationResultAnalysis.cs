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
        internal static (TimeSpan meanBufferingTimePerNode, TimeSpan bufferingTimeStandardDeviation)
            Analyse(SimulationParameters parameters, IReadOnlyList<SimulationEventBase> events)
        {
            var timeBufferingPerNode = new TimeSpan[parameters.Nodes];
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
                            timeBufferingPerNode[buffered.Node] += buffered.EventTime - ((TimeSpan)beforeBuffered);
                            requestedFiles[buffered.Node] = null;
                        }
                        break;

                    case SimulationEventStreamingAborted aborted:
                        var beforeAborted = requestedFiles[aborted.Node];
                        if (beforeAborted != null)
                        {
                            timeBufferingPerNode[aborted.Node] += aborted.EventTime - ((TimeSpan)beforeAborted);
                            requestedFiles[aborted.Node] = null;
                        }
                        break;

                    case SimulationEventSimulationEnd ended:
                        for (var nodeIdx = 0; nodeIdx < parameters.Nodes; nodeIdx++)
                        {
                            var endingBuffer = requestedFiles[nodeIdx];
                            if (endingBuffer != null)
                            {
                                timeBufferingPerNode[nodeIdx] += ended.EventTime - ((TimeSpan)endingBuffer);
                            }
                        }
                        break;
                }
            }

            var averageTimeBuffering = timeBufferingPerNode.Aggregate(TimeSpan.Zero,
                (accumulatedBufferingTime, nodeBufferingTime) => accumulatedBufferingTime + nodeBufferingTime) /
                parameters.Nodes;
            var sumOfSquaredTimeBufferingDifferences = timeBufferingPerNode
                .Aggregate(TimeSpan.Zero,
                    (accumulatedSquaredBufferingTimeDifferences, nodeBufferingTime) =>
                    {
                        var bufferingTimeDifferenceSeconds = (nodeBufferingTime > averageTimeBuffering
                            ? nodeBufferingTime - averageTimeBuffering
                            : averageTimeBuffering - nodeBufferingTime).TotalSeconds;
                        return accumulatedSquaredBufferingTimeDifferences +
                            TimeSpan.FromSeconds(bufferingTimeDifferenceSeconds * bufferingTimeDifferenceSeconds);
                    });
            return (averageTimeBuffering,
                TimeSpan.FromSeconds((long)Math.Sqrt(sumOfSquaredTimeBufferingDifferences.TotalSeconds /
                    parameters.Nodes)));
        }
    }
}

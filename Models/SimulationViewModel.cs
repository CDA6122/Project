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
    internal sealed class SimulationViewModel
    {
        internal int EventIdx { get; private set; } = -1;
        internal bool AtBeginning { get => EventIdx <= 0; }
        internal bool AtEnd { get => EventIdx >= simEvents.Count - 1; }
        internal string? CurrentEventText { get => simEvents.Count == 0 ? null : simEvents[EventIdx]?.ToString(); }
        internal string? CurrentEventNumber { get => $"Event {EventIdx + 1:#,##0} of {simEvents.Count:#,##0}"; }
        internal readonly IReadOnlyList<bool?> ClockwiseConnectionLines;

        private readonly IReadOnlyList<SimulationEventBase> simEvents;
        private readonly (int connectionNode, bool? clockwise, bool buffered)?[] nodeConnections;
        private readonly (int connectionNode, bool? clockwise, bool buffered)?[] lastCompletedNodeConnections;
        private readonly bool?[] writeableLines;

        internal SimulationViewModel(SimulationParameters @params, IReadOnlyList<SimulationEventBase> simEvents)
        {
            this.simEvents = simEvents;
            nodeConnections = new (int, bool?, bool)?[@params.Nodes];
            lastCompletedNodeConnections = new (int, bool?, bool)?[@params.Nodes];
            ClockwiseConnectionLines = new ReadOnlyCollection<bool?>(writeableLines = new bool?[@params.Nodes]);
            if (simEvents.Count > 0)
            {
                NextEvent();
            }
        }

        internal void PreviousEvent()
        {
            switch (simEvents[EventIdx]) // undo effects of current event
            {
                case SimulationEventFileRequested fileRequested:
                    nodeConnections[fileRequested.RequestingNode] = null;
                    ProcessLines();
                    break;

                case SimulationEventFullyBuffered fullyBuffered:
                    if (EventIdx <= 0
                        || ((!(simEvents[EventIdx - 1] is SimulationEventFileRequested bufferedPreviousRequest)
                                || bufferedPreviousRequest.RequestingNode != fullyBuffered.Node
                                || bufferedPreviousRequest.EventTime != fullyBuffered.EventTime)
                            && (!(simEvents[EventIdx - 1] is
                                    SimulationEventBandwidthReallocated bufferedPreviousReallocated)
                                || bufferedPreviousReallocated.Node != fullyBuffered.Node
                                || bufferedPreviousReallocated.EventTime != fullyBuffered.EventTime)))
                    {
                        var (bufferedConnectionNode, bufferedClockwise, _) =
                            nodeConnections[fullyBuffered.Node] ?? throw new NullReferenceException(
                                $"Event {nameof(SimulationEventFullyBuffered)} occurred but did not populate node " +
                                "connections");
                        nodeConnections[fullyBuffered.Node] = (bufferedConnectionNode, bufferedClockwise, false);
                        ProcessLines();
                    }
                    break;

                case SimulationEventBandwidthReallocated bandwidthReallocated:
                    var (reallocatedConnectionNode, reallocatedClockwise, _) =
                        nodeConnections[bandwidthReallocated.Node] ?? throw new NullReferenceException(
                            $"Event {nameof(SimulationEventBandwidthReallocated)} occurred but did not populate " +
                            "node connections");
                    nodeConnections[bandwidthReallocated.Node] =
                        (reallocatedConnectionNode, reallocatedClockwise, false);
                    ProcessLines();
                    break;

                case SimulationEventStreamingComplete streamingComplete:
                    nodeConnections[streamingComplete.Node] = lastCompletedNodeConnections[streamingComplete.Node];
                    ProcessLines();
                    break;

                case SimulationEventStreamingAborted streamingAborted:
                    nodeConnections[streamingAborted.Node] = lastCompletedNodeConnections[streamingAborted.Node];
                    ProcessLines();
                    break;
            }
            EventIdx--;
        }

        internal void NextEvent()
        {
            switch (simEvents[++EventIdx]) // process effects of next event
            {
                case SimulationEventFileRequested fileRequested:
                    nodeConnections[fileRequested.RequestingNode] =
                    (
                        fileRequested.ConnectionNode,
                        fileRequested.Clockwise,
                        buffered: EventIdx + 1 < simEvents.Count
                            && (simEvents[EventIdx + 1] is SimulationEventFullyBuffered requestNextBuffered)
                            && requestNextBuffered.Node == fileRequested.RequestingNode
                            && requestNextBuffered.EventTime == fileRequested.EventTime
                    );
                    ProcessLines();
                    break;

                case SimulationEventFullyBuffered fullyBuffered:
                    var (bufferedConnectionNode, bufferedClockwise, _) =
                        nodeConnections[fullyBuffered.Node] ?? throw new NullReferenceException(
                            $"Event {nameof(SimulationEventFullyBuffered)} occurred without outstanding " +
                            nameof(SimulationEventFileRequested));
                    nodeConnections[fullyBuffered.Node] = (bufferedConnectionNode, bufferedClockwise, true);
                    ProcessLines();
                    break;

                case SimulationEventBandwidthReallocated bandwidthReallocated:
                    var (connectionNode, clockwise, _) =
                        nodeConnections[bandwidthReallocated.Node] ?? throw new NullReferenceException(
                            $"Event {nameof(SimulationEventBandwidthReallocated)} occurred without outstanding " +
                            nameof(SimulationEventFileRequested));
                    if (EventIdx + 1 < simEvents.Count
                        && (simEvents[EventIdx + 1] is SimulationEventFullyBuffered reallocatedNextBuffered)
                        && reallocatedNextBuffered.Node == bandwidthReallocated.Node
                        && reallocatedNextBuffered.EventTime == bandwidthReallocated.EventTime)
                    {
                        nodeConnections[bandwidthReallocated.Node] = (connectionNode, clockwise, true);
                        ProcessLines();
                    }
                    break;

                case SimulationEventStreamingComplete streamingComplete:
                    lastCompletedNodeConnections[streamingComplete.Node] = nodeConnections[streamingComplete.Node];
                    nodeConnections[streamingComplete.Node] = null;
                    ProcessLines();
                    break;

                case SimulationEventStreamingAborted streamingAborted:
                    lastCompletedNodeConnections[streamingAborted.Node] = nodeConnections[streamingAborted.Node];
                    nodeConnections[streamingAborted.Node] = null;
                    ProcessLines();
                    break;
            }
        }

        private void ProcessLines()
        {
            // clear existing lines
            for (var lineIdx = 0; lineIdx < writeableLines.Length; lineIdx++)
            {
                writeableLines[lineIdx] = null;
            }

            // first draw all connections' lines which are fully-buffered
            writeLines(true);

            // next draw all connections' lines which are not fully-buffered
            // (takes precedence when coloring lines; indicates connection bandwidth saturation)
            writeLines(false);

            void writeLines(bool buffered)
            {
                for (var nodeIdx = 0; nodeIdx < nodeConnections.Length; nodeIdx++)
                {
                    (int connectionNode, bool? clockwise, bool buffered)? nodeConnection =
                        nodeConnections[nodeIdx];
                    if (nodeConnection?.clockwise != null && nodeConnection?.buffered == buffered)
                    {
                        int connectionIdx = nodeIdx;
                        do
                        {
                            writeableLines[nodeConnection?.clockwise == true ? connectionIdx
                                : (connectionIdx == 0 ? nodeConnections.Length - 1 : connectionIdx - 1)] = !buffered;
                        } while ((connectionIdx = nodeConnection?.clockwise == true
                                ? (connectionIdx == nodeConnections.Length - 1 ? 0 : connectionIdx + 1)
                                : (connectionIdx == 0 ? nodeConnections.Length - 1 : connectionIdx - 1)) !=
                            nodeConnection?.connectionNode);
                    }
                }
            }
        }
    }
}

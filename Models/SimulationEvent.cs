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
    /* Simulation event types (and observables):
     * SimulationStart (time)
     * FileRequested(time, requesting node, file, connection node, bandwidth)
     * FullyBuffered (time, node, size streamed at time of fully buffered)
     * BandwidthReallocated(time, node, bandwidth)
     * StreamingComplete (time, node)
     *
     * // going to count time to fill partial buffer against quality service
     * StreamingAborted (time, node, size of partial buffer)
     * 
     * SimulationEnd (time)
     */

    internal abstract class SimulationEventBase
    {
        internal readonly TimeSpan EventTime;
        internal protected SimulationEventBase(TimeSpan eventTime)
        {
            EventTime = eventTime;
        }

        public override string ToString() => $@"Time (h:mm:ss.###):
            {EventTime.TotalHours:0}{EventTime:\:mm\:ss\:fff}";
    }

    internal sealed class SimulationEventSimulationStart : SimulationEventBase
    {
        internal SimulationEventSimulationStart()
            : base(TimeSpan.Zero)
        {
        }

        public override string ToString() => $@"{base.ToString()}

            Simulation started";
    }

    internal sealed class SimulationEventFileRequested : SimulationEventBase
    {
        internal readonly int RequestingNode;
        internal readonly int SimulationFile;
        internal readonly int ConnectionNode;
        internal readonly bool? Clockwise;
        internal readonly double Bandwidth;
        internal SimulationEventFileRequested(TimeSpan eventTime, int requestingNode, int simulationFile,
                int connectionNode, bool? clockwise, double bandwidth)
            : base(eventTime)
        {
            RequestingNode = requestingNode;
            SimulationFile = simulationFile;
            ConnectionNode = connectionNode;
            Clockwise = clockwise;
            Bandwidth = bandwidth;
        }

        public override string ToString() => $@"{base.ToString()}

            File requested
            Node {RequestingNode}

            File {SimulationFile}

            Connection:
            Node {ConnectionNode}
            {(Clockwise == null ? "(Local file)" : (Clockwise == true ? "Clockwise" : "Counter-clockwise"))}

            Bandwidth {Bandwidth / 1024d / 1024d:#,##0.##}mbps";
    }

    internal sealed class SimulationEventFullyBuffered : SimulationEventBase
    {
        internal readonly int Node;
        internal readonly double MegabytesStreamedAtTimeOfFullyBuffered;
        internal SimulationEventFullyBuffered(TimeSpan eventTime, int node,
            double megabytesStreamedAtTimeOfFullyBuffered)
            : base(eventTime)
        {
            Node = node;
            MegabytesStreamedAtTimeOfFullyBuffered = megabytesStreamedAtTimeOfFullyBuffered;
        }

        public override string ToString() => $@"{base.ToString()}

            File fully buffered
            Node {Node}

            File size downloaded so far:
            {MegabytesStreamedAtTimeOfFullyBuffered:#,##0.##}MB";
    }

    internal sealed class SimulationEventBandwidthReallocated : SimulationEventBase
    {
        internal readonly int Node;
        internal readonly double Bandwidth;
        internal SimulationEventBandwidthReallocated(TimeSpan eventTime, int node, double bandwidth)
            : base(eventTime)
        {
            Node = node;
            Bandwidth = bandwidth;
        }

        public override string ToString() => $@"{base.ToString()}

            File bandwidth reallocated
            Node {Node}

            Bandwidth {Bandwidth / 1024d / 1024d:#,##0.##}mbps";
    }

    internal sealed class SimulationEventStreamingComplete : SimulationEventBase
    {
        internal readonly int Node;
        internal SimulationEventStreamingComplete(TimeSpan eventTime, int node)
            : base(eventTime)
        {
            Node = node;
        }

        public override string ToString() => $@"{base.ToString()}

            File finished streaming on node {Node}";
    }
    internal sealed class SimulationEventStreamingAborted : SimulationEventBase
    {
        //StreamingAborted(time, node, size of partial buffer)
        internal readonly int Node;
        internal readonly double? PartialBufferMegabytesStreamed;
        internal SimulationEventStreamingAborted(TimeSpan eventTime, int node, double? partialBufferMegabytesStreamed)
            : base(eventTime)
        {
            Node = node;
            PartialBufferMegabytesStreamed = partialBufferMegabytesStreamed;
        }

        public override string ToString() => $@"{base.ToString()}

            File streaming aborted
            Node {Node}" + (PartialBufferMegabytesStreamed == null ? "" : $@"

            Partial buffer discarded of
            size downloaded so far:
            {PartialBufferMegabytesStreamed:#,##0.##}MB");
    }

    internal sealed class SimulationEventSimulationEnd : SimulationEventBase
    {
        internal SimulationEventSimulationEnd(TimeSpan eventTime)
            : base(eventTime)
        {
        }

        public override string ToString()
        {
            return @"Simulation terminated

                Elapsed time: " +
                (EventTime.TotalHours >= 1 ? $"{EventTime.TotalHours:#,##0} hours and " : "") +
                $"{EventTime.TotalMinutes % 3600d:#,##0.##} minutes";
        }
    }
}

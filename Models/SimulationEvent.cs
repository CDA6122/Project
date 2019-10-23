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
     * StreamingComplete (time, node)
     * SimulationEnd (time)
     *
     * ** TODO: support re-routing traffic mid-stream and adjusting bandwidth on complete streams
     */

    internal abstract class SimulationEventBase
    {
        internal readonly TimeSpan EventTime;
        internal protected SimulationEventBase(TimeSpan eventTime)
        {
            EventTime = eventTime;
        }
    }

    internal sealed class SimulationEventSimulationStart : SimulationEventBase
    {
        internal SimulationEventSimulationStart()
            : base(TimeSpan.Zero)
        {
        }

        public override string ToString()
        {
            return "Simulation started";
        }
    }

    internal sealed class SimulationEventFileRequested : SimulationEventBase
    {
        internal readonly int RequestingNode;
        internal readonly string SimulationFile;
        internal readonly int ConnectionNode;
        internal readonly decimal Bandwidth;
        internal SimulationEventFileRequested(TimeSpan eventTime, int requestingNode, string simulationFile, int connectionNode, decimal bandwidth)
            : base(eventTime)
        {
            RequestingNode = requestingNode;
            SimulationFile = simulationFile;
            ConnectionNode = connectionNode;
            Bandwidth = bandwidth;
        }

        public override string ToString()
        {
            return $@"File requested
                Node {RequestingNode}

                File:
                {SimulationFile}

                Connection:
                Node {ConnectionNode}
                Bandwidth {Bandwidth / 1000000M:#,##0.##}mbps";
        }
    }

    internal sealed class SimulationEventFullyBuffered : SimulationEventBase
    {
        internal readonly int Node;
        internal readonly long BytesStreamedAtTimeOfFullyBuffered;
        internal SimulationEventFullyBuffered(TimeSpan eventTime, int node, long bytesStreamedAtTimeOfFullyBuffered)
            : base(eventTime)
        {
            Node = node;
            BytesStreamedAtTimeOfFullyBuffered = bytesStreamedAtTimeOfFullyBuffered;
        }

        public override string ToString()
        {
            return $@"File fully buffered
                Node {Node}

                File size downloaded so far:
                {BytesStreamedAtTimeOfFullyBuffered / 8000000M:#,##0.##}MB";
        }
    }

    internal sealed class SimulationEventStreamingComplete : SimulationEventBase
    {
        internal readonly int Node;
        internal SimulationEventStreamingComplete(TimeSpan eventTime, int node)
            : base(eventTime)
        {
            Node = node;
        }

        public override string ToString()
        {
            return $"File finished streaming on node {Node}";
        }
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

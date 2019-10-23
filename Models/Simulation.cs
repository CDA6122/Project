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
        // TODO: do not assume a constant bitrate
        internal const decimal Bitrate = 5_000_000; // ~ high quality 1080p video

        // Bandwidth assumptions from whitepaper: http://www.strixsystems.com/products/datasheets/strixwhitepaper_multihop.pdf
        // 1 hop: 24mbps
        // 2 hops: 11.9mbps
        // 3 hops: 8mbps
        // 4 hops: 6mbps

        internal static (IReadOnlyList<(string name, long size)> files, IReadOnlyList<SimulationEventBase> events) Run()
        {
            // TODO: create actual random events; for now, this will just return some fake events
            return (files: new ReadOnlyCollection<(string name, long size)>(new[]
                {
                    ("ABC", 375_000_000L), // ~10 minutes
                    ("DEF", 750_000_000L), // ~20 minutes
                }),
                events: new ReadOnlyCollection<SimulationEventBase>(new SimulationEventBase[]
                {
                    new SimulationEventSimulationStart(),

                    // At 2 minutes in, Node 1 requests first file, nearest copy is on node 4
                    new SimulationEventFileRequested(TimeSpan.FromSeconds(120d), 1, "Cat videos", 4, 5_000_000M), // ~5mbps out of 8mbps available for 3 hops
                    new SimulationEventFullyBuffered(TimeSpan.FromSeconds(120d), 1, 0L), // ample bandwidth, no need to buffer

                    // At 4 minutes in, Node 2 requests second file, nearest copy is also on node 4
                    // Existing connection: nodes 1 to 4, 5mbps out of 8mbps (37.5% available)
                    // 11.9mbps normally available for 2 hops; 37.5% of that is 4.46mbps; we have to buffer
                    new SimulationEventFileRequested(TimeSpan.FromSeconds(240d), 2, "TV Show", 4, 4_460_000),

                    // buffering seconds * 4.46mbps = bytes transferred at time of fully-buffered
                    // (750MB - bytes transferred) / 750MB = %remaining at time of fully-buffered
                    // 20 minutes to download %remaining of 750MB = 4.46mbps available bandwidth
                    // 
                    // solve for %remaining:
                    // 120second = (6,000,000,000bytes * %remaining) / 4,460,000bps
                    // %remaining = 89.2%
                    //
                    // solve for bytes transfered at time of fully-buffered:
                    // (6,000,000,000bytes - bytes transferred) / 6,000,000,000bytes = 89.2%
                    // bytes transferred = 648,000,000
                    //
                    // solve for buffering seconds:
                    // buffering seconds * 4,460,000 = 648,000,000
                    // buffering seconds = 145.291
                    new SimulationEventFullyBuffered(TimeSpan.FromSeconds(240d + 145.291d), 2, 648_000_000),

                    // Node 1 finishes first, it took 10minutes to stream 10minutes of video including buffering
                    new SimulationEventStreamingComplete(TimeSpan.FromSeconds(120d + 600d), 1),

                    // Node 2 finishes second, it took 20minutes plus 145.291seconds of buffering to stream 20minutes of video
                    new SimulationEventStreamingComplete(TimeSpan.FromSeconds(240d + 1200d + 145.291d), 2),

                    // arbitrary simulation cutoff of new random events after 30 minutes (simulation parameter)
                    new SimulationEventSimulationEnd(TimeSpan.FromMinutes(30d))
                }));
        }
    }
}

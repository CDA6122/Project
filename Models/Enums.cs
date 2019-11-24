/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Models
{
    internal enum FieldError : byte
    {
        [Description("Number of nodes")] Nodes,
        [Description("Simulation length")] SimulationLength,
        [Description("Number of files per node")] FilesPerNode,
        [Description("Mean file size")] MeanFileSize,
        [Description("File size standard deviation")] FileSizeStandardDeviation,
        [Description("Playback constant bitrate")] PlaybackBitrate,
        [Description("Max storage capacity per node")] NodeCapacity,
        [Description("1 hop max bandwidth")] MaxBandwidth,
        [Description("File catalog size")] FileCatalogSize,
        [Description("File popularity standard deviation")] FilePopularityStandardDeviation,
        [Description("Max sampling attempts")] MaxSamplingAttempts,
        [Description("File catalog size per node must not exceed storage capacity")] FileCatalogGigabyteSizePerNode,
        [Description("How many times to run the simulation")] SimulationRuns
    }
}

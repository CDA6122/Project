/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
 // Uncomment to run in single-threaded mode (much slower on a multi-core system)
 // #define SINGLETHREADED
using Microsoft.AspNetCore.Components;
using Project.Extensions;
using Project.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BulkRunner
{
    internal static class Program
    {
        private static int Main()
        {
            // Program writes comma-separated values so they can be streamed to make a .csv file

            // Start with line for column headers
            Console.WriteLine("Node Capacity Gigabytes,Proportion Of File Catalog Size Allocated To Nodes," +
                "Mean Buffering Time Per Node,Buffering Time Standard Deviation");

            var fieldsInError = new List<FieldError>();
            var @params = new SimulationParameters(fieldsInError);

            #region Parameters
            // Set number of Nodes in the circle network; higher takes longer to run
            @params.OnNodesChanged(new ChangeEventArgs { Value = 50 });

            // Number of data points to collect at each storage capacity;
            // higher increases accuracy, but takes longer to run;
            // minimum of 1 otherwise simulation will not run
            const int dataPointsPerCapacity = 100;

            const double nodeCapacityDelta = 0.2d; // GB; the lower the number, the longer the simulation will take

            // Cannot start Node Capacity Gigabytes any lower;
            // otherwise, fieldsInError will be populated with a FieldError.
            //
            // Minimum equals total file catalog size divided by number of nodes;
            // having less than the minimum would mean that you could not distribute all files to all nodes
            //
            // 6 nodes, minumum: 17.4623046875
            // 50 nodes, minimum: 2.0954765625000005
            // 
            double nodeCapacityGigabytes = 2.0954765625000005d;

            // On a personal desktop, running single-threaded, with 50 nodes, 100 data points per node, a node
            // capacity delta of 10 GB, and all other parameters defaulted (see ../Models/SimulationParameters.cs),
            // it took 4 minutes and 17 seconds to collect 1100 total data points.
            //
            // Running multi-threaded, with 6 nodes, 100 data points per node, a node capacity delta of 0.2 GB, and
            // all other parameters defaulted,
            // it took 20 minutes and 31 seconds to collect 43,700 total data points.
            //
            // Running multi-threaded, with 50 nodes, 100 data points per node, a node capacity delta of 0.2 GB, and
            // all other parameters defaulted,
            // it took 46 minutes and 36 seconds to collect 51,400 total data points.
            //
            // To run single-threaded, have to uncomment #define SINGLETHREADED at the top of this file.

            #endregion Parameters

            // When we loop through the range of capacity values, we have a maximum of the total file catalog
            // size since having the entire catalog on every node guarantees no wireless communication necessary
            do
            {
                // On each iteration of the loop set the storage capacity per node (in GB)
                @params.OnNodeCapacityGigabytesChanged(new ChangeEventArgs { Value = nodeCapacityGigabytes });

                if (fieldsInError.Count > 0)
                {
                    foreach (var fieldInError in fieldsInError)
                    {
                        Console.Error.WriteLine(fieldInError.GetDescription());
                    }
                    return fieldsInError.Count;
                }

#if SINGLETHREADED
                for (var runCounter = 0; runCounter < dataPointsPerCapacity; runCounter++)
                {
                    // files in megabytes (MB)
                    var (files, events) = Simulation.Run(@params);
                    var (meanBufferingTimePerNode, bufferingTimeStandardDeviation) =
                        SimulationResultAnalysis.Analyse(@params, events);
                    Console.WriteLine($"{nodeCapacityGigabytes:f10}," +
                        (files.Sum() / 1024d / @params.FileCatalogSize).ToString("f10") +
                        $",{meanBufferingTimePerNode.TotalMinutes:f10},{bufferingTimeStandardDeviation.TotalMinutes:f10}");
                }
#else
                // Not SINGLETHREADED (runs on all available logical threads)
                Parallel.For(0, dataPointsPerCapacity, _ =>
                {
                    // files in megabytes (MB)
                    var (files, events) = Simulation.Run(@params);
                    var (meanBufferingTimePerNode, bufferingTimeStandardDeviation) =
                        SimulationResultAnalysis.Analyse(@params, events);

                    // IO is generally not threadsafe, but Console is threadsafe (it synchronizes access internally):
                    // https://docs.microsoft.com/en-us/dotnet/api/system.console?view=netcore-3.0#thread-safety
                    Console.WriteLine($"{nodeCapacityGigabytes:f10}," +
                        (files.Sum() / 1024d / @params.FileCatalogSize).ToString("f10") +
                        $",{meanBufferingTimePerNode.TotalMinutes:f10}," +
                        bufferingTimeStandardDeviation.TotalMinutes.ToString("f10"));
                });
#endif
            } while ((nodeCapacityGigabytes += nodeCapacityDelta) < @params.TotalFileCatalogGigabyteSize);

            return 0; // success
        }
    }
}

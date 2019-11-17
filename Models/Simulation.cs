/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using MathNet.Numerics.Distributions;
using Project.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Models
{
    internal static class Simulation
    {
        internal static SimulationResult Run(SimulationParameters parameters)
        {
            // Custom class which overrides base Random class method implementations with
            // .NET Core RandomNumberGenerator (uses Crypto APIs for cryptographically secure random numbers)
            using var randomSource = new RandomGenerator();

            IReadOnlyList<(double fileSizeMegabytes, double randomLow)>? sampleFiles = null;
            IReadOnlyList<IReadOnlyHashSet<int>>? nodes = null;
            int samplingAttemptCounter = 0;
            do
            {
                // Gaussian distributed file sizes and popularity

                IReadOnlyList<(double fileSizeMegabytes, double randomLow)> currentSampleFiles =
                    GenerateRandomSampleFiles(randomSource, parameters);
                IReadOnlyList<IReadOnlyHashSet<int>> currentNodes;
                (currentNodes, currentSampleFiles) =
                    FillNodeDisksByFilePopularity(currentSampleFiles, randomSource, parameters);

                if (sampleFiles == null || currentSampleFiles.Count > sampleFiles.Count)
                {
                    sampleFiles = currentSampleFiles;
                    nodes = currentNodes;
                }
            } while (sampleFiles.Count < parameters.FileCatalogSize
                && ++samplingAttemptCounter <= parameters.MaxSamplingAttempts);

            var averageMinuteBetweenEventsPerNode = parameters.SimulationLengthMinutes /
                parameters.ExpectedFilesPerNode;

            // The class MathNet.Numerics.Distributions.Poisson only gives discrete whole minute samples (integer)
            //
            // new Poisson(averageMillisecondsBetweenEventsPerNode, randomSource);
            //
            // So instead we're going to use the Normal Approximation to Poisson
            var eventPoisson = new Normal(averageMinuteBetweenEventsPerNode,
                Math.Sqrt(averageMinuteBetweenEventsPerNode), randomSource);

            var eventEndTimespan = DateTime.MinValue.AddMinutes(parameters.SimulationLengthMinutes);
            var eventsPerNodeInChronologicalOrder = new SortedDictionary<DateTime, (bool, int)>();
            var connectionsThroughNodes = new Dictionary<int, double>[parameters.Nodes];
            for (var nodeIdx = 0; nodeIdx < parameters.Nodes; nodeIdx++)
            {
                calculateNextFileRequestEvent(DateTime.MinValue, nodeIdx);
                connectionsThroughNodes[nodeIdx] = new Dictionary<int, double>();
            }

            var events = new List<SimulationEventBase> { new SimulationEventSimulationStart() };
            var nodesData = new (int fileIdx, int connectionNodeIdx, DateTime previousEvent, DateTime nextEvent,
                double bandwidthBitsPerSecond, double bufferedMegabytes, bool clockwise)?[parameters.Nodes];

            while (eventsPerNodeInChronologicalOrder.FirstOrDefault()
                    is KeyValuePair<DateTime, (bool newFileRequest, int nodeIdx)> @event
                && @event.Key < eventEndTimespan)
            {
                DateTime? nextScheduledEvent = null;
                var possibleNodeData = nodesData[@event.Value.nodeIdx];
                if (possibleNodeData is (int fileIdx, int connectionNodeIdx, DateTime previousEvent,
                    DateTime nextEvent, double bandwidthBitsPerSecond, double bufferedMegabytes, bool clockwise))
                {
                    if (@event.Value.newFileRequest)
                    {
                        // New file requested before previous one completed (also cleans up and reallocates bandwidth)

                        finishStreaming(@event.Key, @event.Value.nodeIdx, connectionNodeIdx, clockwise,
                            abortBuffer: bufferedMegabytes);
                    }
                    else
                    {
                        if (double.IsNaN(bufferedMegabytes))
                        {
                            // Cleanup on finish (which also reallocates bandwidth)

                            finishStreaming(@event.Key, @event.Value.nodeIdx, connectionNodeIdx, clockwise,
                                abortBuffer: null);
                        }
                        else
                        {
                            // Fully-buffered; maintain same bandwidth till finished streaming

                            nextScheduledEvent = finishBuffering(@event.Key, @event.Value.nodeIdx, fileIdx,
                                connectionNodeIdx, previousEvent, bandwidthBitsPerSecond, bufferedMegabytes,
                                clockwise);
                        }
                    }
                }
                if (@event.Value.newFileRequest)
                {
                    nextScheduledEvent = scheduleNewFileRequest(@event.Key, @event.Value.nodeIdx);
                }

                if (nextScheduledEvent != null)
                {
                    while (eventsPerNodeInChronologicalOrder.ContainsKey((DateTime)nextScheduledEvent))
                    {
                        // Two events could compete for exact same timeslot so we just add a tick to the time
                        nextScheduledEvent = ((DateTime)nextScheduledEvent).AddTicks(1L);
                    }
                    eventsPerNodeInChronologicalOrder.Add((DateTime)nextScheduledEvent,
                        (false, @event.Value.nodeIdx));

                    // Schedule next file request event
                    // (it may occur before the current file even finishes streaming)
                    if (@event.Value.newFileRequest)
                    {
                        calculateNextFileRequestEvent((DateTime)nextScheduledEvent, @event.Value.nodeIdx);
                    }
                }

                eventsPerNodeInChronologicalOrder.Remove(@event.Key);
            }

            events.Add(new SimulationEventSimulationEnd(TimeSpan.FromMinutes(parameters.SimulationLengthMinutes)));

            return new SimulationResult(sampleFiles.Select(sampleFile => sampleFile.fileSizeMegabytes).ToList(), events);

            #region simulation event helpers
            void calculateNextFileRequestEvent(DateTime eventTimer, int nodeIdx)
            {
                double delayMinutesBeforeNextEvent;
                do
                {
                    delayMinutesBeforeNextEvent = eventPoisson.Sample();
                } while (delayMinutesBeforeNextEvent <= 0d);
                DateTime nextNodeEvent = eventTimer.AddMinutes(delayMinutesBeforeNextEvent);

                while (eventsPerNodeInChronologicalOrder.ContainsKey(nextNodeEvent))
                {
                    // Two events could compete for exact same timeslot so we just add a tick to the time
                    nextNodeEvent = nextNodeEvent.AddTicks(1L);
                }

                eventsPerNodeInChronologicalOrder.Add(nextNodeEvent, (true, nodeIdx));
            }

            (int connectionNodeIdx, bool clockwise, double bandwidthBitsPerSecond) routeToFile(int nodeIdx, int fileIdx)
            {
                if (nodes[nodeIdx].Contains(fileIdx))
                {
                    // Node already has the file locally
                    return (nodeIdx, false, parameters.PlaybackBitrate);
                }

                var lowestPercentageBandwidthAvailableClockwise = 1d;
                var nearestConnectionClockwise = nodeIdx;
                var numberOfHopsClockwise = 0;
                do
                {
                    numberOfHopsClockwise++;
                    nearestConnectionClockwise = nearestConnectionClockwise == parameters.Nodes - 1
                        ? 0
                        : nearestConnectionClockwise + 1;
                    var nodeBandwidthPercentageAvailable = 1d -
                        connectionsThroughNodes[nearestConnectionClockwise].Values.Sum();
                    if (nodeBandwidthPercentageAvailable < 0d)
                    {
                        nodeBandwidthPercentageAvailable = 0d; // Handle floating point errors
                    }
                    if (nodeBandwidthPercentageAvailable < lowestPercentageBandwidthAvailableClockwise)
                    {
                        lowestPercentageBandwidthAvailableClockwise = nodeBandwidthPercentageAvailable;
                    }
                } while (!nodes[nearestConnectionClockwise].Contains(fileIdx));

                // Bandwidth over N hops degrades by 1 / N according to Strix Systems.
                // Bibliography reference 1 in Index.razor (main page)
                var bandwidthBitsPerSecondAvailableClockwise = parameters.MaxBandwidthMegabitsPerSecond * 1024 * 1024 *
                    lowestPercentageBandwidthAvailableClockwise / numberOfHopsClockwise;

                if (bandwidthBitsPerSecondAvailableClockwise < 0)
                {
                    bandwidthBitsPerSecondAvailableClockwise = 0d; // Handle floating point errors
                }

                var lowestPercentageBandwidthAvailableCounterClockwise = 1d;
                var nearestConnectionCounterClockwise = nodeIdx;
                var numberOfHopsCounterClockwise = 0;
                do
                {
                    numberOfHopsCounterClockwise++;
                    nearestConnectionCounterClockwise = nearestConnectionCounterClockwise == 0
                        ? parameters.Nodes - 1
                        : nearestConnectionCounterClockwise - 1;
                    var nodeBandwidthPercentageAvailable = 1d -
                        connectionsThroughNodes[nearestConnectionCounterClockwise].Values.Sum();
                    if (nodeBandwidthPercentageAvailable < 0d)
                    {
                        nodeBandwidthPercentageAvailable = 0d; // Handle floating point errors
                    }
                    if (nodeBandwidthPercentageAvailable < lowestPercentageBandwidthAvailableCounterClockwise)
                    {
                        lowestPercentageBandwidthAvailableCounterClockwise = nodeBandwidthPercentageAvailable;
                    }
                } while (!nodes[nearestConnectionCounterClockwise].Contains(fileIdx));

                // Bandwidth over N hops degrades by 1 / N according to Strix Systems.
                // Bibliography reference 1 in Index.razor (main page)
                var bandwidthBitsPerSecondAvailableCounterClockwise = parameters.MaxBandwidthMegabitsPerSecond * 1024 * 1024 *
                    lowestPercentageBandwidthAvailableCounterClockwise / numberOfHopsCounterClockwise;

                if (bandwidthBitsPerSecondAvailableCounterClockwise < 0)
                {
                    bandwidthBitsPerSecondAvailableCounterClockwise = 0d; // Handle floating point errors
                }

                bool clockwise = bandwidthBitsPerSecondAvailableClockwise > bandwidthBitsPerSecondAvailableCounterClockwise;
                double bandwidthBitsPerSecondAvailable = Math.Min(parameters.PlaybackBitrate, clockwise
                    ? bandwidthBitsPerSecondAvailableClockwise
                    : bandwidthBitsPerSecondAvailableCounterClockwise);
                var nearestConnection = nodeIdx;
                var numberOfHops = clockwise ? numberOfHopsClockwise : numberOfHopsCounterClockwise;
                for (int hopIdx = 0; hopIdx < numberOfHops; hopIdx++)
                {
                    nearestConnection = clockwise
                        ? (nearestConnection == parameters.Nodes - 1
                            ? 0
                            : nearestConnection + 1)
                        : (nearestConnection == 0
                            ? parameters.Nodes - 1
                            : nearestConnection - 1);
                    connectionsThroughNodes[nearestConnection].Add(nodeIdx, bandwidthBitsPerSecondAvailable /
                        (clockwise
                            ? bandwidthBitsPerSecondAvailableClockwise
                            : bandwidthBitsPerSecondAvailableCounterClockwise));
                }

                return (nearestConnection, clockwise, bandwidthBitsPerSecondAvailable);
            }

            DateTime? scheduleNewFileRequest(DateTime @event, int nodeIdx)
            {
                // Generate a random uniform distribution number in the range [0, 1)
                // This will be a lookup to our cumulative distribution for our Gaussian file popularities
                var randomLowInsertIndex = sampleFiles.BinarySearch(file => file.randomLow,
                    randomSource.NextDouble());

                // If it does not perfectly match, BinarySearch returns two's complement index of the highest index
                // with a lower value
                var randomLowIndex = randomLowInsertIndex < 0 ? ~randomLowInsertIndex - 1 : randomLowInsertIndex;

                var (fileSizeMegabytes, randomLow) = sampleFiles[randomLowIndex];

                var (newConnectionNodeIdx, clockwise, newBandwidthBitsPerSecond) = routeToFile(nodeIdx, randomLowIndex);

                TimeSpan eventTime = @event - DateTime.MinValue;
                events.Add(new SimulationEventFileRequested(eventTime, nodeIdx, randomLowIndex, newConnectionNodeIdx,
                    nodeIdx == newConnectionNodeIdx ? (bool?)null : clockwise, newBandwidthBitsPerSecond));

                DateTime? nextScheduledEvent;
                bool needsBuffering;
                if (newBandwidthBitsPerSecond == parameters.PlaybackBitrate)
                {
                    // Immediately fully buffered since enough bandwidth is available to cover the file's bitrate
                    needsBuffering = false;
                    events.Add(new SimulationEventFullyBuffered(eventTime, nodeIdx, 0d));
                    nextScheduledEvent = @event.AddSeconds(fileSizeMegabytes * 1024d * 1024d *
                        8d / parameters.PlaybackBitrate);
                }
                else
                {
                    needsBuffering = true;
                    // Calculate when buffering will complete given the available bandwidth
                    //
                    // Solve system of equations:
                    //
                    // file length in seconds = file size in bits / playback bitrate
                    //
                    // fraction which will continue streaming after fully-buffered * playback bitrate = bandwidth
                    // ->
                    // fraction which will continue streaming after fully-buffered = bandwidth / playback bitrate
                    //
                    // (file size in bits - bits transferred at the time of fully-buffered) / file size in bits
                    //   = fraction which will continue streaming after fully-buffered
                    // ->
                    // bits transferred at the time of fully-buffered = file size in bits *
                    //   (1 - fraction which will continue streaming after fully-buffered)
                    //
                    // buffering seconds * bandwidth = bits transferred at the time of fully-buffered
                    // ->
                    // buffering seconds = bits transferred at the time of fully-buffered / bandwidth

                    if (newBandwidthBitsPerSecond == 0)
                    {
                        nextScheduledEvent = null;
                    }
                    else
                    {
                        var fractionAfterFullyBuffered = newBandwidthBitsPerSecond / parameters.PlaybackBitrate;
                        var bufferedBits = fileSizeMegabytes * 1024 * 1024 * 8 *
                            (1 - fractionAfterFullyBuffered);
                        nextScheduledEvent = @event.AddSeconds(bufferedBits / newBandwidthBitsPerSecond);
                    }
                }
                nodesData[nodeIdx] = (randomLowIndex, newConnectionNodeIdx, @event,
                    nextScheduledEvent ?? DateTime.MaxValue, newBandwidthBitsPerSecond,
                    needsBuffering ? 0d : double.NaN, clockwise);
                return nextScheduledEvent;
            }

            void finishStreaming(DateTime @event, int nodeIdx, int connectionNodeIdx, bool clockwise,
                double? abortBuffer)
            {
                var eventTime = @event - DateTime.MinValue;
                if (abortBuffer == null)
                {
                    events.Add(new SimulationEventStreamingComplete(eventTime, nodeIdx));
                }
                else
                {
                    events.Add(new SimulationEventStreamingAborted(eventTime, nodeIdx,
                        double.IsNaN((double)abortBuffer) ? null : abortBuffer));
                }

                nodesData[nodeIdx] = null;

                if (nodeIdx == connectionNodeIdx)
                {
                    // No bandwidth to free up if the Node was streaming a local file
                    return;
                }

                var connectionNodeIndexes = new HashSet<int>();
                var nearestConnection = nodeIdx;
                do
                {
                    nearestConnection = clockwise
                        ? (nearestConnection == parameters.Nodes - 1
                            ? 0
                            : nearestConnection + 1)
                        : (nearestConnection == 0
                            ? parameters.Nodes - 1
                            : nearestConnection - 1);

                    var connectionsThroughNode = connectionsThroughNodes[nearestConnection];
                    connectionsThroughNode.Remove(nodeIdx);

                    foreach (var otherConnectionNodeIdx in connectionsThroughNode.Keys)
                    {
                        connectionNodeIndexes.Add(otherConnectionNodeIdx);
                    }
                }
                while (nearestConnection != connectionNodeIdx);

                reallocateBandwidth(@event, connectionNodeIndexes);
            }

            DateTime finishBuffering(DateTime @event, int nodeIdx, int fileIdx, int connectionNodeIdx,
                DateTime previousEvent, double bandwidthBitsPerSecond, double bufferedMegabytes, bool clockwise)
            {
                double additionalBufferedBits = (@event - previousEvent).TotalSeconds * bandwidthBitsPerSecond;
                events.Add(new SimulationEventFullyBuffered(@event - DateTime.MinValue, nodeIdx, bufferedMegabytes +
                    (additionalBufferedBits / 1024d / 1024d / 8d)));
                DateTime nextScheduledEvent = @event.AddSeconds(
                    ((sampleFiles[fileIdx].fileSizeMegabytes - bufferedMegabytes) * 1024d * 1024d * 8d -
                        additionalBufferedBits) / bandwidthBitsPerSecond);
                nodesData[nodeIdx] = (fileIdx, connectionNodeIdx, @event, nextScheduledEvent, bandwidthBitsPerSecond,
                    double.NaN, clockwise);
                return nextScheduledEvent;
            }

            void reallocateBandwidth(DateTime @event, HashSet<int> connectionNodeIndexes)
            {
                if (connectionNodeIndexes.Count == 0)
                {
                    return;
                }

                double lowestRouteBandwidthBitsPerSecond = double.MaxValue;
                int? nodeWithLowestBandwidth = null;
                foreach (var nodeIdx in connectionNodeIndexes)
                {
                    var possibleNodeData = nodesData[nodeIdx];
                    if (possibleNodeData is (int otherFileIdx, int otherConnectionNodeIdx, DateTime otherPreviousEvent,
                        DateTime otherNextEvent, double otherBandwidthBitsPerSecond, double otherBufferedMegabytes,
                        bool otherClockwise))
                    {
                        if (!double.IsNaN(otherBufferedMegabytes)
                            && otherBufferedMegabytes < lowestRouteBandwidthBitsPerSecond)
                        {
                            lowestRouteBandwidthBitsPerSecond = otherBufferedMegabytes;
                            nodeWithLowestBandwidth = nodeIdx;
                        }
                    }
                }

                if (nodeWithLowestBandwidth != null
                    && nodesData[(int)nodeWithLowestBandwidth] is (int fileIdx, int connectionNodeIdx,
                        DateTime previousEvent, DateTime nextEvent, double bandwidthBitsPerSecond,
                        double bufferedMegabytes, bool clockwise))
                {
                    var nearestConnection = (int)nodeWithLowestBandwidth;
                    double highestUsedBandwidthPercentageAtConnectionNode = double.MinValue;
                    var numberOfHops = 0;
                    do
                    {
                        numberOfHops++;
                        nearestConnection = clockwise
                            ? (nearestConnection == parameters.Nodes - 1
                                ? 0
                                : nearestConnection + 1)
                            : (nearestConnection == 0
                                ? parameters.Nodes - 1
                                : nearestConnection - 1);

                        var usedBandwidthPercentage = connectionsThroughNodes[nearestConnection].Values.Sum();
                        if (usedBandwidthPercentage < 1d
                            && usedBandwidthPercentage > highestUsedBandwidthPercentageAtConnectionNode)
                        {
                            highestUsedBandwidthPercentageAtConnectionNode = usedBandwidthPercentage;
                        }
                    } while (nearestConnection != connectionNodeIdx);

                    double availableBandwidthPercentage;
                    if (highestUsedBandwidthPercentageAtConnectionNode != double.MinValue
                        && (availableBandwidthPercentage = 1d - highestUsedBandwidthPercentageAtConnectionNode) > 0d)
                    {
                        // Bandwidth over N hops degrades by 1 / N according to Strix Systems.
                        // Bibliography reference 1 in Index.razor (main page)
                        var bandwidthBitsPerSecondAvailable = parameters.MaxBandwidthMegabitsPerSecond * 1024 * 1024 *
                            availableBandwidthPercentage / numberOfHops;

                        var eventTime = @event - DateTime.MinValue;
                        var (fileSizeMegabytes, randomLow) = sampleFiles[fileIdx];

                        //
                        // Calculate
                        //
                        // Assuming the Node can allocate the bandwidth required to start streaming now:
                        // the fraction which will continue streaming after full-buffer
                        //   = remaining to buffer bits / file size bits
                        //
                        // remaining to buffer bits = file size - buffered up to last event - buffered since last event
                        //
                        // fraction which will continue streaming after fully-buffered * file length in seconds
                        //   = bandwidth to start streaming now
                        //
                        var alreadyBufferedBits = bufferedMegabytes * 1024d * 1024d * 8d +
                            (@event - previousEvent).TotalSeconds * bandwidthBitsPerSecond;
                        var remainingToBufferBits = fileSizeMegabytes * 1024d * 1024d * 8d -
                            alreadyBufferedBits;
                        var maxUseableBandwidthBitsPerSecond = remainingToBufferBits *
                            (fileSizeMegabytes * 1024d * 1024d * 8d / parameters.PlaybackBitrate);
                        var excessBandwidthBitsPerSecondAvailable = bandwidthBitsPerSecond +
                            bandwidthBitsPerSecondAvailable - maxUseableBandwidthBitsPerSecond;

                        events.Add(new SimulationEventBandwidthReallocated(eventTime, (int)nodeWithLowestBandwidth,
                            maxUseableBandwidthBitsPerSecond));

                        if (excessBandwidthBitsPerSecondAvailable >= 0)
                        {
                            // With the extra bandwidth, Node would be able to start streaming now (full-buffer)
                            if (excessBandwidthBitsPerSecondAvailable > 0)
                            {
                                // Reduce the percentage bandwidth being taken
                                availableBandwidthPercentage *= excessBandwidthBitsPerSecondAvailable /
                                    bandwidthBitsPerSecondAvailable;
                            }

                            events.Add(new SimulationEventFullyBuffered(eventTime, (int)nodeWithLowestBandwidth,
                                alreadyBufferedBits / 1024d / 1024d / 8d));
                        }

                        DateTime nextScheduledEvent = @event.AddSeconds(fileSizeMegabytes * 1024d * 1024d *
                            8d / maxUseableBandwidthBitsPerSecond);
                        while (eventsPerNodeInChronologicalOrder.ContainsKey(nextScheduledEvent))
                        {
                            // Two events could compete for exact same timeslot so we just add a tick to the time
                            nextScheduledEvent = (nextScheduledEvent).AddTicks(1L);
                        }
                        eventsPerNodeInChronologicalOrder.Add(nextScheduledEvent,
                            (false, (int)nodeWithLowestBandwidth));

                        eventsPerNodeInChronologicalOrder.Remove(nextEvent);

                        nodesData[(int)nodeWithLowestBandwidth] = (fileIdx, (int)nodeWithLowestBandwidth, @event,
                            nextScheduledEvent, connectionNodeIdx,
                            excessBandwidthBitsPerSecondAvailable >= 0
                                ? double.NaN
                                : alreadyBufferedBits / 1024d / 1024d / 8d,
                            clockwise);

                        nearestConnection = (int)nodeWithLowestBandwidth;
                        do
                        {
                            nearestConnection = clockwise
                                ? (nearestConnection == parameters.Nodes - 1
                                    ? 0
                                    : nearestConnection + 1)
                                : (nearestConnection == 0
                                    ? parameters.Nodes - 1
                                    : nearestConnection - 1);

                            connectionsThroughNodes[nearestConnection][(int)nodeWithLowestBandwidth] +=
                                availableBandwidthPercentage;
                        } while (nearestConnection != connectionNodeIdx);
                    }

                    connectionNodeIndexes.Remove((int)nodeWithLowestBandwidth);
                    reallocateBandwidth(@event, connectionNodeIndexes);
                }
            }
            #endregion simulation event helpers
        }

        private static IReadOnlyList<(double fileSizeMegabytes, double randomLow)> GenerateRandomSampleFiles(
            RandomGenerator randomSource, SimulationParameters parameters)
        {
            var unstandardizedFilePopularities = new double[parameters.FileCatalogSize];
            Normal filePopularityDistribution = new Normal(
                1d / parameters.FileCatalogSize, parameters.FilePopularityStandardDeviation, randomSource);
            filePopularityDistribution.Samples(unstandardizedFilePopularities);
            var minFilePopularity = unstandardizedFilePopularities.Min();
            var sumOfFilePopularities = unstandardizedFilePopularities.Sum(unstandardizedFilePopularity =>
                unstandardizedFilePopularity - minFilePopularity);

            var fileSizesMegabytes = new double[parameters.FileCatalogSize];
            Normal fileSizeDistribution = new Normal(
                parameters.MeanFileSizeMegabytes, parameters.FileSizeMegabytesStandardDeviation, randomSource);
            fileSizeDistribution.Samples(fileSizesMegabytes);
            var minFileSize = fileSizesMegabytes.Min();

            var result = new (double fileSizeMegabytes, double randomLow)[parameters.FileCatalogSize];
            var randomLow = 0d;
            for (var fileIdx = 0; fileIdx < parameters.FileCatalogSize; fileIdx++)
            {
                result[fileIdx] = ((fileSizesMegabytes[fileIdx] - minFileSize) *
                    parameters.MeanFileSizeMegabytes /
                    (parameters.MeanFileSizeMegabytes - minFileSize), randomLow); // standardize to offset negative file sizes

                // standardize to 100% total probability
                randomLow += (unstandardizedFilePopularities[fileIdx] - minFilePopularity) / sumOfFilePopularities;
            }

            return Array.AsReadOnly(result);
        }

        private static (IReadOnlyList<IReadOnlyHashSet<int>> nodes,
            IReadOnlyList<(double fileSizeMegabytes, double randomLow)> sampleFiles)
            FillNodeDisksByFilePopularity(IReadOnlyList<(double fileSizeMegabytes, double randomLow)> files,
                RandomGenerator randomSource, SimulationParameters parameters)
        {
            var writableNodes = new HashSet<int>[parameters.Nodes];
            var nodes = new IReadOnlyHashSet<int>[parameters.Nodes];
            var nodesRemainingDiskSpaceGigabytes =
                Enumerable.Repeat(parameters.NodeCapacityGigabytes, parameters.Nodes).ToList();
            for (int nodeIdx = 0; nodeIdx < parameters.Nodes; nodeIdx++)
            {
                var diskSpaceMegabytes = parameters.NodeCapacityGigabytes * 1024d;
                var nodeFiles = new HashSet<int>();
                writableNodes[nodeIdx] = nodeFiles;
                nodes[nodeIdx] = new ReadOnlyHashSet<int>(nodeFiles);
                fillNodeDisk(files.Select((file, fileIdx) => (fileIdx, file.fileSizeMegabytes, file.randomLow)).ToList(),
                    nodeFiles, diskSpaceMegabytes);
            }

            var remainingFiles = new HashSet<int>(Enumerable.Range(0, parameters.FileCatalogSize));
            for (int nodeIdx = 0; nodeIdx < parameters.Nodes; nodeIdx++)
            {
                remainingFiles.ExceptWith(nodes[nodeIdx]);
            }

            if (remainingFiles.Count == 0)
            {
                return (Array.AsReadOnly(nodes), files);
            }

            // TODO: Better handling of special case where a file has not been randomly selected even once for any
            //       node's disk. Even though it could be incredibly non-performant to create new random samplings of
            //       files and nodes with files repeatedly until success, it is one way to retain the intended random
            //       distributions.
            //
            // Remove the unused files
            var reducedFiles = new (double fileSizeMegabytes, double randomLow)[parameters.FileCatalogSize - remainingFiles.Count];
            var reducedFileCounter = 0;
            var sumOfRemainingPopularities = 0d;
            var removedFiles = new int[remainingFiles.Count];
            int removedFileCounter = 0;
            for (int fileIdx = parameters.FileCatalogSize - 1; fileIdx >= 0; fileIdx--)
            {
                if (remainingFiles.Contains(fileIdx))
                {
                    removedFiles[remainingFiles.Count - ++removedFileCounter] = fileIdx;
                }
                else
                {
                    var reducedFile = files[fileIdx];
                    reducedFiles[parameters.FileCatalogSize - remainingFiles.Count - ++reducedFileCounter] = reducedFile;
                    sumOfRemainingPopularities += reducedFile.randomLow;
                }
            }

            // File ordinals are used as their indexes, so have to apply deltas to the node file lists for the removed indexes
            for (int nodeIdx = 0; nodeIdx < parameters.Nodes; nodeIdx++)
            {
                var writableNode = writableNodes[nodeIdx];
                foreach (var fileIdx in writableNode.ToList())
                {
                    writableNode.Remove(fileIdx);
                    writableNode.Add(fileIdx - ~Array.BinarySearch(removedFiles, fileIdx));
                }
            }

            return (Array.AsReadOnly(nodes), reducedFiles.Select(reducedFile => (reducedFile.fileSizeMegabytes,
                reducedFile.randomLow / sumOfRemainingPopularities)).ToList().AsReadOnly()); // Normalize to total probability of 100%

            void fillNodeDisk(IReadOnlyList<(int fileIdx, double fileSizeMegabytes, double randomLow)> remainingFiles,
                HashSet<int> nodeFiles, double diskSpaceMegabytes)
            {
                if (remainingFiles.Count == 0)
                {
                    return; // Node full
                }

                // Allow for a certain number of duplicates since we can just loop again and it won't affect
                // distribution since we were going to throw them out anyways; this prevents running the more
                // complex de-duplication steps as often
                int maxDuplicatesBeforeClean = remainingFiles.Count / 2;
                int duplicateCounter = 0;
                while (nodeFiles.Count < files.Count) // Stop adding files if node has every possible file
                {
                    // Generate a random uniform distribution number in the range [0, 1)
                    // This will be a lookup to our cumulative distribution for our Gaussian file popularities
                    var randomLowInsertIndex = remainingFiles.BinarySearch(file => file.randomLow,
                        randomSource.NextDouble());

                    // If it does not perfectly match, BinarySearch returns two's complement index of the highest index
                    // with a lower value
                    var randomLowIndex = randomLowInsertIndex < 0 ? ~randomLowInsertIndex - 1 : randomLowInsertIndex;

                    var (fileIdx, fileSizeMegabytes, randomLow) = remainingFiles[randomLowIndex];

                    // Specific file does not fit in remaining disk space, but other files might;
                    // also, might as well filter out duplicates
                    if (fileSizeMegabytes > diskSpaceMegabytes

                        // Node already has the random file, filter out all duplicates;
                        // also, might as well filter out too-large files
                        || (nodeFiles.Contains(fileIdx) && ++duplicateCounter >= maxDuplicatesBeforeClean))
                    {
                        fillNodeDisk(filterSmallerFilesAndDuplicatesRecurse(), nodeFiles, diskSpaceMegabytes); // Recurse
                        return;
                    }
                    else
                    {
                        nodeFiles.Add(fileIdx);
                        diskSpaceMegabytes -= fileSizeMegabytes;
                    }
                }

                IReadOnlyList<(int fileIdx, double fileSize, double randomLow)> filterSmallerFilesAndDuplicatesRecurse()
                {
                    var smallerFiles = new List<(int fileIdx, double fileSizeMegabytes, double unstandardizedFilePopularity)>();
                    var sumOfSmallerFilePopularities = 0d;
                    for (var remainingFileIdx = 0; remainingFileIdx < remainingFiles.Count; remainingFileIdx++)
                    {
                        var remainingFile = remainingFiles[remainingFileIdx];
                        if (!nodeFiles.Contains(remainingFile.fileIdx) && remainingFile.fileSizeMegabytes <= diskSpaceMegabytes)
                        {
                            var unstandardizedFilePopularity = (remainingFileIdx == remainingFiles.Count - 1
                                    ? 1d // For last file, popularity is 1 - current random low

                                    // For all but last file, popularity is next random low minus current random low
                                    : remainingFiles[remainingFileIdx + 1].randomLow) -
                                remainingFile.randomLow;
                            sumOfSmallerFilePopularities += unstandardizedFilePopularity;
                            smallerFiles.Add((remainingFile.fileIdx, remainingFile.fileSizeMegabytes, unstandardizedFilePopularity));
                        }
                    }

                    // Need to renormalize random lows for 100% total probability
                    var randomLow = 0d;
                    for (var smallerFileIdx = 0; smallerFileIdx < smallerFiles.Count; smallerFileIdx++)
                    {
                        var (fileIdx, fileSizeMegabytes, unstandardizedFilePopularity) = smallerFiles[smallerFileIdx];
                        smallerFiles[smallerFileIdx] = (fileIdx, fileSizeMegabytes, randomLow);
                        randomLow += unstandardizedFilePopularity / sumOfSmallerFilePopularities;
                    }

                    return smallerFiles;
                }
            }
        }
    }
}

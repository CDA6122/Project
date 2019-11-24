/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Models
{
    internal sealed class SimulationParameters
    {
        // Parameters initialized with starting values:
        private int _nodes = 6;
        private double _simulationLengthMinutes = 600d;
        private double _filesPerNode = 60d; // ~1 new file every 600 minutes / 60 = 10 minutes
        private double _meanFileSizeMegabytes = 357.628d; // ~10 minutes at 5,000,000 bitrate
        private double _fileSizeMegabytesStandardDeviation = 150d;
        private double _playbackBitrate = 5_000_000d;
        private double _nodeCapacityGigabytes = 20d;
        private double _maxBandwidthMegabitsPerSecond = 24d;

        // Do not make file catalog size too large:
        // (file catalog size * mean file size / number of nodes) should not be greater than node capacity
        private int _fileCatalogSize = 300;

        private double _filePopularityStandardDeviation = 0.004d;
        private int _maxSamplingAttempts = 100;
        private int _simulationRuns = 1;

        internal int Nodes { get => _nodes; }
        internal double SimulationLengthMinutes { get => _simulationLengthMinutes; }
        internal double FilesPerNode { get => _filesPerNode; }
        internal double MeanFileSizeMegabytes { get => _meanFileSizeMegabytes; }
        internal double FileSizeMegabytesStandardDeviation { get => _fileSizeMegabytesStandardDeviation; }
        internal double PlaybackBitrate { get => _playbackBitrate; }
        internal double NodeCapacityGigabytes { get => _nodeCapacityGigabytes; }
        internal double MaxBandwidthMegabitsPerSecond { get => _maxBandwidthMegabitsPerSecond; }
        internal int FileCatalogSize { get => _fileCatalogSize; }
        internal double FilePopularityStandardDeviation { get => _filePopularityStandardDeviation; }
        internal double MaxSamplingAttempts { get => _maxSamplingAttempts; }
        internal double TotalFileCatalogGigabyteSize { get => _fileCatalogSize * _meanFileSizeMegabytes / 1024d; }
        internal double FileCatalogGigabyteSizePerNode { get => TotalFileCatalogGigabyteSize / _nodes; }
        internal int SimulationRuns { get => _simulationRuns;  }

        private readonly List<FieldError> fieldsInError;
        internal SimulationParameters(List<FieldError> fieldsInError) => this.fieldsInError = fieldsInError;

        internal bool OnNodesChanged(ChangeEventArgs e)
        {
            if (OnParameterChanged(e, FieldError.Nodes, ref _nodes, ref _nodesClass, int.TryParse, belowMinValue: 2))
            {
                FileCatalogGigabyteSizePerNodeChanged();
                return true;
            }
            return false;
        }
        internal string NodesClass { get => _nodesClass; }
        private string _nodesClass = "";

        internal void OnSimulationLengthMinutesChanged(ChangeEventArgs e) =>
            OnParameterChanged(e, FieldError.SimulationLength, ref _simulationLengthMinutes,
                ref _simulationLengthMinutesClass, double.TryParse);
        internal string SimulationLengthMinutesClass { get => _simulationLengthMinutesClass; }
        private string _simulationLengthMinutesClass = "";

        internal void OnFilesPerNodeChanged(ChangeEventArgs e) =>
            OnParameterChanged(e, FieldError.FilesPerNode, ref _filesPerNode, ref _filesPerNodeClass, double.TryParse);
        internal string FilesPerNodeClass { get => _filesPerNodeClass; }
        private string _filesPerNodeClass = "";

        internal void OnMeanFileSizeMegabytesChanged(ChangeEventArgs e)
        {
            if (OnParameterChanged(e, FieldError.MeanFileSize, ref _meanFileSizeMegabytes,
                ref _meanFileSizeMegabytesClass, double.TryParse))
            {
                FileCatalogGigabyteSizePerNodeChanged();
            }
        }
        internal string MeanFileSizeMegabytesClass { get => _meanFileSizeMegabytesClass; }
        private string _meanFileSizeMegabytesClass = "";

        internal void OnFileSizeMegabytesStandardDeviationChanged(ChangeEventArgs e) =>
            OnParameterChanged(e, FieldError.FileSizeStandardDeviation, ref _fileSizeMegabytesStandardDeviation,
                ref _fileSizeMegabytesStandardDeviationClass, double.TryParse);
        internal string FileSizeMegabytesStandardDeviationClass { get => _fileSizeMegabytesStandardDeviationClass; }
        private string _fileSizeMegabytesStandardDeviationClass = "";

        internal void OnPlaybackBitrateChanged(ChangeEventArgs e) =>
            OnParameterChanged(e, FieldError.PlaybackBitrate, ref _playbackBitrate, ref _playbackBitrateClass,
                double.TryParse);
        internal string PlaybackBitrateClass { get => _playbackBitrateClass; }
        private string _playbackBitrateClass = "";

        internal void OnNodeCapacityGigabytesChanged(ChangeEventArgs e)
        {
            if (OnParameterChanged(e, FieldError.NodeCapacity, ref _nodeCapacityGigabytes,
                ref _nodeCapacityGigabytesClass, double.TryParse))
            {
                FileCatalogGigabyteSizePerNodeChanged();
            }
        }
        internal string NodeCapacityGigabytesClass { get => _nodeCapacityGigabytesClass; }
        private string _nodeCapacityGigabytesClass = "";

        internal void OnMaxBandwidthMegabitsPerSecondChanged(ChangeEventArgs e) =>
            OnParameterChanged(e, FieldError.MaxBandwidth, ref _maxBandwidthMegabitsPerSecond,
                ref _maxBandwidthMegabitsPerSecondClass, double.TryParse);
        internal string MaxBandwidthMegabitsPerSecondClass { get => _maxBandwidthMegabitsPerSecondClass; }
        private string _maxBandwidthMegabitsPerSecondClass = "";

        internal void OnFileCatalogSizeChanged(ChangeEventArgs e)
        {
            if (OnParameterChanged(e, FieldError.FileCatalogSize, ref _fileCatalogSize, ref _fileCatalogSizeClass,
                int.TryParse))
            {
                FileCatalogGigabyteSizePerNodeChanged();
            }
        }
        internal string FileCatalogSizeClass { get => _fileCatalogSizeClass; }
        private string _fileCatalogSizeClass = "";

        internal void OnFilePopularityStandardDeviationChanged(ChangeEventArgs e) =>
            OnParameterChanged(e, FieldError.FilePopularityStandardDeviation, ref _filePopularityStandardDeviation,
                ref _filePopularityStandardDeviationClass, double.TryParse);
        internal string FilePopularityStandardDeviationClass { get => _filePopularityStandardDeviationClass; }
        private string _filePopularityStandardDeviationClass = "";

        internal void OnMaxSamplingAttemptsChanged(ChangeEventArgs e) =>
            OnParameterChanged(e, FieldError.MaxSamplingAttempts, ref _maxSamplingAttempts,
                ref _maxSamplingAttemptsClass, int.TryParse, 1);
        internal string MaxSamplingAttemptsClass { get => _maxSamplingAttemptsClass; }
        private string _maxSamplingAttemptsClass = "";

        private void FileCatalogGigabyteSizePerNodeChanged()
        {
            if (FileCatalogGigabyteSizePerNode < _nodeCapacityGigabytes)
            {
                if (FileCatalogGigabyteSizePerNodeClass != "")
                {
                    FileCatalogGigabyteSizePerNodeClass = "";
                    fieldsInError.Remove(FieldError.FileCatalogGigabyteSizePerNode);
                }
            }
            else if (FileCatalogGigabyteSizePerNodeClass == "")
            {
                FileCatalogGigabyteSizePerNodeClass = "error";
                fieldsInError.Add(FieldError.FileCatalogGigabyteSizePerNode);
            }
        }
        internal string FileCatalogGigabyteSizePerNodeClass { get; private set; } = "";

        internal void OnSimulationRunsChanged(ChangeEventArgs e) =>
            OnParameterChanged(e, FieldError.SimulationRuns, ref _simulationRuns, ref _simulationRunsClass,
                int.TryParse);
        internal string SimulationRunsClass { get => _simulationRunsClass; }
        private string _simulationRunsClass = "";

        private bool OnParameterChanged<T>(ChangeEventArgs e, FieldError field, ref T value, ref string @class,
            TryParse<T> tryParse, T belowMinValue = default)
            where T : struct, IComparable<T>
        {
            if (tryParse(e.Value.ToString(), out var tempValue)
                && tempValue.CompareTo(belowMinValue) > 0)
            {
                value = tempValue;
                if (@class != "")
                {
                    fieldsInError.Remove(field);
                    @class = "";
                }
                return true;
            }

            if (@class == "")
            {
                fieldsInError.Add(field);
                @class = "error";
            }
            return false;
        }
        private delegate bool TryParse<T>(string? value, out T result);
    }
}

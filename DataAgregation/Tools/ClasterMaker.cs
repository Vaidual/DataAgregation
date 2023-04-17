using DataAgregation.ClusterModels;
using DataAgregation.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataAgregation.Tools
{
    public class ClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint ClusterId { get; set; }
    }
    public class ClasterMaker
    {
        MLContext _mlContext = new MLContext(seed:42);

        public IDataView CreateDataView(IEnumerable<ClusterInput> data)
        {
            return _mlContext.Data.LoadFromEnumerable(data);
        }

        public TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> CreateModel(IEnumerable<ClusterInput> data, int clusterAmount)
        {
            var pipeline = _mlContext.Transforms
                .Concatenate(
                    "Features",
                    "Value")
                .Append(_mlContext.Clustering.Trainers.KMeans(
                    "Features",
                    numberOfClusters: clusterAmount));

            // Train the K-means model
            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            return pipeline.Fit(dataView);
        }

        public IEnumerable<Interval> FindAgeIntervals(IEnumerable<ClusterInput> data, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            var predictions = model.Transform(dataView);
            var clusters = _mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false);
            var ageData = data.Zip(clusters, (inputData, cluster) => new { Age = inputData.Value, ClusterId = cluster.ClusterId });
            return ageData.GroupBy(a => a.ClusterId)
                .Select(g => new Interval { MinValue = (int)g.Min(a => a.Age), MaxValue = (int)g.Max(a => a.Age) }).OrderBy(e => e.MinValue).ToArray();
        }

        public int[] GetIntervalOccurrences(IEnumerable<ClusterInput> data, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            var predictions = model.Transform(dataView);
            var clusters = _mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false);
            return clusters.GroupBy(a => a.ClusterId).OrderBy(g => g.Key).Select(g => g.Count()).ToArray();
        }

        public IEnumerable<Interval> FindIntervals(IEnumerable<ClusterInput> data, int clusterAmount)
        {
            var pipeline = _mlContext.Transforms
                .Concatenate(
                    "Features",
                    "Value")
                .Append(_mlContext.Clustering.Trainers.KMeans(
                    "Features",
                    numberOfClusters: clusterAmount));

            // Train the K-means model
            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            var model = pipeline.Fit(dataView);
            var predictions = model.Transform(dataView);
            var clusters = _mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false);
            var valueClusters = data.Zip(clusters, (inputData, cluster) => new { Value = inputData.Value, ClusterId = cluster.ClusterId });
            return valueClusters.GroupBy(a => a.ClusterId)
                .Select(g => new Interval { MinValue = (int)g.Min(a => a.Value), MaxValue = (int)g.Max(a => a.Value) }).OrderBy(e => e.MinValue).ToArray();
        }

        public IEnumerable<EntrancesInTheInterval> FindAgeIntervalsAndNumberOdEntrances(IEnumerable<ClusterInput> data)
        {
            int clusterAmount = 4;
            var pipeline = _mlContext.Transforms
                .Concatenate(
                    "Features",
                    "Value")
                .Append(_mlContext.Clustering.Trainers.KMeans(
                    "Features",
                    numberOfClusters: clusterAmount));

            // Train the K-means model
            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            var model = pipeline.Fit(dataView);
            var predictions = model.Transform(dataView);
            var clusters = _mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false);
            var result = data
                .Zip(clusters, (inputData, cluster) => new 
                { 
                    Value = inputData.Value, 
                    ClusterId = cluster.ClusterId 
                })
                .GroupBy(a => a.ClusterId)
                .Select(g => new EntrancesInTheInterval
                {
                    NumberOfEntrances = g.Count(),
                    Interval = new Interval
                    {
                        MinValue = (int)g.Min(a => a.Value),
                        MaxValue = (int)g.Max(a => a.Value)
                    }
                })
                .OrderBy(x => x.Interval.MinValue)
                .ToArray();

            return result;
        }

        public IEnumerable<EntrancesInTheInterval> FindAgeIntervalsAndNumberOdEntrances(IEnumerable<ClusterInput> data, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
        {
            var predictions = model.Transform(_mlContext.Data.LoadFromEnumerable(data));
            var clusters = _mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false);
            var result = data
                .Zip(clusters, (inputData, cluster) => new
                {
                    Value = inputData.Value,
                    ClusterId = cluster.ClusterId
                })
                .GroupBy(a => a.ClusterId)
                .Select(g => new EntrancesInTheInterval
                {
                    NumberOfEntrances = g.Count(),
                    Interval = new Interval
                    {
                        MinValue = (int)g.Min(a => a.Value),
                        MaxValue = (int)g.Max(a => a.Value)
                    }
                })
                .OrderBy(x => x.Interval.MinValue)
                .ToArray();

            return result;
        }
    }
}

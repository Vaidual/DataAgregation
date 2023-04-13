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
        MLContext _mlContext = new MLContext();

        public Dictionary<string, int> CreateClastersByAge3(IEnumerable<UserCluster> data)
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
            Dictionary<string, int> result = new Dictionary<string, int>();
            var ageData = data.Zip(clusters, (inputData, cluster) => new { UserId = inputData.UserId, ClusterId = cluster.ClusterId });
            foreach (var o in ageData)
            {
                result.Add(o.UserId, (int)o.ClusterId);
            }
            return result;
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

        public AgeInterval[] FindIntervals(IEnumerable<ClusterInput> data, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            var predictions = model.Transform(dataView);
            var clusters = _mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false);
            var ageData = data.Zip(clusters, (inputData, cluster) => new { Age = inputData.Value, ClusterId = cluster.ClusterId });
            return ageData.GroupBy(a => a.ClusterId)
                .Select(g => new AgeInterval { MinAge = (int)g.Min(a => a.Age), MaxAge = (int)g.Max(a => a.Age) }).ToArray();
        }

        public int[] GetIntervalsEnters(IEnumerable<ClusterInput> data, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            var predictions = model.Transform(dataView);
            var clusters = _mlContext.Data.CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false);
            return clusters.GroupBy(a => a.ClusterId).OrderBy(g => g.Key).Select(g => g.Count()).ToArray();
        }

        public AgeInterval[] CreateClastersByAge2(IEnumerable<UserCluster> data)
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
            var ageData = data.Zip(clusters, (inputData, cluster) => new { Age = inputData.Value, ClusterId = cluster.ClusterId });
            return ageData.GroupBy(a => a.ClusterId)
                .Select(g => new AgeInterval { MinAge = (int)g.Min(a => a.Age), MaxAge = (int)g.Max(a => a.Age) }).ToArray();
        }
    }
}

// Accord.NET Sample Applications
// http://accord-framework.net
//
// Copyright © 2009-2017, César Souza
// All rights reserved. 3-BSD License:
//
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//
//      * Redistributions of source code must retain the above copyright
//        notice, this list of conditions and the following disclaimer.
//
//      * Redistributions in binary form must reproduce the above copyright
//        notice, this list of conditions and the following disclaimer in the
//        documentation and/or other materials provided with the distribution.
//
//      * Neither the name of the Accord.NET Framework authors nor the
//        names of its contributors may be used to endorse or promote products
//        derived from this software without specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//  DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
//  DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//  LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//  ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Drawing;
using System.Windows.Forms;
using Accord.Controls;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.Multivariate;
using ZedGraph;

namespace SampleApp
{
    /// <summary>
    ///   Clustering sample application. This application produces random data from
    ///   overlapping multivariate Gaussian distributions, and then attempts to 
    ///   estimate the original models from the data again using Gaussian mixtures.
    /// </summary>
    /// 
    public partial class MainForm : Form
    {
        // Visually distinct colors used in the pie graphics
        ColorSequenceCollection colors = new ColorSequenceCollection();

        int k; // the number of clusters assumed present in the data

        double[][] observations; // the data points containing the mixture

        KMeans kmeans;




        /// <summary>
        ///   Creates the initial scatter plot graphs containing some random
        ///   data. This data is generated by sampling Gaussian distributions.
        /// </summary>
        /// 
        private void btnGenerateRandom_Click(object sender, EventArgs e)
        {
            k = (int)numClusters.Value;

            // Generate data with n Gaussian distributions
            double[][][] data = new double[k][][];

            for (int i = 0; i < k; i++)
            {
                // Create random centroid to place the Gaussian distribution
                double[] mean = Vector.Random(2, -6.0, +6.0);

                // Create random covariance matrix for the distribution
                double[,] covariance = Accord.Statistics.Tools.RandomCovariance(2, -5, 5);

                // Create the Gaussian distribution
                var gaussian = new MultivariateNormalDistribution(mean, covariance);

                int samples = Accord.Math.Random.Generator.Random.Next(150, 250);
                data[i] = gaussian.Generate(samples);
            }

            // Join the generated data
            observations = Matrix.Stack(data);

            // Update the scatter plot
            CreateScatterplot(graph, observations, k);

            // Forget previous initialization
            kmeans = null;
        }

        /// <summary>
        ///   Estimates Gaussian distributions from the data.
        /// </summary>
        /// 
        private void btnCompute_Click(object sender, EventArgs e)
        {
            // Create a new Gaussian Mixture Model
            var gmm = new GaussianMixtureModel(k);

            // If available, initialize with k-means
            if (kmeans != null) 
                gmm.Initialize(kmeans);

            // Compute the model
            GaussianClusterCollection clustering = gmm.Learn(observations);

            // Classify all instances in mixture data
            int[] classifications = clustering.Decide(observations);

            // Draw the classifications
            updateGraph(classifications);
        }

        /// <summary>
        ///   Initializes the Gaussian Mixture Models using K-Means
        ///   parameters as an initial parameter guess.
        /// </summary>
        /// 
        private void btnInitialize_Click(object sender, EventArgs e)
        {
            // Creates and computes a new 
            // K-Means clustering algorithm:

            kmeans = new KMeans(k);

            KMeansClusterCollection clustering = kmeans.Learn(observations);

            // Classify all instances in mixture data
            int[] classifications = clustering.Decide(observations);

            // Draw the classifications
            updateGraph(classifications);
        }




        private void updateGraph(int[] classifications)
        {
            // Paint the clusters accordingly
            for (int i = 0; i < k + 1; i++)
                graph.GraphPane.CurveList[i].Clear();

            for (int j = 0; j < observations.Length; j++)
            {
                int c = classifications[j];

                var curveList = graph.GraphPane.CurveList[c + 1];
                double[] point = observations[j];
                curveList.AddPoint(point[0], point[1]);
            }

            graph.Invalidate();
        }

        public MainForm()
        {
            InitializeComponent();
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            btnGenerateRandom_Click(this, e);
        }


        public void CreateScatterplot(ZedGraphControl zgc, double[][] graph, int n)
        {
            GraphPane myPane = zgc.GraphPane;
            myPane.CurveList.Clear();

            // Set graph pane object
            myPane.Title.Text = "Normal (Gaussian) Distributions";
            myPane.XAxis.Title.Text = "X";
            myPane.YAxis.Title.Text = "Y";
            myPane.XAxis.Scale.Max = 10;
            myPane.XAxis.Scale.Min = -10;
            myPane.YAxis.Scale.Max = 10;
            myPane.YAxis.Scale.Min = -10;
            myPane.XAxis.IsAxisSegmentVisible = false;
            myPane.YAxis.IsAxisSegmentVisible = false;
            myPane.YAxis.IsVisible = false;
            myPane.XAxis.IsVisible = false;
            myPane.Border.IsVisible = false;


            // Create mixture pairs
            PointPairList list = new PointPairList();
            for (int i = 0; i < graph.Length; i++)
                list.Add(graph[i][0], graph[i][1]);


            // Add the curve for the mixture points
            LineItem myCurve = myPane.AddCurve("Mixture", list, Color.Gray, SymbolType.Diamond);
            myCurve.Line.IsVisible = false;
            myCurve.Symbol.Border.IsVisible = false;
            myCurve.Symbol.Fill = new Fill(Color.Gray);

            for (int i = 0; i < n; i++)
            {
                // Add curves for the clusters to be detected
                Color color = colors[i];
                myCurve = myPane.AddCurve("D" + (i + 1), new PointPairList(), color, SymbolType.Diamond);
                myCurve.Line.IsVisible = false;
                myCurve.Symbol.Border.IsVisible = false;
                myCurve.Symbol.Fill = new Fill(color);
            }

            // Fill the background of the chart rect and pane
            myPane.Fill = new Fill(Color.WhiteSmoke);

            zgc.AxisChange();
            zgc.Invalidate();
        }

    }
}

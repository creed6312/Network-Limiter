using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Visifire.Charts;

namespace NetworkLimiter
{
    class Chart
    {
        public Visifire.Charts.Chart visiChart;
        public Visifire.Charts.DataSeries dataSeries;
        public Visifire.Charts.DataPoint dataPoint;
        public Visifire.Charts.Title title;
        public Grid ChartLayout;

        public void CreateChart(String Title, Visifire.Charts.RenderAs Type, List<String> AxisX, List<double> AxisY, string yFormat, Grid ChartLayout)
        {
            visiChart = new Visifire.Charts.Chart();
            dataSeries = new Visifire.Charts.DataSeries();
            title = new Visifire.Charts.Title();
            this.ChartLayout = ChartLayout;

            title.Text = Title;
            title.FontSize = 28;
            visiChart.Titles.Add(title);
            dataSeries.RenderAs = Type;
            dataSeries.ShowInLegend = true;

            try
            {
                Visifire.Charts.Axis yAxis = new Visifire.Charts.Axis() { ValueFormatString = yFormat };
                Visifire.Charts.Axis xAxis = new Visifire.Charts.Axis() { };

                xAxis.AxisLabels = new AxisLabels()
                {
                    Angle = 0,
                    Interval = 1,
                    FontSize = 15,
                    FontFamily = new System.Windows.Media.FontFamily("Calibri"),
                };

                yAxis.AxisLabels = new AxisLabels()
                {
                    Angle = 0,
                    FontSize = 15,
                    FontFamily = new System.Windows.Media.FontFamily("Calibri"),
                };
                visiChart.AxesX.Add(xAxis);
                visiChart.AxesY.Add(yAxis);
                dataSeries.YValueFormatString = yFormat;
                for (int i = 0; i < AxisY.Count; i++)
                {
                    dataPoint = new Visifire.Charts.DataPoint();
                    dataPoint.LabelFontSize = 15;
                    dataPoint.FontSize = 14;
                    if (AxisX.Count > i)
                        dataPoint.AxisXLabel = AxisX[i];
                    dataPoint.YValue = AxisY[i];
                    dataSeries.DataPoints.Add(dataPoint);
                }

                visiChart.Series.Add(dataSeries);
            }
            catch (Exception esfa)
            {
                MessageBox.Show(esfa.Message.ToString());
            }
        }

        public void DrawChart()
        {
            ChartLayout.Children.Clear();
            ChartLayout.Children.Add(visiChart);
        }
    }
}

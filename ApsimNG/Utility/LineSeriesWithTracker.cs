﻿namespace Utility
{
    using System;
    using APSIM.Shared.Utilities;
    using OxyPlot;
    using OxyPlot.Series;
    using UserInterface.EventArguments;

    /// <summary>
    /// A line series with a better tracker.
    /// </summary>
    public class LineSeriesWithTracker : LineSeries
    {
        /// <summary>
        /// Invoked when the user hovers over a series point.
        /// </summary>
        public event EventHandler<HoverPointArgs> OnHoverOverPoint;

        /// <summary>
        /// Name of the variable behind the X data.
        /// </summary>
        public string XFieldName { get; set; }

        /// <summary>
        /// Name of the variable behind the Y data.
        /// </summary>
        public string YFieldName { get; set; }

        /// <summary>
        /// Type of the x variable
        /// </summary>
        public Type XType { get; set; }

        /// <summary>
        /// Type of the y variable
        /// </summary>
        public Type YType { get; set; }

        /// <summary>
        /// Tracker is calling to determine the nearest point.
        /// </summary>
        /// <param name="point">The point clicked</param>
        /// <param name="interpolate">A value indicating whether interpolation should be used.</param>
        /// <returns>The return hit result</returns>
        public override TrackerHitResult GetNearestPoint(OxyPlot.ScreenPoint point, bool interpolate)
        {
            TrackerHitResult hitResult = base.GetNearestPoint(point, interpolate);

            if (hitResult != null && OnHoverOverPoint != null)
            {
                HoverPointArgs e = new HoverPointArgs();
                if (Title == null)
                    e.SeriesName = ToolTip;
                else
                    e.SeriesName = Title;

                e.X = hitResult.DataPoint.X;
                e.Y = hitResult.DataPoint.Y;
                OnHoverOverPoint.Invoke(this, e);

                string xInput = "{2}";
                string yInput = "{4}";

                if (XType == typeof(double))
                    xInput = MathUtilities.RoundSignificant(hitResult.DataPoint.X, 2).ToString();

                if (YType == typeof(double))
                    yInput = MathUtilities.RoundSignificant(hitResult.DataPoint.Y, 2).ToString();

                if (e.HoverText != null)
                    hitResult.Series.TrackerFormatString = e.HoverText + "\n" + XFieldName + ": " + xInput + "\n" + YFieldName + ": " + yInput;
            }

            return hitResult;
        }
    }
}
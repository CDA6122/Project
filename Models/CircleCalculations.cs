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
    internal sealed class CircleCalculations
    {
        private readonly SimulationParameters parameters;
        private readonly double maxRadius;

        internal void OnNodesChanged(ChangeEventArgs e)
        {
            if (parameters.OnNodesChanged(e))
            {
                RecalculateCircles();
            }
        }

        // Bibliography reference 5 in Index.razor (main page). Adapted from answer by Michael Hardy to:
        // https://math.stackexchange.com/questions/278642/%D0%9D%D0%BEw-many-equal-circles-can-be-placed-around-a-circle#answer-278666
        // Adapted for Project
        private void RecalculateCircles()
        {
            // For placement of the circle centers
            DegreesPerNode = 360d / parameters.Nodes;

            // For the radius of the circle required to fit them all in nicely, constrained to a max size
            Radius = Math.Min(maxRadius, 35d * Math.Sin(Math.PI / parameters.Nodes / 2d));
        }

        internal double DegreesPerNode { get; private set; }
        internal double Radius { get; private set; }

        internal CircleCalculations(SimulationParameters parameters, double maxRadius)
        {
            this.parameters = parameters;
            this.maxRadius = maxRadius;
            RecalculateCircles();
        }
    }
}

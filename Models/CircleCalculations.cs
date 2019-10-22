/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Models
{
    internal sealed class CircleCalculations
    {
        internal double MaxRadius { get; }

        internal int Nodes
        {
            get => _nodes;
            set
            {
                // For placement of the circle centers
                DegreesPerNode = 360d / value;

                // For the radius of the circle required to fit them all in nicely, constrained to a max size
                Radius = Math.Min(MaxRadius, 35d * Math.Sin(Math.PI / value / 2d));

                _nodes = value;
            }
        }
        int _nodes;

        internal double DegreesPerNode { get; private set; }
        internal double Radius { get; private set; }

        internal CircleCalculations(double maxRadius, int startingNodes)
        {
            MaxRadius = maxRadius;
            Nodes = startingNodes;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Clapeyron
{
    /// <summary>
    /// Class for optimising the beam volume
    /// </summary>
    public class OptimiseBeam
    {
        /// runs * n where n = len(hi)+len(bi)
        public int runs = 20000;
        public double[] b_lim = new double[2];
        public double[] h_lim = new double[2];
        /// s_allowed placeholder, value in kN/m2
        public double s_allowed = 0.0;
        /// safety factor for s_allowed
        public double safety = 1.0;
        private readonly Random rnd = new Random();

        /// <summary>
        /// Optimise the beam
        /// </summary>
        /// <returns>Volume of the optimised beam, b for each beam part, h for each beam part</returns>
        public (double, double[], double[]) optimise(Beam bm, double[] b_l, double[] h_l, double s_allow)
        {
            /// Imported s_allow in MPa, convert to kN/m2
            s_allowed = s_allow * 1000;
            b_lim = b_l;
            h_lim = h_l;
            var cnt = bm.parts.Count;
            double v_beam = 10e10;
            var b = new double[cnt];
            var h = new double[cnt];
            var w = new double[cnt];
            for (int i = 0; i < runs * cnt * 2; i++)
            {
                (double v_tst, double[] b_tst, double[] h_tst) = run(bm);
                if (v_tst < v_beam)
                {
                    v_beam = v_tst;
                    b = b_tst;
                    h = h_tst;
                }
            }
            return (v_beam, b, h);
        }

        /// <summary>
        /// Monte Carlo for optimising the beam
        /// </summary>
        private (double, double[], double[]) run(Beam bm)
        {
            List<double> v_beam = new List<double>();
            var b = new double[bm.parts.Count];
            var h = new double[bm.parts.Count];
            var w = new double[bm.parts.Count];
            for (int i = 0; i < bm.parts.Count; i++)
            {
                b[i] = random_double(b_lim[0], b_lim[1]);
                h[i] = random_double(h_lim[0], h_lim[1]);
                w[i] = b[i] * h[i] * h[i] / 6;
                /// If the beam's s exceeds the allowed, this solution must be discarded. Adding big b and h will discard the solution.
                if (bm.m_y_max[i] / w[i] > s_allowed / safety)
                {
                    b[i] = 100.00;
                    h[i] = 100.00;
                }
                v_beam.Add(b[i] * h[i] * bm.parts[i].length);
            }
            return (v_beam.Sum(), b, h);
        }

        /// <summary>
        /// Get a random double value between minimum and maximum values specified
        /// </summary>
        private double random_double(double minimum, double maximum)
        {
            return rnd.NextDouble() * (maximum - minimum) + minimum;
        }
    }
}


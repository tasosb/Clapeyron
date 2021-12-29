using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Clapeyron
{
    /// <summary>
    /// Class that defines the whole beam
    /// </summary>
    public class Beam
    {
        public List<int> parts_order = new List<int>();
        public List<BeamPart> parts = new List<BeamPart>();
        public List<Load> loads = new List<Load>();
        public double[] v_y_max;
        public double[] m_y_max;


        /// <summary>
        /// Create and add a beam part to the beam
        /// </summary>
        /// <param name="num">ID of the beam part</param>
        /// <param name="lengths">Length of the beam part</param>
        /// <param name="iner">Inertia of the beam</param>
        /// <param name="tp">Type of the beam part</param>
        public void addparts(int num, double lengths, double iner, int tp = 0)
        {
            parts.Add(new BeamPart(num, lengths, iner, tp));
        }

        /// <summary>
        /// Create and add a load to the beam part
        /// </summary>
        /// <param name="nr">ID of the load</param>
        /// <param name="beamp">Beam in which the load is applied</param>
        /// <param name="load">Load value (in kN or kN/m)</param>
        /// <param name="loadtype">True for uniform loads, False for concentrated loads</param>
        /// <param name="load_dist">(Only for concentrated loads) Distance of the load from the left part of the beam</param>
        public void addloads(int nr, BeamPart beamp, double load, bool loadtype, double load_dist = 0)
        {
            Load ld = new Load(nr, beamp, load, loadtype, load_dist);
            loads.Add(ld);
            ld.calcgr();
        }

        /// <summary>
        /// Calculate the moments at the supports of each beam part
        /// </summary>
        /// <returns>Array with the moments</returns>
        public double[] calcM()
        {
            int len = parts.Count;
            double[] m = new double[len + 1];
            double[] d = new double[len];
            double[] maxi = new double[len];
            /// We consider the beam start and end are of type 0 (M=0 at the beam's start and end)
            m[0] = 0;
            m[len] = 0;
            /// Find the maximum inertia of the beam parts for ic
            for (int i = 0; i < len; i++)
            {
                maxi[i] = parts[i].inertia;
            }
            double ic = maxi.Max();
            /// Calculate d for each beam part
            for (int i = 0; i < len; i++)
            {
                d[i] = parts[i].length * ic / parts[i].inertia;
            }
            /// Solve the set of linear equations
            var a = Matrix<double>.Build.Dense(len + 1, len + 1, 0.0);
            var b = Vector<double>.Build.Dense(new double[len + 1]);
            a[0, 0] = 1.0;
            b[0] = m[0];
            a[len, len] = 1.0;
            b[len] = m[1];

            for (int i = 0; i < len - 1; i++)
            {
                a[i + 1, i] = d[i];
                a[i + 1, i + 1] = 2 * (d[i] + d[i + 1]);
                a[i + 1, i + 2] = d[i + 1];
                b[i + 1] = -d[i] * parts[i].r - d[i + 1] * parts[i + 1].g;
            }
            var mr = a.Solve(b);
            return mr.ToArray();
        }

        /// <summary>
        /// Calculate the transverse forces
        /// </summary>
        /// <param name="m">Array of moments at the supports </param>
        /// <returns>Array with the transverse forces</returns>
        public double[] calcV(double[] m)
        {
            double[] v = new double[m.Count()];
            double v_all = 0.0;
            for (int i = 0; i < parts.Count; i++)
            {
                double v_member = 0.0;
                double v_left = m[i + 1] - m[i];
                foreach (Load load in loads)
                {
                    if (!load.inpart(parts[i])) continue;
                    if (load.uniform)
                    {
                        v_member += load.loadval * parts[i].length;
                        v_left += load.loadval * parts[i].length * parts[i].length / 2;
                    }
                    else
                    {
                        v_member += load.loadval;
                        v_left += load.loadval * (parts[i].length - load.distance);
                    }
                }
                v[i] += v_left / parts[i].length;
                v[i + 1] += v_member - v_left / parts[i].length;
                v_all += v_member;
            }
            return v;
        }

        /// <summary>
        /// Calculate M and V for all the beam.
        /// </summary>
        /// <param name="m">Array of M at supports</param>
        /// <param name="v">Array of V at supports</param>
        public (List<double>, List<double>, List<double>) calcVM(double[] m, double[] v)
        {
            List<double> x = new List<double>();
            List<double> v_y = new List<double>();
            List<double> m_y = new List<double>();
            v_y_max = new double[parts.Count];
            m_y_max = new double[parts.Count];

            x.Add(0.00);
            v_y.Add(0.00);
            m_y.Add(0.00);
            for (int i = 0; i < parts.Count; i++)
            {
                double last_x = x.Last();
                double last_v = v_y.Last();
                double len = 0.0;
                while (len <= parts[i].length)
                {
                    double v_add = 0.0;
                    double m_add = 0.0;
                    v_add += last_v - v[i];
                    double len_passed = len;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        len_passed += parts[j].length;
                        m_add -= v[j] * len_passed;
                    }
                    m_add -= v[i] * len;
                    foreach (Load load in loads)
                    {
                        if (is_loadmember_before_beam(load, parts[i]))
                        {
                            var prev_length = get_previous_length(load, parts[i]);
                            if (load.uniform)
                            {
                                m_add += load.loadval * prev_length[1] * (prev_length[0] + len - prev_length[1] / 2);
                            }
                            else
                            {
                                m_add += load.loadval * (len + prev_length[0] - load.distance);
                            }

                        }
                        if (!load.inpart(parts[i])) continue;
                        if (load.uniform)
                        {
                            v_add += load.loadval * len;
                            m_add += load.loadval * len * len / 2;
                        }
                        else
                        {
                            if (len >= load.distance)
                            {
                                v_add += load.loadval;
                                m_add += load.loadval * (len - load.distance);
                            }
                        }
                    }
                    x.Add(len + last_x);
                    v_y.Add(v_add);
                    if (Math.Abs(v_add) > v_y_max[i]) v_y_max[i] = Math.Abs(v_add);
                    m_y.Add(m_add);
                    if (Math.Abs(m_add) > m_y_max[i]) m_y_max[i] = Math.Abs(m_add);
                    len += 0.01;
                }

            }
            x.RemoveAt(0);
            v_y.RemoveAt(0);
            m_y.RemoveAt(0);
            return (x, v_y, m_y);
        }

        /// <summary>
        /// Exports V and M diagrams to a png image
        /// </summary>
        public void drawVM(List<double> x, List<double> v_y, List<double> m_y)
        {
            using (var bmp = new Bitmap(500, 2000))
            using (var gfx = Graphics.FromImage(bmp))
            using (var pen = new Pen(Color.Black))
            {
                gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                gfx.Clear(Color.White);
                var pt1 = new PointF((float)x.First() * 10 + 100, 500);
                var pt2 = new PointF((float)x.Last() * 10 + 100, 500);
                gfx.DrawLine(pen, pt1, pt2);
                pt1 = new PointF((float)x.First() * 10 + 100, 1500);
                pt2 = new PointF((float)x.Last() * 10 + 100, 1500);
                gfx.DrawLine(pen, pt1, pt2);
                for (int i = 0; i < x.Count - 1; i++)
                {
                    pt1 = new PointF((float)x[i] * 10 + 100, (float)(-v_y[i] + 500.00));
                    pt2 = new PointF((float)x[i + 1] * 10 + 100, (float)(-v_y[i + 1] + 500.00));
                    gfx.DrawLine(pen, pt1, pt2);

                    pt1 = new PointF((float)x[i] * 10 + 100, (float)(-m_y[i] + 1500.00));
                    pt2 = new PointF((float)x[i + 1] * 10 + 100, (float)(-m_y[i + 1] + 1500.00));
                    gfx.DrawLine(pen, pt1, pt2);
                }
                bmp.Save("V_and_M.png");
            }
        }

        /// <summary>
        /// Checks if the load is applied left from the beginning of the beam
        /// </summary>
        private bool is_loadmember_before_beam(Load ld, BeamPart bm)
        {
            var bm_order = parts_order.IndexOf(bm.id);
            var ld_member_order = parts.IndexOf(ld.part);
            return bm_order > ld_member_order;
        }

        /// <summary>
        /// Finds the length from the beginning of the load examined to the beginning of the beam examined
        /// </summary>
        /// <returns>Array[0]: DX, Array[1]: Length of beam in which load is applied</returns>
        private double[] get_previous_length(Load ld, BeamPart bm)
        {
            var bm_order = parts_order.IndexOf(bm.id);
            var ld_member_order = parts.IndexOf(ld.part);
            if (bm_order <= ld_member_order)
            {
                throw new Exception("Member in which the load is applied is after the member examined");
            }
            double[] prev_length = new double[2]; // 0->total_length 1-> load_length
            prev_length[0] = 0.00;
            for (int i = ld_member_order; i < bm_order; i++)
            {
                prev_length[0] += parts[i].length;
            }
            prev_length[1] = ld.part.length;
            return prev_length;

        }
    }
}

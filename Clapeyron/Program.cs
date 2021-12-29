using System;
using System.Collections.Generic;
using System.Globalization;

namespace Clapeyron
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] lines = System.IO.File.ReadAllLines(@"D:\temp\beam1.txt");
            var beam = new Beam();
            var tp = 0;
            foreach (string line in lines)
            {
                if (line[0] == '#')
                {
                    continue;
                }
                else if (line.Contains("SPAN"))
                {
                    tp = 1;
                }
                else if (line.Contains("CONCENTRATED LOADS"))
                {
                    tp = 2;
                }
                else if (line.Contains("UNIFORM LOADS"))
                {
                    tp = 3;
                }
                else
                {
                    string[] stringValues = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tp == 1)
                    {
                        int nr = Int16.Parse(stringValues[0]);
                        double len = Double.Parse(stringValues[1], CultureInfo.InvariantCulture);
                        double iner = Double.Parse(stringValues[2], CultureInfo.InvariantCulture);
                        beam.parts_order.Add(nr);
                        beam.addparts(nr, len, iner);
                    }
                    else if (tp == 2)
                    {
                        int nr = Int16.Parse(stringValues[0]);
                        double x = Double.Parse(stringValues[1], CultureInfo.InvariantCulture);
                        double p = Double.Parse(stringValues[2], CultureInfo.InvariantCulture);
                        beam.addloads(nr, beam.parts[beam.parts_order.IndexOf(nr)], p, false, x);
                    }
                    else if (tp == 3)
                    {
                        int nr = Int16.Parse(stringValues[0]);
                        double p = Double.Parse(stringValues[1], CultureInfo.InvariantCulture);
                        beam.addloads(nr, beam.parts[beam.parts_order.IndexOf(nr)], p, true);
                    }

                }
            }
            Console.WriteLine("Input Done!");
            var m = beam.calcM();
            var v = beam.calcV(m);
            (List<double> xy, List<double> v_y, List<double> m_y) = beam.calcVM(m, v);
            beam.drawVM(xy, v_y, m_y);
            Console.WriteLine("Ropes:");
            Console.WriteLine("[{0}]", string.Join(", ", m));
            Console.WriteLine("Antidraseis:");
            Console.WriteLine("[{0}]", string.Join(", ", v));
            var opt = new OptimiseBeam();
            var b_lim = new double[] { 0.2, 0.8 };
            var h_lim = new double[] { 0.5, 1.5 };
            double s_allowed = 20.0;
            (double volume, double[] b, double[] h) = opt.optimise(beam, b_lim, h_lim, s_allowed);
            Console.WriteLine("Optimisation");
            Console.WriteLine("Volume:");
            Console.WriteLine(volume);
            Console.WriteLine("B:");
            Console.WriteLine("[{0}]", string.Join(", ", b));
            Console.WriteLine("H:");
            Console.WriteLine("[{0}]", string.Join(", ", h));
        }

    }
}

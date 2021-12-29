namespace Clapeyron
{
    /// <summary>
    /// Class that defines a single type of load
    /// </summary>
    public class Load
    {
        public int id;
        public BeamPart part;
        public bool uniform;
        public double distance;
        public double loadval;

        /// <summary>
        /// Initialize the load
        /// </summary>
        /// <param name="nr">ID of the load</param>
        /// <param name="beamp">Beam in which the load is applied</param>
        /// <param name="load">Load value (in kN or kN/m)</param>
        /// <param name="loadtype">True for uniform loads, False for concentrated loads</param>
        /// <param name="load_dist">(Only for concentrated loads) Distance of the load from the left part of the beam</param>
        public Load(int nr, BeamPart beamp, double load, bool loadtype, double load_dist = 0)
        {
            id = nr;
            part = beamp;
            uniform = loadtype;
            distance = load_dist;
            loadval = load;
        }

        /// <summary>
        /// Calculate variables G and R for the clapeyron method
        /// </summary>
        public void calcgr()
        {
            if (uniform)
            {
                part.g += loadval * part.length * part.length / 4;
                part.r += part.g;
                return;
            }
            double a = distance;
            double b = part.length - a;
            part.g += loadval * a * b * (part.length + b) / (part.length * part.length);
            part.r += loadval * a * b * (part.length + a) / (part.length * part.length);
        }

        /// <summary>
        /// Check if load is applied on the specified beam member
        /// </summary>
        /// <param name="part_chk">Beam member to be examined</param>
        public bool inpart(BeamPart part_chk)
        {
            return part == part_chk;
        }

    }
}

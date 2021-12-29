namespace Clapeyron
{
    /// <summary>
    /// Class for the parts of the beam
    /// </summary>
    public class BeamPart
    {
        public int id;
        public double length;
        public double inertia;
        public double type;
        public double g = 0;
        public double r = 0;

        /// <summary>
        /// Initialise the class
        /// </summary>
        /// <param name="num">ID of the beam part</param>
        /// <param name="lengths">Length of the beam part</param>
        /// <param name="iner">Inertia of the beam part</param>
        /// <param name="tp">Type of the beam part</param>
        public BeamPart(int num, double lengths, double iner, int tp)
        {
            id = num;
            length = lengths;
            inertia = iner;
            type = tp;
        }
    }
}

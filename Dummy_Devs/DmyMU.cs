using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


namespace Instrument.DummyDevices
{

    /// <summary>
    /// Device for simulating an measurement-device
    /// </summary>
    public class DmyMU
    {
        private Random _rnd;
        private double _lowerBound;
        private double _upperBound;

        public DmyMU(double lowerBound = 100e-15, double upperBound = 100e-9)
        {
            _rnd = new Random(Guid.NewGuid().GetHashCode());
            _lowerBound = lowerBound;
            _upperBound = upperBound;
        }



        /// <summary>
        /// Cleans up the object-ressources.
        /// </summary>
        /// <remark>
        /// Added by MaHa
        /// </remark>
        public void Dispose()
        {
        }



        public string Info()
        {
            return "Dummy Measurement Unit";
        }

        public void Init()
        {
        }

        public double GetValue()
        {
            return _rnd.NextDouble() * (_upperBound - _lowerBound) + _lowerBound;
        }

        public void SafeMode()
        {
        }
    }
}

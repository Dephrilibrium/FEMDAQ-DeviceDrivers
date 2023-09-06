using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


namespace Instrument.DummyDevices
{
    public class DmySMU
    {
        private DmyMU _mu;
        private DmySU _su;



        public DmySMU(double lowerBound = 100e-15, double upperBound = 100e-9)
        {
            _mu = new DmyMU(lowerBound, upperBound);
            _su = new DmySU();
        }



        /// <summary>
        /// Cleans up the object-ressources.
        /// </summary>
        /// <remark>
        /// Added by MaHa
        /// </remark>
        public void Dispose()
        {
            _mu.Dispose();
            _su.Dispose();
        }



        public string Info()
        {
            return "Dummy Source Measurement Unit";
        }



        public void Init()
        {
            _mu.Init();
            _su.Init();
        }



        public void SetOutputValue(double outputValue)
        {
            _su.SetOutputValue(outputValue);
        }



        public double GetOutputValue()
        {
            return _su.GetOutputValue();
        }



        public double GetCurrent()
        {
            return _mu.GetValue();
        }



        public void SafeMode()
        {
            _mu.SafeMode();
            _su.SafeMode();
        }
    }
}

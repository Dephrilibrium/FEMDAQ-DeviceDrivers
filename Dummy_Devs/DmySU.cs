using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


namespace Instrument.DummyDevices
{
    public class DmySU
    {
        private double _lastOutputValue;

        public DmySU()
        {
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
            return "Dummy Source Unit";
        }



        public void Init()
        {
        }



        public void SetOutputValue(double outputValue)
        {
            _lastOutputValue = outputValue;
        }
        
        

        public double GetOutputValue()
        {
            return _lastOutputValue;
        }



        public void SafeMode()
        {
            _lastOutputValue = 0;
        }
    }
}

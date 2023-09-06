using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// own usings
//using NationalInstruments.NI4882;
using KeithleyInstruments.KeithleyDMM7510.Interop;

namespace Keithley
{
    public enum DMM7510_VoltageRange { Error = -1, Auto, U_100mV, U_1V, U_10V, U_100V, U_1kV };
    public enum DMM7510_CurrentRange { Error = -1, Auto, I_10µA, I_100µA, I_1mA, I_10mA, I_100mA, I_1A, I_3A }


    public class DMM7510_USB
    {
        private IKeithleyDMM7510 _device;
        //private Device _device;

        // Common parameters
        private string _measureFunction;    // VOLTage/CURRent as DC/AC. E.g. "VOLT:DC"
        private double _integrationRate;    // [s]
        private int _autoZero;

        // Voltage parameters
        private DMM7510_VoltageRange _voltageRange;

        // Current parameters
        private DMM7510_CurrentRange _currentRange;


        
        /// <summary>
        /// Contructs the communication device.
        /// </summary>
        /// <param name="boardNum"></param>
        /// <param name="primAddr"></param>
        /// <param name="secAddr"></param>
        public DMM7510_USB(string USBAddress)
        {
            //_device = new Device(0, (byte)primAddr, (byte)secAddr);
            _device = new KeithleyDMM7510();
            try
            {
                _device.Initialize(USBAddress, false, false, "");
                if (!_device.Initialized)
                    throw new Exception("Failed initializing DMM7510_USB: " + USBAddress);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        /// <summary>
        /// Cleans up the ressources of the device.
        /// </summary>
        public void Dispose()
        {
            if (_device != null)
                //_device.Dispose();
                _device.Close();
        }



    }
}

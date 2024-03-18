using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// own usings
using NationalInstruments.NI4882;

namespace Keithley
{
    public enum DMM7510_VoltageRange { Error = -1, Auto, U_100mV, U_1V, U_10V, U_100V, U_1kV };
    public enum DMM7510_CurrentRange { Error = -1, Auto, I_10µA, I_100µA, I_1mA, I_10mA, I_100mA, I_1A, I_3A }


    public class DMM7510
    {
        private Device _device;

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
        public DMM7510(int boardNum, int primAddr, int secAddr)
        {
            _device = new Device(0, (byte)primAddr, (byte)secAddr);
            var info = Info; // Check connection
            Init();
        }



        /// <summary>
        /// Cleans up the ressources of the device.
        /// </summary>
        public void Dispose()
        {
            if (_device != null)
                _device.Dispose();
        }



        /// <summary>
        /// Returns a string which contains device-information.
        /// </summary>
        /// <returns></returns>
        public string Info
        {
            get
            {
                _device.Write("*IDN?");             // Get device-info
                return _device.ReadString();
            }
        }



        /// <summary>
        /// Resets the device-parameters and could be used for initialization-stuff.
        /// </summary>
        private void Init()
        {
            _device.Write("*RST");              // Reset device
            _device.IOTimeout = TimeoutValue.T10s;  // Prepare timeout
            /* Put your init-stuff here */
        }


        
        /// <summary>
        /// Sets up the given function type without adjusting any parameters.
        /// </summary>
        /// <param name="Function"></param>
        /// <returns></returns>
        private string SetMeasureFunction(string Function)
        {
            _device.Write("FUNC \"" + Function + "\"");
            _device.Write("FUNC?");
            string response = _device.ReadString();
            //response = response.Remove(0, 1);                   // Remove beginning '"'
            response = response.Remove(response.Length - 1);    // Remove ending "\"\n"
            return response;
        }



        /// <summary>
        /// Changes the state of autozero mode by the given value.
        /// 
        /// Returns -1 if an error appeared. Otherwise you get the currently state (0=OFF/1=ON).
        /// </summary>
        /// <param name="AutoZero"></param>
        /// <returns></returns>
        private int SetAutoZero(bool AutoZero)
        {
            _device.Write(_measureFunction + ":AZER " + (AutoZero ? "ON" : "OFF"));
            _device.Write(_measureFunction + ":AZER?");
            string response = _device.ReadString();
            response = response.Remove(response.Length - 1);
            if (response == "0" || response == "1")
                return (response[0] - '0');

            return -1;
        }



        /// <summary>
        /// Sets the voltage range by the given enum-range.
        /// 
        /// Returns the enum-error state if an Error is appeared. Otherwise you get the currently selected range as enum-range.
        /// </summary>
        /// <param name="Range"></param>
        /// <returns></returns>
        private DMM7510_VoltageRange SetVoltageRange(DMM7510_VoltageRange Range)
        {
            if (!_measureFunction.StartsWith("VOLT:"))
                return DMM7510_VoltageRange.Error;

            DMM7510_VoltageRange range;
            string cmd;
            string rangeCompare;

            switch (Range)
            {
                case DMM7510_VoltageRange.Auto:
                    range = DMM7510_VoltageRange.Auto;
                    cmd = "SENS:" + _measureFunction + ":RANG:AUTO ON";
                    rangeCompare = "1";
                    break;

                case DMM7510_VoltageRange.U_100mV:
                    range = DMM7510_VoltageRange.U_100mV;
                    cmd = "SENS:" + _measureFunction + ":RANG 0.1";
                    rangeCompare = "0.1";
                    break;

                case DMM7510_VoltageRange.U_1V:
                    range = DMM7510_VoltageRange.U_1V;
                    cmd = "SENS:" + _measureFunction + ":RANG 1";
                    rangeCompare = "1";
                    break;

                case DMM7510_VoltageRange.U_10V:
                    range = DMM7510_VoltageRange.U_10V;
                    cmd = "SENS:" + _measureFunction + ":RANG 10";
                    rangeCompare = "10";
                    break;

                case DMM7510_VoltageRange.U_100V:
                    range = DMM7510_VoltageRange.U_100V;
                    cmd = "SENS:" + _measureFunction + ":RANG 100";
                    rangeCompare = "100";
                    break;

                case DMM7510_VoltageRange.U_1kV:
                    range = DMM7510_VoltageRange.U_1kV;
                    cmd = "SENS:" + _measureFunction + ":RANG 1000";
                    rangeCompare = "1000";
                    break;

                default:
                    return DMM7510_VoltageRange.Error;         // Error: Non existing range
            }

            _device.Write(cmd);
            cmd = "SENS:" + _measureFunction + ":RANG" + (Range == DMM7510_VoltageRange.Auto ? ":AUTO" : "") + "?";
            _device.Write(cmd);
            string response = _device.ReadString();
            response = response.Remove(response.Length - 1);
            if (response != rangeCompare)
                return DMM7510_VoltageRange.Error;

            return range;
        }



        /// <summary>
        /// Sets the current range by the given enum-range.
        /// 
        /// Returns the enum-error state if an Error is appeared. Otherwise you get the currently selected range as enum-range.
        /// </summary>
        /// <param name="Range"></param>
        /// <returns></returns>
        private DMM7510_CurrentRange SetCurrentRange(DMM7510_CurrentRange Range)
        {
            if (!_measureFunction.StartsWith("CURR:"))
                return DMM7510_CurrentRange.Error;

            DMM7510_CurrentRange range;
            string cmd;
            string rangeCompare;

            switch (Range)
            {
                case DMM7510_CurrentRange.Auto:
                    range = DMM7510_CurrentRange.Auto;
                    cmd = "SENS:" + _measureFunction + ":RANG:AUTO ON";
                    rangeCompare = "1";
                    break;

                case DMM7510_CurrentRange.I_10µA:
                    range = DMM7510_CurrentRange.I_10µA;
                    cmd = "SENS:" + _measureFunction + ":RANG 10E-06";
                    rangeCompare = "1E-05";
                    break;

                case DMM7510_CurrentRange.I_100µA:
                    range = DMM7510_CurrentRange.I_100µA;
                    cmd = "SENS:" + _measureFunction + ":RANG 100E-06";
                    rangeCompare = "0.0001";
                    break;

                case DMM7510_CurrentRange.I_1mA:
                    range = DMM7510_CurrentRange.I_1mA;
                    cmd = "SENS:" + _measureFunction + ":RANG 1E-03";
                    rangeCompare = "0.001";
                    break;

                case DMM7510_CurrentRange.I_10mA:
                    range = DMM7510_CurrentRange.I_10mA;
                    cmd = "SENS:" + _measureFunction + ":RANG 10E-03";
                    rangeCompare = "0.01";
                    break;

                case DMM7510_CurrentRange.I_100mA:
                    range = DMM7510_CurrentRange.I_100mA;
                    cmd = "SENS:" + _measureFunction + ":RANG 100E-3";
                    rangeCompare = "0.1";
                    break;

                case DMM7510_CurrentRange.I_1A:
                    range = DMM7510_CurrentRange.I_1A;
                    cmd = "SENS:" + _measureFunction + ":RANG 1";
                    rangeCompare = "1";
                    break;

                case DMM7510_CurrentRange.I_3A:
                    range = DMM7510_CurrentRange.I_3A;
                    cmd = "SENS:" + _measureFunction + ":RANG 3";
                    rangeCompare = "3";
                    break;

                default:
                    return DMM7510_CurrentRange.Error;         // Error: Non existing range
            }

            _device.Write(cmd);
            cmd = "SENS:" + _measureFunction + ":RANG" + (Range == DMM7510_CurrentRange.Auto ? ":AUTO" : "") + "?";
            _device.Write(cmd);
            string response = _device.ReadString();
            response = response.Remove(response.Length - 1);
            if (response != rangeCompare)
                return DMM7510_CurrentRange.Error;

            return range;
        }



        /// <summary>
        /// Setting up the integration rate.
        /// 
        /// Returns the currently configured integration time if everything gone well. A negative number indicates an error.
        /// </summary>
        /// <param name="IntegrationRate"></param>
        /// <returns></returns>
        private double SetNplc(double nplc)
        {
            if (nplc < 5e-4 || nplc > 12)
                return -1;                                      // Error: Input out of range

            _device.Write(_measureFunction + ":NPLC " + nplc.ToString());
            _device.Write(_measureFunction + ":NPLC?");
            string response = _device.ReadString();
            if (!double.TryParse(response, out _integrationRate))
                return -2;                                      // Error: Can't convert to double

            if (_integrationRate != nplc)
                return -3;                                      // Error: Values different

            return _integrationRate;
        }



        /// <summary>
        /// Setting up the common parameters of the measure-methods.
        /// 
        /// Returns 0 if everthing gone well. A negative number indicates an error.
        /// </summary>
        /// <param name="IntegrationRate"></param>
        /// <param name="AutoZero"></param>
        /// <returns></returns>
        private int SetMeasurementCommons(double Nplc, bool AutoZero)
        {
            double nplc = SetNplc(Nplc);
            if (nplc != Nplc)
                return -1;                                      // Error: Can't set integrationrate

            _autoZero = SetAutoZero(AutoZero);
            if (_autoZero < 0)
                return -2;                                      // Error: Can't set autozero

            return 0;
        }



        /// <summary>
        /// Configures the device to measure voltage. The parameters can be optionally specified by the caller.
        /// 
        /// Returns 0 if everything gone well. A negative number indicates an error.
        /// </summary>
        /// <param name="AC"></param>
        /// <param name="Range"></param>
        /// <param name="IntegrationRate"></param>
        /// <param name="AutoZero"></param>
        /// <returns></returns>
        public int SetVoltageMeasurement(bool AC = false, DMM7510_VoltageRange Range = DMM7510_VoltageRange.Auto, double IntegrationRate = 0.1, bool AutoZero = false)
        {
            string measureFunction = "VOLT:" + (AC ? "AC" : "DC");
            _measureFunction = SetMeasureFunction(measureFunction);
            if (_measureFunction != measureFunction)
                return -1;                                      // Error: Settings measurement function


            _voltageRange = SetVoltageRange(Range);
            if (_voltageRange == DMM7510_VoltageRange.Error)
                return -2;                                      // Error: Can't set range

            int commonReturn = SetMeasurementCommons(IntegrationRate, AutoZero);
            if (commonReturn != 0)
                return commonReturn - 2;                        // Error in the commons: Return modified errorcode

            return 0;
        }



        /// <summary>
        /// Configures the device to measure current. The parameters can be optionally specified by the caller.
        ///
        /// Returns 0 if everything gone well. A negative number indicates an error.
        /// </summary>
        /// <param name="AC"></param>
        /// <param name="Range"></param>
        /// <param name="IntegrationRate"></param>
        /// <param name="AutoZero"></param>
        /// <returns></returns>
        public int SetCurrentMeasurement(bool AC = false, DMM7510_CurrentRange Range = DMM7510_CurrentRange.Auto, double IntegrationRate = 20e-3, bool AutoZero = false)
        {
            string measureFunction = "CURR:" + (AC ? "AC" : "DC");
            _measureFunction = SetMeasureFunction(measureFunction);
            if (_measureFunction != measureFunction)
                return -1;                                      // Error: Settings measurement function

            _currentRange = SetCurrentRange(Range);
            if (_currentRange == DMM7510_CurrentRange.Error)
                return -2;                                      // Error: Can't set range

            int commonReturn = SetMeasurementCommons(IntegrationRate, AutoZero);
            if (commonReturn != 0)
                return commonReturn - 2;                        // Error in the commons: Return modified errorcode

            return 0;
        }



        /// <summary>
        /// Initiates a measurement on the device and get the result.
        /// 
        /// Returns the result in [V].
        /// </summary>
        /// <returns></returns>
        public double Measure()
        {
            double current;
            string response;

            _device.Write("READ?");
            response = _device.ReadString();
            if (!double.TryParse(response, out current))
                return -1;              // Error: Can't parse to double

            return current;            // No complications on single read
        }

        
        
        /// <summary>
        /// Converts a given coupling string to bool.
        /// 
        /// Returns true if "AC" is given. Otherwise you always get false! (DC = false = default)
        /// </summary>
        /// <param name="Coupling"></param>
        /// <returns></returns>
        static public bool ConvertCoupling(string Coupling)
        {
            if (Coupling == "AC")
                return true;

            return false;
        }



        /// <summary>
        /// Converts a double-range into the next higher possible range which is supported by the device.
        /// 
        /// Returns error if Range is to high!
        /// </summary>
        /// <param name="Range"></param>
        /// <returns></returns>
        static public DMM7510_VoltageRange ConvertVoltageRange(double Range)
        {
            if (Range == 0)
                return DMM7510_VoltageRange.Auto;
            else if (Range > 0 && Range <= 100e-3)
                return DMM7510_VoltageRange.U_100mV;
            else if (Range > 100e-3 && Range <= 1)
                return DMM7510_VoltageRange.U_1V;
            else if (Range > 1 && Range <= 10)
                return DMM7510_VoltageRange.U_10V;
            else if (Range > 10 && Range <= 100)
                return DMM7510_VoltageRange.U_100V;
            else if (Range > 100 && Range <= 1e3)
                return DMM7510_VoltageRange.U_1kV;

            return DMM7510_VoltageRange.Error;
        }



        /// <summary>
        /// Converts a double-range into the next higher possible range which is supported by the device.
        /// 
        /// Returns error if Range is to high!
        /// </summary>
        /// <param name="Range"></param>
        /// <returns></returns>
        static public DMM7510_CurrentRange ConvertCurrentRange(double Range)
        {
            if (Range == 0)
                return DMM7510_CurrentRange.Auto;
            else if (Range > 0 && Range <= 10e-6)
                return DMM7510_CurrentRange.I_10µA;
            else if (Range > 10e-6 && Range <= 100e-6)
                return DMM7510_CurrentRange.I_100µA;
            else if (Range > 100e-6 && Range <= 1e-3)
                return DMM7510_CurrentRange.I_1mA;
            else if (Range > 1e-3 && Range <= 10e-3)
                return DMM7510_CurrentRange.I_10mA;
            else if (Range > 10e-3 && Range <= 100e-3)
                return DMM7510_CurrentRange.I_100mA;
            else if (Range > 100e-3 && Range <= 1)
                return DMM7510_CurrentRange.I_1A;
            else if (Range > 1 && Range <= 3)
                return DMM7510_CurrentRange.I_3A;
            
            return DMM7510_CurrentRange.Error;
        }
    }
}

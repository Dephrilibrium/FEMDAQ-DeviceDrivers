using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


/*
 * File is a copy of Langers FUG350-driver and modified by haum
 */

namespace Instrument.FUGMCP140
{
    public class MCP140
    {
        Device _dev;
        private double _currentOffset0V { get; set; }

        public MCP140(int boardNumber, byte primAddr, byte secAddr)
        {
            _dev = new Device(boardNumber, primAddr, secAddr);
            Info(); // Check connection
        }



        /// <summary>
        /// Cleans up the object-res_devs.
        /// </summary>
        /// <remark>
        /// Added by MaHa
        /// </remark>
        public void Dispose()
        {
            if (_dev != null)
            {
                // added by haum
                try { SafeMode(); }
                catch (Exception) { }
                // ----------------
                _dev.Dispose();
            }
        }




        public string Info()
        {
            _dev.Write("*IDN?");
            return _dev.ReadString();
        }

        public void Init()
        {
            SafeMode();
        }

        public void SetOutput(bool state)
        {
            lock (_dev)
            {
                if (state == true) _dev.Write(">BON 1"); //set output ON
                if (state == false) _dev.Write(">BON 0"); //set output OFF
            }
        }

        public void SetVoltage(double voltageValue)
        {
            // U does the same as >S0
            // MCP has no polarity (only by switching outputs by hand!
            lock (_dev)
            {
                _dev.Write("U " + voltageValue.ToString()); //send command "U" (">S0") to set the value of output voltage
            }
        }

        public void SetCurrent(double currentValue)
        {
            // I does the same as >S1
            _dev.Write("I " + currentValue.ToString()); //send command "I" (">S1") to set the value of output current
        }

        public bool GetOutput()
        {
            _dev.Write(">DON?");
            string debug = _dev.ReadString();
            if (debug == "DON:1\n")
                return true; //output is ON
            else
                return false; //output is OFF
        }



        public double GetVoltage()
        {
            lock (_dev)
            {
                _dev.Write(">M0?"); //send command ">M0" to get the value of output voltage
                var response = _dev.ReadString();
                response = response.Substring(3);
                return Convert.ToDouble(response); //read response of r 
            }
        }

        public double GetCurrent()
        {
            _dev.Write(">M1?"); //send command ">M1" to get the value of output current
            return Convert.ToDouble(_dev.ReadString().Substring(3));

        }

        public void SafeMode()
        {
            SetOutput(false);
            SetCurrent(0.0);
            SetVoltage(0.0);
        }
    }
}

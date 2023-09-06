using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


namespace Instrument.FUGHCP350
{
    public class FUGHCP350
    {
        Device fughcp350;

        public FUGHCP350(int boardNumber, byte primAddr, byte secAddr)
        {
            fughcp350 = new Device(boardNumber, primAddr, secAddr);
            Info(); // Check connection
        }



        /// <summary>
        /// Cleans up the object-ressources.
        /// </summary>
        /// <remark>
        /// Added by MaHa
        /// </remark>
        public void Dispose()
        {
            if (fughcp350 != null)
            {
                // added by haum
                try { SafeMode(); }
                catch (Exception) { }
                // ----------------
                fughcp350.Dispose();
            }
        }




        public string Info()
        {
            fughcp350.Write("*IDN?");
            return fughcp350.ReadString();
        }

        public void Init()
        {
            SafeMode();
            //string test;
            //test = fughcp350.ReadString();
            //test = fughcp350.ReadString();
            ////fughcp350.Write("="); //clear device MACHT PROBLEME --- WARUM???
            //test = fughcp350.ReadString();
            //System.Threading.Thread.Sleep(2000);
        }

        public void SetOutput(bool state)
        {
            if (state == true) fughcp350.Write("F1"); //set output ON
            if (state == false) fughcp350.Write("F0"); //set output OFF
        }

        public void SetPolarity(bool positivePolarity)
        {
            if (positivePolarity == true) fughcp350.Write("P0"); //set positive polarity of output voltage
            if (positivePolarity == false) fughcp350.Write("P1"); //set negative polarity of output voltage
        }

        public void SetVoltage(double voltageValue)
        {
            bool actualPolarity = this.GetPolarity();

            if (voltageValue >= 0)
            {
                if (actualPolarity == false)
                {
                    fughcp350.Write("U 0.0"); //set voltage to zero before changing the polarity
                    //2DO --- wait for a voltage value below 100V to change the polarity
                    this.SetPolarity(true);
                }
                fughcp350.Write("U " + voltageValue.ToString()); //send command "U" (">S0") to set the value of output voltage
            }
            else
            {
                if (actualPolarity == true)
                {
                    fughcp350.Write("U 0.0"); //set voltage to zero before changing the polarity
                    //2DO --- wait for a voltage value below 100V to change the polarity
                    this.SetPolarity(false);
                }
                fughcp350.Write("U " + (voltageValue*-1).ToString()); //send command "U" (">S0") to set the value of output voltage
            }
        }
        
        public void SetCurrent(double currentValue)
        {
            //auto polarity change for current mode is not implemented!!!
            fughcp350.Write("I " + currentValue.ToString()); //send command "I" (">S1") to set the value of output current
        }

        public bool GetOutput()
        {
            fughcp350.Write(">DON?");
            string debug = fughcp350.ReadString();
            if (debug == "DON:1\n") return true; //output is ON
            else return false; //output is OFF
        }

        public bool GetPolarity()
        {
            fughcp350.Write(">DX?");
            string debug = fughcp350.ReadString();
            if (debug == "DX:1\n") return true; //positive polarity
            else return false; //negative polarity
        }
        
        public double GetVoltage()
        {
            fughcp350.Write(">M0?"); //send command ">M0" to get the value of output voltage
            return Convert.ToDouble(fughcp350.ReadString().Substring(3)); //read response of r 
        }

        public double GetCurrent()
        {
            fughcp350.Write(">M1?"); //send command ">M1" to get the value of output current
            return Convert.ToDouble(fughcp350.ReadString().Substring(3));

        }

        public void SafeMode()
        {
            this.SetOutput(false);
            this.SetCurrent(0.0);
            this.SetVoltage(0.0);
        }
    }
}

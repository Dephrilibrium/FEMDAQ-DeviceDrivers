using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


namespace Instrument.KE6485
{
    public class KE6485
    {
        Device ke6485;

        public KE6485(int boardNumber, byte primAddr, byte secAddr)
        {
            ke6485 = new Device(boardNumber, primAddr, secAddr);
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
            if (ke6485 != null)
            {
                // added by haum
                try { SafeMode(); }
                catch (Exception) { }
                // ----------------
                ke6485.Dispose();
            }
        }



        public string Info()
        {
            ke6485.Write("*IDN?;");
            return ke6485.ReadString();
        }

        public void Init()
        {
            ke6485.Write("*RST"); //reset
            ke6485.Write("FORM:ELEM READ");

        }

        public void SetAutoZero(bool state)
        {
            if (state == true) ke6485.Write("SYST:AZER:STAT ON");
            else ke6485.Write("SYST:AZER:STAT OFF");
        }

        public void SetZeroCheck(bool state)
        {
            if (state == true) ke6485.Write("SYST:ZCH:STAT ON");
            else ke6485.Write("SYST:ZCH:STAT OFF");
        }

        public void SetZeroCorrect(bool state)
        {
            if (state == true) ke6485.Write("SYST:ZCOR:STAT ON");
            else ke6485.Write("SYST:ZCOR:STAT OFF");
        }

        public void AcquireNewZeroCorrectValue(double range)
        {
            //keithley model 6487 reference manual, page 3-7
            ke6485.Write("SYST:ZCH:STAT ON");
            ke6485.Write("SENS:CURR:RANG " + range.ToString());
            ke6485.Write("INIT");
            ke6485.Write("SYST:ZCOR:STAT OFF");
            System.Threading.Thread.Sleep(500);
            ke6485.Write("SYST:ZCOR:ACQ");
            ke6485.Write("SYST:ZCH:STAT OFF");
            System.Threading.Thread.Sleep(500);
            ke6485.Write("SYST:ZCOR:STAT ON");
        }

        public void SetRange(double range)
        {
            ke6485.Write("SENS:CURR:RANG " + range.ToString());
        }

        public void SetAutoRange(bool state)
        {
            ke6485.Write("CURR:RANG:AUTO " + (state ? "ON" : "OFF"));                  // select auto range
        }

        public void SetRate(double value)
        {
            string command = "";
            
            if(value>=0.01 && value<=50.0) command += "SENS:CURR:NPLC " + value.ToString();

            ke6485.Write(command);
        }

        public void SetDamping(bool state)
        {
            if (state == true) ke6485.Write("SENS:CURR:DAMP:STAT ON");
            else ke6485.Write("SENS:CURR:DAMP:STAT OFF");
        }

        public void SetMedianFilter(bool state, int rank) 
        {
            string command = "";

            if (rank >= 1 && rank <= 5) command += "SENS:MED:RANK " + rank.ToString() + ";";
            if (state == true) command += "SENS:MED:STAT ON;";
            else command += "SENS:MED:STAT OFF;";

            ke6485.Write(command);
        }

        public void SetDigitalFilter(bool state, int type, int count) 
        {
            string command = "";

            //type=0: MOVING, type=1: REPEATING 
            if (type == 0) command += "SENS:AVER:TCON MOV;";
            else command += "SENS:AVER:TCON REP;";
            if (count >= 2 && count <= 100) command += "SENS:AVER:COUN " + count.ToString() + ";";
            if (state == true) command += "SENS:AVER:STAT ON;";
            else command += "SENS:AVER:STAT OFF;";

            ke6485.Write(command);
        }

        public double GetCurrent()
        {
            //ke6485.Write("MEAS:CURR:DC?");
            ke6485.Write("READ?");
            return Convert.ToDouble(ke6485.ReadString());
        }

        //public double[] GetTrace(int points, int offset, double delta)
        //{ return 0.0; }

        // with time output, useful for debugging
        //public void GetTraceWithTime(int points, int offset, double delta, out double[] value, out double[] time)
        //{}

        public void SafeMode()
        {
            this.SetZeroCheck(true); //ammeter disabled for connecting and disconnecting actions
        }
    }
}

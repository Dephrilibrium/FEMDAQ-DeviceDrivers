using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


namespace Instrument.KE6487
{
    public enum VoltageRange
    {
        V10 = 1,
        V50 = 2,
        V500 = 3
    }

    public class KE6487
    {
        Device ke6487;

        public KE6487(int boardNumber, byte primAddr, byte secAddr)
        {
            ke6487 = new Device(boardNumber, primAddr, secAddr);
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
            if (ke6487 != null)
            {
                // added by haum
                try { SafeMode(); }
                catch (Exception) { }
                // ----------------
                ke6487.Dispose();
            }
        }



        public string Info()
        {
            ke6487.Write("*IDN?;");
            return ke6487.ReadString();
        }

        public void Init()
        {
            ke6487.Write("*RST"); //reset
            ke6487.Write("FORM:ELEM READ"); //read only current without unit
        }

        public void SetVoltage(double voltageValue, VoltageRange voltageRange, double currentLimit, bool state)
        {
            if (voltageRange == VoltageRange.V10) ke6487.Write("SOUR:VOLT:RANG 10");
            else if (voltageRange == VoltageRange.V50) ke6487.Write("SOUR:VOLT:RANG 50");
            else if (voltageRange == VoltageRange.V500) ke6487.Write("SOUR:VOLT:RANG 500");
            else ke6487.Write("SOUR:VOLT:RANG 500");

            ke6487.Write("SOUR:VOLT:LEV:IMM:AMPL " + voltageValue.ToString());
            
            ke6487.Write("SOUR:VOLT:ILIM " + currentLimit.ToString());

            SetOutput(state);
        }

        /// <summary>
        /// Enables or disables the output.
        /// </summary>
        /// <remarks>Added by MaHa</remarks>
        /// <param name="state"></param>
        public void SetOutput(bool State)
        {
            ke6487.Write("SOUR:VOLT:STAT " + (State ? "ON" : "OFF"));
        }

        public void SetAutoZero(bool state)
        {
            if (state == true) ke6487.Write("SYST:AZER:STAT ON");
            else ke6487.Write("SYST:AZER:STAT OFF");
        }

        public void SetZeroCheck(bool state)
        {
            if (state == true) ke6487.Write("SYST:ZCH:STAT ON");
            else ke6487.Write("SYST:ZCH:STAT OFF");
        }

        public void SetZeroCorrect(bool state)
        {
            if (state == true) ke6487.Write("SYST:ZCOR:STAT ON");
            else ke6487.Write("SYST:ZCOR:STAT OFF");
        }

        public void AcquireNewZeroCorrectValue(double range)
        {
            //keithley model 6487 reference manual, page 3-7
            ke6487.Write("SYST:ZCH:STAT ON");
            ke6487.Write("SENS:CURR:RANG " + range.ToString());
            ke6487.Write("INIT");
            ke6487.Write("SYST:ZCOR:STAT OFF");
            System.Threading.Thread.Sleep(500);
            ke6487.Write("SYST:ZCOR:ACQ");
            ke6487.Write("SYST:ZCH:STAT OFF");
            System.Threading.Thread.Sleep(500);
            ke6487.Write("SYST:ZCOR:STAT ON");
        }

        public void SetRange(double range)
        {
            ke6487.Write("SENS:CURR:RANG " + range.ToString());
        }

        public void SetAutoRange(bool state)
        {
            ke6487.Write("CURR:RANG:AUTO " + (state ? "ON" : "OFF"));                  // select auto range
        }

        public void SetRate(double value)
        {
            string command = "";

            if (value >= 0.01 && value <= 50.0) command += "SENS:CURR:NPLC " + value.ToString();

            ke6487.Write(command);
        }

        public void SetDamping(bool state)
        {
            if (state == true) ke6487.Write("SENS:CURR:DAMP:STAT ON");
            else ke6487.Write("SENS:CURR:DAMP:STAT OFF");
        }

        public void SetMedianFilter(bool state, int rank)
        {
            string command = "";

            if (rank >= 1 && rank <= 5) command += "SENS:MED:RANK " + rank.ToString() + ";";
            if (state == true) command += "SENS:MED:STAT ON;";
            else command += "SENS:MED:STAT OFF;";

            ke6487.Write(command);
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

            ke6487.Write(command);
        }

        public double GetCurrent()
        {
            //ke6487.Write("MEAS:CURR:DC?");
            ke6487.Write("READ?");
            return Convert.ToDouble(ke6487.ReadString());
        }

        public double[] GetTrace(int points, int offset, double delta)
        {
            string result = "";
            string[] aResult = new string[points];
            double[] value = new double[points];

            // min. delta: 0.1s @ this settings
            if (delta < 0.1) delta = 0.1;

            //dummy readout before the measurement to aviod long delays at the first values 
            ke6487.Write("FORM:ELEM READ");                     // return reading (READ) and time (TIME)
            ke6487.Write("ARM:SOUR TIM");                       // set ARM source to timer
            ke6487.Write("ARM:TIM 0.1");                        // set ARM timer value to 0.1s
            ke6487.Write("ARM:COUN " + points.ToString());      // set ARM count to number of points
            ke6487.Write("TRIG:COUN 1");                        // set trigger count to 1
            ke6487.Write("TRIG:DEL 0");                         // set trigger delay to value
            ke6487.Write("TRAC:POIN " + offset.ToString());     // set buffer size to value of points
            ke6487.Write("TRAC:CLE");                           // clear buffer
            ke6487.Write("TRAC:FEED SENS");                     // store raw input readings
            ke6487.Write("TRAC:FEED:CONT NEXT");                // set storage control to start on next reading
            ke6487.Write("CURR:NPLC 4");                        // set integration rate to 0.01 PLC
            ke6487.Write("CURR:RANG:AUTO ON");                  // select auto range
            ke6487.Write("SYST:ZCH OFF");                       // disable zero check
            ke6487.Write("SYST:AZER:STAT OFF");                 // disable auto zero
            ke6487.Write("INIT");                               // start taking and storing readings

            //readout values
            ke6487.Write("FORM:ELEM READ");                     // return reading (READ) and time (TIME)
            ke6487.Write("ARM:SOUR TIM");                       // set ARM source to timer
            ke6487.Write("ARM:TIM " + delta.ToString());        // set ARM timer value to value of delta
            ke6487.Write("ARM:COUN " + points.ToString());      // set ARM count to number of points
            ke6487.Write("TRIG:COUN 1");                        // set trigger count to 1
            ke6487.Write("TRIG:DEL 0");                         // set trigger delay to value
            ke6487.Write("TRAC:TST:FORM DELT");                 // timestamp format: ABSolute or DELta
            ke6487.Write("TRAC:POIN " + points.ToString());     // set buffer size to value of points
            ke6487.Write("TRAC:CLE");                           // clear buffer
            ke6487.Write("TRAC:FEED SENS");                     // store raw input readings
            ke6487.Write("TRAC:FEED:CONT NEXT");                // set storage control to start on next reading
            ke6487.Write("CURR:NPLC 4");                        // set integration rate to 0.01 PLC
            //ke6487.Write("SYST:AZER:STAT ON");                  // enable auto zero (min delta: 0.25s)
            ke6487.Write("INIT");                               // start taking and storing readings
            ke6487.Write("TRAC:DATA?");                         // request data from buffer

            System.Threading.Thread.Sleep((int)(delta * points * 1000));   // wait with read data, to avoid timeout problems

            // alternative 1: supports an infinite number of samples
            //do // ReadString()-Method supports only 1024 Characters
            //{
            //    result += ke6487.ReadString();
            //} while (result[result.Length - 1] != '\n');

            // alternative 2: supports only 4096/(2*14)=146 samples
            result = ke6487.ReadString(4096);
            
            result = result.Substring(0, result.Length - 1);
            aResult = result.Split(',');

            for (int i = 0; i < aResult.Length; i++) value[i] = Convert.ToDouble(aResult[i]);

            return value;
        }

        // with time output, useful for debugging
        public void GetTraceWithTime(int points, int offset, double delta, out double[] value, out double[] time)
        {
            string result = "";
            string[] aResult = new string[points * 2];
            value = new double[points];
            time = new double[points];

            // min. delta: 0.1s @ this settings
            if (delta < 0.1) delta = 0.1;

            //dummy readout before the measurement to aviod long delays at the first values 
            ke6487.Write("FORM:ELEM READ,TIME");                // return reading (READ) and time (TIME)
            ke6487.Write("ARM:SOUR TIM");                       // set ARM source to timer
            ke6487.Write("ARM:TIM 0.1");                        // set ARM timer value to 0.1s
            ke6487.Write("ARM:COUN " + points.ToString());      // set ARM count to number of points
            ke6487.Write("TRIG:COUN 1");                        // set trigger count to 1
            ke6487.Write("TRIG:DEL 0");                         // set trigger delay to value
            ke6487.Write("TRAC:POIN " + offset.ToString());     // set buffer size to value of points
            ke6487.Write("TRAC:CLE");                           // clear buffer
            ke6487.Write("TRAC:FEED SENS");                     // store raw input readings
            ke6487.Write("TRAC:FEED:CONT NEXT");                // set storage control to start on next reading
            ke6487.Write("CURR:NPLC 4");                        // set integration rate to 0.01 PLC
            ke6487.Write("CURR:RANG:AUTO ON");                  // select auto range
            ke6487.Write("SYST:ZCH OFF");                       // disable zero check
            ke6487.Write("SYST:AZER:STAT OFF");                 // disable auto zero
            ke6487.Write("INIT");                               // start taking and storing readings

            //readout of the values
            ke6487.Write("FORM:ELEM READ,TIME");                // return reading (READ) and time (TIME)
            ke6487.Write("ARM:SOUR TIM");                       // set ARM source to timer
            ke6487.Write("ARM:TIM " + delta.ToString());        // set ARM timer value to value of delta
            ke6487.Write("ARM:COUN " + points.ToString());      // set ARM count to number of points
            ke6487.Write("TRIG:COUN 1");                        // set trigger count to 1
            ke6487.Write("TRIG:DEL 0");                         // set trigger delay to value
            ke6487.Write("TRAC:TST:FORM DELT");                 // timestamp format: ABSolute or DELta
            ke6487.Write("TRAC:POIN " + points.ToString());     // set buffer size to value of points
            ke6487.Write("TRAC:CLE");                           // clear buffer
            ke6487.Write("TRAC:FEED SENS");                     // store raw input readings
            ke6487.Write("TRAC:FEED:CONT NEXT");                // set storage control to start on next reading
            ke6487.Write("CURR:NPLC 4");                        // set integration rate to 0.01 PLC
            //ke6487.Write("SYST:AZER:STAT ON");                  // enable auto zero (min delta: 0.25s)
            ke6487.Write("INIT");                               // start taking and storing readings
            ke6487.Write("TRAC:DATA?");                         // request data from buffer

            System.Threading.Thread.Sleep((int)(delta * points * 1000));   // wait with read data, to avoid timeout problems
          
            // alternative 1: supports an infinite number of samples
            //do // ReadString()-Method supports only 1024 Characters
            //{
            //    result += ke6487.ReadString();
            //} while (result[result.Length-1] != '\n');

            // alternative 2: supports only 4096/(2*14)=146 samples
            //result = ke6487.ReadString(4096);
 
            result = result.Substring(0, result.Length - 1);
            aResult = result.Split(',');

            for (int i = 0; i < aResult.Length; i++)
            {
                if (i % 2 == 0) value[i / 2] = Convert.ToDouble(aResult[i]);
                else time[(i - 1) / 2] = Convert.ToDouble(aResult[i]);
            }
        }

        public double GetVoltage()
        {
            ke6487.Write("SOUR:VOLT:LEV:IMM:AMPL?");
            var response = ke6487.ReadString();
            return Convert.ToDouble(response);
        }

        public void SafeMode()
        {
            this.SetVoltage(0.0, VoltageRange.V500, 2.5e-3, false);
            this.SetZeroCheck(true); //ammeter disabled for connecting and disconnecting actions
        }
    }
}

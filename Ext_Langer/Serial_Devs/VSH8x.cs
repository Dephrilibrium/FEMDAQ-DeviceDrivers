using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.SerialInterface;


namespace Instrument.VSH8x
{
    public class VSH8x
    {
        //Commandset - Source: Thyracont Protokoll v1
        const string GET_SENSORTYPE = "T";
        const string GET_MEASUREMENTVALUE = "M";
        const string GET_DEGAS = "D";
        const string SET_DEGAS = "d";
        const string GET_SWPOINT1 = "S1";
        const string GET_SWPOINT2 = "S2";
        const string UNLOCK_SWPOINT1 = "s1";
        const string UNLOCK_SWPOINT2 = "s2";
        const string SET_SWPOINT = "s";
        const string GET_CORRVALUE1 = "C1";
        const string GET_CORRVALUE2 = "C2";
        const string UNLOCK_CORRVALUE1 = "c1";
        const string UNLOCK_CORRVALUE2 = "c2";
        const string SET_CORRVALUE = "c";
        const string GET_HOTCMODE = "I";
        const string SET_HOTCMODE = "i";
        const string GET_VALUEADAPTMODE = "W";
        const string SET_VALUEADAPTMODE = "w";
        const string UNLOCK_ADJUSTVALUE0 = "j0";
        const string UNLOCK_ADJUSTVALUE1 = "j1";
        const string SET_ADJUSTVALUE = "j";

        const bool ON = true;
        const bool OFF = false;

        SerialInterface rs485Interface;
        private string rs485Address;

        public VSH8x(string rs485Address, string comPort, int baudRate) // baudRate added by Haum
        {
            this.rs485Address = rs485Address;
            rs485Interface = new SerialInterface(comPort, baudRate, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, 500);
            rs485Interface.OpenInterface();
            rs485Interface.SendRequest("\r");
        }

        public void Close()
        {
            rs485Interface.CloseInterface();
        }

        public void Dispose()
        {
            if(rs485Interface != null)
            {
                // added by haum
                try { SafeMode(); }
                catch (Exception) { }
                // ----------------
                Close();
            }
        }

        private void Dummy()
        {
            rs485Interface.SendRequest("\r");
        }

        public string GetSensorType()
        {
            //rs485Interface.OpenInterface();
            //this.Dummy();
            rs485Interface.SendRequest(CreateStringForRequest(GET_SENSORTYPE));
            string temp = ExtractDataFromReply(rs485Interface.ReceiveReply(), GET_SENSORTYPE);
            //rs485Interface.CloseInterface();
            return temp;
        }

        public double GetMeasurementValue()
        {
            //rs485Interface.OpenInterface();
            //this.Dummy();
            rs485Interface.SendRequest(CreateStringForRequest(GET_MEASUREMENTVALUE));
            double temp = StringToDouble(ExtractDataFromReply(rs485Interface.ReceiveReply(), GET_MEASUREMENTVALUE));
            //rs485Interface.CloseInterface();
            return temp;
        }

        public bool SetDegas(bool value)
        { return false; }

        public bool GetDegas()
        { return false; }

        public double GetSwitchPoint(int SwitchPointNumber)
        { return 0; }

        public double SetSwitchPoint(double value, int SwitchPointNumber)
        { return 0; }

        public double GetCorrectionValue(int CorrectionValueNumber)
        { return 0; }

        public double SetCorrectionValue(double value, int CorrectionValueNumber)
        { return 0; }

        public bool SetHotCathodeMode(bool value)
        { return false; }

        public bool GetHotCathodeMode()
        { return false; }

        public bool SetValueAdjustMode(bool value)
        { return false; }

        public bool GetValueAdjustMode()
        { return false; }

        public double SetAdjustPoint(double value, int AdjustPointNumber)
        { return 0; }

        private char GenerateCKS(string field)
        {
            int value = 0;

            for (int i = 0; i < field.Length; i++)
            {
                value += Convert.ToInt16(field[i]);
            }

            return Convert.ToChar(value % 64 + 64);
        }

        private bool VerifyCKS(string field)
        {
            if (field[field.Length - 1] == GenerateCKS(field.Remove(field.Length - 1))) return true;
            else return false;
        }

        private double StringToDouble(string value)
        {
            if (value.Length == 6)
            {
                int exponent;
                int mantisse;

                exponent = Convert.ToInt32(value.Substring(4));
                mantisse = Convert.ToInt32(value.Remove(4));

                return ((double)mantisse / 1000 * (double)Math.Pow(10, (exponent - 20)));
            }
            else return 0;
        }

        private string DoubleToString(double value)
        {
            if (value > 1.0e-21 && value < 1.0e80)
            {
                string exponent;
                string mantisse;

                exponent = String.Format("{0:00}", (20 + Math.Floor(Math.Log10(Math.Abs(value)))));
                mantisse = String.Format("{0:0000}", (int)Math.Round((value / Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value)))) * 1000)));

                return mantisse + exponent;
            }
            else return "";
        }

        private double StringToDecimal(string value)
        {
            if (value.Length == 6) return Convert.ToDouble(value) / 100;
            else return 0;
        }

        private string DecimalToString(double value)
        {
            if (value < 9999.99) return String.Format("{0:000000}", Math.Round((value * 100)));
            else return "";
        }

        private string CreateStringForRequest(string Command)
        {
            return rs485Address + Command + GenerateCKS(rs485Address + Command) + "\r";
        }

        private string CreateStringForRequest(string Command, bool value)
        {
            if (value == true) return rs485Address + Command + "1" + GenerateCKS(rs485Address + Command + "1") + "\r";
            else return rs485Address + Command + "0" + GenerateCKS(rs485Address + Command + "0") + "\r";
        }

        private string CreateStringForRequest(string Command, double value)
        {
            return rs485Address + Command + DoubleToString(value) + GenerateCKS(rs485Address + Command + DoubleToString(value)) + "\r"; // früher "\n", warum?
        }

        private string ExtractDataFromReply(string reply, string Command)
        {

            if (reply.Length > 1)
            {
                reply = reply.Trim(new char[] { '\0' }); // Added by MaHa because of the new Thyracont-Protocol?
                // string terminated by "\r", otherwise skip
                if (reply.Substring(reply.Length - 1, 1) == "\r")
                {
                    // remove the last "\r" from the string
                    reply = reply.Remove(reply.Length - 1, 1);

                    // remove an "\r" at the beginning of the string AND
                    // remove a ECHO of the RS485 interface
                    int index = reply.IndexOf("\r");
                    do
                    {
                        reply = reply.Remove(0, index + 1);
                        index = reply.IndexOf("\r");
                    }
                    while (index != -1);

                    // verify CKS and remove it from string
                    if (VerifyCKS(reply) == true)
                    {
                        reply = reply.Remove(reply.Length - 1);
                        if (reply.Substring(0, Command.Length + 3) == rs485Address + Command)
                            return reply.Remove(0, Command.Length + 3);
                        else return "0";
                    }
                    else return "0";
                }
                else return "0";
            }
            else return "0";
        }

        internal void SafeMode()
        {
            return;
        }
    }
}

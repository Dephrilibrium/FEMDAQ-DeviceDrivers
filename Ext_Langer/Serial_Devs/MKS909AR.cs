using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.SerialInterface;


namespace Instrument.MKS909AR
{
    class MKS909AR
    {//Commandset - Source: Manual Series 909AR MIGT
        const string GET_MODEL = "MD?";
        const string SET_UNIT_MBAR = "U!MBAR";
        const string SET_FILAMENT_ON = "FP!ON";
        const string SET_FILAMENT_OFF = "FP!OFF";
        const string GET_PRESSURE = "PR1?";
        const string SET_EC_AUTO = "EC!AUTO";


        const bool ON = true;
        const bool OFF = false;

        SerialInterface rs485Interface;
        private string rs485Address;

        public MKS909AR(string rs485Address, string comPort)
        {
            this.rs485Address = rs485Address;
            rs485Interface = new SerialInterface(comPort, 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, 500);
            rs485Interface.OpenInterface();
            //rs485Interface.SendRequest("\r");
        }

        public void Close()
        {
            rs485Interface.CloseInterface();
        }

        public void Dispose()
        {
            if (rs485Interface != null)
                Close();
        }

        public string GetSensorType()
        {
            //rs485Interface.OpenInterface();
            rs485Interface.SendRequest(CreateStringForRequest(GET_MODEL));
            string temp = ExtractDataFromReply(rs485Interface.ReceiveReply());
            //rs485Interface.CloseInterface();
            return temp;
        }

        public void SetUnitToMBar()
        {
            //rs485Interface.OpenInterface();
            rs485Interface.SendRequest(CreateStringForRequest(SET_UNIT_MBAR));
            rs485Interface.ReceiveReply();
            //rs485Interface.CloseInterface();
        }

        public void SetFilament(bool value)
        {
            //rs485Interface.OpenInterface();
            if(value==ON) rs485Interface.SendRequest(CreateStringForRequest(SET_FILAMENT_ON));
            else rs485Interface.SendRequest(CreateStringForRequest(SET_FILAMENT_OFF));
            rs485Interface.ReceiveReply();
            //rs485Interface.CloseInterface();
        }

        public void SetEmissionCurrentToAuto()
        {
            rs485Interface.SendRequest(CreateStringForRequest(SET_EC_AUTO));
            rs485Interface.ReceiveReply();
        }

        public double GetMeasurementValue()
        {
            //rs485Interface.OpenInterface();
            rs485Interface.SendRequest(CreateStringForRequest(GET_PRESSURE));
            double temp = StringToDouble(ExtractDataFromReply(rs485Interface.ReceiveReply()));
            //rs485Interface.CloseInterface();
            return temp;
        }

        private double StringToDouble(string strValue)
        {
            //
            // --- version 2.0 ---
            //
            double dValue;
            if (Double.TryParse(strValue, out dValue))
                return dValue;
            else
                return Double.NaN;

            
            //
            // --- version 1.0 ---
            //
            //if (value != "OFF") return Convert.ToDouble(value);
            //else return 0;
        }

        private string CreateStringForRequest(string Command)
        {
            return "@" + rs485Address + Command + ";FF";
        }

        private string ExtractDataFromReply(string reply)
        {
            //
            // --- version 2.0 ---
            //
            // example reply string "@253PR1?;FF\0@253ACK4.7E-8;FF\0"
            // frame1 "253ACK", frame2 = ";FF" and data between
            //

            string frame1 = rs485Address.ToString() + "ACK"; //default: "253"
            string frame2 = ";FF";

            // find first frame (beginning of data) and remove with all chars before
            while (reply.IndexOf(frame1) > 0)
                reply = reply.Remove(0, reply.IndexOf(frame1) + frame1.Length);

            // find end frame (delimiter) and remove with all chars after it
            reply = reply.Remove(reply.IndexOf(frame2));

            return reply;


            //
            // --- version 1.0 ---
            //

            //if (reply.Length > 3)
            //{
            //    // string terminated by "\r", otherwise skip
            //    if (reply.Substring(reply.Length - 3, 3) == ";FF")
            //    {
            //        // remove the last "\r" from the string
            //        reply. = reply.Remove(reply.Length - 3, 3);

            //        // remove an "\r" at the beginning of the string AND
            //        // remove a ECHO of the RS485 interface
            //        int index = reply.IndexOf(";FF");
            //        do
            //        {
            //            reply = reply.Remove(0, index + 3);
            //            index = reply.IndexOf(";FF");
            //        }
            //        while (index != -1);


            //        return reply.Remove(0, 7);
            //    }
            //    else return "0";
            //}
            //else return "0";
        }

        internal void SafeMode()
        {
            this.SetFilament(OFF);
            return;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.SerialInterface;


namespace Instrument.MOVE1250
{
    public class MOVE1250
    {
        //Commandset - Source: Thyracont Protokoll
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

        SerialInterface rs232Interface;
        
        public MOVE1250(string comPort)
        {
            rs232Interface = new SerialInterface(comPort, 300, 7, System.IO.Ports.StopBits.Two, System.IO.Ports.Parity.None, 500);
            
            rs232Interface.OpenInterface();
            //this.Dummy();
        }

        public void Close()
        {
            rs232Interface.CloseInterface();
        }

        public void Dispose()
        {
            if (rs232Interface != null)
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
            rs232Interface.SendRequest("\r\n");
        }

        public void SetDigitalMode(bool value)
        {
            string temp;
            
            if (value == false)
            {
                rs232Interface.SendRequest("h01\r\n");
            }
            if (value == true)
            {
                rs232Interface.SendRequest("h02\r\n");
                temp = rs232Interface.ReceiveReply();
            }
        }

        public void ImmediateClosing()
        {
            rs232Interface.SendRequest("x\r\n");
        }

        public void SlowClosing()
        {
            rs232Interface.SendRequest("j\r\n");
        }

        public void ImmediateVenting()
        {
            rs232Interface.SendRequest("y\r\n");
        }

        public void StopValveMovement()
        {
            rs232Interface.SendRequest("z\r\n");
        }

        public void SlowVenting()
        {
            rs232Interface.SendRequest("i\r\n");
        }

        public void SetAbsoluteValvePosition(int value)
        {
            if (value <= 2048) //max 6248
            {
                string temp = "g" + ((value + 512) / 2).ToString("X3") + "\r\n";
                rs232Interface.SendRequest(temp);
            }
        }

        public void IncrementValvePosition(int value)
        {
            if (value <= 0xFF)
            {
                string temp = "g+" + (value).ToString("X2") + "\r\n";
                rs232Interface.SendRequest(temp);
            }
        }

        public void DecrementValvePosition(int value)
        {
            if (value <= 0xFF)
            {
                string temp = "g-" + (value).ToString("X2") + "\r\n";
                rs232Interface.SendRequest(temp);
            }
        }

        public string GetAbsoluteValvePosition()
        {
            rs232Interface.SendRequest("p?\r\n");
            string temp = rs232Interface.ReceiveReply();

            return temp;
            
        }

        internal void SafeMode()
        {
            this.ImmediateClosing();
            this.Close();
            return;
        }
    }
}
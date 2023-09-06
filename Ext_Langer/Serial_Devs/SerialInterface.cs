using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text;


namespace Interface.SerialInterface
{
    public class SerialInterface
    {
        private SerialPort serialPort;

        public SerialInterface(string comPort, Int32 baudRate, Int32 dataBits, StopBits stopBits, Parity parity, Int32 timeOut)
        {
            serialPort = new SerialPort();
            serialPort.PortName = comPort;
            serialPort.BaudRate = baudRate;
            serialPort.DataBits = dataBits;
            serialPort.StopBits = stopBits;
            serialPort.Parity = parity;
            serialPort.ReadTimeout = timeOut;
        }

        public void OpenInterface()
        {
            serialPort.Open();
        }

        public void CloseInterface()
        {
            serialPort.Close();
        }

        public void SendRequest(string request)
        {
            serialPort.Write(request);
        }

        public string ReceiveReply()
        {
            string buffer = "";
            string temp = "";
            //StringBuilder buffer = new StringBuilder(32);
            //char temp;

            int tryCnt = 10;
            //int t0 = Environment.TickCount;

            try
            {
                do
                {
                    System.Threading.Thread.Sleep(20);
                    temp = serialPort.ReadExisting();
                    if (temp == "") tryCnt--;
                    else
                    {
                        buffer += temp;
                        tryCnt = 10;
                    }
                } while (tryCnt != 0);
                //int t1=Environment.TickCount-t0;

                return buffer;
                ////old version (only working for VHS82)
                //do
                //{
                //    temp = (char)serialPort.ReadChar();
                //    if(temp!=0x00) // bekannten übertragungsfehler abfangen
                //        buffer.Append(temp);
                //}
                //while (temp != '\r');
            }
            catch (TimeoutException) { return ""; }

            //return buffer.ToString();
        }
    }
}

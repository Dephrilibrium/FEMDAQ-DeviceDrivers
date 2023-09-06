
using System;
using System.IO.Ports;


namespace Thyracont
{
    public class VD9x
    {
        private SerialPort _serialPort;
        public string DeviceType;


        public VD9x(string comPort, int baud, int databits, StopBits stopbits, Parity parity, int timeout_ms = 250)
        {
            // Guard-clauses
            _serialPort = new SerialPort(comPort, baud, parity, databits, stopbits);
            _serialPort.ReadTimeout = timeout_ms;
            _serialPort.Open();

            // Check if device can be accessed!
            try { DeviceType = GetDeviceType(); }
            catch (Exception e)
            {
                _serialPort.Close();
                throw new TimeoutException("GetDeviceType() timed out.\r\n\r\n" + e.Message);
            }
            
        }

        public void Dispose()
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();
        }

        public string ComPort { get { return _serialPort.PortName; } }
        public int Baudrate { get { return _serialPort.BaudRate; } }
        public int Databits { get { return _serialPort.DataBits; } }
        public StopBits Stopbits { get { return _serialPort.StopBits; } }
        public Parity Parity { get { return _serialPort.Parity; } }
        public int TimoutMS
        {
            get { return _serialPort.ReadTimeout; }
            set { _serialPort.ReadTimeout = value; }
        }

        #region communication-helper (preparation, sending, receiving, query)
        private byte[] GenerateSendbytearray(string cmd)
        {
            // Checksum = (sumOfByteContents(cmd) % 64) + 64 = WX -> hex -> 0xXY
            var sendBytes = new byte[cmd.Length + 2];
            int chksum = 0;
            var byteIndex = 0;
            for (; byteIndex < cmd.Length; byteIndex++)
            {
                sendBytes[byteIndex] = (byte)cmd[byteIndex];
                chksum += (byte)cmd[byteIndex];
            }
            chksum = chksum % 64 + 64;
            sendBytes[byteIndex] = (byte)chksum;
            sendBytes[byteIndex + 1] = (byte)'\r';
            return sendBytes;
        }


        private void WriteCommand(string cmd)
        {
            var bufferString = "001" + cmd; // Concatenate address and actioncommand
            var sendBytes = GenerateSendbytearray(bufferString); // Attach residual chksum
            _serialPort.Write(sendBytes, 0, sendBytes.Length);
        }


        private string ReadResponse()
        {
            var response = _serialPort.ReadTo("\r");
            return response;
        }


        private string QueryCommand(string cmd)
        {
            WriteCommand(cmd);
            var response = ReadResponse();
            return response;
        }
        #endregion



        #region Commands (getdevicetype, getpressureMbar, getpressureBar)
        private string GetDeviceType()
        {
            var response = QueryCommand("T");
            response = response.Substring(4, 3);
            return response;
        }


        public double GetPressureValueMbar()
        {
            var response = QueryCommand("M");
            double mantissa = double.Parse(response.Substring(4, 4)); // / 1000; moved to exponent!
            double exponent = -(23 - double.Parse(response.Substring(8, 2))); // Biasvalue of 20 found by experimental sweep! Added 3 from mantissa!
            double pressure =  mantissa * Math.Pow(10, exponent);
            return pressure;
        }


        public double GetPressureValueBar()
        {
            var mbar = GetPressureValueMbar();
            var bar = mbar * (10^-3); // mbar to bar
            return bar;
        }
        #endregion
    }
}

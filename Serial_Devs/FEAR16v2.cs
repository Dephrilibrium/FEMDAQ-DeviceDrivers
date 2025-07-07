using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace HaumOTH
{

    public enum FEAR16v2ChannelRequestStatus
    {
        Unknown = -2,
        Error = -1,
        None = 0,
        Nak = 1,
        Ack = 2,
        Requested = 3,
        NewValue = 4,
    }

    public class FEAR16v2ChannelRequest
    {
        public bool Requested = false;
        public bool _newValAvailable = false;
        public bool NewValAvailable // = false;
        {
            get
            {
                if (_newValAvailable)
                {
                    _newValAvailable = false;
                    return true;
                }

                return false;
            }
            set
            {
                _newValAvailable = value;
            }
        }

        public bool Nak = false;
        //public FEAR16v2ChannelRequestStatus Status = FEAR16v2ChannelRequestStatus.None; // Implementation maybe in the future
        //private double _value; // Try with custom getter caused problems!
        public double Value { get; set; }
    }

    public class FEAR16v2
    {
        private SerialPort _serialPort;
        public int AmountOfChannels { get; private set; }
        public bool ConnectionEtablished { get { return _serialPort.IsOpen; } }
        public string DeviceType { get; private set; }
        public string FirmwareVersion { get; private set; }

        public List<FEAR16v2ChannelRequest> CurrCtrlChannels { get; set; }
        public List<FEAR16v2ChannelRequest> CurrFlowChannels { get; set; }
        public List<FEAR16v2ChannelRequest> UFETDropChannels { get; set; }

        public FEAR16v2(string comPort, int baud = 115200, int dataBits = 8, StopBits stopBits = StopBits.One, Parity parity = Parity.None, int timeout_ms = 1000)
        {
            if (_serialPort == null)
            {
                _serialPort = new SerialPort(comPort, 115200, parity, dataBits, stopBits); // Standard connection baud 115200 until it gets changed!
                changeTimeout_ms(timeout_ms);
                _serialPort.NewLine = "\r\n";
                _serialPort.Open();

                try
                {
                    QueryCommand("echo test"); // Send anything to remove old commands
                }
                catch (Exception e)
                {
                    // Clear up ressource. Otherwise some weird connections-oks appears, even if the device is not connected!
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                    throw e;
                }

                ChangeBaudrate(baud);

                var response = QueryCommand("IDN?");
                var split = response.Split(new char[] { ' ', ',' });
                if (split.Length == 0)
                    throw new Exception("Incorrect response (wrong device?): " + response);
                // split[0] == "ack" or "nak"
                if (!split[1].StartsWith("Device"))
                    throw new Exception("Response not starting with \"Device:\":" + response);
                if (split.Length != 5)
                    throw new Exception("Response can't splitted into 5 parts (maybe wrong device?): " + response);
                if (!split[3].StartsWith("Firmware-Build"))
                    throw new Exception("2nd responsepart not starting with \"Firmware-Build:\":" + response);

                DeviceType = split[2];
                FirmwareVersion = split[4];

                AmountOfChannels = 16;
                CurrCtrlChannels = new List<FEAR16v2ChannelRequest>();
                CurrFlowChannels = new List<FEAR16v2ChannelRequest>();
                UFETDropChannels = new List<FEAR16v2ChannelRequest>();
                for (int iCh = 0; iCh < 16; iCh++) // 16 Channels!
                {
                    CurrCtrlChannels.Add(new FEAR16v2ChannelRequest());
                    CurrFlowChannels.Add(new FEAR16v2ChannelRequest());
                    UFETDropChannels.Add(new FEAR16v2ChannelRequest());
                }
            }
        }


        public void changeTimeout_ms(int timeout_ms)
        {
            _serialPort.ReadTimeout = timeout_ms;
        }


        public void Dispose()
        {
            CloseConnection();
            _serialPort = null; // Delete phy-connection
        }


        public void OpenConnection()
        {
            if (_serialPort != null && !_serialPort.IsOpen)
                _serialPort.Open();
        }


        public void CloseConnection()
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();
        }


        #region Communication-Helper (sending, receiving, query)
        private void WriteCommand(string cmd)
        {
            //cmd += "\r\n";
            _serialPort.WriteLine(cmd);
        }

        private string ReadResponse()
        {
            // Because of the hardware (I/O is the same pin) and the baud of 300 there is a problem with the response!
            //  this routine is just used to empty the read-buffer
            return _serialPort.ReadLine(); // Response from device is maybe incorrect!
        }


        private string QueryCommand(string cmd)
        {
            WriteCommand(cmd);
            return ReadResponse();
        }
        #endregion


        #region Controls, Valueset, Valueget
        public void ChangeBaudrate(int baud)
        {
            return; // DO NOTHING! Encountered some connection problems otherwise!

            //if (baud == _serialPort.BaudRate)
            //    return;

            //if (baud <= 0)
            //    throw new Exception("Invalid baud: " + baud.ToString());

            //if (baud == _serialPort.BaudRate)
            //    return;

            //var response = QueryCommand("TERM:BAUD " + baud.ToString());
            //if (!response.StartsWith("ack"))
            //    throw new Exception("Couldn't change baud of FEAR16v2");

            //CloseConnection();
            //_serialPort.BaudRate = baud;
            //OpenConnection();
            //response = QueryCommand("echo test");
            //if (response != "test")
            //    throw new Exception("Baudrate change failed");
        }

        public void ChangeAdcNMeanPoints(uint nMeanPnts)
        {
            var response = QueryCommand($"ADC:NMEAN {nMeanPnts}");

            if (!response.StartsWith("ack"))
            {
                var fPos = response.IndexOf(' ');
                response = response.Remove(0, fPos+1);
                throw new Exception($"Couldn't change NMEAN-Points of FEAR16v2: {response}");
            }
        }

        public void ChangeAdcMDeltaTime(uint deltaTime_ms)
        {
            var response = QueryCommand($"ADC:MDELT {deltaTime_ms}");

            if (!response.StartsWith("ack"))
            {
                var fPos = response.IndexOf(' ');
                response = response.Remove(0, fPos + 1);
                throw new Exception($"Couldn't change MeasurementDELTa-timeof FEAR16v2: {response}");
            }
        }

        public void ResetCurrentControl()
        {
            if (CurrCtrlChannels != null)
            {
                foreach (var outChannel in CurrCtrlChannels)
                {
                    outChannel.Requested = true;
                    outChannel.Value = 0;
                }
                UpdateCurrentControlRequests();
                //resetRequestedChannels(CurrCtrlChannels); // Done in UpdateCurrentControlRequests()
            }
        }

        public void UpdateCurrentControlRequests()
        {
            string basicCmd = "DAC:SET ";
            string cmd = basicCmd;
            for (int iCh = 0; iCh < CurrCtrlChannels.Count; iCh++)
            {
                if (CurrCtrlChannels[iCh].Requested)
                {
                    CurrCtrlChannels[iCh].Requested = false;
                    cmd += string.Format("Ch{0}:{1},", iCh.ToString(), CurrCtrlChannels[iCh].Value.ToString("0.00000"));
                }
            }

            if (cmd == basicCmd)
                return;

            cmd = cmd.Remove(cmd.Length - 1); // remove last ,
            var response = QueryCommand(cmd);
            resetRequestedChannels(CurrCtrlChannels);
        }

        public void MeasureCurrentFlowRequests()
        {
            string basicCmd = "ADC:GET SHNT ";
            internalMeasurementRoutine(basicCmd, CurrFlowChannels);
        }

        public void MeasureUFETDropRequests()
        {
            string basicCmd = "ADC:GET DROP ";
            internalMeasurementRoutine(basicCmd, UFETDropChannels);

            /*
             * Factor is calculated from electric circuit
             * 1.) Downscale from voltage divider (1TOhm / 5GOhm)
             * 2.) Upscale from operational amplifier (1500V should be 10V)
             * 3.) Factor = 1500V / 10V = 150
             */
            multiplyNewChannelValuesByFactor(UFETDropChannels, 150);
        }

        private void multiplyNewChannelValuesByFactor(List<FEAR16v2ChannelRequest> channelList, double factor)
        {
            foreach (FEAR16v2ChannelRequest channel in channelList)
                if (channel.NewValAvailable) // Reading NewValAvailable resets the flag!
                {
                    channel.NewValAvailable = true; // Set back to true
                    channel.Value *= factor; // Reading from value resets NewValAvailable!
                }
        }

        private void internalMeasurementRoutine(string basicCmd, List<FEAR16v2ChannelRequest> requestList)
        {
            string cmd = basicCmd + buildRequestedChannelString(requestList);

            if (cmd == basicCmd)
                return;

            var response = QueryCommand(cmd); // Get response without ack/nak
            bool acknowledged = false;
            if (response.StartsWith("ack"))
                acknowledged = true;

            var split = response.Remove(0, 4).Split(new char[] { ',' });
            // Assign values to requested channels
            int iVal = 0;
            for (int iCh = 0; iCh < requestList.Count; iCh++)
            {
                if (requestList[iCh].Requested)
                {
                    if (acknowledged)
                    {
                        requestList[iCh].Value = double.Parse(split[iVal]);
                        requestList[iCh].NewValAvailable = true;
                    }
                    else // When not acknowledged don't touch the value and set not acknowledged flag
                        requestList[iCh].Nak = true;

                    requestList[iCh].Requested = false;
                    iVal++;
                }
            }
        }

        private string buildRequestedChannelString(List<FEAR16v2ChannelRequest> requestList)
        {
            string channelList = "";

            for (int iCh = 0; iCh < requestList.Count; iCh++)
            {
                if (requestList[iCh].Requested)
                    channelList += string.Format("Ch{0},", iCh);
            }
            if (channelList.Length > 0)
                channelList = channelList.Remove(channelList.Length - 1); // Remove last ,

            return channelList;
        }

        private void resetRequestedChannels(List<FEAR16v2ChannelRequest> requestList)
        {
            for (int iCh = 0; iCh < requestList.Count; iCh++)
                requestList[iCh].Requested = false;
        }
        #endregion

    }
}
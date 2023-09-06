using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


namespace HP
{
    public enum HP4155BChannel { Invalid, SMU1, SMU2, SMU3, SMU4, VM1, VM2, VS1, VS2 }

    public enum HP4155BMeasureMode { None, Current, Voltage }

    public enum HP4155BIntegrationMode { Invalid, Short, Medium, Long }

    public enum HP4155BAutoCalibration { Off, On }



    public class HP4155B
    {
        private static int _activeChannelCount = 0;

        private Device _device;
        private HP4155BChannel _channel = HP4155BChannel.Invalid;
        private string _channelAsString;
        private HP4155BMeasureMode _measureMode = HP4155BMeasureMode.None;

        public HP4155B(int boardNumber, byte primAddr, byte secAddr, HP4155BChannel channel, HP4155BMeasureMode measureMode)
        {
            _device = new Device(boardNumber, primAddr, secAddr);
            Info(); // Try to communicate with the device. Throws Exception when device is unreachable!

            _activeChannelCount++;
            _channel = channel;
            _channelAsString = _channel.ToString();
            _measureMode = measureMode;

            Init();
        }



        public void Dispose()
        {
            if (_device != null)
                _device.Dispose();

            _activeChannelCount--;
        }



        public string Info()
        {
            _device.Write("*IDN?;");
            return _device.ReadString();
        }



        private void Init()
        {
            ChannelConfig();
        }



        private void ChannelConfig()
        {
            var cmd = ":PAGE:CHAN:CDEF:MODE SAMP";
            _device.Write(cmd);

            cmd = ":PAGE:CHAN:" + _channelAsString + ":MODE " + (_measureMode == HP4155BMeasureMode.Voltage ? "I" : "V"); // This represents the output-Mode!
            _device.Write(cmd);

            cmd = ":PAGE:CHAN:" + _channelAsString + ":FUNC CONS";
            _device.Write(cmd);
        }



        public HP4155BIntegrationMode IntegrationMode
        {
            get
            {
                _device.Write(":PAGE:MEAS:MSET:ITIM?");
                var response = _device.ReadString();
                switch (response)
                {
                    case "SHOR\n":
                        return HP4155BIntegrationMode.Short;

                    case "MED\n":
                        return HP4155BIntegrationMode.Medium;

                    case "LONG\n":
                        return HP4155BIntegrationMode.Long;

                    default:
                        throw new InvalidOperationException(string.Format("Can't read IntegrationMode from an unknown MeasureType ({0})", _measureMode.ToString()));
                }
            }
            set
            {
                switch (value)
                {
                    case HP4155BIntegrationMode.Short:
                        _device.Write(":PAGE:MEAS:MSET:ITIM SHOR");
                        return;
                    case HP4155BIntegrationMode.Medium:
                        _device.Write(":PAGE:MEAS:MSET:ITIM MED");
                        return;
                    case HP4155BIntegrationMode.Long:
                        _device.Write(":PAGE:MEAS:MSET:ITIM LONG");
                        return;
                    default:
                        return;
                }
            }
        }



        public HP4155BAutoCalibration AutoCalibration
        {
            get
            {
                _device.Write(":CAL:AUTO?");
                var response = _device.ReadString();
                return (response == "1\n" ? HP4155BAutoCalibration.On : HP4155BAutoCalibration.Off);
            }
            set
            {
                _device.Write(":CAL:AUTO " + (value == HP4155BAutoCalibration.On ? "1" : "0"));
            }
        }




        public void SetOutput(double VoltageValue, double CurrentValue)
        {
            string cmd = null;
            switch(_measureMode)
            {
                case HP4155BMeasureMode.Current:
                    cmd = ":PAGE:MEAS:SAMP:CONS:" + _channelAsString + ":COMP " + CurrentValue.ToString("E3");
                    _device.Write(cmd);
                    cmd = ":PAGE:MEAS:SAMP:CONS:" + _channelAsString + " " + VoltageValue.ToString("E3");
                    _device.Write(cmd);
                    break;

                case HP4155BMeasureMode.Voltage:
                default:
                    cmd = ":PAGE:MEAS:SAMP:CONS:" + _channelAsString + ":COMP " + VoltageValue.ToString("E3");
                    _device.Write(cmd);
                    cmd = ":PAGE:MEAS:SAMP:CONS:" + _channelAsString + " " + CurrentValue.ToString("E3");
                    _device.Write(cmd);
                    break;
            }
        }



        public double GetOutput()
        {
            _device.Write(":PAGE:MEAS:SAMP:CONS:" + _channelAsString + "?");
            var response = _device.ReadString();

            return double.Parse(response);
        }



        //public bool Output
        //{
        //    get
        //    {

        //    }
        //    set
        //    {

        //    }
        //}


        //public void SafeMode()
        //{
        //    // NOT IMPLEMENTED, BE CAREFUL!!!
        //}
    }
}

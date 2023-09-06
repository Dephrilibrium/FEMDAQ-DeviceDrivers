using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.NI4882;


namespace Instrument.HP4145B
{
    public enum Channel
    {
        SMU1 = 1,
        SMU2 = 2,
        SMU3 = 3,
        SMU4 = 4,
        VM1 = 5,
        VM2 = 6,
        VS1 = 7,
        VS2 = 8
    }

    public enum Mode
    {
        Voltage = 0,
        Current = 1
    }

    public enum IntegrationTime
    {
        Short = 1, Medium = 2, Long = 3
    }

    public enum AutoCalibration
    {
        Off = 0,
        On = 1
    }

    public class HP4145B
    {
        Device hp4145b;

        public HP4145B(int boardNumber, byte primAddr, byte secAddr)
        {
            hp4145b = new Device(boardNumber, primAddr, secAddr);
            Info(); // Check connection
        }

        public void Dispose()
        {
            if (hp4145b != null)
            {
                // added by haum
                try { SafeMode(); }
                catch (Exception) { }
                // ----------------
                hp4145b.Dispose();
            }
        }

        public string Info()
        {
            hp4145b.Write("*IDN?;");
            return hp4145b.ReadString();
        }

        public void SetParameter(IntegrationTime integrationTime, AutoCalibration autoCalibration)
        {
            string command = "US;"; //select USERMODE

            hp4145b.Write(command);

            switch (integrationTime)
            {
                case IntegrationTime.Short: command = "IT1 "; break;
                case IntegrationTime.Medium: command = "IT2 "; break;
                case IntegrationTime.Long: command = "IT3 "; break;
            }

            switch (autoCalibration)
            {
                case AutoCalibration.Off: command += "CA0 "; break;
                case AutoCalibration.On: command += "CA1 "; break;
            }

            command += "BC;";

            hp4145b.Write(command);
        }

        public void SetChannel(Mode mode, Channel channel, double voltageValue, double currentValue)
        {
            string command = "";

            if (mode == Mode.Current)
            {
                command = "DI";

                switch (channel)
                {
                    case Channel.SMU1: command += "1,"; break;
                    case Channel.SMU2: command += "2,"; break;
                    case Channel.SMU3: command += "3,"; break;
                    case Channel.SMU4: command += "4,"; break;
                    default: command += "4,"; break;
                }

                command += "0,"; //select AUTORANGE
                command += currentValue.ToString("0.0000E0") + ",";
                command += voltageValue.ToString("0.0000E0") + ";";

            }
            else // Mode.Voltage
            {
                if ((int)channel < 5)
                {
                    command = "DV";

                    switch (channel)
                    {
                        case Channel.SMU1: command += "1,"; break;
                        case Channel.SMU2: command += "2,"; break;
                        case Channel.SMU3: command += "3,"; break;
                        case Channel.SMU4: command += "4,"; break;
                    }

                    command += "0,"; //select AUTORANGE
                    command += voltageValue.ToString("0.0000E0") + ",";
                    command += currentValue.ToString("0.0000E0") + ";";
                }
                else
                {
                    command = "DS";

                    switch (channel)
                    {
                        case Channel.VS1: command += "1,"; break;
                        case Channel.VS2: command += "2,"; break;
                        default: command += "2,"; break;
                    }

                    command += voltageValue.ToString("0.0000E0") + ";";
                }
            }

            hp4145b.Write(command);
        }

        public double GetChannel(Mode mode, Channel channel)
        {
            string command = "";
            string value = "0";
            //string status;

            if ((int)channel < 5)
            {
                switch (mode)
                {
                    case Mode.Voltage: command = "TV"; break;
                    case Mode.Current: command = "TI"; break;
                }

                switch (channel)
                {
                    case Channel.SMU1: command += "1;"; break;
                    case Channel.SMU2: command += "2;"; break;
                    case Channel.SMU3: command += "3;"; break;
                    case Channel.SMU4: command += "4;"; break;
                }

                hp4145b.Write(command);
                value = hp4145b.ReadString();

                //Valid Data??? Check status, check channel, check mode!
                //status = value.Substring(0, 1);

                value = value.Substring(3, value.Length - (3 + 2));
            }
            else
            {
                command = "TV";

                switch (channel)
                {
                    case Channel.VM1: command += "5;"; break;
                    case Channel.VM2: command += "6;"; break;
                    default: command += "6;"; break;
                }

                hp4145b.Write(command);
                value = hp4145b.ReadString();

                //Valid Data??? Check status, check channel, check mode!
                //status = value.Substring(0, 1);

                value = value.Substring(4, value.Length - (4 + 2));
            }

            return Convert.ToDouble(value);
        }

        public void SafeMode()
        {
            // NOT IMPLEMENTED, BE CAREFUL!!!
        }
    }
}

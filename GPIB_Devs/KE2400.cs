using System;
using System.Threading;

using NationalInstruments.NI4882;

namespace Keithley
{
    public enum KE2400MeasureType { None, Current, Voltage, Ohmic }

    public class KE2400
    {
        private Device _device;
        private KE2400MeasureType _measureType = KE2400MeasureType.None;


        public KE2400(int BoardNumber, int PrimaryAddress, int SecondaryAddress)
        {
            _device = new Device(BoardNumber, (byte)PrimaryAddress, (byte)SecondaryAddress);
            if (_device == null) throw new GpibException(string.Format("Can't find KE2400 (GPIB: {0},{1},{2})",
                                                                BoardNumber,
                                                                PrimaryAddress,
                                                                SecondaryAddress));

            Info(); // Try to communicate with the device. Throws an timeoutexception when device is turned off!
        }



        public void Dispose()
        {
            if (_device != null)
            {
                PowerDownSource();
                _device.Dispose();
            }
        }



        public string Info()
        {
            _device.Write("*IDN?");
            var response = _device.ReadString();
            return response;
        }



        public void Reset()
        {
            _device.Write("*RST");
        }



        public void Init()
        {
        }



        public KE2400MeasureType MeasureType
        {
            get
            {
                _device.Write(":SOUR:FUNC?");
                var response = _device.ReadString();
                switch (response)
                {
                    case "VOLT\n":
                        return KE2400MeasureType.Voltage;

                    case "RES\n":
                        return KE2400MeasureType.Ohmic;

                    case "CURR\n":
                        return KE2400MeasureType.Current;

                    default:
                        return KE2400MeasureType.None;
                }
            }
            set
            {
                _device.Write("FUNC:OFF:ALL");
                switch (value)
                {
                    case KE2400MeasureType.Ohmic:// Not really tested
                        _device.Write("FUNC 'RES'");
                        //_device.Write("SOUR:FUNC CURR");
                        //_device.Write("SOUR:CURR:MODE FIXED");
                        //_device.Write("CURR:PROT MAX");
                        break;

                    case KE2400MeasureType.Voltage:
                        _device.Write("SOUR:FUNC CURR");
                        _device.Write("SOUR:CURR:MODE FIXED");
                        _device.Write("SOUR:CURR:LEV 0");
                        _device.Write("SENS:FUNC 'VOLT'");// Select current as measurement
                        _device.Write("VOLT:PROT MAX");
                        _device.Write("FORM:ELEM VOLT");
                        break;

                    case KE2400MeasureType.Current:
                    default:
                        _device.Write("SOUR:FUNC VOLT");
                        _device.Write("SOUR:VOLT:MODE FIXED");
                        _device.Write("SOUR:VOLT:LEV 0");
                        _device.Write("SOUR:VOLT:RANG MAX");
                        _device.Write("SENS:FUNC 'CURR'"); // Select current as measurement
                        _device.Write("CURR:PROT MAX");
                        _device.Write("FORM:ELEM CURR");
                        value = KE2400MeasureType.Current;
                        break;
                }
                _measureType = value;
            }
        }



        public double Range
        {
            get
            {
                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                        _device.Write("VOLT:RANG?");
                        break;

                    case KE2400MeasureType.Ohmic:
                        _device.Write("RES:RANG?");
                        break;

                    case KE2400MeasureType.Current:
                        _device.Write("CURR:RANG?");
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("Can't read Range from an unknown MeasureType ({0})", _measureType.ToString()));
                }
                var response = _device.ReadString().TrimEnd(new char[] { '\n' });
                return double.Parse(response);
            }
            set
            {
                if (value == 0)
                {
                    AutoRange = true;
                    return;
                }

                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                        _device.Write("VOLT:RANG " + value.ToString("E3"));
                        break;

                    case KE2400MeasureType.Ohmic:
                        _device.Write("RES:RANG " + value.ToString("E3"));
                        break;

                    case KE2400MeasureType.Current:
                        _device.Write("CURR:RANG " + value.ToString("E3"));
                        break;

                    default:
                        break;
                }
            }
        }



        private bool AutoRange
        {
            get
            {
                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                        _device.Write("VOLT:RANG:AUTO?");
                        break;

                    case KE2400MeasureType.Ohmic:
                        _device.Write("RES:RANG:AUTO?");
                        break;

                    case KE2400MeasureType.Current:
                        _device.Write("CURR:RANG:AUTO?");
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("Can't read AutoRange-State from an unknown MeasureType ({0})", _measureType.ToString()));
                }

                var response = _device.ReadString();
                return (response == "1" ? true : false);
            }
            set
            {
                var stateAsString = (value ? "1" : "0");

                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                        _device.Write("VOLT:RANG:AUTO " + stateAsString);
                        return;

                    case KE2400MeasureType.Ohmic:
                        _device.Write("RES:RANG:AUTO " + stateAsString);
                        return;

                    case KE2400MeasureType.Current:
                        _device.Write("CURR:RANG:AUTO " + stateAsString);
                        return;

                    default:
                        return;
                }
            }
        }


        public double NPLC
        {
            get
            {
                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                        _device.Write("VOLT:NPLC?");
                        break;

                    case KE2400MeasureType.Ohmic:
                        _device.Write("RES:NPLC?");
                        break;

                    case KE2400MeasureType.Current:
                        _device.Write("CURR:NPLC?");
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("Can't read Range from an unknown MeasureType ({0})", _measureType.ToString()));
                }

                var response = _device.ReadString();
                return double.Parse(response);
            }
            set
            {
                var nplcAsString = value.ToString("E2");

                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                        _device.Write("VOLT:NPLC " + nplcAsString);
                        return;

                    case KE2400MeasureType.Ohmic:
                        _device.Write("RES:NPLC " + nplcAsString);
                        return;

                    case KE2400MeasureType.Current:
                        _device.Write("CURR:NPLC " + nplcAsString);
                        return;

                    default:
                        return;
                }
            }
        }



        public double MeasureAndReadValue()
        {
            _device.Write("READ?");
            var response = _device.ReadString();
            return double.Parse(response);
        }




        public double Compliance
        {
            get
            {
                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                    case KE2400MeasureType.Ohmic:
                        _device.Write("CURR:PROT?");
                        break;

                    case KE2400MeasureType.Current:
                        _device.Write("CURR:PROT?");
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("Can't read Compliance from an unknown MeasureType ({0})", _measureType.ToString()));
                }
                var response = _device.ReadString();
                return double.Parse(response);
            }
            set
            {
                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                        if (value <= 0 || value > 210)
                            value = 210;
                        _device.Write("VOLT:PROT " + value.ToString("E2"));
                        break;

                    case KE2400MeasureType.Current:
                        if (value <= 0 || value > 1.05)
                            value = 1.05; // Highest possible compliance
                        _device.Write("CURR:PROT " + value.ToString("E2"));
                        break;

                    case KE2400MeasureType.Ohmic: // Can't set a compliance
                    default:
                        return;
                }
            }
        }




        public double OutputValue
        {
            get
            {
                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                    case KE2400MeasureType.Ohmic:
                        _device.Write(":SOUR:CURR?");
                        break;

                    case KE2400MeasureType.Current:
                        _device.Write(":SOUR:VOLT?");
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("Can't read OutputValue from an unknown MeasureType ({0})", _measureType.ToString()));
                }
                var response = _device.ReadString();
                return double.Parse(response);
            }
            set
            {
                switch (_measureType)
                {
                    case KE2400MeasureType.Voltage:
                    case KE2400MeasureType.Ohmic:
                        _device.Write(":SOUR:CURR " + value.ToString("E3"));
                        break;

                    case KE2400MeasureType.Current:
                        _device.Write(":SOUR:VOLT " + value.ToString("E1"));
                        break;

                    default:
                        return;
                }
            }
        }


        public bool Output
        {
            get
            {
                _device.Write(":OUTP?");
                var response = _device.ReadString();
                return (response == "1" ? true : false);
            }
            set
            {
                _device.Write(":OUTP " + (value ? "1" : "0"));
            }
        }



        private void PowerDownSource()
        {
            Output = false;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// own usings
using Ivi.Driver.Interop;
using Agilent.AgInfiniiVision.Interop;

namespace Keysight
{
    class DSOX3000WavGen
    {
        // Physical instance of the wavgen
        private AgInfiniiVision _device = null;
        private IAgInfiniiVisionWaveGen _waveGen;


        public DSOX3000WavGen(string VisaAddress)
        {
            if (VisaAddress == null) throw new ArgumentNullException("VisaAddress can't be null.");

            _device = new AgInfiniiVision();
            try { _device.Initialize(VisaAddress, false, false); }
            catch (Exception) { throw new FormatException("Can't initialize connection to DSOX3034T. (Maybe the device is turned off or the VISA-address has the wrong format)"); }

            _waveGen = _device.WaveGens2.Item2["WaveGen1"];

            Init();
        }



        public void Dispose()
        {
            if (_device != null)
            {
                try { PowerDownSource(); }
                catch (Exception) { }
                _device.Close();
            }
        }



        public string Info()
        {
            return "Manufacturer: " + _device.Identity.InstrumentManufacturer +
                   ", Model: " + _device.Identity.InstrumentModel +
                   ", Firmware: " + _device.Identity.InstrumentFirmwareRevision;
        }



        public void Init()
        {
            _device.System.TimeoutMilliseconds = 500; // 500ms timeout
        }



        public AgInfiniiVisionWaveformGeneratorOutputLoadImpedanceEnum OutputLoad
        {
            get { return _waveGen.OutputLoadImpedance; }
            set { _waveGen.OutputLoadImpedance = value; }
        }



        public bool OutputInverted
        {
            get { return _waveGen.OutputInverted; }
            set { _waveGen.OutputInverted = value; }
        }



        public bool Output
        {
            get { return _waveGen.OutputEnabled; }
            set { _waveGen.OutputEnabled = value; }
        }


        public AgInfiniiVisionWaveformGeneratorFunctionEnum Waveform
        {
            get { return _waveGen.Function; }
            set { _waveGen.Function = value; }
        }



        public double Frequency
        {
            get { return _waveGen.Frequency; }
            set { _waveGen.Frequency = value; }
        }



        public void SetAmplitude(double Amplitude)
        {
            _waveGen.Amplitude = Amplitude;
        }



        public void SetAmplitude(double High, double Low)
        {
            _waveGen.HighVoltage = High;
            _waveGen.LowVoltage = Low;
        }



        public void GetAmplitude(out double Amplitude)
        {
            Amplitude = _waveGen.Amplitude;
        }


        public void GetAmplitude(out double High, out double Low)
        {
            High = _waveGen.HighVoltage;
            Low = _waveGen.LowVoltage;
        }


        public double Offset
        {
            get { return _waveGen.OffsetVoltage; }
            set { _waveGen.OffsetVoltage = value; }
        }



        public double DutyCycle
        {
            get { return _waveGen.DutyCycle; }
            set { _waveGen.DutyCycle = value; }
        }



        public double RampSymmetry
        {
            get { return _waveGen.RampSymmetry; }
            set { _waveGen.RampSymmetry = value; }
        }



        public double PulseWidth
        {
            get { return _waveGen.PulseWidth; }
            set { _waveGen.PulseWidth = value; }
        }



        public void PowerDownSource()
        {
            Output = false;
        }
    }
}

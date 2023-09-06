using System;

// own usings
using NationalInstruments.NI4882;
using System.Collections.Generic;

namespace StanfordResearch
{
    // Statusflag enum
    [Flags]
    public enum sr785StatusFlag { INST = 1, DISP = 2, INPT = 4, IERR = 8, MAV = 16, ESB = 32, SRQ = 64, IFC = 128 }

    // Display setup enum
    public enum sr785Display { A = 0, B, Both }
    public enum sr785DisplayFormat { Single = 0, Dual, Overlay }
    public enum sr785XAxisView { Linear = 0, Logarithmic }
    internal enum sr785MeasureGroup { FFT = 0, Correlation, Octave, SweptSine, Order, TimeHistrogram }
    public enum sr785Measurement { FFT1 = 0, FFT2, PowerSpectrum1, PowerSpectrum2, Time1, Time2, WindowTime1, WindowTime2, Orbit, Coherence, CrossSpectrum, FrequencySpectrum, Capture1, Capture2 }
    public enum sr785YAxisView { LogMag = 0, LinMag, MagSquared, Real, Imaginary }
    public enum sr785YAxisPeakUnit { Peak = 1, PeakRMS, PeakPeak }
    public enum sr785YAxisdBUnit { Off = 0, dB = 1, dBm }

    // Frequency enum
    internal enum sr785BaseFrequency { kHz100 = 0, kHz102_4 }
    public enum sr785FftLines { Lines100 = 0, Lines200, Lines400, Lines800 }

    // Averaging enum
    public enum sr785AveragingType { Linear_FixLength = 0, Exponential_Continuous }
    public enum sr785AveragingDisplayed { None = 0, Vector, RMS, PeakHold }

    // Standard-Window enum
    public enum sr785WindowType { Uniform_Rect = 0, Flattop, Hanning, BlackmanHarris, Kaiser, Exponential }


    // Measurementresult
    public struct SR785Result
    {
        public int ErrorState;
        public double[] Values;
        public double StartFrequency;
        public double StopFrequency;
        public double FrequencySpan;
        public int FftLines;
        public double ResolutionBandwidth;
        public double AcquisitionTime;
    }



    public class SR785
    {
        private Device _device;

        private sr785Display _display;




        public SR785(int boardNum, int primAddr, int secAddr, sr785Display display)
            : this(boardNum, primAddr, secAddr)
        {
            SetDisplay(display);
        }



        /// <summary>
        /// Constructs the communication device
        /// </summary>
        /// <param name="boardNum"></param>
        /// <param name="primAddr"></param>
        /// <param name="secAddr"></param>
        public SR785(int boardNum, int primAddr, int secAddr)
        {
            _device = new Device(boardNum, (byte)primAddr, (byte)secAddr);
            var info = Info; // Check connection
            Init();
        }



        /// <summary>
        /// Cleans up the ressources of the device.
        /// </summary>
        public void Dispose()
        {
            if (_device != null)
                _device.Dispose(); // Cleanup ressources
        }



        /// <summary>
        /// Returns a string which contains device-information
        /// </summary>
        public string Info
        {
            get
            {
                _device.Write("*IDN?"); // Get deviceinfo
                return _device.ReadString();
            }
        }



        /// <summary>
        /// Resets the device-params and could be used for initialization-stuff.
        /// </summary>
        private void Init()
        {
            _device.IOTimeout = TimeoutValue.T10s;  // Prepare timeout
            /* Do your other init-stuff here */
        }




        #region Display
        public void SetDisplay(sr785Display display)
        {
            _display = display;
            var cmd = string.Format("ACTD {0}", ((int)display).ToString());
            _device.Write(cmd);
            cmd = string.Format("DISP {0}, 1", ((int)display).ToString());
            _device.Write(cmd);
        }



        public sr785Display GetDisplay()
        {
            var cmd = string.Format("ACTD?");
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785Display.A : sr785Display.B);
            return retVal;
        }



        public void SetDisplayFormat(sr785DisplayFormat format)
        {
            var cmd = string.Format("DFMT {0}", ((int)format).ToString());
            _device.Write(cmd);
        }


        public sr785DisplayFormat GetDisplayFormat()
        {
            var cmd = string.Format("DFMT?");
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785DisplayFormat.Single :
                          response[0] == '1' ? sr785DisplayFormat.Dual : sr785DisplayFormat.Overlay);
            return retVal;
        }
        #endregion



        #region AxisView
        public void SetXAxisView(sr785XAxisView axisView)
        {
            var cmd = string.Format("XAXS {0}, {1}", ((int)_display).ToString(), ((int)axisView).ToString());
            _device.Write(cmd);
        }



        public sr785XAxisView GetXAxisView()
        {
            var cmd = string.Format("XAXS? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785XAxisView.Linear : sr785XAxisView.Logarithmic);
            return retVal;
        }



        public void SetFrequencyBorders(sr785FftLines fftLines, double fStart, double fSpan)
        {
            var cmd = string.Format("FBAS {0}, {1} Hz", ((int)sr785Display.Both).ToString(), ((int)sr785BaseFrequency.kHz102_4).ToString());
            _device.Write(cmd);

            cmd = string.Format("FSTR {0}, {1} Hz", ((int)sr785Display.Both).ToString(), fStart.ToString());
            _device.Write(cmd);

            cmd = string.Format("FLIN {0}, {1}", ((int)sr785Display.Both).ToString(), ((int)fftLines).ToString());
            _device.Write(cmd);

            cmd = string.Format("FSPN {0}, {1} Hz", ((int)sr785Display.Both).ToString(), fSpan.ToString());
            _device.Write(cmd);
        }



        public sr785FftLines GetFftLines()
        {
            var cmd = string.Format("FLIN? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785FftLines.Lines100 :
                          response[0] == '1' ? sr785FftLines.Lines200 :
                          response[0] == '2' ? sr785FftLines.Lines400 : sr785FftLines.Lines800);
            return retVal;
        }



        public double GetStartFrequency()
        {
            var cmd = string.Format("FSTR? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = double.Parse(response);
            return retVal;
        }



        public double GetFrequencySpan()
        {
            var cmd = string.Format("FSPN? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = double.Parse(response);
            return retVal;
        }



        public double GetEndFrequency()
        {
            var cmd = string.Format("FEND? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = double.Parse(response);
            return retVal;
        }



        public double GetAcquisitionTime()
        {
            var fftLines = 100 * Math.Pow(2, (int)GetFftLines());
            var fSpan = GetFrequencySpan(); // store in [kHz]

            return fftLines / fSpan;
        }



        public void SetYAxisView(sr785YAxisView axisView, sr785YAxisdBUnit dBUnit, sr785YAxisPeakUnit peakUnit, double yMin, double yMax)
        {
            var cmd = string.Format("VIEW {0}, {1}", ((int)_display).ToString(), ((int)axisView).ToString());
            _device.Write(cmd);

            cmd = string.Format("UNDB {0}, {1}", ((int)_display).ToString(), ((int)dBUnit).ToString());
            _device.Write(cmd);

            cmd = string.Format("UNPK {0}, {1}", ((int)_display).ToString(), ((int)peakUnit).ToString());
            _device.Write(cmd);

            cmd = string.Format("YMIN {0}, {1}", ((int)_display).ToString(), yMin.ToString());
            _device.Write(cmd);

            cmd = string.Format("YMAX {0}, {1}", ((int)_display).ToString(), yMax.ToString());
            _device.Write(cmd);
        }



        public sr785YAxisView GetYAxisView()
        {
            var cmd = string.Format("VIEW? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785YAxisView.LogMag :
                          response[0] == '0' ? sr785YAxisView.LinMag :
                          response[0] == '0' ? sr785YAxisView.MagSquared :
                          response[0] == '0' ? sr785YAxisView.Real : sr785YAxisView.Imaginary);
            return retVal;
        }



        public sr785YAxisdBUnit GetYAxisdBUnit()
        {
            var cmd = string.Format("UNIT? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString().ToUpper();
            var retVal = (response.Contains("DBM") ? sr785YAxisdBUnit.dBm :
                          response.Contains("DB") ? sr785YAxisdBUnit.dB : sr785YAxisdBUnit.Off);
            return retVal;
        }


        public sr785YAxisPeakUnit GetYAxisPeakUnit()
        {
            var cmd = string.Format("UNIT? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString().ToUpper();
            var retVal = (response.Contains("VPK") ? sr785YAxisPeakUnit.Peak :
                          response.Contains("VRMS") ? sr785YAxisPeakUnit.PeakRMS : sr785YAxisPeakUnit.PeakPeak);
            return retVal;
        }


        public double GetYAxisMax()
        {
            var cmd = string.Format("YMAX? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = double.Parse(response);
            return retVal;
        }



        public double GetYAxisMin()
        {
            var cmd = string.Format("YMIN? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = double.Parse(response);
            return retVal;
        }
        #endregion



        #region Measurement setup
        private void SetMeasureGroup(sr785MeasureGroup measureGroup)
        {
            // MGRP needs always "both" displays!
            var cmd = string.Format("MGRP {0}, {1}", ((int)sr785Display.Both).ToString(), ((int)measureGroup).ToString());
            _device.Write(cmd);
        }



        private sr785MeasureGroup GetMeasureGroup()
        {
            var cmd = string.Format("MGRP? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785MeasureGroup.FFT :
                          response[0] == '1' ? sr785MeasureGroup.Correlation :
                          response[0] == '2' ? sr785MeasureGroup.Octave :
                          response[0] == '3' ? sr785MeasureGroup.SweptSine :
                          response[0] == '4' ? sr785MeasureGroup.Order : sr785MeasureGroup.FFT);
            return retVal;
        }



        public void SetMeasurement(sr785Measurement measurement)
        {
            SetMeasureGroup(sr785MeasureGroup.FFT);
            var cmd = string.Format("MEAS {0}, {1}", ((int)_display).ToString(), ((int)measurement).ToString());
            _device.Write(cmd);
        }



        public sr785Measurement GetMeasurement()
        {
            var cmd = string.Format("MEAS? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            response = response.Remove(response.Length - 1); // remove \n
            var retVal = (response == "0" ? sr785Measurement.FFT1 :
                          response == "1" ? sr785Measurement.FFT2 :
                          response == "2" ? sr785Measurement.PowerSpectrum1 :
                          response == "3" ? sr785Measurement.PowerSpectrum2 :
                          response == "4" ? sr785Measurement.Time1 :
                          response == "5" ? sr785Measurement.Time2 :
                          response == "6" ? sr785Measurement.WindowTime1 :
                          response == "7" ? sr785Measurement.WindowTime2 :
                          response == "8" ? sr785Measurement.Orbit :
                          response == "9" ? sr785Measurement.Coherence :
                          response == "10" ? sr785Measurement.CrossSpectrum :
                          response == "11" ? sr785Measurement.FrequencySpectrum :
                          response == "12" ? sr785Measurement.Capture1 : sr785Measurement.Capture2);
            return retVal;
        }
        #endregion



        #region Average
        public void SetAverage(bool active)
        {
            var state = (active ? (byte)1 : (byte)0);
            var cmd = string.Format("FAVG {0}, {1}", ((int)sr785Display.Both).ToString(), state.ToString());
            _device.Write(cmd);
        }


        public bool GetAverage()
        {
            var cmd = string.Format("FAVG? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? true : false);
            return retVal;
        }



        public void SetupAverage(sr785AveragingType averagingType, sr785AveragingDisplayed averagingDisplayed, int averagesCount, double timeRecordIncrement = 100)
        {
            var cmd = string.Format("FAVT {0}, {1}", ((int)sr785Display.Both).ToString(), ((int)averagingType).ToString());
            _device.Write(cmd);

            cmd = string.Format("FAVM {0}, {1}", ((int)_display).ToString(), ((int)averagingDisplayed).ToString());
            _device.Write(cmd);

            cmd = string.Format("FAVN {0}, {1}", ((int)sr785Display.Both).ToString(), averagesCount.ToString());
            _device.Write(cmd);

            cmd = string.Format("FOVL {0}, {1}", ((int)sr785Display.Both).ToString(), timeRecordIncrement.ToString());
            _device.Write(cmd);
        }



        public sr785AveragingType GetAveragingType()
        {
            var cmd = string.Format("FAVT? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785AveragingType.Linear_FixLength : sr785AveragingType.Exponential_Continuous);
            return retVal;
        }



        public sr785AveragingDisplayed GetAveragingDisplayed()
        {
            var cmd = string.Format("FAVM? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785AveragingDisplayed.None :
                          response[0] == '1' ? sr785AveragingDisplayed.Vector :
                          response[0] == '2' ? sr785AveragingDisplayed.RMS : sr785AveragingDisplayed.PeakHold);
            return retVal;
        }



        public int GetAveragesCount()
        {
            var cmd = string.Format("FAVN? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = int.Parse(response);
            return retVal;
        }



        public double GetTimeRecordIncrement()
        {
            var cmd = string.Format("FOVL? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = double.Parse(response);
            return retVal;
        }
        #endregion



        #region Window
        public void SetWindowType(sr785WindowType window, double exponentialTimeConstant = 100)
        {
            var cmd = string.Format("FWIN {0}, {1}", ((int)sr785Display.Both).ToString(), ((int)window).ToString());
            _device.Write(cmd);
            if (window == sr785WindowType.Exponential)
            {
                cmd = string.Format("FWTC {0}, {1}", ((int)sr785Display.Both).ToString(), exponentialTimeConstant.ToString());
                _device.Write(cmd);
            }
        }



        public sr785WindowType GetWindowType()
        {
            var cmd = string.Format("FWIN? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = (response[0] == '0' ? sr785WindowType.Uniform_Rect :
                          response[0] == '1' ? sr785WindowType.Flattop :
                          response[0] == '2' ? sr785WindowType.Hanning :
                          response[0] == '3' ? sr785WindowType.BlackmanHarris :
                          response[0] == '4' ? sr785WindowType.Kaiser : sr785WindowType.Exponential);
            return retVal;
        }



        /// <summary>
        /// Returns only a valid number if exponentialwindow is active. Otherwise you get -1
        /// </summary>
        /// <returns></returns>
        public double GetExponentialTimeConstant()
        {
            if(GetWindowType() != sr785WindowType.Exponential)
                return -1;

            var cmd = string.Format("FWTC? {0}", ((int)_display).ToString());
            _device.Write(cmd);
            var response = _device.ReadString();
            var retVal = double.Parse(response);
            return retVal;
        }
        #endregion



        #region Measure
        public SR785Result Measure()
        {
            var cmd = string.Format("DSPY? {0}", ((int)sr785Display.A).ToString());
            _device.Write(cmd);
            var response = string.Empty;
            while (!sr785Ready())      // Store the measurement values
                response += _device.ReadString();

            var splitResponse = response.Split(',');
            var valueList = new List<double>();
            foreach (var @string in splitResponse)
                valueList.Add(double.Parse(@string));

            var result = new SR785Result();
            result.ErrorState = 0;
            result.Values = valueList.ToArray();
            result.StartFrequency = GetStartFrequency();
            result.StopFrequency = GetEndFrequency();
            result.FrequencySpan = GetFrequencySpan();
            result.FftLines = (int)(100 * Math.Pow(2, (double)GetFftLines()));
            result.ResolutionBandwidth = result.FrequencySpan / result.FftLines;
            result.AcquisitionTime = GetAcquisitionTime();
            return result;
        }



        private bool sr785Ready()
        {
            var flags = (int)_device.SerialPoll();

            var retVal = (flags & (int)sr785StatusFlag.IFC) != 0 ? true : false;
            return retVal;
        }
        #endregion
    }
}

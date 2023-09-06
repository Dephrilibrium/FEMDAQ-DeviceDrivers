using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// own usings
using RohdeSchwarz.RsScope;
using Ivi.Driver;


namespace RohdeUndSchwarz
{
    public enum RTO2034Channel { CH1 = 1, CH2, CH3, CH4 }
    public enum RTO2034Waveform { W1 = 1, W2, W3 }
    public enum RTO2034MathWindow { M1 = 1, M2, M3, M4 }

    public struct RTO2034WaveformResult
    {
        public int ErrorState;
        public double[] Values;
        public PrecisionTimeSpan TotalTime;
        public double SampleInterval;
        public int SampleRate;
    }

    public struct RTO2034FFTResult
    {
        public int ErrorState;
        public double[] Values;
        public double FrequencySpan;
        public double ResolutionBandwidth;
        public double StartFrequency;
        public double StopFrequency;
        public FFTWindowType Window;
    }




    public class RTO2034
    {
        // Static variables
        static private int _activeChannelCount = 0;
        static private int _activeMathematicCount = 0;

        // Global variables
        private RsScope _device;
        private RTO2034Channel _channel;
        private RTO2034Waveform _waveform;
        private RTO2034MathWindow _mathChannel;

        /// <summary>
        /// Construction of the communication-device.
        /// 
        /// Returns an object.
        /// </summary>
        /// <param name="TCP_IP"></param>
        /// <param name="UpdateDisplay"></param>
        /// <param name="Reset"></param>
        /// <param name="IDQuery"></param>
        public RTO2034(string TCP_IP, RTO2034Channel Channel, bool UpdateDisplay = true, bool Reset = false, bool IDQuery = false)
        {
            _activeChannelCount++;
            _device = new RsScope("TCPIP::" + TCP_IP + "::HISLIP", IDQuery, Reset);
            _channel = Channel;
            _waveform = RTO2034Waveform.W1;
            Init();
            DisplayUpdate(UpdateDisplay);
        }



        /// <summary>
        /// Cleans up the reserved system resources.
        /// </summary>
        public void Dispose()
        {
            if(ActiveMathematicCount > 0)
            {
                _device.Mathematics[_mathChannel.ToString()].General.MathWaveformEnabled = false;
                _activeMathematicCount--;
            }

            if (_device != null)
            {
                _activeChannelCount--;
                _device.Channel[_channel.ToString()].Enabled = false;
                _device.Dispose();
            }
        }



        /// <summary>
        /// Returns the amount of active channels.
        /// </summary>
        static public int ActiveChannelCount
        {
            get { return _activeChannelCount; }
        }



        /// <summary>
        /// Returns the amount of active measurements
        /// </summary>
        public static int ActiveMathematicCount
        {
            get { return _activeMathematicCount; }
        }



        /// <summary>
        /// Requests device-information.
        /// 
        /// Returns a string which contains device-information
        /// </summary>
        public string Info
        {
            get
            {
                return _device.UtilityFunctions.IDQueryResponse;
            }
        }



        /// <summary>
        /// Resets the device-params and could be used for initialization-stuff.
        /// </summary>
        private void Init()
        {
            _device.UtilityFunctions.VisaTimeout = 100000; // 100 s timeout
            _device.UtilityFunctions.OPCTimeout = 100000; // 100 s timeout
            /* Do your other init-stuff here */
            _device.Channel[_channel.ToString()].Enabled = true;
        }



        /// <summary>
        /// Turns the display on (true) or off (false).
        /// 
        /// Returns the set display update-state.
        /// </summary>
        /// <param name="UpdateState"></param>
        /// <returns></returns>
        public bool DisplayUpdate(bool UpdateDisplay = true)
        {
            _device.Display.DisplayUpdate = UpdateDisplay;
            return UpdateDisplay;
        }



        /// <summary>
        /// Adjusts the samplingrate.
        /// 
        /// Returns the set samplingrate. Is the returned value different from the given one the parameter was adjusted to a valid one and returned.
        /// </summary>
        /// <param name="SamplingRate"></param>
        /// <returns></returns>
        public double SetSamplingRate(double SamplingRate)
        {
            _device.Acquisition.HorizontalSampleRate = SamplingRate;
            //int recordLength = (int)(SamplingRate * _device.Acquisition.AcquisitionTime);
            //_device.Acquisition.HorizontalRecordLength = recordLength;

            return _device.Acquisition.HorizontalSampleRate; // No complications
        }



        /// <summary>
        /// Adjusts the X-Scale for the given channel.
        /// 
        /// Returns the set X-Scale per div. Is the returned value different from the given one the parameter was adjusted to a valid one and returned.
        /// </summary>
        /// <param name="QuantPerDiv"></param>
        /// <param name="Channel"></param>
        /// <returns></returns>
        public double SetXDivScale(double QuantPerDiv)
        {
            if (QuantPerDiv < 25e-15 || QuantPerDiv > 10e3)
                return -1; // Error: Not in range

            if (QuantPerDiv >= 25e-12 && QuantPerDiv < 100e-12)
                QuantPerDiv = Math.Round(QuantPerDiv, 11); // 10ps steps
            else if (QuantPerDiv >= 100e-12 && QuantPerDiv < 1e-9)
                QuantPerDiv = Math.Round(QuantPerDiv, 10); // 100ps steps
            else if (QuantPerDiv >= 1e-9 && QuantPerDiv < 10e-9)
                QuantPerDiv = Math.Round(QuantPerDiv, 9); // 1ns steps
            else if (QuantPerDiv >= 10e-9 && QuantPerDiv < 100e-9)
                QuantPerDiv = Math.Round(QuantPerDiv, 8); // 10ns steps
            else if (QuantPerDiv >= 100e-9 && QuantPerDiv < 1e-6)
                QuantPerDiv = Math.Round(QuantPerDiv, 7); // 100ns steps
            else if (QuantPerDiv >= 1e-6 && QuantPerDiv < 10e-6)
                QuantPerDiv = Math.Round(QuantPerDiv, 6); // 1µs steps
            else if (QuantPerDiv >= 10e-6 && QuantPerDiv < 100e-6)
                QuantPerDiv = Math.Round(QuantPerDiv, 5); // 10µs steps
            else if (QuantPerDiv >= 100e-6 && QuantPerDiv < 1e-3)
                QuantPerDiv = Math.Round(QuantPerDiv, 4); // 100µs steps
            else if (QuantPerDiv >= 1e-3 && QuantPerDiv < 10e-3)
                QuantPerDiv = Math.Round(QuantPerDiv, 3); // 1ms steps
            else if (QuantPerDiv >= 10e-3 && QuantPerDiv < 100e-3)
                QuantPerDiv = Math.Round(QuantPerDiv, 2); // 10ms steps
            else if (QuantPerDiv >= 100e-3 && QuantPerDiv < 1)
                QuantPerDiv = Math.Round(QuantPerDiv, 1); // 100ms steps
            else if (QuantPerDiv >= 1 && QuantPerDiv < 10)
                QuantPerDiv = Math.Round(QuantPerDiv, 0); // 1s steps
            else if (QuantPerDiv >= 10 && QuantPerDiv < 100)
            {
                int stepInterval = 10; // 10s
                QuantPerDiv = Math.Round(QuantPerDiv / stepInterval) * stepInterval;
            }
            else if (QuantPerDiv >= 10 && QuantPerDiv < 1e3)
            {
                int stepInterval = 100; // 100s
                QuantPerDiv = Math.Round(QuantPerDiv / stepInterval) * stepInterval;
            }
            else if (QuantPerDiv >= 100 && QuantPerDiv < 10e3)
            {
                int stepInterval = 1000; // 100s
                QuantPerDiv = Math.Round(QuantPerDiv / stepInterval) * stepInterval;
            }

            _device.Acquisition.HorizontalScale = QuantPerDiv; // Setting perDiv value
            
            return QuantPerDiv; // No complications
        }



        /// <summary>
        /// Adjusts the Y-Scale for the given channel.
        /// 
        /// Returns the set Y-Scale per div. Is the returned value different from the given one the parameter was adjusted to a valid one and returned.
        /// </summary>
        /// <param name="QuantPerDiv"></param>
        /// <param name="Channel"></param>
        /// <returns></returns>
        public double SetYDivScale(double QuantPerDiv)
        {
            if (QuantPerDiv < 1e-3 || QuantPerDiv > 100)
                return -1; // Error: Not in range

            if (QuantPerDiv >= 1e-3 && QuantPerDiv < 10e-3)
                QuantPerDiv = Math.Round(QuantPerDiv, 4); // 0.1mV steps
            else if (QuantPerDiv >= 10e-3 && QuantPerDiv < 100e-3)
                QuantPerDiv = Math.Round(QuantPerDiv, 3); // 1mV steps
            else if (QuantPerDiv >= 100e-3 && QuantPerDiv < 1)
                QuantPerDiv = Math.Round(QuantPerDiv, 2); // 10mV steps
            else if (QuantPerDiv >= 1 && QuantPerDiv < 10)
                QuantPerDiv = Math.Round(QuantPerDiv, 1); // 100mV steps
            else if (QuantPerDiv >= 10 && QuantPerDiv < 100)
                QuantPerDiv = Math.Round(QuantPerDiv / 10, 0) * 10; // 10V steps

            _device.Channel[_channel.ToString()].Scale = QuantPerDiv;

            return QuantPerDiv; // No complications
        }



        /// <summary>
        /// The triggersetup sets up the trigger.
        /// 
        /// It return the set value back. Is the returned value different from the given one the parameter was adjusted to a valid one and returned.
        /// </summary>
        /// <param name="Level"></param>
        /// <param name="SourceChannel"></param>
        /// <param name="Mode"></param>
        /// <param name="Type"></param>
        /// <param name="EdgeSlope"></param>
        /// <returns></returns>
        public double TriggerSetup(double Level = 0f, RohdeSchwarz.RsScope.TriggerSource SourceChannel = RohdeSchwarz.RsScope.TriggerSource.Channel1, TriggerModifier Mode = TriggerModifier.Normal,
                                 TriggerType Type = TriggerType.Edge, Slope EdgeSlope = Slope.Positive)
        {
            //string channelName = "CH" + ((int)SourceChannel).ToString();
            var TrigA = _device.Trigger["TrigA"];
            TrigA.Modifier = Mode;
            TrigA.Timeout.TimeoutValue = 10; // 10 s timeout
            TrigA.Source = SourceChannel;
            if (SourceChannel.ToString().StartsWith("Channel"))
            {
                TrigA.Type = Type;
                TrigA.Edge.Slope = EdgeSlope;
            }

            TrigA.Channel[_channel.ToString()].Level = Level; // Set triggerlevel
            
            return Level; // No complications!
        }



        /// <summary>
        /// Sets up a waveformwindow for the given channel.
        /// </summary>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        /// <param name="samplingRate"></param>
        public void WaveformSetup(double scaleX, double scaleY, double samplingRate)
        {
            SetXDivScale(scaleX);
            SetYDivScale(scaleY);
            SetSamplingRate(samplingRate);
        }



        /// <summary>
        /// Fetching a transient waveform. TriggerBeforeRead is used for generating a new trigger. So you can read back the waveform of the last FFT or generate a new one!
        /// 
        /// Is the errorstate of the structure negative it indicates an error. Otherwise you get 0.
        /// /// </summary>
        /// <param name="TriggerBeforeRead"></param>
        /// <returns></returns>
        public RTO2034WaveformResult GetWaveform(bool TriggerBeforeRead)
        {
            IWaveform<double> responsedWaveform = null;
            RTO2034WaveformResult waveformStructure = new RTO2034WaveformResult();

            if (TriggerBeforeRead)
            {
                _device.WaveformAcquisition.RunSingleWithoutWait();
                var timeout = _device.Acquisition.AcquisitionTime * 1.1 * 1000; // integer as [ms]
                if (timeout < 15e3)
                    timeout = 15e3;
                try
                {
                    _device.WaveformAcquisition.WaitForMeasurementComplete((int)timeout);
                } catch
                {
                    _device.WaveformAcquisition.Stop();
                    waveformStructure.ErrorState = -1;
                    return waveformStructure;
                }
            }
            responsedWaveform = _device.Channel[_channel.ToString()].Waveform[_waveform.ToString()].FetchWaveform(responsedWaveform);
            if (responsedWaveform == null)
            {
                waveformStructure.ErrorState = -1; // Error: Can't get waveform
                return waveformStructure;
            }

            waveformStructure.Values = responsedWaveform.GetAllElements();
            waveformStructure.TotalTime = responsedWaveform.TotalTime;
            waveformStructure.SampleInterval = responsedWaveform.IntervalPerPoint.TotalSeconds;
            waveformStructure.SampleRate = (int)Math.Round(1 / waveformStructure.SampleInterval, MidpointRounding.ToEven);

            return waveformStructure; // No complications!
        }



        /// <summary>
        /// Fetching a FFT. TriggerBeforeRead is used for generating a new trigger. So you can read back the FFT of the last waveform or generate a new one!
        /// 
        /// Is the errorstate of the structure negative it indicates an error. Otherwise you get 0.
        /// </summary>
        /// <param name="TriggerBeforeRead"></param>
        /// <returns></returns>
        public RTO2034FFTResult GetFFT(bool TriggerBeforeRead)
        {
            RTO2034FFTResult fftStructure = new RTO2034FFTResult();

            IWaveform<double> fftResult = null;
            if (TriggerBeforeRead)
            {
                _device.WaveformAcquisition.RunSingleWithoutWait();
                var timeout = _device.Acquisition.AcquisitionTime * 1.1 * 1000; // integer as [ms]
                if (timeout < 15e3)
                    timeout = 15e3;
                try
                {
                    _device.WaveformAcquisition.WaitForMeasurementComplete((int)timeout);
                }
                catch
                {
                    _device.WaveformAcquisition.Stop();
                    fftStructure.ErrorState = -1;
                    return fftStructure;
                }
            }

            fftResult = _device.Mathematics[_mathChannel.ToString()].WaveformData.FetchMathWaveform(fftResult);
            if(fftResult==null)
            {
                fftStructure.ErrorState = -1; // Error: Can't get fft-form
                return fftStructure;
            }

            fftStructure.Values = fftResult.GetAllElements();
            if (fftResult.ContainsInvalidElement)
                fftStructure.ErrorState = -2; // Error: Invalid points
            if (fftResult.ContainsOutOfRangeElement)
                fftStructure.ErrorState = -3; // Error: Points out of range
            fftStructure.FrequencySpan = _device.Mathematics[_mathChannel.ToString()].FFT.FrequencySpan;
            fftStructure.StartFrequency = _device.Mathematics[_mathChannel.ToString()].FFT.StartFrequency;
            fftStructure.StopFrequency = _device.Mathematics[_mathChannel.ToString()].FFT.StopFrequency;
            fftStructure.ResolutionBandwidth = fftStructure.FrequencySpan / (fftStructure.Values.LongLength - 1);
            fftStructure.Window = _device.Mathematics[_mathChannel.ToString()].FFT.WindowType;


            return fftStructure;
        }



        /// <summary>
        /// Configurates a math-window to measure a FFT of the instance-channel.
        /// 
        /// Returns 0 if everything gone well. A negative number indicates an error.
        /// </summary>
        /// <param name="MathChannel"></param>
        /// <param name="StartFrequency"></param>
        /// <param name="StopFrequency"></param>
        /// <param name="MagnitudeOffset"></param>
        /// <param name="MagnitudeRange"></param>
        /// <returns></returns>
        public int FFTSetup(RTO2034MathWindow MathChannel, double StartFrequency, double FrequencyResolution, double StopFrequency, double MagnitudeOffset, double MagnitudeRange, FFTWindowType Window = FFTWindowType.KaiserBessel, double ResolutionBandwithRatio = 0)
        {
            _activeMathematicCount++;
            _mathChannel = MathChannel;
            IRsScopeMathematics mathematic = _device.Mathematics[_mathChannel.ToString()];
            mathematic.General.MathWaveformEnabled = true;
            mathematic.General.MathExpression = "FFTmag(" + _channel.ToString() + ")";

            if (ResolutionBandwithRatio > 0)
            {
                mathematic.FFT.ResolutionBandwidthCouplingEnabled = true;
                mathematic.FFT.ResolutionBandwidthRatio = (int)ResolutionBandwithRatio;
            }
            else
                mathematic.FFT.ResolutionBandwidthCouplingEnabled = false;

            mathematic.FFT.StartFrequency = StartFrequency;
            mathematic.FFT.StopFrequency = StopFrequency;
            mathematic.FFT.ResolutionBandwidth = FrequencyResolution;
            mathematic.FFT.UseColorTable = true;
            mathematic.FFT.CenterFrequency = StartFrequency + (StopFrequency - StartFrequency) / 2;
            mathematic.FFT.WindowType = Window;

            mathematic.FFT.MagnitudeUnit = FFTMagnitudeUnit.dBV;
            mathematic.General.MathVerticalRange = MagnitudeRange;
            var offset = (MagnitudeOffset + MagnitudeRange) / -2;
            mathematic.General.MathVerticalOffset = offset;

            return 0;
        }
    }

}

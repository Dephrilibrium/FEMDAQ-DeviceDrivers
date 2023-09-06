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
    public enum DSOX3034TTriggerSource { Channel1 = 1, Channel2, Channel3, Channel4, External}
    public enum DSOX3034TChannel { NoChannel = 0, Channel1, Channel2, Channel3, Channel4 }

    public struct DSOX2034TWaveformResult
    {
        public int ErrorState;
        public double[] Values;
        public double TotalTime;
        public double StartTime;
        public double SampleInterval;
        public double SampleRate;
    }


    class DSOX3034T
    {
        // Static variables
        static private int _activeChannels = 0;
        static private bool _useClearResults = true;


        // Device
        private AgInfiniiVision _device = null;


        // ChannelInfo
        public DSOX3034TChannel Channel { get; private set; }
        private string ChannelName { get; set; }

        // Additional Triggervariables
        private bool _forceTrigger { get; set; }

        public DSOX3034T(string VisaAddress, DSOX3034TChannel Channel, bool ResetSettings = true, bool IdQuery = false)
        {
            if (VisaAddress == null) throw new ArgumentNullException("visaAddress");

            //_device = new AgInfiniiVision();
            _device = new AgInfiniiVision();
            try { _device.Initialize(VisaAddress, IdQuery, ResetSettings); }
            catch (Exception) { throw new FormatException("Can't initialize connection to DSOX3034T. (Maybe the device is turned off or the VISA-address has the wrong format)"); }

            if ((int)Channel < 1 && (int)Channel > 4) throw new ArgumentOutOfRangeException("Channelnumber not in Range 1-4!");
            this.Channel = Channel;
            ChannelName = Channel.ToString();
            _activeChannels++;
            Init();
        }



        public void Dispose()
        {
            if (_device != null)
            {
                _device.Channels.Item[ChannelName].Enabled = false;
                _device.Close();
                _device = null;
                _activeChannels--;
            }
        }



        public int ActiveChannelCount { get { return _activeChannels; } }



        public string Info()
        {
            return "Manufacturer: " + _device.Identity.InstrumentManufacturer +
                   ", Model: " + _device.Identity.InstrumentModel +
                   ", Firmware: " + _device.Identity.InstrumentFirmwareRevision;
        }


        private void Init(bool UseClearResults = true)
        {
            if (UseClearResults)
                ClearResults();
            _device.Channels.Item[ChannelName].Enabled = true;
            _forceTrigger = false;
        }



        public void ClearResults()
        {
            _useClearResults = true;
            _device.Measurements.Clear();
        }



        public int TimeoutIVICom
        {
            get { return _device.System.TimeoutMilliseconds; }
            set { _device.System.TimeoutMilliseconds = value; }
        }


        public double XDivScale
        {
            get { return _device.Timebase.HorizontalScale; }
            set { if (value > 0) _device.Timebase.HorizontalScale = value; }
        }

        public double XOffset
        {
            get { return _device.Acquisition.StartTime; }
            set
            {
                // Calculating the time on the left screen!
                var offset = _device.Timebase.HorizontalScale * -5; // 10divs / 2 as negative number!
                offset += value;
                _device.Acquisition.StartTime = offset;
            }
        }



        public double TimerPerRecord
        {
            get { return _device.Acquisition.TimePerRecord; }
            set { _device.Acquisition.TimePerRecord = value; }
        }



        public double SampleRate { get { return _device.Acquisition.SampleRate; } }



        public double YDivScale
        {
            get { return _device.Channels.Item[ChannelName].Scale; }
            set { if (value > 0) _device.Channels.Item[ChannelName].Scale = value; }
        }



        public double YOffset
        {
            get { return _device.Channels.Item[ChannelName].Offset; }
            set { _device.Channels.Item[ChannelName].Offset = value; }
        }



        public void TriggerSetup(double Level = 0, DSOX3034TTriggerSource TriggerSource = DSOX3034TTriggerSource.Channel1, AgInfiniiVisionTriggerModifierEnum Mode = AgInfiniiVisionTriggerModifierEnum.AgInfiniiVisionTriggerModifierNone, AgInfiniiVisionTriggerTypeEnum TriggerType = AgInfiniiVisionTriggerTypeEnum.AgInfiniiVisionTriggerTypeEdge, AgInfiniiVisionTriggerSlopeEnum EdgeSlope = AgInfiniiVisionTriggerSlopeEnum.AgInfiniiVisionTriggerSlopePositive, bool ForceTrigger = false)
        {
            _device.Trigger.Level = Level;
            _device.Trigger.Source = TriggerSource.ToString();
            _device.Trigger.Type = TriggerType;
            _device.Trigger.Edge.Slope = EdgeSlope;
            _device.Trigger.Modifier = Mode;
            _forceTrigger = ForceTrigger;
            _device.Acquisition.Stop();
            ClearResults();
        }



        public void WaveformSetup(double ScaleX, double ScaleY)
        {
            XDivScale = ScaleX;
            YDivScale = ScaleY;
        }



        public DSOX2034TWaveformResult GetWaveform(bool TriggerBeforeFetch)
        {
            DSOX2034TWaveformResult waveform = new DSOX2034TWaveformResult();

            if(TriggerBeforeFetch || _useClearResults)
            {
                _useClearResults = false;
                TimeoutIVICom = (int)(TimerPerRecord * 1000 + 500); // RecordTime [s] * 1000 [ms/s] + 500 [ms]
                _device.Acquisition.SingleAcquisition();
                if(_forceTrigger)
                    _device.Trigger2.ForceTrigger();
            }

            // Fetching waveform
            try { _device.Measurements.Item[ChannelName].FetchWaveform(ref waveform.Values, ref waveform.StartTime, ref waveform.SampleInterval); }
            catch (Exception) { throw new Exception("DSOX3034T hasn't triggered yet and can't read."); }

            if (waveform.Values != null)
            {
                waveform.ErrorState = 0;
                waveform.TotalTime = waveform.StartTime + (waveform.Values.LongLength - 1) * waveform.SampleInterval;
            }
            else
            {
                waveform.ErrorState = -1;
                waveform.Values = null;
            }

            return waveform;
        }
    }

}
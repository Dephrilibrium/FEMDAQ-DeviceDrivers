using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// own usings
using NationalInstruments.NI4882;

namespace Keysight
{
    public class WavGen33511B
    {
        private Device _device;

        // Burstparameters
        private bool _burstActive;
        //private int _cycles;
        private double _burstPeriod;



        /// <summary>
        /// Constructs the communication device
        /// </summary>
        /// <param name="boardNum"></param>
        /// <param name="primAddr"></param>
        /// <param name="secAddr"></param>
        public WavGen33511B(int boardNum, int primAddr, int secAddr)
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
            {
                try { PowerDownSource(); }
                catch (Exception) { }
                _device.Dispose(); // Cleanup ressources
            }
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
            //_device.Write("*RST"); // Reset the device-params
            _device.IOTimeout = TimeoutValue.T10s;  // Prepare timeout
            /* Do your other init-stuff here */
            _device.Write("VOLT:UNIT VPP");            // Set VPP as voltage-unit
            _device.Write("UNIT:ANGL DEG");            // Set degree as phase-unit
        }



        /// <summary>
        /// Adjusts the expected channel output load.
        /// 
        /// Returns an int which holds the ohm-value on success. Otherwise you find an negative error-number.
        /// </summary>
        /// <param name="Ohms"></param>
        /// <returns></returns>
        public int OutputLoad(int Ohms)
        {
            // HighZ
            if (Ohms == -1)
            {
                _device.Write("OUTP:LOAD INF");
                return -1;
            }

            if (Ohms < 1 || Ohms > 10e6) // Between 1 Ohm and 10 MegOhm
                return -2;                          // Error: Input out of range

            _device.Write("OUTP:LOAD " + Ohms.ToString()); // Set load if it's within the boundaries
            _device.Write("OUTP:LOAD?");
            string response = _device.ReadString();
            int ohms;
            if (!int.TryParse(response, out ohms)) // Compare input with set
                return -3;                      // Error: Can't convert into int

            if(ohms != Ohms)
                return -4;                      // Error: Values different

            return ohms;                        // No complications
        }



        /// <summary>
        /// Sets the output active or deactive.
        /// 
        /// The return holds the actual output-state (0 off; 1 on). If an error appears you find a negative error-code.
        /// </summary>
        /// <param name="Active"></param>
        /// <returns></returns>
        public int Output(bool Active)
        {
            return Output(1, Active);               // Pass through return
        }



        private int Output(int Channel, bool Active)
        {
            if (Channel < 1 || Channel > 2)
                return -1;                          // Error: Channelnumber doesn't exist
            
            _device.Write("OUTP" + Channel.ToString() + " " + (Active ? "ON" : "OFF")); // Set channel active or deactive
            _device.Write("OUTP?");
            string response = _device.ReadString();
            return (response[0] - '0');             // Convert state to number and return
        }



        public int OutputBurst(int Cycles, double BurstPeriod = 0, bool TriggerInstantly = false)
        {
            int cycles = OutputBurstSetup(true, Cycles, BurstPeriod);
            if (cycles < 0)
                return cycles;                      // Error: Pass through returned error-code.

            if (TriggerInstantly) // Check instant trigger flag
                OutputBurstTriggerOnly();

            return 0;                               // No complications
        }



        public bool OutputBurstTriggerOnly()
        {
            if (!_burstActive)
                return false;                       // Error: Can't trigger burstmode when it's deactive

            Output(true);           // Otherwise enable output
            _device.Write("*TRG");  //  and pull the trigger
            return true;                            // No complications
        }



        public int OutputBurstOff()
        {
            int outputState = Output(false);
            int burstState = OutputBurstSetup(false);

            if (outputState != 0)
                return -1;                          // Error: Output isn't disabled   

            if (burstState != 0)
                return -2;                          // Error: Burstmode isn't disabled

            return 0;                               // No complications
        }



        private int OutputBurstSetup(bool UseBurst, int Cycles = 1, double BurstPeriod = 1)
        {
            int cycles = 0; // Amount of cycles or error-code
            string response;
            
            // Just set up if it should be used!
            if (UseBurst)
            {
                // Use bus-trigger (GPIB)
                _device.Write("TRIG:SOUR BUS"); // Triggersource is GPIB
                _device.Write("TRIG:SOUR?");
                response = _device.ReadString();
                if (response != "BUS\n")
                    return -1;                      // Error: Could not set GPIB as triggersource

                _device.Write("BURS:MODE TRIG"); // Burst starts on bus-trigger
                _device.Write("BURS:MODE?");
                response = _device.ReadString();
                if (response != "TRIG\n")
                    return -2;                      // Error: Could not set manual trigger
                
                // Check and set burstperiod
                bool burstPeriodFitted = false; // This indicates, that the given period was to short and has to be fitted. Look down -> cycles will be set to 0 to indicate this adjustment!

                var frequency = GetFrequency();

                double burstPeriod = Cycles * (1 / frequency) + 100e-6; // Min. period for the burst have to be: periodTime > cycles * periodPerCycle. 1µs is the minimal step!
                if (burstPeriod < BurstPeriod)
                    _device.Write("BURS:INT:PER " + BurstPeriod.ToString("F6")); // Given period is ok
                else
                {
                    _device.Write("BURS:INT:PER " + burstPeriod.ToString("F6")); // Setting fitted period
                    burstPeriodFitted = true;                                    //  and mark this
                }

                _device.Write("BURS:INT:PER?");
                response = _device.ReadString();
                if (!double.TryParse(response, out _burstPeriod))
                    return -4;                      // Error: Can't convert into double

                if (_burstPeriod != BurstPeriod && !burstPeriodFitted)
                    return -5;                      // Error: Non-matching periods without fitting.
                
                // Setting cycles
                if (Cycles >= 1 && Cycles <= 100e6) // Cycles can be between 1 and 100Meg-Cycles
                    _device.Write("BURS:NCYC " + Cycles.ToString());

                _device.Write("BURS:NCYC?");
                response = _device.ReadString();
                double cyclesConverter;
                if (!double.TryParse(response, out cyclesConverter))
                    return -6;                      // Error: Can't convert into double

                cycles = (int)cyclesConverter; // Make double to int
                if (cycles != Cycles)
                    return -7;                      // Error: Non-matching values

                if (burstPeriodFitted)
                    cycles = 0;                     // Indicating fitted period-time
            }

            // Activate the burstmode
            _device.Write("BURS:STAT " + (UseBurst ? "ON" : "OFF"));
            _device.Write("BURS:STAT?");
            response = _device.ReadString();
            if (response != (UseBurst ? "1\n" : "0\n"))
                return -8;                          // Error: Couldn't set burst-state correctly

            _burstActive = UseBurst;
            
            return cycles;                          // No complications
        }



        public void SetWaveform(string waveformName)
        {
            _device.Write("SOUR:FUNC " + waveformName);
        }



        public string GetWaveform()
        {
            _device.Write("SOUR:FUNC?");
            return _device.ReadString();
        }



        public void SetFrequency(double Frequency)
        {
            if (Frequency < 1e-6 || Frequency > 20e6) // Frequency can be between 1µH and 20MHz
                throw new ArgumentOutOfRangeException("Frequency");

            _device.Write("SOUR:FREQ " + Frequency.ToString("F6"));
        }



        public double GetFrequency()
        {
            _device.Write("SOUR:FREQ?");
            string response = _device.ReadString();
            double frequency;
            if (!double.TryParse(response, out frequency))
                return -1;                          // Error: Cant convert into double

            return frequency;
        }



        public void SetAmplitude(double Amplitude)
        {
            _device.Write("DISP:UNIT:VOLT AMPL");

            // Setup voltage
            if (Amplitude < 1e-3 || Amplitude > 20.0) // Voltage can be between 1mV and 10V (outputload != HighZ) or 20V (Output = High-Z)!
                throw new ArgumentOutOfRangeException("Amplitude");

            _device.Write("VOLT " + Amplitude.ToString("F3") + " V");
        }



        public void SetAmplitude(double high, double low)
        {
            if (high < low + 2e-3)
                throw new ArgumentOutOfRangeException("high >= low + 2mV");

            _device.Write("DISP:UNIT:VOLT HIGH");
            _device.Write("VOLT:HIGH " + high.ToString("F3") + " V");
            _device.Write("VOLT:LOW " + low.ToString("F3") + " V");
        }



        public void GetAmplitude(out double amplitude)
        {
            _device.Write("VOLT?");
            string response = _device.ReadString();
            if (!double.TryParse(response, out amplitude))
                amplitude = 100e3;                      // Error: Can't convert into double
        }


        public void GetAmplitude(out double low, out double high)
        {
            _device.Write("VOLT:LOW?");
            string response = _device.ReadString();
            if (!double.TryParse(response, out low))
                low = 100e3;

            _device.Write("VOLT:HIGH?");
            response = _device.ReadString();
            if (!double.TryParse(response, out high))
                high = 100e3;
        }



        public void SetOffset(double Offset)
        {
            if (Offset < -5 || Offset > 5) // Offset can be between -5V and +5V
                throw new ArgumentOutOfRangeException("Offset");

            _device.Write("VOLT:OFFS " + Offset.ToString("F4") + " V");
        }



        public double GetOffset()
        {
            _device.Write("VOLT:OFFS?");
            string response = _device.ReadString();
            double offset;
            if (!double.TryParse(response, out offset))
                return 100e3;                      // Error: Can't convert into double

            return offset;
        }



        public void SetPhase(double Phase)
        {
            if (Phase < -360.0 || Phase > 360.0) // Phase can be between -360° and +360°
                throw new ArgumentOutOfRangeException("Phase");

            _device.Write("SOUR:PHAS " + Phase.ToString("F3"));
        }



        public double GetPhase()
        {
            _device.Write("SOUR:PHAS?");
            string response = _device.ReadString();
            double phase;
            if (!double.TryParse(response, out phase))
                return 100e3;                      // Error: Can't convert into double

            return phase;
        }



        public double SetSquareDutyCycle(double DutyCycle)
        {
            if (DutyCycle < 0.01 || DutyCycle > 99.99) // Dutycycle can be between 0.01% and 99.99%
                return -1;                          // Error: Dutycycle is out of bound

            _device.Write("FUNC:SQU:DCYC " + DutyCycle.ToString("F2"));
            double dutyCycle = GetSquareDutyCycle();

            if (dutyCycle != DutyCycle)
                return -3;                          // Error: Non-matching values

            return dutyCycle;                       // No complications
        }



        public double GetSquareDutyCycle()
        {
            _device.Write("FUNC:SQU:DCYC?");
            string response = _device.ReadString();
            double dutyCycle;
            try
            {
                dutyCycle = Convert.ToDouble(response);
            }catch
            {
                dutyCycle = -1.0;
            }
            return dutyCycle;
        }



        private double SetRampSymmetry(double Symmetry)
        {
            if (Symmetry < 0 || Symmetry > 100) // Rampsymmetry can be between 0% and 100%
                return -1;                          // Error: Symmetry out of bound

            _device.Write("FUNC:RAMP:SYMM " + Symmetry.ToString("F2"));
            _device.Write("FUNC:RAMP:SYMM?");
            string response = _device.ReadString();
            double symmetry;
            if (!double.TryParse(response, out symmetry))
                return -2;                          // Error: Can't convert into double

            if (symmetry != Symmetry)
                return -3;                          // Error: Non-matching values

            return symmetry;                        // No complications
        }



        private double SetPulseWidth(double PulseWidth)
        {
            var frequency = GetFrequency();
            double maxPulseWidth = 1 / frequency - 1e-6;          // Max. pulsewidth must be smaller than 1/Frequency. Smallest diff equals to 1µs
            if (PulseWidth < 16e-6 || PulseWidth > maxPulseWidth) // PulseWidth can be between 16ns and Frequency
                return -1;                          // Error: Pulsewidth out of bound

            _device.Write("FUNC:PULS:WIDT " + PulseWidth.ToString());
            _device.Write("FUNC:PULS:WIDT?");
            string response = _device.ReadString();
            double pulseWidth;
            if (!double.TryParse(response, out pulseWidth))
                return -2;                          // Error: Can't convert into double

            if (pulseWidth != PulseWidth)
                return -3;                          // Error: Non-matching values;

            return pulseWidth;                      // No complications
        }



        private double SetPulseLeadEdge(double LeadEdge)
        {
            if (LeadEdge < 8.4e-9 || LeadEdge > 1e-3) // Leadedge can be between 8.4 ns and 1 ms
                return -1;                          // Error: Leadedge out of bound

            _device.Write("FUNC:PULS:TRAN:LEAD " + LeadEdge.ToString());
            _device.Write("FUNC:PULS:TRAN:LEAD?");
            string response = _device.ReadString();
            double leadEdge;
            if (!double.TryParse(response, out leadEdge))
                return -2;                          // Error: Can't convert into double

            if (leadEdge != LeadEdge)
                return -3;                          // Error: Non-matching values

            return leadEdge;                        // No complications
        }



        private double SetPulseTailEdge(double TrailEdge)
        {
            if (TrailEdge < 8.4e-9 || TrailEdge > 1e-3) // Trailedge can be between 8.4 ns and 1 ms
                return -1;                          // Error: Trailedge out of bound

            _device.Write("FUNC:PULS:TRAN:TRA " + TrailEdge.ToString());
            _device.Write("FUNC:PULS:TRAN:TRA?");
            string response = _device.ReadString();
            double trailEdge;
            if (!double.TryParse(response, out trailEdge))
                return -2;                          // Error:  Can't convert into double

            if (trailEdge != TrailEdge)
                return -3;                          // Error: Non-matching values

            return trailEdge;                       // No complications
        }



        #region Waveformoverhead
        public void SetDC()
        {
            SetWaveform("DC");
        }



        public void SetSine()
        {
            SetWaveform("SIN");
        }



        public void SetSquare(double DutyCycle)
        {
            SetWaveform("SQU");
            SetSquareDutyCycle(DutyCycle);
        }



        public void SetRamp(double Symmetry)
        {
            SetWaveform("RAMP");
            SetRampSymmetry(Symmetry);
        }



        public void SetPulse(double PulseWidth, double BothEdges)
        {
            SetPulse(PulseWidth, BothEdges, BothEdges);     // Pass through error-code
        }



        public void SetPulse(double PulseWidth, double LeadEdge, double TrailEdge)
        {
            SetWaveform("PULS");
            SetPulseWidth(PulseWidth);
            SetPulseLeadEdge(LeadEdge);
            SetPulseTailEdge(TrailEdge);
        }
        #endregion


        public void PowerDownSource()
        {
            Output(false);
            OutputBurstOff();
            SetAmplitude(1e-3);
        }
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Threading;

namespace Leybold
{
    public class MOVE1250
    {
        private SerialPort _serialPort;
        public bool ConnectionEtablished { get { return _serialPort.IsOpen; } }
        public string DeviceType { get; private set; }

        // Constant min and max set-positions of the valve!
        private const int _min = 0x100;
        public int MinPosition { get { return _min; } }
        private const int _max = 0xD34;
        public int MaxPosition { get { return _max; } }
        private int _lastPositionAbs;

        // Timer for avoiding communication problems because of the slow baudrate 
        private Stopwatch _stopwatch;
        private int _communicationTime_ms;


        public MOVE1250(string comPort, int baud, int dataBits, StopBits stopBits, Parity parity, int timeout_ms, int communicationTime_ms)
        {
            if (timeout_ms < communicationTime_ms)
                throw new ArgumentOutOfRangeException(string.Format("timeout_ms) ({0}) must be bigger than communicationTime_ms ({1})", timeout_ms, communicationTime_ms));

            _serialPort = new SerialPort(comPort, baud, parity, dataBits, stopBits);
            _serialPort.ReadTimeout = timeout_ms;
            _serialPort.NewLine = "\r\n";
            _serialPort.Open();

            _communicationTime_ms = communicationTime_ms;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            DeviceType = "MOVE1250";
            OpenConnection();
            try
            {
                QueryCommand("h02"); // Digital-mode
                _lastPositionAbs = GetPositionAbsFromDevice();
            }
            catch (Exception e)
            {
                CloseConnection();
                throw new TimeoutException("Connection timed out while getting last position from device\r\n\r\n" + e.Message);
            }
            CloseConnection();
        }


        public void Dispose()
        {
            CloseConnection();
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
            cmd += "\r\n";
            while (_stopwatch.ElapsedMilliseconds < _communicationTime_ms) { } // Wait for finishing action
            _serialPort.Write(cmd);
            _stopwatch.Restart();
        }

        private string ReadResponse()
        {
            // Because of the hardware (I/O is the same pin) and the baud of 300 there is a problem with the response!
            //  this routine is just used to empty the read-buffer
            while (_stopwatch.ElapsedMilliseconds < _communicationTime_ms) { } // Wait for finishing action and response
            var response = _serialPort.ReadExisting(); // Response from device is maybe incorrect!
            _stopwatch.Restart();
            return response;
        }


        private string QueryCommand(string cmd)
        {
            WriteCommand(cmd);
            var response = ReadResponse();
            return response;
        }
        #endregion



        #region Commands (Mode, Positoncontrol)
        // ATTENTION! Setting-commands can return the wrong answer or sometimes also NO answer which is leading to exceptions!
        //  Check ReadResponse for more info
        #region Close, Vent and Stop action
        public void CloseImmediately()
        {
            QueryCommand("x");
            _lastPositionAbs = _min;
        }


        public void CloseSlow()
        {
            QueryCommand("j");
            _lastPositionAbs = _min;
        }


        public void VentImmediately()
        {
            QueryCommand("y");
            _lastPositionAbs = _max;
        }


        public void VentSlow()
        {
            QueryCommand("i");
            _lastPositionAbs = _max;
        }


        public void StopMovement()
        {
            QueryCommand("z");
            _lastPositionAbs = GetPositionAbs();
        }
        #endregion


        #region Getter position
        private int GetPositionAbsFromDevice()
        {
            // Because of initialposition it's necessary to get the correct answer! Therefore one Query is done without query-
            WriteCommand("p?");
            string response = null;
            while (_stopwatch.ElapsedMilliseconds < _communicationTime_ms) { } // Wait time
            while (_serialPort.BytesToRead > 4)
                response = _serialPort.ReadLine();
            _stopwatch.Restart();

            try { return int.Parse(response, NumberStyles.HexNumber) / 2; }
            catch (Exception e) { throw new FormatException("Can't convert returned value: " + e.Message); }
        }



        public int GetPositionAbs()
        {
            // Workaround, because of communication-problems!
            return _lastPositionAbs;
        }


        public int GetPositionAbsOffsetCorrected()
        {
            var position = GetPositionAbs();
            position -= _min;
            return position;
        }


        public double GetPositionRel()
        {
            var position = GetPositionAbsOffsetCorrected();
            var positionRel = OffsetCorrectedAbsToRel(position);
            return positionRel;
        }
        #endregion


        #region Setter position
        public void SetPositionAbs(int position)
        {
            if (position < _min || position > _max)
                throw new ArgumentOutOfRangeException("position", string.Format("MOVE1250 position out of range! (closed) 0x{0:X} <= position (0x{1:X}) <= 0x{2:X} (open)", _min, position, _max));

            QueryCommand("g" + position.ToString("x3"));
            _lastPositionAbs = position;
        }


        public void SetPositionAbsOffsetcorrected(int position)
        {
            // Offsetcorrected min and max
            const int offsetCorrectedMin = _min - _min;
            const int offsetCorrectedMax = _max - _min;

            if (position < offsetCorrectedMin || position > offsetCorrectedMax)
                throw new ArgumentOutOfRangeException("position", string.Format("Offsetcorrected MOVE1250 position out of range! (closed) {0} <= {1} <= {2} (open)", offsetCorrectedMin, position, offsetCorrectedMax));

            position += _min; // Shift position into correct position range
            SetPositionAbs(position);
        }


        public void SetPositionRel(double percentage)
        {
            var scaledPosition = RelToOffsetCorrectedAbs(percentage);
            SetPositionAbsOffsetcorrected(scaledPosition);
        }


        public void IncreasePositionBy(int delta)
        {
            var newPosition = _lastPositionAbs + delta;
            if (newPosition > _max)
                newPosition = _max;

            SetPositionAbs(newPosition);
        }


        public void DecreasePositionBy(int delta)
        {
            var newPosition = _lastPositionAbs - delta;
            if (newPosition < _min)
                newPosition = _min;

            SetPositionAbs(newPosition);
        }


        public void IncreasePositionRel(int deltaPercentage)
        {
            var delta = RelToOffsetCorrectedAbs(deltaPercentage);
            IncreasePositionBy(delta);
        }


        public void DecreasePositionRel(int deltaPercentage)
        {
            var delta = RelToOffsetCorrectedAbs(deltaPercentage);
            DecreasePositionBy(delta);
        }
        #endregion


        #region Abs <--> Rel helper
        private double OffsetCorrectedAbsToRel(int offCorrAbsPos)
        {
            const double fullRange = _max - _min;
            double relPos = offCorrAbsPos / fullRange;
            relPos *= 100; // Factor to percentage
            return relPos;
        }


        private int RelToOffsetCorrectedAbs(double relPos)
        {
            const double fullRange = _max - _min;
            relPos = relPos * fullRange;
            relPos /= 100; // Percentage to factor
            var offCorrAbsPos = (int)Math.Round(relPos);
            return offCorrAbsPos;
        }
        #endregion




        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using NationalInstruments.NI4882;

namespace Keithley
{
    public enum KE6517B_VoltageRange { Default, V100, V1000 }
    public enum KE6517B_MeasurementType { Voltage, Current, Charge, Resistance }
    public enum KE6517B_DigFiltType { Repeating, Moving }
    enum KE6517B_DataAquisitionMode { SingleAquisition, TraceAquisition }


    public class KE6517B
    {
        Device _device;
        KE6517B_DataAquisitionMode _dataAquisitionMode;


        public KE6517B(int boardNumber, int primAddr, int secAddr)
        {
            _device = new Device(boardNumber, (byte)primAddr, (byte)secAddr);
            Thread.Sleep(500); // Temporary solution to avoid crashes!
            Info(); //Check connection
            SetSingleDataAquisition();
        }

        public void Dispose()
        {
            if (_device != null)
            {
                try { PowerDownSource(); }
                catch (Exception) { }
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
            //Reset();

        }

        public void SetVoltage(double voltage, bool outputOn, KE6517B_VoltageRange vRange = KE6517B_VoltageRange.V1000)
        {
            lock (_device)
            {
                _device.Write("SOUR:VOLT:RANG " + (vRange == KE6517B_VoltageRange.V1000 ? "MAX" : "MIN"));
                _device.Write("SOUR:VOLT:LEV:IMM:AMPL " + voltage);
                SetOutput(outputOn);
            }
        }

        public double GetVoltage()
        {
            lock (_device)
            {
                _device.Write("SOUR:VOLT:LEV:IMM:AMPL?");
                var response = _device.ReadString();
                return Convert.ToDouble(response);
            }
        }

        private void SetOutput(bool state)
        {
            lock (_device)
            {
                _device.Write("OUTP:STAT " + (state ? "ON" : "OFF"));
            }
        }

        public void PowerDownSource()
        {
            SetOutput(false);
            SetVoltage(0, false);
        }


        public void SetZeroCheck(bool zeroCheckOn)
        {
            _device.Write("SYST:ZCH " + (zeroCheckOn ? "ON" : "OFF"));
        }

        public bool GetZeroCheckState()
        {
            _device.Write("SYST:ZCH?");
            var response = _device.ReadString();
            return (response[0] == '1' ? true : false);
        }


        public void SetZeroCorrect(bool zeroCheckOn)
        {
            _device.Write("SYST:ZCOR " + (zeroCheckOn ? "ON" : "OFF"));
        }

        public bool GetZeroCorrectState()
        {
            _device.Write("SYST:ZCOR?");
            var response = _device.ReadString();
            return (response[0] == '1' ? true : false);
        }

        public void AquireZeroCorrectionValue()
        {
            SetZeroCheck(true);
            Thread.Sleep(500);
            _device.Write("SYST:ZCOR:ACQ");
            SetZeroCheck(false);
            SetZeroCorrect(true);
        }


        public void SetMeasurementType(KE6517B_MeasurementType measurementType)
        {
            SetZeroCheck(true); // ZeroCheck on type-change
            var command = "SENS:FUNC ";
            switch(measurementType)
            {
                case KE6517B_MeasurementType.Voltage:
                    command += "\"VOLT\"";
                    break;

                case KE6517B_MeasurementType.Charge:
                    command += "\"CHAR\"";
                    break;

                case KE6517B_MeasurementType.Resistance:
                    command += "\"RES\"";
                    break;

                default:
                    command += "\"CURR\"";
                    break;
            }
            _device.Write(command);
            SetZeroCheck(false);
        }

        public KE6517B_MeasurementType GetMeasurementType()
        {
            switch(GetMeasurementTypeAsStringForFurtherUse())
            {
                case "VOLT":
                    return KE6517B_MeasurementType.Voltage;

                case "CHAR":
                    return KE6517B_MeasurementType.Charge;

                case "RES":
                    return KE6517B_MeasurementType.Resistance;

                default:
                    return KE6517B_MeasurementType.Current;
            }
        }

        private string GetMeasurementTypeAsStringForFurtherUse()
        {
            _device.Write("SENS:FUNC?");
            var response = _device.ReadString();
            response = response.Remove(0, 1);
            response = response.Remove(4);
            if (response.StartsWith("RES"))
                response = response.Remove(3);
            return response;
        }


        public void SetAutoRange(bool autoRangeOn)
        {
            string command = GetMeasurementTypeAsStringForFurtherUse();
            command += ":RANG:AUTO ";
            _device.Write(command + (autoRangeOn ? "ON" : "OFF"));
        }

        public void SetRange(double range)
        {
            if (range == 0)
            {
                SetAutoRange(true);
                return;
            }

            SetAutoRange(false);
            var command = "SENS:" + GetMeasurementTypeAsStringForFurtherUse() + ":RANG ";
            _device.Write(command + range);
        }

        public double GetRange()
        {
            var measuementType = GetMeasurementType();
            var command = "SENS:" + GetMeasurementTypeAsStringForFurtherUse() + ":RANG?";
            _device.Write(command);
            var response = _device.ReadString();
            return Convert.ToDouble(response);
        }


        public void SetIntegrationInNPLC(double nplc)
        {
            if (nplc < 0.01)
                nplc = 0.01;
            if (nplc > 10)
                nplc = 10;

            var measuementType = GetMeasurementType();
            var command = "SENS:" + GetMeasurementTypeAsStringForFurtherUse() + ":NPLC ";
            _device.Write(command + nplc);
        }

        public double GetIntegrationInNPLC()
        {
            var measuementType = GetMeasurementType();
            var command = "SENS:" + GetMeasurementTypeAsStringForFurtherUse() + ":NPLC?";
            _device.Write(command);
            var response = _device.ReadString();
            return Convert.ToDouble(response);
        }


        /// <summary>
        /// Only possible in Current or Resistance-Measurement. Otherwise nothing will happen
        /// </summary>
        public void SetDamping(bool dampingOn)
        {
            var measType = GetMeasurementType();
            var command = "SENS:";
            switch (measType)
            {
                case KE6517B_MeasurementType.Current:
                    command += "CURR:DC:DAMP ";
                    break;

                case KE6517B_MeasurementType.Resistance:
                    command += "RES:DAMP ";
                    break;

                default:
                    return;
            }

            command += (dampingOn ? "1" : "0");
            _device.Write(command);
        }


        public bool GetDamping()
        {
            var measType = GetMeasurementType();
            var command = "SENS:";
            switch (measType)
            {
                case KE6517B_MeasurementType.Current:
                    command += "CURR:DC:DAMP?";
                    break;

                case KE6517B_MeasurementType.Resistance:
                    command += "RES:DAMP?";
                    break;

                default:
                    return false;
            }
            _device.Write(command);
            var response = _device.ReadString();
            return (response[0] == '1' ? true : false);
        }


        public void SetMedian(bool medianOn, int rank = 3)
        {
            if (!medianOn)
            {
                _device.Write("SENS:" + GetMeasurementTypeAsStringForFurtherUse() + ":MED 0");
                return;
            }

            if (rank < 1)
                rank = 1;
            if (rank > 5)
                rank = 5;

            var command = "SENS:" + GetMeasurementTypeAsStringForFurtherUse();
            _device.Write(command + ":MED 1");
            _device.Write(command + ":MED:RANK " + rank);
        }


        public int GetMedian()
        {
            var command = "SENS:" + GetMeasurementTypeAsStringForFurtherUse();
            // Check median active
            _device.Write(command + ":MED?");
            var response = _device.ReadString();
            var medianOn = Convert.ToInt32(response);
            if (medianOn == 0)
                return 0;
            // Check ranklevel
            _device.Write(command + ":MED:RANK?");
            response = _device.ReadString();
            var rank = Convert.ToInt32(response);
            return rank;
        }



        public void SetDigitalFilter(bool filterOn, int count, KE6517B_DigFiltType filtType =KE6517B_DigFiltType.Moving)
        {
            var command = GetMeasurementTypeAsStringForFurtherUse() + ":AVER";
            if (!filterOn)
            {
                _device.Write(command + ":STAT 0");
                return;
            }

            if (count < 2) // Filter with 1 tap is a deactive filter --> Minimum required = 2
                count = 2;
            if (count > 100)
                count = 100;

            _device.Write(command + ":STAT 1");
            _device.Write(command + ":TCON " + (filtType == KE6517B_DigFiltType.Repeating ? "REP" : "MOV"));
            _device.Write(command + ":COUN " + count);
        }

        public void GetDigitalFilter(out bool filterOn, out int count, out KE6517B_DigFiltType filtType)
        {
            var command = GetMeasurementTypeAsStringForFurtherUse() + ":AVER";

            // State
            _device.Write(command + ":STAT?");
            var response = _device.ReadString();
            filterOn = (response[0] == '1' ? true : false);

            // Filtertype
            _device.Write(command + ":TCON?");
            response = _device.ReadString();
            filtType = (response[0] == 'R' ? KE6517B_DigFiltType.Repeating : KE6517B_DigFiltType.Moving);

            // Averagecounts
            _device.Write(command + ":COUN?");
            response = _device.ReadString();
            count = Convert.ToInt32(response);
        }


        public void SetSingleDataAquisition()
        {
            _dataAquisitionMode = KE6517B_DataAquisitionMode.SingleAquisition;
            _device.Write("TRAC:FEED:CONT NEV"); // Deactivate buffer
            _device.Write("FORM:ELEM READ");     // Only values without stamps or something

            _device.Write("INIT:CONT OFF"); // Send device to idle
        }

        public double GetValue()
        {
            if (_dataAquisitionMode != KE6517B_DataAquisitionMode.SingleAquisition)
                throw new InvalidOperationException("Single aquisition was requested but the device is configured for trace data aquisition!");

            string response = "";
            lock (_device)
            {
                if (GetZeroCheckState()) // When zerocheck is enabled 9.91e37 will be returned on read instead of overflow (9.9e37)
                    return 1e-12;        //   Use the minimum possible measurevalue of it's smallest range (2e-12 A)!

                _device.Write("READ?");
                response = _device.ReadString();
            }
            return Convert.ToDouble(response);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Threading;
using Renci.SshNet;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace HaumOTH
{
    public enum PiCamStatus
    {
        Error = -1,
        Ok,
        Unconnected,
    };

    public enum PiCamExposureMode
    {
        off,
        auto,
        night,
        nightpreview,
        backlight,
        spotlight,
        sports,
        snow,
        beach,
        verylong,
        fixedfps,
        antishake,
        fireworks,
    };

    public enum PiCamAwbMode
    {
        // awbgain only valid when AwbMode is 'off'
        off,
        auto,
        sunlight,
        cloudy,
        shade,
        tungsten,
        fluorescent,
        incandescent,
        flash,
        horizon,
    };

    class PiCam
    {
        SshClient _ssh = null;
        SshCommand _cmd = null;
        Socket _socket = null;
        ScpClient _scp = null;

        public string PyScriptPath { get; private set; }
        public string PyLogPath { get; private set; }
        public string _srvRawPath { get; private set; }
        public string _srvPicPath { get; private set; }
        //string _currPID = null;

        const string ackStr = "ack";
        const string nakStr = "nak";




        public PiCam(string Ip = "ccdkammer", UInt16 Port = 5060, string User = "pi", string Passwd = "ccdkammer", string PyCamScriptPath = "/home/pi/rPiHQCam.py")
        {
            if (Ip == null)
                throw new ArgumentNullException("IP is null");
            if (User == null)
                throw new ArgumentNullException("username is null");
            if (Passwd == null)
                throw new ArgumentNullException("password is null");
            //if (PyCamPath == null) // Can't be NULL
            //    throw new ArgumentNullException("Pythonscript-path is null");

            PyScriptPath = PyCamScriptPath;

            // Create and connect SSH
            _ssh = new SshClient(Ip, User, Passwd);
            try { _ssh.Connect(); }
            catch (Exception e) { throw new Exception("Can't connect via SSH: " + e.Message); }
            _cmd = _ssh.CreateCommand("cd .");

            // Kill old script-instance, run new instance and connect to it
            try
            {
                if (PyScriptPath != null)
                    RunPyCamScript();

                Thread.Sleep(1000);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                //_socket.ReceiveTimeout = 10000;
                //_socket.SendTimeout = 10000;
                _socket.Connect(Ip, Port);
            }
            catch (Exception e)
            {
                throw new AccessViolationException("Couldn't connect to remote server (wrong ip/port or server not startet.)\n\nAdditional Info:\n" + e.Message);
            }

            _srvPicPath = QuerySocket("SRV:PATH:PIC?");
            _srvRawPath = QuerySocket("SRV:PATH:RAW?");
            _scp = new ScpClient(Ip, 22, User, Passwd);
        }

        public void Dispose()
        {
            if (_socket.Connected) // Try to close regularly
                QuerySocket("SRV:CLOSE");

            KillPyCamScriptInstances(); // Afterwards check and force kill if not closed

            if (_cmd != null)
                _cmd.Dispose();

            if (_ssh != null)
            {
                _ssh.Disconnect();
                _ssh.Dispose();
            }
        }


        private void KillPyCamScriptInstances()
        {
            if (_ssh.IsConnected)
            {
                string[] pids = null;
                do
                {
                    //response = _cmd.Execute("sudo ps ax | grep " + _pythonPath + " | grep - v grep | awk '{print $1}'");
                    pids = FindPIDsByName(PyScriptPath);
                    if (pids != null && pids.Length > 0)
                    {
                        KillPID(pids[0]); // Kill first pid
                        continue;
                    }

                    break;
                } while (true);
            }
        }

        private void RunPyCamScript()
        {
            string response = null;
            KillPyCamScriptInstances();

            Thread.Sleep(100); // Give rPi a little bit time to close the process completely
            PyLogPath = Path.Combine(Path.GetDirectoryName(PyScriptPath), "rPiHQCam.log").Replace('\\', '/');
            response = _cmd.Execute(string.Format("(python -u {0} > {1}) &", PyScriptPath, PyLogPath));
            Thread.Sleep(100); // Wait a little bit so that the log-file can be cleared before first read! (otherwise may the old "Socket created." message is available there)

            int pyCamScriptStatusRetries = 12; // 12 * 500ms = 6s
            int pyCamScriptCheckTimeout_ms = 500;
            string[] logLines = null;
            //string socketSuccessfully = "Socket created.";
            string socketSuccessfully = "Socket bind complete.";
            while (pyCamScriptStatusRetries > 0)
            {
                Thread.Sleep(pyCamScriptCheckTimeout_ms);

                // LogLines is overwritten on every restart (see '>' in the start command above!)
                logLines = CatFile(PyLogPath).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (logLines.Length >= 1) // Be sure, that at least 1 entry is received before trying to acces the log-entries
                {
                    //if (logLines[logLines.Length - 2] == socketSuccessfully  // In LogLine[3] (server reloaded) should stand a specific success-msg.
                    //    || logLines[logLines.Length - 1] == socketSuccessfully) // In LogLine[2] (first server run) should stand a specific success-msg.
                    if (logLines[logLines.Length - 1] == socketSuccessfully)
                        goto breakNested; // There's no "double-break"^^
                }
                pyCamScriptStatusRetries--;
            }
            //if (pyCamScriptStatusRetries <= 0)
            throw new Exception("Server-socket not created. Please run again or check the server manually");

        breakNested:
            return;
        }

        #region SSH-Commands
        private string[] FindPIDsByName(string PName)
        {
            var response = _cmd.Execute("ps ax | grep '" + PName + "' | grep -v grep | awk '{print $1}'");
            return response.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void KillPID(string Pid, bool UseSudo = false)
        {
            _cmd.Execute((UseSudo ? "sudo" : "") + "kill " + Pid);
        }

        public string ChangeDir(string path = "/home/pi")
        {
            if (path == null)
                throw new ArgumentNullException("path is null");

            var response = _cmd.Execute("cd " + path);
            return response;
        }

        public string ListDir()
        {
            return _cmd.Execute("ls -la");
        }

        public string ListDir(string path)
        {
            return _cmd.Execute("ls -la " + path);
        }

        public string CatFile(string path)
        {
            return _cmd.Execute("cat " + path);
        }


        public void DownloadFile(string SrcPath, string DstPath)
        {
            _scp.Connect();

            var fWriter = new FileStream(DstPath, FileMode.OpenOrCreate, FileAccess.Write);
            _scp.Download(SrcPath, fWriter);
            fWriter.Close();
            _scp.Disconnect();
            fWriter.Dispose();
        }

        public void DownloadFile(string SrcFolderPath, string SrcFilename, string DstFolderPath, string DstFilename)
        {
            DownloadFile(Path.Combine(SrcFolderPath, SrcFilename), Path.Combine(DstFolderPath, DstFilename));
        }

        public void ReceiveAllRawAsTar(string TarGzFullName)
        {
            var srcPath = string.Format("{0}/{1}.tar.gz", _srvRawPath, Path.GetFileName(TarGzFullName));
            _cmd.Execute(string.Format("tar -czf {0} -C {1} .", srcPath, _srvRawPath));
            var dstPath = string.Format("{0}.tar.gz", TarGzFullName);

            DownloadFile(srcPath, dstPath);
            //_scp.Connect();

            //var fWriter = new FileStream(dstPath, FileMode.OpenOrCreate, FileAccess.Write);
            //_scp.Download(srcPath, fWriter);
            //fWriter.Close();
            //_scp.Disconnect();
            //fWriter.Dispose();
            ClearRamDisk();
        }

        public void ReceiveAllRawAsTar(string DownloadFolderpath, string TarGzName)
        {
            ReceiveAllRawAsTar(Path.Combine(DownloadFolderpath, TarGzName));
        }

        private void ClearRamDisk()
        {
            _cmd.Execute(string.Format("rm {0}/*", _srvRawPath));
        }
        #endregion SSH-Commands

        #region Socket-Commands
        public PiCamStatus TakeRaw2Ramdisk(string Filename)
        {
            return EvaluatedQuery("CAM:RAW " + Filename);
        }

        public PiCamStatus TakePic2SD(string Filename)
        {
            return EvaluatedQuery("CAM:PIC " + Filename);
        }

        public PiCamStatus TakePicSequence2Ramdisk(string Filename, uint Count)
        {
            // SEQuence
            return EvaluatedQuery(string.Format("CAM:SEQ {0} {1}", Filename, Count));
        }
        public PiCamStatus TakePicSequence2Ramdisk(string Filename, uint Count, bool DynamicExposureTime)
        {
            // SEQuence Dynamic Exposure Times (SEQDET)
            return EvaluatedQuery(string.Format("CAM:SEQDET {0} {1}", Filename, Count));
        }
        public PiCamStatus TakePicSequence2Ramdisk(string Filename, uint Count, uint[] ExposureTimes, double IntervallTime_s, bool Bayer)
        {
            if (ExposureTimes == null)
                return PiCamStatus.Error;

            string expTimes = string.Empty;
            foreach (var exposureTime in ExposureTimes)
                expTimes += exposureTime.ToString() + ":"; // PyScript is using ':' as separator
            expTimes = expTimes.Remove(expTimes.Length - 1); // Remove last separator

            // SEQuence Fixed Exposure Times (SEQFET)
            return EvaluatedQuery(string.Format("CAM:SEQFET {0} {1} {2} {3} {4}", Filename, expTimes, Count, IntervallTime_s.ToString(), Bayer.ToString()));
        }

        public PiCamStatus ConfFrameRate(double FrameRate)
        {
            return EvaluatedQuery("CAM:CONF:FR " + FrameRate);
        }

        public PiCamStatus ConfShutterSpeed(uint Shutterspeed_us = 1000)
        {
            return EvaluatedQuery("CAM:CONF:SS " + Shutterspeed_us);
        }
        public PiCamStatus ConfISO(uint Iso = 500)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:ISO {0}", Iso));
        }

        public PiCamStatus ConfAnalogGain(double Ag)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:AG {0}", Ag));
        }
        public PiCamStatus ConfDigitalGain(double Dg)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:DG {0}", Dg));
        }
        public PiCamStatus ConfAnalogAndDigitalGain(double Ag, double Dg)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:AGDG {0} {1}", Ag, Dg));
        }

        public PiCamStatus ConfResolution(int Width = 1920, int Height = 1080)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:RES {0} {1}", Width, Height));
        }

        public PiCamStatus ConfResolution(uint Width, uint Height)
        {
            return EvaluatedQuery(String.Format("CAM:CONF:RES {0} {1}", Width, Height));
        }
        //public PiCamStatus ConfResolution (int width = 1920,  double ratio = 16.0/9.0)
        //{
        //    return ConfResolution(width, (int)(width * ratio));
        //}
        //public PiCamStatus ConfResolution(double ratio, int height)
        //{
        //    return ConfResolution((int)(height/ratio), height);
        //}

        public PiCamStatus ConfExposureMode(PiCamExposureMode ExposureMode = PiCamExposureMode.off)
        {
            return EvaluatedQuery("CAM:CONF:EXPMODE " + Enum.GetName(typeof(PiCamExposureMode), ExposureMode));
        }

        //AWB-Gains only valid on AWB-Mode "off" (this is configured automatically from ConfAwbGains-method). AWB-Gains not used on other cases.
        public PiCamStatus ConfAwbMode(PiCamAwbMode AwbMode = PiCamAwbMode.off)
        {
            double awbGainRBalance = 1.2; // Usual defaults
            double awbGainBBalance = 1.2; // Usual defaults
            return EvaluatedQuery(string.Format("CAM:CONF:AWB {0}:{1} {2}", awbGainRBalance, awbGainBBalance, Enum.GetName(typeof(PiCamAwbMode), PiCamAwbMode.off)));
        }

        // AWB-Gains only valid on AWB-Mode "off". Previous AWB-Modes will auto-reconfigured to "off" by this method!
        public PiCamStatus ConfAwbGains(double AwbGainRBalance, double AwbGainBBalance)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:AWB {0}:{1} {2}", AwbGainRBalance, AwbGainBBalance, Enum.GetName(typeof(PiCamAwbMode), PiCamAwbMode.off)));
        }
        // AWB-Gains only valid on AWB-Mode "off". Previous AWB-Modes will auto-reconfigured to "off" by this method!
        public PiCamStatus ConfAwbGains(double AwbGains = 1.2)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:AWB {0} {1}", AwbGains, Enum.GetName(typeof(PiCamAwbMode), PiCamAwbMode.off)));
        }

        public PiCamStatus ConfAll(uint Shutterspeed_us = 1000, uint Iso = 500, uint Width = 1920, uint Height = 1080, PiCamExposureMode ExposureMode = PiCamExposureMode.off, PiCamAwbMode AwbMode = PiCamAwbMode.off, double AwbGainRBalance = 1.2, double AwbGainBBalance = 1.2)
        {
            // Parameter order:
            // C#-Call:  0 iso, 1 SS, 2 & 3 AWB, 4 width, 5 height
            // PyScript: 0 iso, 1 SS, 2     AWB, 3 width, 4 height
            return EvaluatedQuery(string.Format("CAM:CONF:ALL {0} {1} {2}:{3} {4} {5}", Iso, Shutterspeed_us, AwbGainRBalance, AwbGainBBalance, Width, Height));
        }
        public PiCamStatus ConfAll(int Shutterspeed_us = 1000, int Iso = 500, uint Width= 1920, uint Height=1080, PiCamExposureMode ExposureMode = PiCamExposureMode.off, PiCamAwbMode AwbMode = PiCamAwbMode.off, double AwbGains = 1.2)
        {
            // Parameter order:
            // C#-Call:  0 iso, 1 SS, 2 AWB, 3 width, 4 height
            // PyScript: 0 iso, 1 SS, 2 AWB, 3 width, 4 height
            return EvaluatedQuery(string.Format("CAM:CONF:ALL {0} {1} {2} {3} {4}", Iso, Shutterspeed_us, AwbGains, Width, Height));
        }
        #endregion Socket-Commands


        #region Socket-Communication
        private PiCamStatus EvaluatedQuery(string Message)
        {
            var response = QuerySocket(Message);
            if (response == ackStr)
                return PiCamStatus.Ok;

            return PiCamStatus.Error;
        }

        private string QuerySocket(string Message)
        {
            Send2Socket(Message);
            return ReceiveFromSocket();
        }

        private PiCamStatus Send2Socket(string Message)
        {
            if (!_socket.Connected)
                return PiCamStatus.Unconnected;

            _socket.Send(Encoding.ASCII.GetBytes(Message));
            return PiCamStatus.Ok;
        }

        private string ReceiveFromSocket(uint ExceptedSize = 32)
        {
            if (!_socket.Connected)
                return "";

            byte[] responseBuffer = new byte[ExceptedSize];
            _socket.Receive(responseBuffer);
            int strSize = FindInByteArray(responseBuffer, '\0'); // Find Stringtermination
            if (strSize <= 0) // Nothing return or no stringtermination found
                return "";

            return Encoding.ASCII.GetString(responseBuffer, 0, FindInByteArray(responseBuffer, '\0'));
        }
        #endregion Socket-Communication

        private int FindInByteArray(byte[] ByteArray, char Comparison)
        {
            for (int i = 0; i < ByteArray.Length; i++)
                if (ByteArray[i] == Comparison)
                    return i;

            return -1;
        }
    }
}
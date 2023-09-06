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
using System.Linq;

namespace HaumOTH
{
    public enum PiCam2Status
    {
        Error = -1,
        Ok,
        Unconnected,
    };

    //public enum PiCam2ExposureMode
    //{
    //    off,
    //    auto,
    //    night,
    //    nightpreview,
    //    backlight,
    //    spotlight,
    //    sports,
    //    snow,
    //    beach,
    //    verylong,
    //    fixedfps,
    //    antishake,
    //    fireworks,
    //};

    //public enum PiCam2AwbMode
    //{
    //    // awbgain only valid when AwbMode is 'off'
    //    off,
    //    auto,
    //    sunlight,
    //    cloudy,
    //    shade,
    //    tungsten,
    //    fluorescent,
    //    incandescent,
    //    flash,
    //    horizon,
    //};

    class PiCam2
    {
        SshClient _ssh = null;
        //SshCommand _cmd = null;
        ShellStream _shellStream = null;
        //SshCommand _pyServCmd = null;
        Socket _socket = null;
        //ScpClient _scp = null;
        ConnectionInfo _connInfo = null;

        public string PyScriptPath { get; private set; }
        public string PyLogPath { get; private set; }
        public string _srvRamDiskPath { get; private set; }
        public string _srvSDCardPath { get; private set; }
        public string _srvImgCapPath { get; private set; }
        //string _currPID = null;

        const string ackStr = "ack";
        const string nakStr = "nak";




        public PiCam2(string Ip = "ccdkammer", UInt16 Port = 5060, string User = "pi", string Passwd = "ccdkammer", string PyCamScriptPath = "/home/pi/rPiHQCamServer2.py")
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
            //_cmd = _ssh.CreateCommand("cd ."); // Dummy-Command to create SSH-Command
            _shellStream = _ssh.CreateShellStream("rPiHQCamServer2", 0, 0, 0, 0, 1000);
            //_cmd = _ssh.CreateCommand("cd .");
            Thread.Sleep(100); // Ugly but needed to wait a little or else shell isn't ready

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

            _srvSDCardPath = QuerySocket("SRV:PATH:SDDIR?");
            _srvRamDiskPath = QuerySocket("SRV:PATH:RDDIR?");
            _srvImgCapPath = QuerySocket("SRV:PATH:IMDIR?");
            //_scp = new ScpClient(Ip, 22, User, Passwd);
            _connInfo = new PasswordConnectionInfo(Ip, 22, User, Passwd);
        }

        public void Dispose()
        {
            if (_socket.Connected) // Try to close regularly
                QuerySocket("SRV:CLOSE");

            KillPyCamScriptInstances(); // Afterwards check and force kill if not closed

            //if (_cmd != null)
            //    _cmd.Dispose();
            if (_shellStream!= null)
                _shellStream.Dispose();

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
            PyLogPath = Path.Combine(Path.GetDirectoryName(PyScriptPath), "rPiHQCamServer2.log").Replace('\\', '/');

            // Workaround to run the python script anyway. Otherwise _cmd.Execute freeze and waits for the python-script to finish (even it's started as a process? o.O)
            var _pyStartCmd = $"(python -u {PyScriptPath} > {PyLogPath}) &";
            _shellStream.WriteLine(_pyStartCmd);
            //_pyServCmd = _ssh.CreateCommand($"nohup \"python -u {PyScriptPath} > {PyLogPath}\"");
            //_pyServCmd.CommandTimeout = new TimeSpan((int)1e6);   // Set 100ms-timeout before throw an intended exception
            //try { response = _pyServCmd.Execute(); }
            //catch (Exception) { }
            Thread.Sleep(100); // Wait a little bit so that the log-file can be cleared before first read! (otherwise may the old "Socket created." message is available there)

            int pyCamScriptStatusRetries = 12; // 12 * 500ms = 6s
            int pyCamScriptCheckTimeout_ms = 500;
            string[] logLines = null;
            //string socketSuccessfully = "Socket created.";
            string logRdy4Client = "Awaiting connection";
            while (pyCamScriptStatusRetries > 0)
            {
                Thread.Sleep(pyCamScriptCheckTimeout_ms);

                // LogLines is overwritten on every restart (see '>' in the start command above!)
                logLines = CatFile(PyLogPath).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (logLines.Length > 0) // Be sure, that at least 1 entry is received before trying to acces the log-entries
                {
                    if (logLines[logLines.Length - 1].StartsWith(logRdy4Client))
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
            var _cmd = _ssh.CreateCommand("ps ax | grep '" + PName + "' | grep -v grep | awk '{print $1}'");
            var response = _cmd.Execute();
            return response.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void KillPID(string Pid, bool UseSudo = false)
        {
            var _cmd = _ssh.CreateCommand((UseSudo ? "sudo" : "") + "kill " + Pid);
            _cmd.Execute();
        }

        public string ChangeDir(string path = "/home/pi")
        {
            if (path == null)
                throw new ArgumentNullException("path is null");

            var _cmd = _ssh.CreateCommand("cd " + path);
            var response = _cmd.Execute();
            return response;
        }

        public string ListDir()
        {
            var _cmd = _ssh.CreateCommand("ls -la");
            return _cmd.Execute();
        }

        public string ListDir(string path)
        {
            var _cmd = _ssh.CreateCommand("ls -la " + path);
            return _cmd.Execute();
        }

        public List<string> lsDirectory(string path, string lsOptions="-A")
        {
            var _cmd = _ssh.CreateCommand($"ls {lsOptions} " + path);
            List<string> fNames = new List<string>(_cmd.Execute().Split('\n'));
            fNames.RemoveAt(fNames.Count - 1);
            return fNames;
        }

        public string CatFile(string path)
        {
            var _cmd = _ssh.CreateCommand("cat " + path);
            return _cmd.Execute();
        }


        public void DownloadFile(string SrcPath, string DstPath)
        {
            var _scp = new ScpClient(_connInfo);
            _scp.Connect();

            var fWriter = new FileStream(DstPath, FileMode.OpenOrCreate, FileAccess.Write);
            _scp.Download(SrcPath, fWriter);
            fWriter.Close();
            _scp.Disconnect();
            _scp.Dispose();
            fWriter.Dispose();
        }

        public void DownloadFile(string SrcFolderPath, string SrcFilename, string DstFolderPath, string DstFilename)
        {
            DownloadFile(Path.Combine(SrcFolderPath, SrcFilename), Path.Combine(DstFolderPath, DstFilename));
        }


        public void ReceiveAllRaw(string srcFolderPath, string dstFolderPath)
        {
            var fNames = lsDirectory(srcFolderPath);

            string srcPath = string.Empty;
            string dstPath = string.Empty;
            foreach (string fName in fNames)
            {
                srcPath = string.Format($"{srcFolderPath}/{fName}");
                dstPath = string.Format($"{dstFolderPath}/{fName}");
                DownloadFile(srcPath, dstPath);
            }
        }

        public string Raw2Archive(string TarGzFullName, uint Compress2TarGz, uint CompressMulticore, uint CompressWithParentDir)
        {
            var srcPath = string.Format("{0}/{1}.tar{2}", _srvRamDiskPath, Path.GetFileName(TarGzFullName), (Compress2TarGz != 0 ? ".gz" : ""));
            //_cmd.Execute(string.Format("tar -czf {0} -C {1} .", srcPath, _srvRamDiskPath));
            // PiServer-Command is:
            //  reply = CompressFolder(compressPath=payload[0], tarGzFName=payload[1], Multicore=DecodeBoolStr(payload[2]), SuppressParents=DecodeBoolStr(payload[3]))
            QuerySocket(string.Format("SRV:ARCHV {0} {1} {2} {3} {4}", _srvImgCapPath, srcPath, Compress2TarGz, CompressMulticore, CompressWithParentDir));

            ClearDirContent(_srvImgCapPath); // Remove images for next capture!

            return srcPath;
        }

        public void ReceiveAllRawAsArchive(string TarGzFullName, uint Compress2TarGz, uint CompressMulticore, uint CompressWithParentDir)
        {
            var srcPath = Raw2Archive(TarGzFullName, Compress2TarGz, CompressMulticore, CompressWithParentDir);
            var dstPath = string.Format("{0}.tar{1}", TarGzFullName, (Compress2TarGz != 0 ? ".gz" : "")); // Append .gz if compression is enabled

            //ClearDirContent(_srvImgCapPath); // Remove images for next capture!

            DownloadFile(srcPath, dstPath);
            //_scp.Connect();

            //var fWriter = new FileStream(dstPath, FileMode.OpenOrCreate, FileAccess.Write);
            //_scp.Download(srcPath, fWriter);
            //fWriter.Close();
            //_scp.Disconnect();
            //fWriter.Dispose();
            
            ClearFile(srcPath);              // Remove archive
        }

        public void ReceiveAllRawAsArchive(string DownloadFolderpath, string TarGzName, uint Compress2TarGz, uint CompressMulticore, uint CompressWithParentDir)
        {
            ReceiveAllRawAsArchive(Path.Combine(DownloadFolderpath, TarGzName), Compress2TarGz, CompressMulticore, CompressWithParentDir);
        }

        //private void ClearRamDisk()
        //{
        //    _cmd.Execute(string.Format("rm -rf {0}", _srvRamDiskPath));
        //}

        public void ClearDir(string dirPath)
        {
            var _cmd = _ssh.CreateCommand(string.Format("rm -rf {0}", dirPath));
            _cmd.Execute();
        }

        public void ClearDirContent(string dirPath)
        {
            var _cmd = _ssh.CreateCommand(string.Format("rm {0}/*", dirPath));
            _cmd.Execute();
        }

        public void ClearFile(string filePath)
        {
            var _cmd = _ssh.CreateCommand(string.Format("rm {0}", filePath));
            _cmd.Execute();
        }

        #endregion SSH-Commands

        #region Socket-Commands
        public PiCamStatus CaptureShutterSpeedSequence(string Filename, uint nPicsPerSS, uint[] SSList, double IntervallTime_s, uint SaveSSLog)
        {
            if (SSList == null)
                return PiCamStatus.Error;

            string _ss = string.Empty;
            foreach (var SS in SSList)
                _ss += SS.ToString() + ":"; // PyScript is using ':' as separator
            _ss = _ss.Remove(_ss.Length - 1); // Remove last separator

            // SEQuence Fixed Exposure Times (SEQFET)

            // Call within PyServer2:
            //  CaptureShutterspeedSequence(Prefix = payload[0], StorePath = imFolderPath, SS = payload[1], nPics = payload[2], tMax = payload[3], SaveSSLog = DecodeBoolStr(payload[4]))
            return EvaluatedQuery(string.Format("CAP:SEQFET {0} {1} {2} {3} {4}", Filename, _ss, nPicsPerSS, IntervallTime_s.ToString(), SaveSSLog));
        }

        public PiCamStatus ConfFrameRate(double FrameRate)
        {
            return EvaluatedQuery("CAM:CONF:FR " + FrameRate);
        }

        public PiCamStatus ConfShutterSpeed(uint Shutterspeed_us = 1000)
        {
            return EvaluatedQuery("CAM:CONF:SS " + Shutterspeed_us);
        }

        public PiCamStatus ConfAnalogGain(double Ag)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:AG {0}", Ag));
        }

        public PiCamStatus ConfScalerCrop(uint x = 0, uint y = 0, uint Width = 4056, uint Height = 3040)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:SCLCRP {0}:{1} {2}:{3}", x, y, Width, Height));
        }

        public PiCamStatus ServerBayerClipSize(uint Width, uint Height)
        {
            return EvaluatedQuery(String.Format("SRV:IMG:BCLP {0}:{1}", Width, Height));
        }
        public PiCamStatus ServerBayerClipSize(uint x, uint y, uint Width, uint Height)
        {
            return EvaluatedQuery(String.Format("SRV:IMG:BCLP {0}:{1}:{2}:{3}", x, y, Width, Height));
        }
        public PiCamStatus ServerDeBayerClippedBayer(bool DebayerOnOff)
        {
            // Use "1", "0" -> Shorter
            return EvaluatedQuery(String.Format("SRV:IMG:DBAY {0}", (DebayerOnOff ? 1 : 0)));
        }
        public PiCamStatus ServerShrinkHalfDebayeredImageIterations(uint iterations)
        {
            return EvaluatedQuery(String.Format("SRV:IMG:SRNK {0}", iterations));
        }


        // AWB-Gains only valid on AWB-Mode "off". Previous AWB-Modes will auto-reconfigured to "off" by this method!
        public PiCamStatus ConfAwbGains(double AwbGainRBalance, double AwbGainBBalance)
        {
            return EvaluatedQuery(string.Format("CAM:CONF:AWB {0}:{1}", AwbGainRBalance, AwbGainBBalance));
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
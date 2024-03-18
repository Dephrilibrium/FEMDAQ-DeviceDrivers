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
using IVINET.Internal;
using Ivi.Driver;
using System.Reflection;

namespace ISICAS
{

    public class FEAS
    {
        internal Core _feas = null;


        public FEAS(string IpOrDns = "ccdkammer", UInt16 Port = 5060, string User = "pi", string Passwd = "ccdkammer", string PyCamScriptPath = "/home/pi/rPiHQCam.py")
        {
            Core core = null;
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            core = Activator.CreateInstance(type, resourceName, idQuery, resetDevice, lockType, accessKey, optionString, driver) as Core;

        }

        public void Dispose()
        {
            
        }


    }
}
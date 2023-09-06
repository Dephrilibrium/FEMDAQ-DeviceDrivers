using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

// own usings
using zhinst;

namespace ZurichInstruments
{
    public class HF2LI : IDisposable
    {
        private ziDotNET _device = new ziDotNET();

        public HF2LI(string hostIP, string port)
        {
            //_device = new ziDotNET();
            //try { var portAsUShort = ushort.Parse(port); }
            //catch (Exception e) { MessageBox.Show("Can't parse HF2LI-Port: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            //_device.init(hostIP, , ZIAPIVersion_enum.ZI_API_VERSION_1); // API-Level 1 MUST be used for HF2-Devices (info from programming manual)
        }

        public void Dispose()
        {
            _device.Dispose();
        }

    }

}

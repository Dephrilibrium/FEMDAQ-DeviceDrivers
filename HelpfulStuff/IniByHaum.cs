using System;
using System.Collections.Generic;
using System.IO;



namespace HelpfulStuff.IniByHaum
{
    public class IniHaum
    {
        public static string ParseStringValueFromKeyValuePair(string KeyValuePair)
        {
            var keyValuePairSplitted = KeyValuePair.Split(new char[] { '=' });
            return keyValuePairSplitted[1];
        }

        public static double ParseDoubleFromKeyValuePair(string KeyValuePair)
        {
            var valueAsString = ParseStringValueFromKeyValuePair(KeyValuePair);
            return double.Parse(valueAsString);
        }



        public List<string> IniWithoutComments { get; private set; }
        private List<int> _sectionIndicies;


        public IniHaum(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("File \"" + filename + "\" not found");
            var reader = new StreamReader(filename);
            var cont = reader.ReadToEnd();
            reader.Close();
            reader.Dispose();

            var contSplitted = cont.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            IniWithoutComments = new List<string>();
            foreach (var iniLine in contSplitted)
            {
                if (!iniLine.StartsWith("#"))
                    IniWithoutComments.Add(iniLine);
            }


            _sectionIndicies = ScanSectionIndicies(IniWithoutComments);
        }

        private List<int> ScanSectionIndicies(List<string> iniWithoutComments)
        {
            var sectionIndicies = new List<int>();
            for(var index = 0; index < iniWithoutComments.Count; index++)
            {
                if (IniWithoutComments[index].StartsWith("["))
                    sectionIndicies.Add(index);
            }

            return sectionIndicies;
        }

        public string FindKey(string sectionname, string keyname)
        {
            int startIndex = -1; // -1 to check if already inici
            int endIndex = -1;
            if (sectionname == null || sectionname == "")
            {
                startIndex = 0; // Keys without Section starts at 0
                if (_sectionIndicies.Count > 0)
                    endIndex = _sectionIndicies[1] - 1;
                else
                    endIndex = IniWithoutComments.Count - 1;
            }
            else
            {
                // Find starting and ending index
                for (int sectionFinderIndex = 0; sectionFinderIndex < _sectionIndicies.Count; sectionFinderIndex++)
                {
                    if (IniWithoutComments[_sectionIndicies[sectionFinderIndex]] == sectionname)
                    {
                        startIndex = _sectionIndicies[sectionFinderIndex];
                        if ((sectionFinderIndex + 1) < _sectionIndicies.Count)
                            endIndex = _sectionIndicies[sectionFinderIndex + 1] - 1; // Last line before next section
                        else
                            endIndex = IniWithoutComments.Count - 1; // Last line!
                    }
                }
            }

            // Find Key in Section
            for (; startIndex <= endIndex; startIndex++)
            {
                if (IniWithoutComments[startIndex].StartsWith(keyname))
                    return IniWithoutComments[startIndex];
            }
            return null;
        }

        public string GetValueOfKey(string sectionname, string keyname, string keyStartsWith)
        {
            return null;
        }

    }
}
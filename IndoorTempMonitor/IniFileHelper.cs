using System.Runtime.InteropServices;
using System.Text;

namespace IndoorTempMonitor
{
    public class IniFileHelper
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
            string key, string def, StringBuilder retVal, int size, string filePath);

        public IniFileHelper(string INIPath)
        {
            path = INIPath;
        }

        public void Write(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        public string Read(string Section, string Key, string Default)
        {
            StringBuilder buffer = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, Default, buffer, 255, this.path);

            return buffer.ToString();
        }

        public void Write(string Section, string Key, int Value)
        {
            WritePrivateProfileString(Section, Key, Value.ToString(), this.path);
        }

        public int Read(string Section, string Key, int Default)
        {
            StringBuilder buffer = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, Default.ToString(), buffer, 255, this.path);

            return int.Parse(buffer.ToString());
        }
    }
}
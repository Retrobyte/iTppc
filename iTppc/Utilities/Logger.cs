using iTppc.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iTppc.Utilities
{
    public static class Logger
    {
        private static string _log = string.Empty;

        public static string write(string value)
        {
            string toAdd = "[" + DateTime.Now.ToString("hh:mm:ss tt") + "] " + value + Environment.NewLine;

            lock (_log)
                _log += toAdd;

            return toAdd;
        }

        public static string Log { get { return _log; } }
    }
}

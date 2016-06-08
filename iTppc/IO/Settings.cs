using iTppc.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTppc.IO
{
    public class Settings : IniFile
    {
        public Settings(string filename) : base(filename)
        {
            Username = readValue("Credentials", "Username", _filename);
            Password = readValue("Credentials", "Password", _filename);
            ClickLower = getDouble(readValue("Authenticity", "ClickLower", _filename), 4.0);
            ClickUpper = getDouble(readValue("Authenticity", "ClickUpper", _filename), 7.0);
            WaitLower = getInt(readValue("Authenticity", "WaitLower", _filename), 5);
            WaitUpper = getInt(readValue("Authenticity", "WaitUpper", _filename), 10);
            IntervalLower = getInt(readValue("Authenticity", "IntervalLower", _filename), 60);
            IntervalUpper = getInt(readValue("Authenticity", "IntervalUpper", _filename), 75);
            PokemonNumber = getInt(readValue("BattleSettings", "PokemonNumber", _filename), 1);
            PokemonLevel = getInt(readValue("BattleSettings", "PokemonLevel", _filename), 2499);
            ShowMessage = getBool(readValue("BattleSettings", "ShowMessage", _filename), false);
            PlaySound = getBool(readValue("BattleSettings", "PlaySound", _filename), false);
        }

        private Double getDouble(string val, Double def)
        {
            Double ret = def;
            Double.TryParse(val, out ret);

            return ret;
        }

        private int getInt(string val, int def)
        {
            int ret = def;
            Int32.TryParse(val, out ret);

            return ret;
        }

        private DateTime getDateTime(string val, DateTime def)
        {
            DateTime ret = def;
            DateTime.TryParse(val, out ret);

            return new DateTime(1753, 1, 1, ret.Hour, ret.Minute, 0);
        }

        private bool getBool(string val, bool def)
        {
            bool ret = def;
            Boolean.TryParse(val, out ret);

            return ret;
        }

        public void save()
        {
            writeValue("Credentials", "Username", Username, _filename);
            writeValue("Credentials", "Password", Password, _filename);
            writeValue("Authenticity", "ClickLower", ClickLower.ToDecimalString(), _filename);
            writeValue("Authenticity", "ClickUpper", ClickUpper.ToDecimalString(), _filename);
            writeValue("Authenticity", "WaitLower", WaitLower.ToString(), _filename);
            writeValue("Authenticity", "WaitUpper", WaitUpper.ToString(), _filename);
            writeValue("Authenticity", "IntervalUpper", IntervalUpper.ToString(), _filename);
            writeValue("Authenticity", "IntervalLower", IntervalLower.ToString(), _filename);
            writeValue("BattleSettings", "PokemonNumber", PokemonNumber.ToString(), _filename);
            writeValue("BattleSettings", "PokemonLevel", PokemonLevel.ToString(), _filename);
            writeValue("BattleSettings", "ShowMessage", ShowMessage.ToString(), _filename);
            writeValue("BattleSettings", "PlaySound", PlaySound.ToString(), _filename);
        }

        public string Username { get; set; }

        public string Password { get; set; }

        public double ClickLower { get; set; }

        public double ClickUpper { get; set; }

        public int WaitLower { get; set; }

        public int WaitUpper { get; set; }

        public int IntervalUpper { get; set; }

        public int IntervalLower { get; set; }

        public int PokemonNumber { get; set; }

        public int PokemonLevel { get; set; }

        public bool ShowMessage { get; set; }

        public bool PlaySound { get; set; }
    }
}

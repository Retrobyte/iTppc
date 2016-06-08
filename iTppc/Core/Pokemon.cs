using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTppc.Core
{
    public class Pokemon
    {
        public Pokemon(string id, string name, string level, string item, Image image)
        {
            Id = id;
            Name = name;
            Level = level;
            Item = string.IsNullOrEmpty(item) ? "None" : item;
            Image = image;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Level { get; set; }

        public string Item { get; private set; }

        public Image Image { get; private set; }
    }
}

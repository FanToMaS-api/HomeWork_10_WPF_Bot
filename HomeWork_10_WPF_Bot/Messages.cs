using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeWork_10_WPF_Bot
{
    public class Messages
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Time { get; set; }
        public string Text { get; set; }
        public Messages(string _name, string _text, long _id, string _date)
        {
            Id = _id;
            Name = _name;
            Time = _date;
            Text = _text;
        }
    }
}

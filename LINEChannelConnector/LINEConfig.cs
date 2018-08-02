using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LINEChannelConnector
{
    public class LINEConfig
    {
        public string ChannelSecret { get; set; }
        public string ChannelAccessToken { get; set; }
        public string Uri { get; set; } = "https://api.line.me/v2";
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PushNinja.Models
{
    public class DeviceTokenResultViewModel
    {
        public string device { get; set; }
        public string token { get; set; }
        public string response { get; set; }
        public string data { get; set; }
    }

    public class NotificationResultViewModel
    {
        public int id { get; set; }
        public string pushContent { get; set; }
        public List<DeviceTokenResultViewModel> deviceTokens { get; set; }

    }
}
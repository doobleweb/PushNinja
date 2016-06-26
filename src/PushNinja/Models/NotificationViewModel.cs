using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PushNinja.Models
{
    public class NotificationViewModel
    {
        public string Json { get; set; }
        public List<string> AndroidRegistrationIds { get; set; }
        public List<string> AppleDeviceTokens { get; set; }
        public bool IsTest { get; set; }
    }
}
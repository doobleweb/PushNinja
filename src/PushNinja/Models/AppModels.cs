using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PushNinja.Models
{
    public enum Device : int
    {
        Android = 0,
        iOS = 1
    }

    public class App
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] AppleCertificate { get; set; }
        public string AppleCertificatePassword { get; set; }
        public string GcmAuthorizationToken { get; set; }
        public string UserId { get; set; }
        public string AppToken { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }
        public virtual int AppId { get; set; }
        public string Json { get; set; }
        public DateTime CreateDate { get; set; }
        public string Stack { get; set; }
        public bool IsTest { get; set; }
        public virtual App App { get; set; }
        public virtual ICollection<NotificationDeviceToken> DeviceTokens { get; set; }
    }

    public class NotificationDeviceToken
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public string DeviceToken { get; set; }
        public Device Device { get; set; }
        public string ResponseStatus { get; set; }
        public string ResponseData { get; set; }
        public virtual Notification Notification { get; set; }
    }
}
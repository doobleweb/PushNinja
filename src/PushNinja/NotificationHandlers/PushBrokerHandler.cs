using Newtonsoft.Json.Linq;
using PushNinja.Models;
using PushSharp;
using PushSharp.Android;
using PushSharp.Apple;
using PushSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PushNinja
{
    public class PushBrokerHandler 
    {
        public List<PushBroker> Brokers { get; set; }
        public NotificationLogger Logger { get; set; }
        public bool Finish { get; set; }
        public PushBrokerHandler()
        {

            Finish = false;
            Brokers = new List<PushBroker>();
            _notification = new PushNinja.Models.Notification();

            // Set PushSharp Log
            Logger = new NotificationLogger();
            Log.Logger = Logger;
            Log.Level = LogLevel.Debug;

        }
        PushNinja.Models.Notification _notification;
        public async Task<NotificationResultViewModel> Send()
        {            
            foreach (var broker in Brokers)
            {
                broker.StopAllServices(true);
                Thread.Sleep(1000);
            }

            var sb = new StringBuilder(_notification.Stack);            
            sb.AppendLine("** InternalLog");
            var sent = Logger.Log.Count(p => p.Message.Equals("NotificationSent"));
            var fail = Logger.Log.Count(p => p.Message.Equals("NotificationFailed"));
            var expired = Logger.Log.Count(p => p.Message.Equals("DeviceSubscriptionExpired"));
            var changed = Logger.Log.Count(p => p.Message.Equals("DeviceSubscriptionChanged"));
            sb.AppendLine(string.Format("NotificationSent: {0}", sent));
            sb.AppendLine(string.Format("NotificationFailed: {0}", fail));
            sb.AppendLine(string.Format("DeviceSubscriptionExpired: {0}", expired));
            sb.AppendLine(string.Format("DeviceSubscriptionChanged: {0}", changed));
#if DEBUG
            Logger.Log.OrderBy(l => l.Time).ToList().ForEach((item) =>
            {   
                System.Diagnostics.Debug.WriteLine(string.Format("{0}:{1}", DateTime.Now.ToString("hh.mm.ss.ffffff"), item.Message));
            });
#endif
            _notification.Stack = sb.ToString();

            Finish = true;

            using (var _db = new ApplicationDbContext())
            {
                _db.Notifications.Add(_notification);
                await _db.SaveChangesAsync();
                return new NotificationResultViewModel
                {
                    id = _notification.Id,
                    pushContent = _notification.Json,
                    deviceTokens = _notification.DeviceTokens.ToList()
                        .Select(d => new DeviceTokenResultViewModel
                        {
                            device = d.Device == Device.Android ? "Android" : "iOS",
                            token = d.DeviceToken,
                            response = d.ResponseStatus,
                            data = d.ResponseData
                        }).ToList()
                };

            }


        }

        public void AddToQueue(string token, NotificationViewModel model, bool isTest)
        {            
            lock (_notification)
            {
                _notification.IsTest = isTest;
                byte[] appleCertificate;
                string appleCertificatePassword;
                string gcmAuthorizationToken;
                using (var db = new ApplicationDbContext())
                {
                    var app = db.Apps.SingleOrDefault(p => p.AppToken == token);
                    if (app == null) throw new UnauthorizedAccessException();
                    _notification.AppId = app.Id;
                    gcmAuthorizationToken = app.GcmAuthorizationToken;
                    appleCertificate = app.AppleCertificate;
                    appleCertificatePassword = app.AppleCertificatePassword;
                }

                _notification.Json = model.Json;
                _notification.DeviceTokens = new List<NotificationDeviceToken>();
                _notification.CreateDate = DateTime.Now;

                foreach (var registrationId in model.AndroidRegistrationIds)
                    _notification.DeviceTokens.Add(new NotificationDeviceToken()
                    {
                        DeviceToken = registrationId,
                        Device = Device.Android
                    });


                foreach (var deviceToken in model.AppleDeviceTokens)
                    _notification.DeviceTokens.Add(new NotificationDeviceToken()
                    {
                        DeviceToken = deviceToken,
                        Device = Device.iOS
                    });

                if (gcmAuthorizationToken != null && _notification.DeviceTokens.Where(p => p.Device == Device.Android).Count() > 0)
                {
                    var itemsPerBulk = 1000;
                    var totalAndroidTokens = _notification.DeviceTokens
                                .Where(p => p.Device == Device.Android).Count();
                    var bulks = totalAndroidTokens <= itemsPerBulk ? 1 : Convert.ToInt32(Math.Ceiling((double)totalAndroidTokens / itemsPerBulk));
                    for (var bulk = 0; bulk < bulks; bulk++)
                    {
                        var broker = new PushBroker();
                        SetEvents(broker);
                        broker.RegisterGcmService(new GcmPushChannelSettings(gcmAuthorizationToken), new PushServiceSettings() { NotificationSendTimeout = 60000 });
                        var tokens = _notification.DeviceTokens
                                    .Where(p => p.Device == Device.Android).Skip(itemsPerBulk * bulk).Take(itemsPerBulk)
                                    .Select(p => p.DeviceToken).ToList();
#if DEBUG
                        // Set test mode, when google will return a response without really send the notifications
                        broker.QueueNotification(new GcmNotification()
                                .ForDeviceRegistrationId(tokens)
                                .WithJson(_notification.Json)
                                .WithDryRun());
#else
                    if (isTest)
                        broker.QueueNotification(new GcmNotification()
                                .ForDeviceRegistrationId(tokens)
                                .WithJson(_notification.Json)
                                .WithDryRun());                           
                    else
                        broker.QueueNotification(new GcmNotification()
                                .ForDeviceRegistrationId(tokens)
                                .WithJson(_notification.Json));                           
#endif
                        Brokers.Add(broker);

                    }
                }
                if (appleCertificate != null && _notification.DeviceTokens.Where(p => p.Device == Device.iOS).Count() > 0)
                {
                    var broker = new PushBroker();
                    SetEvents(broker);
                    broker.RegisterAppleService(new ApplePushChannelSettings(appleCertificate, appleCertificatePassword));

                    foreach (var deviceToken in _notification.DeviceTokens.Where(p => p.Device == Device.iOS).Select(d => d.DeviceToken))
                    {
                        broker.QueueNotification(new AppleNotification()
                                .ForDeviceToken(deviceToken)
                                .WithAlert(JObject.Parse(_notification.Json)["message"].Value<string>())
                                .WithSound("sound.caf")
                                .WithCustomItem("data", _notification.Json));
                    }
                    Brokers.Add(broker);
                }
            }
        }

        private void SetEvents(PushBroker broker)
        {
            broker.OnNotificationSent += NotificationSent;
            broker.OnNotificationFailed += NotificationFailed;
            broker.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
            broker.OnDeviceSubscriptionChanged += DeviceSubscriptionChanged;
            broker.OnChannelException += ChannelException;
            broker.OnServiceException += ServiceException;
            broker.OnChannelCreated += ChannelCreated;
            broker.OnChannelDestroyed += ChannelDestroyed;
        }
        public void ChannelDestroyed(object sender)
        {
            lock(_notification) {            
                var sb = new StringBuilder(_notification.Stack);                
                sb.AppendLine(string.Format("** ChannelDestroyed ({0}) {1}", DateTime.Now.ToString("hh.mm.ss.ffffff"), sender));
                var sent = _notification.DeviceTokens.Count(p => p.ResponseStatus.Equals("SENT"));
                var fail = _notification.DeviceTokens.Count(p => p.ResponseStatus.Equals("NOTIFICATION_FAILED"));
                var expired = _notification.DeviceTokens.Count(p => p.ResponseStatus.Equals("DEVICE_SUBSCRIPTION_EXPIRED"));
                var changed = _notification.DeviceTokens.Count(p => p.ResponseStatus.Equals("DEVICE_SUBSCRIPTION_CHANGED"));                
                sb.AppendLine(string.Format("SENT: {0}", sent));
                sb.AppendLine(string.Format("FAIL: {0}", fail));
                sb.AppendLine(string.Format("EXPIRED: {0}", expired));
                sb.AppendLine(string.Format("CHANGED: {0}", changed));                
                _notification.Stack = sb.ToString();
                Finish = true;
            }
        }

        public void ChannelCreated(object sender, IPushChannel pushChannel) 
        {
            lock (_notification)
            {
                var sb = new StringBuilder(_notification.Stack);                
                sb.AppendLine(string.Format("** ChannelCreated ({0}) {1}", DateTime.Now.ToString("hh.mm.ss.ffffff"), sender));                
                _notification.Stack = sb.ToString();
                Finish = true;
            }
        }
        public void ServiceException(object sender, Exception error) 
        {
            lock (_notification)
            {
                var sb = new StringBuilder(_notification.Stack);                
                sb.AppendLine(string.Format("** ServiceException ({0})", DateTime.Now.ToString("hh.mm.ss.ffffff")));
                sb.AppendLine(error.ToString());
                sb.AppendLine(error.StackTrace);                
                _notification.Stack = sb.ToString();
                Finish = true;
            }
        }
        public void ChannelException(object sender, IPushChannel pushChannel, Exception error) 
        {
            lock (_notification)
            {
                var sb = new StringBuilder(_notification.Stack);                
                sb.AppendLine(string.Format("** ChannelException ({0})", DateTime.Now.ToString("hh.mm.ss.ffffff")));
                sb.AppendLine(error.ToString());
                sb.AppendLine(error.StackTrace);                
                _notification.Stack = sb.ToString();
                Finish = true;
            }
        }
        public void DeviceSubscriptionChanged(object sender, string oldSubscriptionId, string newSubscriptionId, INotification notification)
        {
            //Currently this event will only ever happen for Android GCM
            setNotificationDeviceResponse(notification, "DEVICE_SUBSCRIPTION_CHANGED", newSubscriptionId);
        }
        public void NotificationSent(object sender, INotification notification)
        {
            setNotificationDeviceResponse(notification, "SENT", null);
        }
        public void NotificationFailed(object sender, INotification notification, Exception notificationFailureException)
        {
            if (notificationFailureException is PushSharp.Apple.NotificationFailureException)
            {
                var ex = (PushSharp.Apple.NotificationFailureException)notificationFailureException;
                setNotificationDeviceResponse(notification, "NOTIFICATION_FAILED", string.Format("\"code\": {0},\"description\":{1}", ex.ErrorStatusCode, ex.ErrorStatusDescription));
            }
            else
                setNotificationDeviceResponse(notification, "NOTIFICATION_FAILED", string.Format("{0}\n{1}", notificationFailureException.Message, notificationFailureException.StackTrace));
        }
        public void DeviceSubscriptionExpired(object sender, string expiredDeviceSubscriptionId, DateTime timestamp, INotification notification)
        {
            setNotificationDeviceResponse(notification, "DEVICE_SUBSCRIPTION_EXPIRED", expiredDeviceSubscriptionId);
        }
        private void setNotificationDeviceResponse(INotification notification, string message, string data = "")
        {
            var registrationId = string.Empty;
            if (notification is GcmNotification)
                registrationId = ((GcmNotification)notification).RegistrationIds.First();
            else if (notification is AppleNotification)
                registrationId = ((AppleNotification)notification).DeviceToken;
            lock (_notification)
            {
                var dbDevice = _notification.DeviceTokens.First(d => d.DeviceToken.Equals(registrationId));
                dbDevice.ResponseData = data;
                dbDevice.ResponseStatus = message;
            }
        }        
    }
}
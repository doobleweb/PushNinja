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
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace PushNinja.Controllers
{
    public class NotificationsController : ApiController
    {   
        // POST api/Notifications
        [ResponseType(typeof(NotificationResultViewModel))]
        public async Task<IHttpActionResult> PostNotification(NotificationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var appToken = Request.Headers.GetValues("X-APP-TOKEN").First();
                var brokerHandler = new PushBrokerHandler();
                brokerHandler.AddToQueue(appToken, model, model.IsTest);
                var result = await brokerHandler.Send();             
                return CreatedAtRoute("DefaultApi", new { id = result.id }, result);
            } 
            catch(UnauthorizedAccessException)
            {
                return Unauthorized();
            }            
        }
    }
}
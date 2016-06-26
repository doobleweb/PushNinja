using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PushNinja.Models;
using System.IO;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;

namespace PushNinja.Controllers
{
    [Authorize]
    public class AppsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: /Apps/
        public async Task<ActionResult> Index()
        {
            var apps = db.Apps;
            return View(await apps.ToListAsync());
        }

        // GET: /Apps/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Apps/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Name,AppleCertificatePassword,GcmAuthorizationToken")] App app, HttpPostedFileBase appleCertificate)
        {
            if (ModelState.IsValid)
            {
                app.UserId = User.Identity.GetUserId();
                app.AppToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                if (appleCertificate != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        appleCertificate.InputStream.CopyTo(memoryStream);
                        app.AppleCertificate = memoryStream.ToArray();
                    }
                }

                db.Apps.Add(app);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(app);
        }

        // GET: /Apps/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            App app = await db.Apps.FindAsync(id);
            if (app == null)
            {
                return HttpNotFound();
            }
            ViewBag.UserId = new SelectList(db.Users, "Id", "UserName", app.UserId);
            return View(app);
        }

        // POST: /Apps/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,AppleCertificatePassword,GcmAuthorizationToken")] App app, HttpPostedFileBase appleCertificate)
        {
            if (ModelState.IsValid)
            {
                var dbApp = db.Apps.SingleOrDefault(p => p.Id == app.Id);
                dbApp.Name = app.Name;
                dbApp.GcmAuthorizationToken = app.GcmAuthorizationToken;
                dbApp.AppleCertificatePassword = app.AppleCertificatePassword;

                if (appleCertificate != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        appleCertificate.InputStream.CopyTo(memoryStream);
                        dbApp.AppleCertificate = memoryStream.ToArray();
                    }
                }

                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(app);
        }

        // GET: /Apps/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            App app = await db.Apps.FindAsync(id);
            if (app == null)
            {
                return HttpNotFound();
            }
            return View(app);
        }

        // POST: /Apps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            App app = await db.Apps.FindAsync(id);
            db.Apps.Remove(app);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

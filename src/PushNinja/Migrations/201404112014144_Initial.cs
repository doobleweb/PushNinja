namespace PushNinja.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Apps",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        AppleCertificate = c.Binary(),
                        GcmAuthorizationToken = c.String(),
                        UserId = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.NotificationDeviceTokens",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        NotificationId = c.Int(nullable: false),
                        DeviceToken = c.String(),
                        Device = c.Int(nullable: false),
                        ResponseStatus = c.String(nullable: true)
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Notifications", t => t.NotificationId, cascadeDelete: true)
                .Index(t => t.NotificationId);
            
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AppId = c.Int(nullable: false),
                        Json = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Apps", t => t.AppId, cascadeDelete: true)
                .Index(t => t.AppId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.NotificationDeviceTokens", "NotificationId", "dbo.Notifications");
            DropForeignKey("dbo.Notifications", "AppId", "dbo.Apps");
            DropForeignKey("dbo.Apps", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.NotificationDeviceTokens", new[] { "NotificationId" });
            DropIndex("dbo.Notifications", new[] { "AppId" });
            DropIndex("dbo.Apps", new[] { "UserId" });
            DropTable("dbo.Notifications");
            DropTable("dbo.NotificationDeviceTokens");
            DropTable("dbo.Apps");
        }
    }
}

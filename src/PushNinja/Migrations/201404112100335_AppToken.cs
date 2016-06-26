namespace PushNinja.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AppToken : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Apps", "AppToken", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Apps", "AppToken");
        }
    }
}

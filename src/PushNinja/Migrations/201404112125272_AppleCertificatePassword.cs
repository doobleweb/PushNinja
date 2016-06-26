namespace PushNinja.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AppleCertificatePassword : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Apps", "AppleCertificatePassword", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Apps", "AppleCertificatePassword");
        }
    }
}

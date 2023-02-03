using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TMS.API.Models
{
    public partial class TMSContext : DbContext
    {
        public TMSContext()
        {
        }

        public TMSContext(DbContextOptions<TMSContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Allotment> Allotment { get; set; }
        public virtual DbSet<ApprovalConfig> ApprovalConfig { get; set; }
        public virtual DbSet<Approvement> Approvement { get; set; }
        public virtual DbSet<BankAccount> BankAccount { get; set; }
        public virtual DbSet<Booking> Booking { get; set; }
        public virtual DbSet<BookingList> BookingList { get; set; }
        public virtual DbSet<BrandShip> BrandShip { get; set; }
        public virtual DbSet<CheckFeeHistory> CheckFeeHistory { get; set; }
        public virtual DbSet<CommodityValue> CommodityValue { get; set; }
        public virtual DbSet<Component> Component { get; set; }
        public virtual DbSet<ComponentGroup> ComponentGroup { get; set; }
        public virtual DbSet<Dictionary> Dictionary { get; set; }
        public virtual DbSet<Entity> Entity { get; set; }
        public virtual DbSet<EntityRef> EntityRef { get; set; }
        public virtual DbSet<ErrorLog> ErrorLog { get; set; }
        public virtual DbSet<Expense> Expense { get; set; }
        public virtual DbSet<Feature> Feature { get; set; }
        public virtual DbSet<FeaturePolicy> FeaturePolicy { get; set; }
        public virtual DbSet<FileUpload> FileUpload { get; set; }
        public virtual DbSet<FreightRate> FreightRate { get; set; }
        public virtual DbSet<GridPolicy> GridPolicy { get; set; }
        public virtual DbSet<ImageEntity> ImageEntity { get; set; }
        public virtual DbSet<InsuranceFeesRate> InsuranceFeesRate { get; set; }
        public virtual DbSet<InsurancePremium> InsurancePremium { get; set; }
        public virtual DbSet<Ledger> Ledger { get; set; }
        public virtual DbSet<LedgerService> LedgerService { get; set; }
        public virtual DbSet<Location> Location { get; set; }
        public virtual DbSet<LocationService> LocationService { get; set; }
        public virtual DbSet<MasterData> MasterData { get; set; }
        public virtual DbSet<Quotation> Quotation { get; set; }
        public virtual DbSet<QuotationExpense> QuotationExpense { get; set; }
        public virtual DbSet<QuotationExpenseRoute> QuotationExpenseRoute { get; set; }
        public virtual DbSet<QuotationService> QuotationService { get; set; }
        public virtual DbSet<QuotationUpdate> QuotationUpdate { get; set; }
        public virtual DbSet<ReturnPlan> ReturnPlan { get; set; }
        public virtual DbSet<Revenue> Revenue { get; set; }
        public virtual DbSet<Role> Role { get; set; }
        public virtual DbSet<Route> Route { get; set; }
        public virtual DbSet<RouteUser> RouteUser { get; set; }
        public virtual DbSet<Services> Services { get; set; }
        public virtual DbSet<SettingPolicy> SettingPolicy { get; set; }
        public virtual DbSet<SettingPolicyDetail> SettingPolicyDetail { get; set; }
        public virtual DbSet<SettingTransportation> SettingTransportation { get; set; }
        public virtual DbSet<Ship> Ship { get; set; }
        public virtual DbSet<TaskNotification> TaskNotification { get; set; }
        public virtual DbSet<Teus> Teus { get; set; }
        public virtual DbSet<Transportation> Transportation { get; set; }
        public virtual DbSet<TransportationContract> TransportationContract { get; set; }
        public virtual DbSet<TransportationPlan> TransportationPlan { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<UserClick> UserClick { get; set; }
        public virtual DbSet<UserLogin> UserLogin { get; set; }
        public virtual DbSet<UserRole> UserRole { get; set; }
        public virtual DbSet<UserRoute> UserRoute { get; set; }
        public virtual DbSet<UserSetting> UserSetting { get; set; }
        public virtual DbSet<Vendor> Vendor { get; set; }
        public virtual DbSet<VendorContact> VendorContact { get; set; }
        public virtual DbSet<VendorLocation> VendorLocation { get; set; }
        public virtual DbSet<VendorService> VendorService { get; set; }
        public virtual DbSet<Webhook> Webhook { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Allotment>(entity =>
            {
                entity.Property(e => e.Notes).HasMaxLength(250);

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<ApprovalConfig>(entity =>
            {
                entity.Property(e => e.DataSource).HasMaxLength(200);

                entity.Property(e => e.Description).HasMaxLength(50);

                entity.Property(e => e.MaxAmount).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.MinAmount).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<Approvement>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LevelName).HasMaxLength(50);

                entity.Property(e => e.ReasonOfChange).HasMaxLength(200);
            });

            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.Property(e => e.BankNo)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.Name).HasMaxLength(250);
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(e => e.BookingNo)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.Files).HasMaxLength(500);

                entity.Property(e => e.Note).HasMaxLength(250);

                entity.Property(e => e.Note1).HasMaxLength(250);

                entity.Property(e => e.Note2).HasMaxLength(250);

                entity.Property(e => e.Note3).HasMaxLength(250);

                entity.Property(e => e.Note4).HasMaxLength(250);

                entity.Property(e => e.Teus20).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus20Remain).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus20Using).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus40).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus40Remain).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus40Using).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Trip)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.Ship)
                    .WithMany(p => p.Booking)
                    .HasForeignKey(d => d.ShipId)
                    .HasConstraintName("FK_Booking_Ship");
            });

            modelBuilder.Entity<BookingList>(entity =>
            {
                entity.Property(e => e.ActShipPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasComputedColumnSql("(case when [ShipPrice]>(0) then [ShipPrice] else [ShipUnitPrice] end)", false)
                    .HasComment("");

                entity.Property(e => e.InvNo).HasMaxLength(250);

                entity.Property(e => e.Note).HasMaxLength(250);

                entity.Property(e => e.Note1).HasMaxLength(250);

                entity.Property(e => e.OrtherFeePrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ShipPolicyPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ShipPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ShipUnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalFee)
                    .HasColumnType("decimal(32, 5)")
                    .HasComputedColumnSql("([ShipUnitPrice]*[Count]+isnull([OrtherFeePrice],(0)))", false);

                entity.Property(e => e.TotalPrice)
                    .HasColumnType("decimal(31, 5)")
                    .HasComputedColumnSql("([ShipUnitPrice]*[Count])", false)
                    .HasComment("");

                entity.Property(e => e.Trip).HasMaxLength(250);
            });

            modelBuilder.Entity<BrandShip>(entity =>
            {
                entity.Property(e => e.Code).HasMaxLength(100);

                entity.Property(e => e.Name).HasMaxLength(250);

                entity.Property(e => e.OldCode).HasMaxLength(100);
            });

            modelBuilder.Entity<CommodityValue>(entity =>
            {
                entity.Property(e => e.Notes).HasMaxLength(250);

                entity.Property(e => e.TotalPrice).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<Component>(entity =>
            {
                entity.Property(e => e.CascadeField)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ChildStyle)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.ClassName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ComponentType)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DateTimeField)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DefaultVal).HasMaxLength(500);

                entity.Property(e => e.DescFieldName)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.DescValue).IsUnicode(false);

                entity.Property(e => e.DisabledExp).HasMaxLength(2000);

                entity.Property(e => e.Events)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.FieldName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FormatData).HasMaxLength(1500);

                entity.Property(e => e.FormatSumaryField).HasMaxLength(250);

                entity.Property(e => e.GroupBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.GroupEvent)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.GroupFormat).HasMaxLength(1500);

                entity.Property(e => e.HotKey)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Icon)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.IdField)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.IsDoubleLine).HasDefaultValueSql("((0))");

                entity.Property(e => e.Label).HasMaxLength(50);

                entity.Property(e => e.ListClass).HasMaxLength(250);

                entity.Property(e => e.OrderBySumary).HasMaxLength(250);

                entity.Property(e => e.PlainText).HasMaxLength(250);

                entity.Property(e => e.RefClass)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.RefName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Style)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.System)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.UpperCase).HasDefaultValueSql("((0))");

                entity.Property(e => e.Validation).HasMaxLength(1000);

                entity.Property(e => e.VirtualScroll).HasDefaultValueSql("((0))");

                entity.Property(e => e.Width)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.HasOne(d => d.ComponentGroup)
                    .WithMany(p => p.Component)
                    .HasForeignKey(d => d.ComponentGroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Component_ComponentGroup");

                entity.HasOne(d => d.Reference)
                    .WithMany(p => p.Component)
                    .HasForeignKey(d => d.ReferenceId)
                    .HasConstraintName("FK_Component_Entity");
            });

            modelBuilder.Entity<ComponentGroup>(entity =>
            {
                entity.Property(e => e.ClassName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Description).HasMaxLength(200);

                entity.Property(e => e.DisabledExp).HasMaxLength(2000);

                entity.Property(e => e.Events)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Icon).HasMaxLength(100);

                entity.Property(e => e.IsCollapsible).HasDefaultValueSql("((0))");

                entity.Property(e => e.Label).HasMaxLength(50);

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.Style)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.TabGroup)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Width)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.Feature)
                    .WithMany(p => p.ComponentGroup)
                    .HasForeignKey(d => d.FeatureId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ComponentGroup_Feature");

                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.InverseParent)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_ComponentGroup_ComponentGroup");
            });

            modelBuilder.Entity<Dictionary>(entity =>
            {
                entity.HasIndex(e => new { e.LangCode, e.Key }, "UQ_Dictionary_Key")
                    .IsUnique();

                entity.Property(e => e.Key).HasMaxLength(250);

                entity.Property(e => e.LangCode)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Entity>(entity =>
            {
                entity.Property(e => e.AliasFor)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Description).HasMaxLength(200);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.RefDetailClass)
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.RefListClass)
                    .HasMaxLength(150)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<EntityRef>(entity =>
            {
                entity.Property(e => e.FieldName).HasMaxLength(250);

                entity.Property(e => e.MenuText)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TargetFieldName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ViewClass)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.HasOne(d => d.Com)
                    .WithMany(p => p.EntityRef)
                    .HasForeignKey(d => d.ComId)
                    .HasConstraintName("FK_EntityRef_Component");
            });

            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.Property(e => e.Credit).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Note).HasMaxLength(1000);

                entity.Property(e => e.VendorAmount).HasColumnType("decimal(20, 5)");

                entity.HasOne(d => d.AccountableUser)
                    .WithMany(p => p.ErrorLog)
                    .HasForeignKey(d => d.AccountableUserId)
                    .HasConstraintName("FK_ErrorLog_User");

                entity.HasOne(d => d.AccountableVendor)
                    .WithMany(p => p.ErrorLog)
                    .HasForeignKey(d => d.AccountableVendorId)
                    .HasConstraintName("FK_ErrorLog_Vendor");
            });

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.Property(e => e.CommodityValue)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.CommodityValueNotes).HasMaxLength(250);

                entity.Property(e => e.Cont20).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Cont40).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ContainerNo).HasMaxLength(250);

                entity.Property(e => e.InsuranceFeeNotes).HasMaxLength(250);

                entity.Property(e => e.InsuranceFeeRate)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.Property(e => e.Quantity).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.SealNo).HasMaxLength(250);

                entity.Property(e => e.TotalPriceAfterTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPriceBeforeTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Trip)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Vat).HasColumnType("decimal(20, 5)");

                entity.HasOne(d => d.Allotment)
                    .WithMany(p => p.Expense)
                    .HasForeignKey(d => d.AllotmentId)
                    .HasConstraintName("FK_Expense_Allotment");

                entity.HasOne(d => d.Transportation)
                    .WithMany(p => p.Expense)
                    .HasForeignKey(d => d.TransportationId)
                    .HasConstraintName("FK_Expense_Transportation");
            });

            modelBuilder.Entity<Feature>(entity =>
            {
                entity.Property(e => e.ClassName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DataSource).HasMaxLength(2500);

                entity.Property(e => e.Description).HasMaxLength(500);

                entity.Property(e => e.Events)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.FeatureGroup).HasMaxLength(50);

                entity.Property(e => e.Icon).HasMaxLength(250);

                entity.Property(e => e.Label).HasMaxLength(100);

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.Property(e => e.RequireJS)
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.Style)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.StyleSheet).IsUnicode(false);

                entity.Property(e => e.ViewClass)
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.HasOne(d => d.Entity)
                    .WithMany(p => p.Feature)
                    .HasForeignKey(d => d.EntityId)
                    .HasConstraintName("FK_Feature_Entity");

                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.InverseParent)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_Feature_Parent");
            });

            modelBuilder.Entity<FeaturePolicy>(entity =>
            {
                entity.Property(e => e.Desc).HasMaxLength(200);

                entity.HasOne(d => d.Feature)
                    .WithMany(p => p.FeaturePolicy)
                    .HasForeignKey(d => d.FeatureId)
                    .HasConstraintName("FK_FeaturePolicy_Feature");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.FeaturePolicy)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_FeaturePolicy_Role");
            });

            modelBuilder.Entity<FileUpload>(entity =>
            {
                entity.Property(e => e.EntityName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FieldName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.FileName).HasMaxLength(200);

                entity.Property(e => e.FilePath).HasMaxLength(3000);
            });

            modelBuilder.Entity<FreightRate>(entity =>
            {
                entity.Property(e => e.GCContainerType).HasMaxLength(250);

                entity.Property(e => e.InsuranceFee)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.IsLocation).HasDefaultValueSql("((0))");

                entity.Property(e => e.Notes).HasMaxLength(250);

                entity.Property(e => e.OrtherUnitPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ProfitUnitPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.Reason).HasMaxLength(250);

                entity.Property(e => e.ReceivedCVCUnitPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReceivedReturnUnitPriceAVG)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReceivedReturnUnitPriceMax)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReceivedUnitPriceAVG)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReceivedUnitPriceMax)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReturnCVCUnitPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReturnUnitPriceAVG)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReturnUnitPriceMax)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ShipUnitPriceAVG)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ShipUnitPriceMax)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalPriceAVG)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalPriceMax)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.UnitPriceCont20)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.UnitPriceCont40)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.UnitPriceNoVatCont20)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.UnitPriceNoVatCont40)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.UnitPriceNoVatTon)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.UnitPriceTon)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<GridPolicy>(entity =>
            {
                entity.Property(e => e.CascadeField)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ChildStyle)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.ClassName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ComponentType)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DataSource)
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.DefaultVal).HasMaxLength(500);

                entity.Property(e => e.Description).HasMaxLength(100);

                entity.Property(e => e.DisabledExp).HasMaxLength(2000);

                entity.Property(e => e.Events)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FieldName).HasMaxLength(100);

                entity.Property(e => e.FilterTemplate)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.FormatCell).HasMaxLength(250);

                entity.Property(e => e.FormatExcell).HasMaxLength(200);

                entity.Property(e => e.GroupBy)
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.GroupName).HasMaxLength(50);

                entity.Property(e => e.Icon)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.IsSumary).HasDefaultValueSql("((0))");

                entity.Property(e => e.ListClass).HasMaxLength(250);

                entity.Property(e => e.MaxWidth)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.MinWidth)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.PlainText).HasMaxLength(250);

                entity.Property(e => e.RefClass)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.RefName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ShortDesc).HasMaxLength(50);

                entity.Property(e => e.Style)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Summary).HasMaxLength(50);

                entity.Property(e => e.System)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TextAlign)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.UpperCase).HasDefaultValueSql("((0))");

                entity.Property(e => e.Validation).HasMaxLength(1000);

                entity.Property(e => e.VirtualScroll).HasDefaultValueSql("((0))");

                entity.Property(e => e.Width)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.HasOne(d => d.Entity)
                    .WithMany(p => p.GridPolicyEntity)
                    .HasForeignKey(d => d.EntityId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GridPolicy_Entity");

                entity.HasOne(d => d.Feature)
                    .WithMany(p => p.GridPolicy)
                    .HasForeignKey(d => d.FeatureId)
                    .HasConstraintName("FK_GridPolicy_Feature");

                entity.HasOne(d => d.Reference)
                    .WithMany(p => p.GridPolicyReference)
                    .HasForeignKey(d => d.ReferenceId)
                    .HasConstraintName("FK_GridPolicy_RefEntity");
            });

            modelBuilder.Entity<ImageEntity>(entity =>
            {
                entity.Property(e => e.Link).HasMaxLength(250);

                entity.Property(e => e.Url).HasMaxLength(250);
            });

            modelBuilder.Entity<InsuranceFeesRate>(entity =>
            {
                entity.Property(e => e.IsSOC).HasDefaultValueSql("((0))");

                entity.Property(e => e.IsVAT).HasDefaultValueSql("((0))");

                entity.Property(e => e.Rate).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<Ledger>(entity =>
            {
                entity.Property(e => e.Address).HasMaxLength(250);

                entity.Property(e => e.Attach).HasMaxLength(250);

                entity.Property(e => e.Attachments).HasMaxLength(500);

                entity.Property(e => e.BankName).HasMaxLength(250);

                entity.Property(e => e.BankNo)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.BankUserName).HasMaxLength(250);

                entity.Property(e => e.BillNo)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ContainerNo).HasMaxLength(250);

                entity.Property(e => e.Credit)
                    .HasColumnType("decimal(38, 6)")
                    .HasComputedColumnSql("([OriginCredit]*[ExchangeRate])", false);

                entity.Property(e => e.Debit)
                    .HasColumnType("decimal(38, 6)")
                    .HasComputedColumnSql("([OriginDebit]*[ExchangeRate])", false);

                entity.Property(e => e.ExchangeRate).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.InvoiceNo)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Items).HasMaxLength(250);

                entity.Property(e => e.MakeUpPrice).HasColumnType("decimal(38, 5)");

                entity.Property(e => e.Note).HasMaxLength(1500);

                entity.Property(e => e.OriginCredit).HasColumnType("decimal(38, 5)");

                entity.Property(e => e.OriginDebit).HasColumnType("decimal(38, 5)");

                entity.Property(e => e.OriginMakeUpPrice).HasColumnType("decimal(38, 5)");

                entity.Property(e => e.OriginPriceAfterTax)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.OriginPriceBeforeTax)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.OriginRealTotalPrice)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.OriginReturnTotalPrice)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.OriginTotalPrice)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.OriginUnitPrice)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.OriginVatAmount)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.PriceAfterTax)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.PriceBeforeTax)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.SealNo).HasMaxLength(250);

                entity.Property(e => e.SerialNo)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TaxVendor).HasMaxLength(250);

                entity.Property(e => e.Taxcode)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Trip)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.Vat).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.VatAmount)
                    .HasColumnType("decimal(38, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.VatMakeUp).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<LedgerService>(entity =>
            {
                entity.HasOne(d => d.Invoice)
                    .WithMany(p => p.LedgerServiceInvoice)
                    .HasForeignKey(d => d.InvoiceId)
                    .HasConstraintName("FK_Ledger_LedgerService_Invoice");

                entity.HasOne(d => d.TargetInvoice)
                    .WithMany(p => p.LedgerServiceTargetInvoice)
                    .HasForeignKey(d => d.TargetInvoiceId)
                    .HasConstraintName("FK_Ledger_LedgerService_TargetInvoice");
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.Property(e => e.Description)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Description1)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Description2)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Description3)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Description4)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Description5)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.DescriptionEn).HasMaxLength(250);

                entity.Property(e => e.Name).HasMaxLength(250);
            });

            modelBuilder.Entity<LocationService>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(250);

                entity.HasOne(d => d.Location)
                    .WithMany(p => p.LocationService)
                    .HasForeignKey(d => d.LocationId)
                    .HasConstraintName("FK_LocationService_Location");
            });

            modelBuilder.Entity<MasterData>(entity =>
            {
                entity.Property(e => e.Additional).HasMaxLength(200);

                entity.Property(e => e.Code)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Description).UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Path)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.HasOne(d => d.Parent)
                    .WithMany(p => p.InverseParent)
                    .HasForeignKey(d => d.ParentId)
                    .HasConstraintName("FK_MasterData_MasterData");
            });

            modelBuilder.Entity<Quotation>(entity =>
            {
                entity.Property(e => e.Note).HasMaxLength(250);

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.UnitPrice1).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.UnitPrice2).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.UnitPrice3).HasColumnType("decimal(20, 5)");

                entity.HasOne(d => d.Location)
                    .WithMany(p => p.Quotation)
                    .HasForeignKey(d => d.LocationId)
                    .HasConstraintName("FK_Quotation_Location");

                entity.HasOne(d => d.QuotationUpdate)
                    .WithMany(p => p.Quotation)
                    .HasForeignKey(d => d.QuotationUpdateId)
                    .HasConstraintName("FK_Quotation_QuotationUpdate");
            });

            modelBuilder.Entity<QuotationExpense>(entity =>
            {
                entity.Property(e => e.DOUnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.VS20UnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.VS40UnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.VSC)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.HasOne(d => d.Quotation)
                    .WithMany(p => p.QuotationExpense)
                    .HasForeignKey(d => d.QuotationId)
                    .HasConstraintName("FK_QuotationExpense_Quotation");
            });

            modelBuilder.Entity<QuotationExpenseRoute>(entity =>
            {
                entity.HasOne(d => d.QuotationExpense)
                    .WithMany(p => p.QuotationExpenseRoute)
                    .HasForeignKey(d => d.QuotationExpenseId)
                    .HasConstraintName("FK_QuotationExpenseRoute_QuotationExpense");
            });

            modelBuilder.Entity<QuotationService>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(250);
            });

            modelBuilder.Entity<QuotationUpdate>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<ReturnPlan>(entity =>
            {
                entity.Property(e => e.Cont20).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Cont40).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ContNo).HasMaxLength(50);

                entity.Property(e => e.Dem).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Notes).HasMaxLength(250);

                entity.Property(e => e.NotifiNo).HasMaxLength(50);

                entity.Property(e => e.SealNo).HasMaxLength(50);

                entity.Property(e => e.Trip).HasMaxLength(50);
            });

            modelBuilder.Entity<Revenue>(entity =>
            {
                entity.Property(e => e.CollectOnBehaftPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.InvoinceNo).HasMaxLength(250);

                entity.Property(e => e.LotNo).HasMaxLength(250);

                entity.Property(e => e.Note).HasMaxLength(250);

                entity.Property(e => e.NotePayment).HasMaxLength(250);

                entity.Property(e => e.ReceivedPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPriceBeforTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.UnitPriceAfterTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.UnitPriceBeforeTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Vat).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.VatPrice).HasColumnType("decimal(20, 5)");

                entity.HasOne(d => d.Transportation)
                    .WithMany(p => p.Revenue)
                    .HasForeignKey(d => d.TransportationId)
                    .HasConstraintName("FK_Revenue_Transportation");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(100);

                entity.Property(e => e.Path)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.RoleName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.HasOne(d => d.ParentRole)
                    .WithMany(p => p.InverseParentRole)
                    .HasForeignKey(d => d.ParentRoleId)
                    .HasConstraintName("FK_Role_ParentRole");
            });

            modelBuilder.Entity<Route>(entity =>
            {
                entity.Property(e => e.Code).HasMaxLength(50);

                entity.Property(e => e.Name)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Used).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<RouteUser>(entity =>
            {
                entity.Property(e => e.Used).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<Services>(entity =>
            {
                entity.Property(e => e.CmdType)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Content).IsRequired();

                entity.Property(e => e.Environment)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Path).HasMaxLength(3000);
            });

            modelBuilder.Entity<SettingPolicy>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(250);

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<SettingPolicyDetail>(entity =>
            {
                entity.Property(e => e.Value).HasMaxLength(250);

                entity.HasOne(d => d.SettingPolicy)
                    .WithMany(p => p.SettingPolicyDetail)
                    .HasForeignKey(d => d.SettingPolicyId)
                    .HasConstraintName("FK_SettingPolicyDetail_SettingPolicy");
            });

            modelBuilder.Entity<Ship>(entity =>
            {
                entity.Property(e => e.Code).HasMaxLength(100);

                entity.Property(e => e.Name)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.OldCode).HasMaxLength(100);
            });

            modelBuilder.Entity<TaskNotification>(entity =>
            {
                entity.Property(e => e.Progress).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TimeConsumed).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TimeRemained).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Title).HasMaxLength(100);

                entity.HasOne(d => d.Entity)
                    .WithMany(p => p.TaskNotification)
                    .HasForeignKey(d => d.EntityId)
                    .HasConstraintName("FK_TaskNotification_Entity");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.TaskNotification)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_TaskNotification_Role");
            });

            modelBuilder.Entity<Teus>(entity =>
            {
                entity.Property(e => e.Note).HasMaxLength(250);

                entity.Property(e => e.Note1)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Note2).HasMaxLength(250);

                entity.Property(e => e.Note3).HasMaxLength(250);

                entity.Property(e => e.Note4).HasMaxLength(250);

                entity.Property(e => e.Teus20).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus20Remain).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus20Using).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus40).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus40Remain).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Teus40Using).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Trip)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Transportation>(entity =>
            {
                entity.HasIndex(e => new { e.ClosingDate, e.Active, e.ShipDate, e.RouteId }, "IX_Transportation");

                entity.Property(e => e.Bet).HasMaxLength(250);

                entity.Property(e => e.BetAmount).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.BetFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.BillNo)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.BossCheck).HasMaxLength(250);

                entity.Property(e => e.BossCheckUpload).HasMaxLength(250);

                entity.Property(e => e.BossReturnCheck).HasMaxLength(250);

                entity.Property(e => e.CheckFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingCombinationUnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingCombinationUnitPriceCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingCombinationUnitPriceReturnCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingCombinationUnitPriceUpload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingDriverId).HasMaxLength(250);

                entity.Property(e => e.ClosingFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingNotes).HasMaxLength(250);

                entity.Property(e => e.ClosingPercent).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingPercentCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingPercentReturnCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingPercentUpload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingTruckId).HasMaxLength(250);

                entity.Property(e => e.ClosingUnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ClosingUserId).HasMaxLength(250);

                entity.Property(e => e.CollectOnBehaftFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnBehaftFeeCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnBehaftFeeReturnCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnBehaftFeeUpload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnBehaftInvoinceNoFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnBehaftInvoinceNoFeeCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnBehaftInvoinceNoFeeReturnCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnBehaftInvoinceNoFeeUpload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnBehaftPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnSupPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.CollectOnSupPriceCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnSupPriceReturnCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CollectOnSupPriceUpload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CombinationFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CommodityValue).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CommodityValueNotes).HasMaxLength(250);

                entity.Property(e => e.Cont20).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Cont40).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ContainerNo).HasMaxLength(250);

                entity.Property(e => e.ContainerNoCheck).HasMaxLength(250);

                entity.Property(e => e.ContainerNoReturnCheck).HasMaxLength(250);

                entity.Property(e => e.ContainerNoUpload).HasMaxLength(250);

                entity.Property(e => e.Cp1).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Cp2).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.CustomerReturnFee)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.DeliveryBetNotes).HasMaxLength(250);

                entity.Property(e => e.Dem).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.DoVsLiftUnitPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.Fee1).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee1Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee2).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee2Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee3).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee3Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee4).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee4Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee5).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee5Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee6).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Fee6Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.FeeVat1).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.FeeVat1Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.FeeVat2).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.FeeVat2Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.FeeVat3).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.FeeVat3Upload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.FreeText).HasMaxLength(250);

                entity.Property(e => e.FreeText1).HasMaxLength(250);

                entity.Property(e => e.FreeText2).HasMaxLength(250);

                entity.Property(e => e.FreeText3).HasMaxLength(250);

                entity.Property(e => e.FreeText4).HasMaxLength(250);

                entity.Property(e => e.FreeText5).HasMaxLength(250);

                entity.Property(e => e.FreeText6).HasMaxLength(250);

                entity.Property(e => e.FreeText7).HasMaxLength(250);

                entity.Property(e => e.FreeText8).HasMaxLength(250);

                entity.Property(e => e.FreeText9).HasMaxLength(250);

                entity.Property(e => e.InsuranceFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.InsuranceFeeNoVAT).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.InsuranceFeeNotes).HasMaxLength(250);

                entity.Property(e => e.InsuranceFeeVAT).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.InsuranceFeesRate).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.InsuranceFeesRateVAT).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.InvoinceNo).HasMaxLength(500);

                entity.Property(e => e.LandingFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LandingFeeCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LandingFeeReturnCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LandingFeeUpload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LiftFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LiftFeeCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LiftFeeCheckUpload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LiftFeeReturnCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.LotNo).HasMaxLength(250);

                entity.Property(e => e.MonthText).HasMaxLength(50);

                entity.Property(e => e.Name).HasMaxLength(250);

                entity.Property(e => e.Note).HasMaxLength(250);

                entity.Property(e => e.Note1).HasMaxLength(250);

                entity.Property(e => e.Note2).HasMaxLength(250);

                entity.Property(e => e.Note3).HasMaxLength(250);

                entity.Property(e => e.Note4).HasMaxLength(250);

                entity.Property(e => e.NotePayment).HasMaxLength(250);

                entity.Property(e => e.NoteReturnReport).HasMaxLength(250);

                entity.Property(e => e.NoteReturnReport2).HasMaxLength(250);

                entity.Property(e => e.Notes).HasMaxLength(250);

                entity.Property(e => e.NotificationCount).HasMaxLength(250);

                entity.Property(e => e.OrtherFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.OrtherFeeInvoinceNo).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.PaymentNote).HasMaxLength(250);

                entity.Property(e => e.PaymentReturnNote).HasMaxLength(250);

                entity.Property(e => e.PickupEmptyCheck).HasMaxLength(250);

                entity.Property(e => e.PickupEmptyReturnCheck).HasMaxLength(250);

                entity.Property(e => e.PickupEmptyUpload).HasMaxLength(250);

                entity.Property(e => e.PortLoadingCheck).HasMaxLength(250);

                entity.Property(e => e.PortLoadingReturnCheck).HasMaxLength(250);

                entity.Property(e => e.PortLoadingUpload).HasMaxLength(250);

                entity.Property(e => e.ReasonUnLockAccountant)
                    .HasMaxLength(250)
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReasonUnLockAll).HasMaxLength(250);

                entity.Property(e => e.ReasonUnLockExploit).HasMaxLength(250);

                entity.Property(e => e.ReasonUnLockShip).HasMaxLength(250);

                entity.Property(e => e.ReceivedCheck).HasMaxLength(250);

                entity.Property(e => e.ReceivedCheckUpload).HasMaxLength(250);

                entity.Property(e => e.ReceivedPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReceivedReturnCheck).HasMaxLength(250);

                entity.Property(e => e.ReturnCheckFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnClosingFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnClosingFeeReport)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReturnCollectOnBehaftFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnCollectOnBehaftInvoinceFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnDo)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReturnDriverId).HasMaxLength(250);

                entity.Property(e => e.ReturnLiftFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnLiftFeeReport)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReturnNotes).HasMaxLength(250);

                entity.Property(e => e.ReturnNotes1).HasMaxLength(250);

                entity.Property(e => e.ReturnNotes2).HasMaxLength(250);

                entity.Property(e => e.ReturnOrtherFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnOrtherInvoinceFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnPlusFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnTotalFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnTruckId).HasMaxLength(250);

                entity.Property(e => e.ReturnUnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ReturnUserId).HasMaxLength(250);

                entity.Property(e => e.ReturnVs).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.SealCheck).HasMaxLength(250);

                entity.Property(e => e.SealCheckUpload).HasMaxLength(250);

                entity.Property(e => e.SealNo).HasMaxLength(250);

                entity.Property(e => e.SealReturnCheck).HasMaxLength(250);

                entity.Property(e => e.ShellDate).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ShipNotes).HasMaxLength(250);

                entity.Property(e => e.ShipPolicyPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ShipPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ShipRoses).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.ShipUnitPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.SplitBill).HasMaxLength(250);

                entity.Property(e => e.TotalBet)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalCOBNoTaxClosing)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalCOBNoTaxReturn)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalCOBTaxClosing)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalCOBTaxReturn)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalFee).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPriceAfterTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPriceAfterTaxCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPriceAfterTaxReturnCheck).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPriceAfterTaxUpload).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPriceBeforTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.TotalPriceNoTaxClosing)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalPriceNoTaxReturn)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalPriceTaxClosing)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalPriceTaxReturn)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalWeightReport).HasMaxLength(50);

                entity.Property(e => e.Trip)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.UnitPriceAfterTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.UnitPriceBeforeTax).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.Vat).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.VatPrice).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.WareHouseUnitPrice)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.Weight).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.YearText).HasMaxLength(50);
            });

            modelBuilder.Entity<TransportationContract>(entity =>
            {
                entity.Property(e => e.Code)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CompanyName).HasMaxLength(250);

                entity.Property(e => e.ContractName).HasMaxLength(250);

                entity.Property(e => e.ContractNo).HasMaxLength(250);

                entity.Property(e => e.Files).HasMaxLength(500);

                entity.Property(e => e.Notes).HasMaxLength(250);

                entity.Property(e => e.SystemNotes).HasMaxLength(250);

                entity.Property(e => e.TotalPrice).HasColumnType("decimal(20, 5)");
            });

            modelBuilder.Entity<TransportationPlan>(entity =>
            {
                entity.Property(e => e.CommodityValue)
                    .HasColumnType("decimal(20, 5)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.Files).HasMaxLength(250);

                entity.Property(e => e.Name).HasMaxLength(250);

                entity.Property(e => e.Notes).HasMaxLength(250);

                entity.Property(e => e.NotesContract).HasMaxLength(250);

                entity.Property(e => e.RequestChangeNote).HasMaxLength(250);

                entity.Property(e => e.ReturnNotes).HasMaxLength(500);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Address).HasMaxLength(200);

                entity.Property(e => e.Avatar).HasMaxLength(1000);

                entity.Property(e => e.ContactId).HasMaxLength(50);

                entity.Property(e => e.Email)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FirstName).HasMaxLength(50);

                entity.Property(e => e.FullName)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.LastName).HasMaxLength(100);

                entity.Property(e => e.MiddleName).HasMaxLength(100);

                entity.Property(e => e.Password).HasMaxLength(100);

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Recover)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Salt)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Ssn)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.UserName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.Vendor)
                    .WithMany(p => p.User)
                    .HasForeignKey(d => d.VendorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_Vendor");
            });

            modelBuilder.Entity<UserLogin>(entity =>
            {
                entity.Property(e => e.IpAddress)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.RefreshToken).HasMaxLength(50);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasOne(d => d.Role)
                    .WithMany(p => p.UserRole)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserRole_Role");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserRole)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserRole_User");
            });

            modelBuilder.Entity<UserRoute>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserRoute)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_UserRoute_User");
            });

            modelBuilder.Entity<UserSetting>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(200);

                entity.Property(e => e.Path)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.UserSetting)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_UserSetting_Role");
            });

            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.Property(e => e.Address).HasMaxLength(250);

                entity.Property(e => e.AddressReport).HasMaxLength(250);

                entity.Property(e => e.BankName).HasMaxLength(250);

                entity.Property(e => e.BankNo).HasMaxLength(250);

                entity.Property(e => e.CityName).HasMaxLength(250);

                entity.Property(e => e.ClassifyName).HasMaxLength(250);

                entity.Property(e => e.Code).HasMaxLength(250);

                entity.Property(e => e.CompanyName).HasMaxLength(250);

                entity.Property(e => e.DisplayName).HasMaxLength(250);

                entity.Property(e => e.Email).HasMaxLength(250);

                entity.Property(e => e.Logo).HasMaxLength(250);

                entity.Property(e => e.Name)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.NameReport)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.NameSys).HasMaxLength(250);

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(111)
                    .IsUnicode(false);

                entity.Property(e => e.PhoneNumberReport).HasMaxLength(250);

                entity.Property(e => e.PositionName).HasMaxLength(250);

                entity.Property(e => e.ReturnRate).HasColumnType("decimal(20, 5)");

                entity.Property(e => e.StaffName).HasMaxLength(250);

                entity.Property(e => e.TaxCode).HasMaxLength(250);
            });

            modelBuilder.Entity<VendorContact>(entity =>
            {
                entity.Property(e => e.ContactName)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.ContactPhoneNumber)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.ContactUser)
                    .HasMaxLength(250)
                    .UseCollation("SQL_Latin1_General_CP1_CI_AI");

                entity.Property(e => e.Note).HasMaxLength(250);

                entity.HasOne(d => d.Boss)
                    .WithMany(p => p.VendorContact)
                    .HasForeignKey(d => d.BossId)
                    .HasConstraintName("FK_VendorContact_Vendor");

                entity.HasOne(d => d.Location)
                    .WithMany(p => p.VendorContact)
                    .HasForeignKey(d => d.LocationId)
                    .HasConstraintName("FK_VendorContact_VendorContact");
            });

            modelBuilder.Entity<VendorLocation>(entity =>
            {
                entity.Property(e => e.ContactName).HasMaxLength(250);

                entity.Property(e => e.ContactName1).HasMaxLength(250);

                entity.HasOne(d => d.Location)
                    .WithMany(p => p.VendorLocation)
                    .HasForeignKey(d => d.LocationId)
                    .HasConstraintName("FK_VendorLocation_Location");

                entity.HasOne(d => d.Vendor)
                    .WithMany(p => p.VendorLocation)
                    .HasForeignKey(d => d.VendorId)
                    .HasConstraintName("FK_VendorLocation_Vendor");
            });

            modelBuilder.Entity<VendorService>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(250);

                entity.HasOne(d => d.Vendor)
                    .WithMany(p => p.VendorService)
                    .HasForeignKey(d => d.VendorId)
                    .HasConstraintName("FK_VendorService_Vendor");
            });

            modelBuilder.Entity<Webhook>(entity =>
            {
                entity.Property(e => e.AccessTokenField)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.ApiKey)
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.ApiKeyHeader)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.LoginUrl)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.Method)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.PasswordKey)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.SubName).HasMaxLength(100);

                entity.Property(e => e.SubPassword)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.SubUrl).IsUnicode(false);

                entity.Property(e => e.SubUsername)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TokenPrefix)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.UsernameKey)
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

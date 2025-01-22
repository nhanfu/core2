using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.UIModels;

public partial class UIContext : DbContext
{
    public UIContext(DbContextOptions<UIContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountNo> AccountNo { get; set; }

    public virtual DbSet<AccountTransferSetup> AccountTransferSetup { get; set; }

    public virtual DbSet<AggregatedCounter> AggregatedCounter { get; set; }

    public virtual DbSet<ApprovalConfig> ApprovalConfig { get; set; }

    public virtual DbSet<Approvement> Approvement { get; set; }

    public virtual DbSet<Asset> Asset { get; set; }

    public virtual DbSet<Booking> Booking { get; set; }

    public virtual DbSet<BookingDetail> BookingDetail { get; set; }

    public virtual DbSet<ChatEntity> ChatEntity { get; set; }

    public virtual DbSet<Cluster> Cluster { get; set; }

    public virtual DbSet<Component> Component { get; set; }

    public virtual DbSet<ComponentDefaultValue> ComponentDefaultValue { get; set; }

    public virtual DbSet<Config> Config { get; set; }

    public virtual DbSet<ConfigUnique> ConfigUnique { get; set; }

    public virtual DbSet<Conversation> Conversation { get; set; }

    public virtual DbSet<ConversationDetail> ConversationDetail { get; set; }

    public virtual DbSet<Counter> Counter { get; set; }

    public virtual DbSet<DefaultFee> DefaultFee { get; set; }

    public virtual DbSet<Dictionary> Dictionary { get; set; }

    public virtual DbSet<EntityAttached> EntityAttached { get; set; }

    public virtual DbSet<EntityContainer> EntityContainer { get; set; }

    public virtual DbSet<EntityDimension> EntityDimension { get; set; }

    public virtual DbSet<EntityProcess> EntityProcess { get; set; }

    public virtual DbSet<EntityProcessDetail> EntityProcessDetail { get; set; }

    public virtual DbSet<EntityTransit> EntityTransit { get; set; }

    public virtual DbSet<Enum> Enum { get; set; }

    public virtual DbSet<ExchangeRate> ExchangeRate { get; set; }

    public virtual DbSet<Feature> Feature { get; set; }

    public virtual DbSet<FeaturePolicy> FeaturePolicy { get; set; }

    public virtual DbSet<FeeType> FeeType { get; set; }

    public virtual DbSet<FileUpload> FileUpload { get; set; }

    public virtual DbSet<Hash> Hash { get; set; }

    public virtual DbSet<History> History { get; set; }

    public virtual DbSet<Inquiry> Inquiry { get; set; }

    public virtual DbSet<InquiryDetail> InquiryDetail { get; set; }

    public virtual DbSet<InvoiceConfig> InvoiceConfig { get; set; }

    public virtual DbSet<Job> Job { get; set; }

    public virtual DbSet<JobParameter> JobParameter { get; set; }

    public virtual DbSet<JobQueue> JobQueue { get; set; }

    public virtual DbSet<List> List { get; set; }

    public virtual DbSet<MarkupPricing> MarkupPricing { get; set; }

    public virtual DbSet<MasterData> MasterData { get; set; }

    public virtual DbSet<Partner> Partner { get; set; }

    public virtual DbSet<PartnerBankAccount> PartnerBankAccount { get; set; }

    public virtual DbSet<PartnerCare> PartnerCare { get; set; }

    public virtual DbSet<PartnerContact> PartnerContact { get; set; }

    public virtual DbSet<PartnerCreditPeriod> PartnerCreditPeriod { get; set; }

    public virtual DbSet<PlanEmail> PlanEmail { get; set; }

    public virtual DbSet<PlanEmailDetail> PlanEmailDetail { get; set; }

    public virtual DbSet<Pricing> Pricing { get; set; }

    public virtual DbSet<PricingDetail> PricingDetail { get; set; }

    public virtual DbSet<Role> Role { get; set; }

    public virtual DbSet<SaleFunction> SaleFunction { get; set; }

    public virtual DbSet<Schema> Schema { get; set; }

    public virtual DbSet<Server> Server { get; set; }

    public virtual DbSet<Services> Services { get; set; }

    public virtual DbSet<Set> Set { get; set; }

    public virtual DbSet<Shipment> Shipment { get; set; }

    public virtual DbSet<ShipmentDO> ShipmentDO { get; set; }

    public virtual DbSet<ShipmentFee> ShipmentFee { get; set; }

    public virtual DbSet<ShipmentFreight> ShipmentFreight { get; set; }

    public virtual DbSet<ShipmentInvoice> ShipmentInvoice { get; set; }

    public virtual DbSet<ShipmentInvoiceDetail> ShipmentInvoiceDetail { get; set; }

    public virtual DbSet<ShipmentSI> ShipmentSI { get; set; }

    public virtual DbSet<ShipmentTask> ShipmentTask { get; set; }

    public virtual DbSet<State> State { get; set; }

    public virtual DbSet<TableName> TableName { get; set; }

    public virtual DbSet<Tanent> Tanent { get; set; }

    public virtual DbSet<TaskNotification> TaskNotification { get; set; }

    public virtual DbSet<Terminal> Terminal { get; set; }

    public virtual DbSet<User> User { get; set; }

    public virtual DbSet<UserLogin> UserLogin { get; set; }

    public virtual DbSet<UserRole> UserRole { get; set; }

    public virtual DbSet<UserSetting> UserSetting { get; set; }

    public virtual DbSet<Voucher> Voucher { get; set; }

    public virtual DbSet<VoucherDetail> VoucherDetail { get; set; }

    public virtual DbSet<WebConfig> WebConfig { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountNo>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.NameEnglish).HasMaxLength(250);
            entity.Property(e => e.OgCurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AccountTransferSetup>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CompanyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.FromAccountNoId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ToAccountNoId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AggregatedCounter>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("PK_HangFire_CounterAggregated");

            entity.ToTable("AggregatedCounter", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_AggregatedCounter_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<ApprovalConfig>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NameId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleIds)
                .HasMaxLength(1500)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserIds)
                .HasMaxLength(1500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Approvement>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ReasonOfChange).HasMaxLength(250);
            entity.Property(e => e.RecordId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserApproveId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccAmoutId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccCostId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccDepId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccGroupId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Accumulated).HasColumnType("money");
            entity.Property(e => e.Amount).HasColumnType("money");
            entity.Property(e => e.AttachedFile).HasMaxLength(500);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CountryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DepAmount).HasColumnType("money");
            entity.Property(e => e.DepAmountMonth).HasColumnType("money");
            entity.Property(e => e.DepMonth).HasColumnType("money");
            entity.Property(e => e.DepartmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DepreciatedAmount).HasColumnType("money");
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DescriptionIdText).HasMaxLength(500);
            entity.Property(e => e.GroupCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.PartnerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReasonChange).HasMaxLength(255);
            entity.Property(e => e.RemainAmount).HasColumnType("money");
            entity.Property(e => e.RemainMonth).HasColumnType("money");
            entity.Property(e => e.TypeText).HasMaxLength(255);
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UseMonth).HasColumnType("money");
            entity.Property(e => e.WarrantyPeriod).HasColumnType("money");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("Booking_Seq");
                    tb.HasTrigger("Booking_Update");
                });

            entity.HasIndex(e => e.BookingNo, "UC_BookingNo").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AgentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AttachedFile).HasMaxLength(500);
            entity.Property(e => e.BookingLocalId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BookingNo).HasMaxLength(250);
            entity.Property(e => e.BookingNoteId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BookingOrderId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CBM).HasColumnType("money");
            entity.Property(e => e.CW).HasColumnType("money");
            entity.Property(e => e.CarrierId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CheckedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CommodityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConsigneeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConsigneeText).HasMaxLength(500);
            entity.Property(e => e.ContPickupAtId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContReturnAtId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContactStuffing).HasMaxLength(500);
            entity.Property(e => e.CustomerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomsId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
            entity.Property(e => e.DeliveryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DeliveryTerm).HasMaxLength(250);
            entity.Property(e => e.DemFree).HasColumnType("money");
            entity.Property(e => e.DetFree).HasColumnType("money");
            entity.Property(e => e.DetailsGoods).HasMaxLength(500);
            entity.Property(e => e.Dimension).HasMaxLength(500);
            entity.Property(e => e.DropOffAt).HasMaxLength(500);
            entity.Property(e => e.DropOffId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FileNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FinalDestinationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FlightNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FlightNoConnect)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FormatChat).HasMaxLength(500);
            entity.Property(e => e.ForwardId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FreightRate).HasColumnType("money");
            entity.Property(e => e.GW).HasColumnType("money");
            entity.Property(e => e.HblNo).HasMaxLength(250);
            entity.Property(e => e.HblTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IncotermId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Kgs).HasColumnType("money");
            entity.Property(e => e.MblNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MblTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.OceanVessel).HasMaxLength(500);
            entity.Property(e => e.OceanVesselVoy).HasMaxLength(500);
            entity.Property(e => e.PaymentTermId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber).HasMaxLength(250);
            entity.Property(e => e.Pic).HasMaxLength(500);
            entity.Property(e => e.PicVendor).HasMaxLength(500);
            entity.Property(e => e.PickupAddress).HasMaxLength(500);
            entity.Property(e => e.PickupAt).HasMaxLength(500);
            entity.Property(e => e.PickupId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlaceDeliveryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlaceReceiptId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlaceStuffingId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlaceStuffingText).HasMaxLength(500);
            entity.Property(e => e.PoNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PodId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PolId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PortTransitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.QuotationCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.QuotationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RateComfirm).HasColumnType("money");
            entity.Property(e => e.ReceiverIds)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.RefNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReferenceNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Remark).HasMaxLength(1500);
            entity.Property(e => e.SaleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceText).HasMaxLength(250);
            entity.Property(e => e.ShipmentDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipperId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipperText).HasMaxLength(500);
            entity.Property(e => e.SpecialRequest).HasMaxLength(500);
            entity.Property(e => e.TruckingTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TypeMoveId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.UserApprovedIds).HasMaxLength(500);
            entity.Property(e => e.UserViewIds).HasMaxLength(500);
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vessel).HasMaxLength(500);
            entity.Property(e => e.VesselVoy).HasMaxLength(500);
            entity.Property(e => e.Volume).HasColumnType("money");
            entity.Property(e => e.VolumeWeight).HasColumnType("money");
            entity.Property(e => e.WareHouseId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("tr_BookingDetail"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnType("money");
            entity.Property(e => e.AmountTax).HasColumnType("money");
            entity.Property(e => e.BookingId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.Flight).HasMaxLength(250);
            entity.Property(e => e.FromId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GroupFee).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(250);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.ToId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TotalAmount).HasColumnType("money");
            entity.Property(e => e.TotalAmountTax).HasColumnType("money");
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vat).HasColumnType("money");
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ChatEntity>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Avatar).HasMaxLength(400);
            entity.Property(e => e.FromId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Icon).HasMaxLength(400);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.RecordId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TextContent).HasMaxLength(500);
            entity.Property(e => e.ToId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Cluster>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ClusterRole)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Env)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Host)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Scheme)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.TenantCode).HasMaxLength(150);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Component>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("Component_Lock"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AddRowExp).HasMaxLength(255);
            entity.Property(e => e.ChildStyle)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ClassName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComponentGroupId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComponentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComponentType).HasMaxLength(500);
            entity.Property(e => e.DatabaseName).HasMaxLength(1000);
            entity.Property(e => e.DateTimeField)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DefaultVal).HasMaxLength(2500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DisabledExp).HasMaxLength(2000);
            entity.Property(e => e.EntityId)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.EntityName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Events).HasMaxLength(1500);
            entity.Property(e => e.ExcelFieldName).HasMaxLength(1000);
            entity.Property(e => e.ExcelUrl).HasMaxLength(500);
            entity.Property(e => e.FeatureId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FieldName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.FilterTemplate).HasMaxLength(1000);
            entity.Property(e => e.FormatExcell).HasMaxLength(1000);
            entity.Property(e => e.FormatSumaryField).HasMaxLength(250);
            entity.Property(e => e.GroupBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GroupEvent)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.GroupFormat).HasMaxLength(1500);
            entity.Property(e => e.GroupName).HasMaxLength(1000);
            entity.Property(e => e.GroupReferenceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GroupReferenceName).HasMaxLength(250);
            entity.Property(e => e.GroupTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HotKey)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Label).HasMaxLength(250);
            entity.Property(e => e.Lang)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ListClass).HasMaxLength(250);
            entity.Property(e => e.MaxWidth)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MinWidth)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OrderBy)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.OrderBySumary).HasMaxLength(250);
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlainText).HasMaxLength(250);
            entity.Property(e => e.RefClass)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.RefName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ShortDesc).HasMaxLength(1000);
            entity.Property(e => e.ShowExp).HasMaxLength(1000);
            entity.Property(e => e.Style)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Summary).HasMaxLength(1000);
            entity.Property(e => e.TabGroup).HasMaxLength(250);
            entity.Property(e => e.TableName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TenantCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TextAlign).HasMaxLength(1000);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Validation).HasMaxLength(1000);
            entity.Property(e => e.Width)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.created_by).HasMaxLength(255);
        });

        modelBuilder.Entity<ComponentDefaultValue>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComponentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Config>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.GroupKey).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedByIds).HasMaxLength(250);
            entity.Property(e => e.P1).HasMaxLength(50);
            entity.Property(e => e.P2).HasMaxLength(50);
            entity.Property(e => e.P3).HasMaxLength(50);
            entity.Property(e => e.P4).HasMaxLength(50);
            entity.Property(e => e.P5).HasMaxLength(50);
            entity.Property(e => e.RecordId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ResetTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleIds).HasMaxLength(250);
            entity.Property(e => e.TableName).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ConfigUnique>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComponentIds)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ObjectId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ObjectText).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EntityId).HasMaxLength(150);
            entity.Property(e => e.FormatChat).HasMaxLength(2500);
            entity.Property(e => e.Icon).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Message).HasMaxLength(2500);
            entity.Property(e => e.RecordId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserIds).HasMaxLength(1500);
        });

        modelBuilder.Entity<ConversationDetail>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("tr_create"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Avatar).HasMaxLength(400);
            entity.Property(e => e.ConversationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FromId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FromName).HasMaxLength(2500);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Message).HasMaxLength(2500);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Counter>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Id }).HasName("PK_HangFire_Counter");

            entity.ToTable("Counter", "HangFire");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<DefaultFee>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("tr_default_defualtFee"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnType("money");
            entity.Property(e => e.AmountTax).HasColumnType("money");
            entity.Property(e => e.CmId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExchangeRate).HasColumnType("money");
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("money");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("money");
            entity.Property(e => e.GroupFee).HasMaxLength(250);
            entity.Property(e => e.InquiryDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InquiryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MinUnitPrice).HasColumnType("money");
            entity.Property(e => e.Note).HasMaxLength(250);
            entity.Property(e => e.OtherUnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.ServiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Tax).HasColumnType("money");
            entity.Property(e => e.TotalAmount).HasColumnType("money");
            entity.Property(e => e.TotalAmountTax).HasColumnType("money");
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vat).HasColumnType("money");
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Dictionary>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Key).HasMaxLength(250);
            entity.Property(e => e.LangCode).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Value).HasMaxLength(250);
        });

        modelBuilder.Entity<EntityAttached>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EntityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.File).HasMaxLength(1500);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.TableName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EntityContainer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BookingContainer");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CBM).HasColumnType("money");
            entity.Property(e => e.ContainerNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContainerTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EntityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HSCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PKG).HasColumnType("money");
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.SealNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TableName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Tare).HasColumnType("money");
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Weight).HasColumnType("money");
        });

        modelBuilder.Entity<EntityDimension>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_InquiryDeminsion");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EntityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Height).HasColumnType("money");
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Length).HasColumnType("money");
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.TableName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Width).HasColumnType("money");
        });

        modelBuilder.Entity<EntityProcess>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EntityProcessDetail>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EntityProcessId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TaskName).HasMaxLength(500);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EntityTransit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BookingTransit");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EntityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FromId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PortTransitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TableName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ToId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TransitNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Enum>(entity =>
        {
            entity.Property(e => e.Enum1).HasColumnName("Enum");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.TableName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ExchangeRate>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RateProfitUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.RateProfitVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.RateSaleUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.RateSaleVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.RateUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.RateVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Feature>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feature__3214EC071D5C24E1");

            entity.ToTable(tb => tb.HasTrigger("Feature_FeaturePolicy"));

            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.ClassName).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.Events).HasMaxLength(1500);
            entity.Property(e => e.Icon).HasMaxLength(250);
            entity.Property(e => e.InsertedBy).HasMaxLength(50);
            entity.Property(e => e.Label).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ParentId).HasMaxLength(50);
            entity.Property(e => e.Style).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.ViewClass).HasMaxLength(150);
        });

        modelBuilder.Entity<FeaturePolicy>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("FeaturePolicy_Create"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FeatureId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Label).HasMaxLength(50);
            entity.Property(e => e.RecordId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TableName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<FeeType>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("tr_update_FeeType"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code).HasMaxLength(250);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.GroupId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GroupName).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OtherUnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnitPrice).HasColumnType("money");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<FileUpload>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EntityName).HasMaxLength(250);
            entity.Property(e => e.FieldName).HasMaxLength(100);
            entity.Property(e => e.FileName).HasMaxLength(400);
            entity.Property(e => e.FilePath).HasMaxLength(400);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RecordId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SectionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Hash>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Field }).HasName("PK_HangFire_Hash");

            entity.ToTable("Hash", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Hash_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Field).HasMaxLength(100);
        });

        modelBuilder.Entity<History>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComponentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RecordId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TableName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.created_by).HasMaxLength(255);
        });

        modelBuilder.Entity<Inquiry>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("IQ_Update");
                    tb.HasTrigger("Inquiry_Seq");
                });

            entity.HasIndex(e => e.Code, "UC_InquiryCode").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AgentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AgentIdText).HasMaxLength(500);
            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Attachment).HasMaxLength(500);
            entity.Property(e => e.CBM).HasColumnType("money");
            entity.Property(e => e.CW).HasColumnType("money");
            entity.Property(e => e.Code)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CommodityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConsigneeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConsigneeIdText).HasMaxLength(500);
            entity.Property(e => e.Cont20Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cont20x2Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cont40HCId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cont40Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cont45Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContainerTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomsOfficeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Delivery).HasMaxLength(500);
            entity.Property(e => e.DeliveryTerm).HasMaxLength(250);
            entity.Property(e => e.DemFree).HasColumnType("money");
            entity.Property(e => e.DeminsionHeight).HasColumnType("money");
            entity.Property(e => e.DeminsionLength).HasColumnType("money");
            entity.Property(e => e.DeminsionQuantity).HasColumnType("money");
            entity.Property(e => e.DeminsionText).HasMaxLength(250);
            entity.Property(e => e.DeminsionWidth).HasColumnType("money");
            entity.Property(e => e.DestinationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DetFree).HasColumnType("money");
            entity.Property(e => e.EmptyPickupId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EmptyReturnId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("money");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("money");
            entity.Property(e => e.FormatChat).HasMaxLength(500);
            entity.Property(e => e.ForwardId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GW).HasColumnType("money");
            entity.Property(e => e.GroupReceiverId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GroupReceiverIds).HasMaxLength(1250);
            entity.Property(e => e.GroupReceiverIdsText).HasMaxLength(1250);
            entity.Property(e => e.IncotermId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InquiryDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Kgs).HasColumnType("money");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(250);
            entity.Property(e => e.Pic).HasMaxLength(500);
            entity.Property(e => e.Pickup).HasMaxLength(500);
            entity.Property(e => e.PkEmptyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PodId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PolId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.Remark).HasMaxLength(1500);
            entity.Property(e => e.ReturnsId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SaleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SendBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceText).HasMaxLength(250);
            entity.Property(e => e.ShipperId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipperIdName).HasMaxLength(500);
            entity.Property(e => e.StatusText).HasMaxLength(250);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.TotalAmount).HasColumnType("money");
            entity.Property(e => e.TransitPortId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.UserApprovedIds).HasMaxLength(500);
            entity.Property(e => e.UserReceiverIds).HasMaxLength(250);
            entity.Property(e => e.UserReceiverIdsText).HasMaxLength(1250);
            entity.Property(e => e.UserReceiverText)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserViewIds).HasMaxLength(500);
            entity.Property(e => e.VolumeWeight).HasColumnType("money");
        });

        modelBuilder.Entity<InquiryDetail>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("tr_default_iq");
                    tb.HasTrigger("tr_update_InquiryDetail");
                });

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CBM).HasColumnType("money");
            entity.Property(e => e.CW).HasColumnType("money");
            entity.Property(e => e.CarrierId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cost).HasColumnType("money");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Delivery).HasMaxLength(250);
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.FinalDestinationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FormatChat).HasMaxLength(500);
            entity.Property(e => e.Freq).HasMaxLength(250);
            entity.Property(e => e.GW).HasColumnType("money");
            entity.Property(e => e.GroupFee).HasMaxLength(500);
            entity.Property(e => e.GroupReceiverId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InquiryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MinLCLCost).HasColumnType("money");
            entity.Property(e => e.MinLCLRate).HasColumnType("money");
            entity.Property(e => e.MinQuantityCost).HasColumnType("money");
            entity.Property(e => e.MinQuantityRate).HasColumnType("money");
            entity.Property(e => e.Note).HasMaxLength(250);
            entity.Property(e => e.OtherUnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PayeeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PayerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Pickup).HasMaxLength(250);
            entity.Property(e => e.PlaceDeliveryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlaceReceiptId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PodId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PolId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PricingId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProviderId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.QuotationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Rate).HasColumnType("money");
            entity.Property(e => e.RouteId).HasMaxLength(500);
            entity.Property(e => e.ScheduleIds)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ScheduleIdsText).HasMaxLength(250);
            entity.Property(e => e.TotalAmount).HasColumnType("money");
            entity.Property(e => e.TransitTime).HasMaxLength(250);
            entity.Property(e => e.TruckTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnitPrice).HasColumnType("money");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserApprovedId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserDeclineId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserReceiverId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vat).HasColumnType("money");
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ViaId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<InvoiceConfig>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CompanyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.Form).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SeriNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HangFire_Job");

            entity.ToTable("Job", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Job_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.HasIndex(e => e.StateName, "IX_HangFire_Job_StateName").HasFilter("([StateName] IS NOT NULL)");

            entity.Property(e => e.Arguments).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.InvocationData).IsRequired();
            entity.Property(e => e.StateName).HasMaxLength(20);
        });

        modelBuilder.Entity<JobParameter>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.Name }).HasName("PK_HangFire_JobParameter");

            entity.ToTable("JobParameter", "HangFire");

            entity.Property(e => e.Name).HasMaxLength(40);

            entity.HasOne(d => d.Job).WithMany(p => p.JobParameter)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_HangFire_JobParameter_Job");
        });

        modelBuilder.Entity<JobQueue>(entity =>
        {
            entity.HasKey(e => new { e.Queue, e.Id }).HasName("PK_HangFire_JobQueue");

            entity.ToTable("JobQueue", "HangFire");

            entity.Property(e => e.Queue).HasMaxLength(50);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FetchedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<List>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Id }).HasName("PK_HangFire_List");

            entity.ToTable("List", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_List_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<MarkupPricing>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MarkupLevel).HasColumnType("money");
            entity.Property(e => e.PartnerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PodId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PolId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PriceTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MasterData>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code).HasMaxLength(500);
            entity.Property(e => e.CodeMn)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Path).HasMaxLength(1000);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("Partner_Seq"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccountBranch).HasMaxLength(250);
            entity.Property(e => e.AccountName).HasMaxLength(255);
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdditionTypeIds)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.AdditionTypeIdsText).HasMaxLength(255);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AddressInv).HasMaxLength(250);
            entity.Property(e => e.AssignId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AssignmentDebitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AssignmentInvId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Attachment).HasMaxLength(500);
            entity.Property(e => e.BankId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.CompanyName).HasMaxLength(500);
            entity.Property(e => e.CompanyNameInv).HasMaxLength(500);
            entity.Property(e => e.ConditionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContactEmail).HasMaxLength(255);
            entity.Property(e => e.ContactName).HasMaxLength(500);
            entity.Property(e => e.ContactPhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreditLimit).HasColumnType("money");
            entity.Property(e => e.CustomerTypeId)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DebitAccountId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DebitAmount).HasColumnType("money");
            entity.Property(e => e.DebitName).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DistributeInformationIds)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DistributeInformationIdsText).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.EmailInv).HasMaxLength(255);
            entity.Property(e => e.Fax)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FormatChat).HasMaxLength(250);
            entity.Property(e => e.FullName).HasMaxLength(500);
            entity.Property(e => e.GenderContactId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GenderId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GroupId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.History).HasMaxLength(500);
            entity.Property(e => e.IdCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Industry).HasMaxLength(255);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Logo).HasMaxLength(500);
            entity.Property(e => e.Mail).HasMaxLength(200);
            entity.Property(e => e.MinProfitMonth).HasColumnType("money");
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OpenedAt).HasMaxLength(250);
            entity.Property(e => e.PartnerTypeIds)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PartnerTypeIdsText).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PicId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Po).HasMaxLength(255);
            entity.Property(e => e.RaitingId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Rating).HasMaxLength(255);
            entity.Property(e => e.ReceivesAccountName).HasMaxLength(255);
            entity.Property(e => e.ReceivesAccountNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReceivesBankId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReceivesSwiftCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RegularShippingFrom).HasMaxLength(255);
            entity.Property(e => e.ResidenceTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ResidenceTypeIdText)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.SaleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SourseId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SwiftCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TaxCode)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ToastWarning).HasMaxLength(255);
            entity.Property(e => e.TrackingURL)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Web).HasMaxLength(255);
            entity.Property(e => e.WebSite).HasMaxLength(500);
            entity.Property(e => e.ZipCode)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PartnerBankAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BankAccount");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccountBranch).HasMaxLength(250);
            entity.Property(e => e.AccountName).HasMaxLength(250);
            entity.Property(e => e.AccountNumber).HasMaxLength(250);
            entity.Property(e => e.BankId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OpenedAt).HasMaxLength(250);
            entity.Property(e => e.PartnerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SwiftCode).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PartnerCare>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CustomerCare");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AssigneeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Attachment).HasMaxLength(500);
            entity.Property(e => e.CategoryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PartnerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PriorityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TaskName).HasMaxLength(500);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PartnerContact>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_TerminalPartner");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContactEmail).HasMaxLength(250);
            entity.Property(e => e.ContactName).HasMaxLength(250);
            entity.Property(e => e.ContactPhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GenderId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.JobTitles).HasMaxLength(250);
            entity.Property(e => e.Note).HasMaxLength(250);
            entity.Property(e => e.PartnerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PartnerCreditPeriod>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreditPeriodTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PartnerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShippmentTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PlanEmail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_EmailTemplate");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComponentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EmailFieldId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FeatureId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Field1Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Field2Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Field3Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FromEmail).HasMaxLength(250);
            entity.Property(e => e.FromName).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.PassEmail).HasMaxLength(250);
            entity.Property(e => e.SubjectMail).HasMaxLength(250);
            entity.Property(e => e.ToEmail).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Value1).HasMaxLength(500);
            entity.Property(e => e.Value2).HasMaxLength(500);
            entity.Property(e => e.Value3).HasMaxLength(500);
        });

        modelBuilder.Entity<PlanEmailDetail>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlanEmailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RecordId)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.TableName).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Pricing>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AgentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AmountText).HasMaxLength(250);
            entity.Property(e => e.AttachedFile).HasMaxLength(500);
            entity.Property(e => e.CarrierId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CommodityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cont20Text).HasMaxLength(250);
            entity.Property(e => e.Cont40Text).HasMaxLength(250);
            entity.Property(e => e.ContText).HasMaxLength(250);
            entity.Property(e => e.CutOff).HasMaxLength(250);
            entity.Property(e => e.DemFree).HasColumnType("money");
            entity.Property(e => e.DetFree).HasColumnType("money");
            entity.Property(e => e.EmptyReturnId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExchangeRate).HasColumnType("money");
            entity.Property(e => e.FormatChat).HasMaxLength(2500);
            entity.Property(e => e.FreeTime).HasColumnType("money");
            entity.Property(e => e.Fsc).HasColumnType("money");
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MakeupLevel)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MinQuantity).HasColumnType("money");
            entity.Property(e => e.ModeId).HasMaxLength(250);
            entity.Property(e => e.Note).HasMaxLength(250);
            entity.Property(e => e.PkEmptyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PodId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PolId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReturnsId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Scc).HasColumnType("money");
            entity.Property(e => e.ScheduleIds)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ScheduleIdsText).HasMaxLength(250);
            entity.Property(e => e.ServiceText).HasMaxLength(250);
            entity.Property(e => e.Sto).HasColumnType("money");
            entity.Property(e => e.TransitTime).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VendorTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ViaId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PricingDetail>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("tr_default_pricingDetail"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DemFree).HasColumnType("money");
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DetFree).HasColumnType("money");
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.GroupFee).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MinUnitPrice).HasColumnType("money");
            entity.Property(e => e.Note).HasMaxLength(250);
            entity.Property(e => e.OtherUnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PkEmptyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PricingId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReturnsId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnitPrice).HasColumnType("money");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vat).HasColumnType("money");
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.created_by).HasMaxLength(255);
        });

        modelBuilder.Entity<SaleFunction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SaleFuntion");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code).HasMaxLength(250);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.GroupName).HasMaxLength(500);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Value).HasMaxLength(500);
        });

        modelBuilder.Entity<Schema>(entity =>
        {
            entity.HasKey(e => e.Version).HasName("PK_HangFire_Schema");

            entity.ToTable("Schema", "HangFire");

            entity.Property(e => e.Version).ValueGeneratedNever();
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HangFire_Server");

            entity.ToTable("Server", "HangFire");

            entity.HasIndex(e => e.LastHeartbeat, "IX_HangFire_Server_LastHeartbeat");

            entity.Property(e => e.Id).HasMaxLength(200);
            entity.Property(e => e.LastHeartbeat).HasColumnType("datetime");
        });

        modelBuilder.Entity<Services>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Action).HasMaxLength(250);
            entity.Property(e => e.ComId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Env)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleIds)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.System)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.TenantCode)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Set>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Value }).HasName("PK_HangFire_Set");

            entity.ToTable("Set", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Set_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.HasIndex(e => new { e.Key, e.Score }, "IX_HangFire_Set_Score");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(256);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("Shipment_Delete");
                    tb.HasTrigger("Shipment_Seq");
                    tb.HasTrigger("Shipment_Seq_HBL");
                    tb.HasTrigger("Shipment_TotalParent");
                    tb.HasTrigger("Shipment_UPDATE_FEE");
                });

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccountNo).HasMaxLength(250);
            entity.Property(e => e.AccountingInformation).HasMaxLength(250);
            entity.Property(e => e.AgentCode).HasMaxLength(250);
            entity.Property(e => e.AgentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AirportDeparture).HasMaxLength(500);
            entity.Property(e => e.AirportDepartureId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AmountInsurance).HasMaxLength(250);
            entity.Property(e => e.BookingLocalId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BookingNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.By1).HasMaxLength(250);
            entity.Property(e => e.By2).HasMaxLength(250);
            entity.Property(e => e.ByFirstCarrier).HasMaxLength(250);
            entity.Property(e => e.CBM).HasColumnType("money");
            entity.Property(e => e.CCCharges).HasMaxLength(500);
            entity.Property(e => e.CFlights1).HasMaxLength(250);
            entity.Property(e => e.CFlights2).HasMaxLength(250);
            entity.Property(e => e.CHGSCode).HasMaxLength(250);
            entity.Property(e => e.CW).HasColumnType("money");
            entity.Property(e => e.CargoType).HasMaxLength(250);
            entity.Property(e => e.CarrierId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CdsTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ChargeableWeight).HasColumnType("money");
            entity.Property(e => e.ClauseId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ClearanceNo)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.CoFormId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CoQuantity).HasColumnType("money");
            entity.Property(e => e.Code)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CommercialInvoiceNo)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.CommodityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CommodityItemNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CommodityText).HasMaxLength(250);
            entity.Property(e => e.ConsigneeDes).HasMaxLength(500);
            entity.Property(e => e.ConsigneeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContainerNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContainerText).HasMaxLength(500);
            entity.Property(e => e.ContainerTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CsdEdit).HasColumnType("money");
            entity.Property(e => e.Currency).HasMaxLength(250);
            entity.Property(e => e.CurrencyRatesText).HasMaxLength(500);
            entity.Property(e => e.CustomerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomsOfficeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DeclaredCarriage).HasMaxLength(250);
            entity.Property(e => e.DeclaredCustoms).HasMaxLength(250);
            entity.Property(e => e.DeliveryAddress).HasMaxLength(250);
            entity.Property(e => e.DeliveryContact).HasMaxLength(250);
            entity.Property(e => e.DeliveryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DeliveryTermId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DemFree).HasColumnType("money");
            entity.Property(e => e.DetFree).HasColumnType("money");
            entity.Property(e => e.DetailsGoods).HasMaxLength(1000);
            entity.Property(e => e.DocsNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Driver).HasMaxLength(50);
            entity.Property(e => e.EmtyPickupId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExportReferences).HasMaxLength(100);
            entity.Property(e => e.FinalDestinationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ForDeliveryGoodsDes).HasMaxLength(250);
            entity.Property(e => e.ForDeliveryGoodsId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ForwardId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Freetime).HasColumnType("money");
            entity.Property(e => e.FreightChargeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FreightId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FreightPayableId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GW).HasColumnType("money");
            entity.Property(e => e.HSCode).HasMaxLength(100);
            entity.Property(e => e.HblNo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.HblSeaNo)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.HblTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InWords).HasMaxLength(250);
            entity.Property(e => e.IncotermId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IssuingDes).HasMaxLength(500);
            entity.Property(e => e.IssuingId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.KGS)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.KgsLgl)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MblNo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MblTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Measuament).HasColumnType("money");
            entity.Property(e => e.NatureQuantityGoods).HasMaxLength(250);
            entity.Property(e => e.NoPieces).HasColumnType("money");
            entity.Property(e => e.NominationParty).HasMaxLength(250);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.NotifyPartyDes).HasMaxLength(500);
            entity.Property(e => e.NotifyPartyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NumberDaysMooc).HasColumnType("money");
            entity.Property(e => e.NumberDaysTruck).HasColumnType("money");
            entity.Property(e => e.NumberOriginalId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NumberOriginalIdText)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OceanVessel).HasMaxLength(250);
            entity.Property(e => e.OceanVesselVoy).HasMaxLength(250);
            entity.Property(e => e.OptinalShippingInformation).HasMaxLength(250);
            entity.Property(e => e.OtherCharges).HasMaxLength(250);
            entity.Property(e => e.OtherId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OtherRefno)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Packages).HasColumnType("money");
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PartyContact).HasMaxLength(250);
            entity.Property(e => e.PaymentTermId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PickupAddress).HasMaxLength(250);
            entity.Property(e => e.PickupContact).HasMaxLength(250);
            entity.Property(e => e.PickupId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Place).HasMaxLength(500);
            entity.Property(e => e.PlaceDeliveryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlaceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlaceIssueId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PlaceReceiptId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PoNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PodId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PolId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PortTransit2Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PortTransit3Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PortTransitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PrepaidId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.QuotationCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.QuotationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RateChange).HasColumnType("money");
            entity.Property(e => e.RateClass)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReceiverIds)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(250);
            entity.Property(e => e.SCI).HasMaxLength(250);
            entity.Property(e => e.SaleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SaleIds)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SeriBillNo).HasMaxLength(100);
            entity.Property(e => e.Service).HasMaxLength(250);
            entity.Property(e => e.ServiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentDOId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentSIId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipperDes).HasMaxLength(500);
            entity.Property(e => e.ShipperId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShippingMark).HasMaxLength(250);
            entity.Property(e => e.SpecialHandling).HasMaxLength(250);
            entity.Property(e => e.Storage).HasColumnType("money");
            entity.Property(e => e.SubService).HasMaxLength(250);
            entity.Property(e => e.SubscribersIds)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ThreadingCdsId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.To1).HasMaxLength(250);
            entity.Property(e => e.To2).HasMaxLength(250);
            entity.Property(e => e.To3).HasMaxLength(250);
            entity.Property(e => e.ToId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("money");
            entity.Property(e => e.TotalCBM).HasColumnType("money");
            entity.Property(e => e.TotalCW).HasColumnType("money");
            entity.Property(e => e.TotalPrepaidId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TruckNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TruckerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TypeMoveId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserApprovedIds)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.UserViewIds)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vessel).HasMaxLength(250);
            entity.Property(e => e.VesselVoy).HasMaxLength(250);
            entity.Property(e => e.VolumeWeight).HasColumnType("money");
            entity.Property(e => e.WTVALId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WarehouseId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ShipmentDO>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("ShipmentDO_Seq"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConditionGoods).HasMaxLength(500);
            entity.Property(e => e.ConsigneeDes).HasMaxLength(500);
            entity.Property(e => e.ConsigneeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConsigneeIdText).HasMaxLength(500);
            entity.Property(e => e.DeliveryPlaceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DescriptionGoods).HasMaxLength(500);
            entity.Property(e => e.DoNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Evidence).HasMaxLength(500);
            entity.Property(e => e.IndentityCard1)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IndentityCard2)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Position1).HasMaxLength(250);
            entity.Property(e => e.Position2).HasMaxLength(250);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Representative1).HasMaxLength(250);
            entity.Property(e => e.Representative2).HasMaxLength(250);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipperDes).HasMaxLength(500);
            entity.Property(e => e.ShipperId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipperIdText).HasMaxLength(500);
            entity.Property(e => e.SpecialNotes).HasMaxLength(500);
            entity.Property(e => e.Tel1)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Tel2)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ShipmentFee>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("tr_Delete_ShipmentFEE");
                    tb.HasTrigger("tr_default_Shipment");
                });

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnType("money");
            entity.Property(e => e.AmountTax).HasColumnType("money");
            entity.Property(e => e.AssignId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BasedId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BookingId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Docs).HasMaxLength(250);
            entity.Property(e => e.ExAmount).HasColumnType("money");
            entity.Property(e => e.ExAmountTax).HasColumnType("money");
            entity.Property(e => e.ExProfitUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExProfitVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExSaleUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExSaleVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExTotalAmount).HasColumnType("money");
            entity.Property(e => e.ExTotalAmountTax).HasColumnType("money");
            entity.Property(e => e.ExchangeRate).HasColumnType("money");
            entity.Property(e => e.ExchangeRateINV).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.FileId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HblNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.ObhId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentRequestDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentRequestId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PmTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.SaleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SettlementNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentInvoiceCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentInvoiceDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentInvoiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Tax).HasColumnType("money");
            entity.Property(e => e.TotalAmount).HasColumnType("money");
            entity.Property(e => e.TotalAmountTax).HasColumnType("money");
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vat).HasColumnType("money");
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VoucherId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ShipmentFreight>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Tax).HasColumnType("money");
            entity.Property(e => e.Total).HasColumnType("money");
            entity.Property(e => e.TotalDueAgent).HasColumnType("money");
            entity.Property(e => e.TotalDueCarrier).HasColumnType("money");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ValuationCharge).HasColumnType("money");
            entity.Property(e => e.WeightCharge).HasColumnType("money");
        });

        modelBuilder.Entity<ShipmentInvoice>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("ShipmentInvoice_Seq");
                    tb.HasTrigger("ShipmentInvoice_UPDATE");
                });

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AmountTax).HasColumnType("money");
            entity.Property(e => e.AmountText).HasMaxLength(500);
            entity.Property(e => e.AttachedFile).HasMaxLength(500);
            entity.Property(e => e.BuyerName).HasMaxLength(250);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CompanyAddress).HasMaxLength(250);
            entity.Property(e => e.CompanyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CompanyIdText).HasMaxLength(250);
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerIdText).HasMaxLength(250);
            entity.Property(e => e.DateFieldId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DebitAmount).HasColumnType("money");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DocsNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EinvoiceCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EinvoiceLink).HasMaxLength(500);
            entity.Property(e => e.ExchangeRateINV).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateINV2).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.FileIds).IsUnicode(false);
            entity.Property(e => e.FileNo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FormatChat).HasMaxLength(500);
            entity.Property(e => e.ForwardId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HblIds).IsUnicode(false);
            entity.Property(e => e.HblNo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceConfigId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MblNos)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.OtherCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReceiverIds)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.RequestTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SeriNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceIds)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TotalAmount).HasColumnType("money");
            entity.Property(e => e.TotalAmountTax).HasColumnType("money");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserApprovedIds).HasMaxLength(500);
            entity.Property(e => e.UserViewIds).HasMaxLength(500);
            entity.Property(e => e.VatInv).HasColumnType("money");
            entity.Property(e => e.VendorIds)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.VendorIdsText).HasMaxLength(500);
        });

        modelBuilder.Entity<ShipmentInvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_InvoiceDetail");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("ShipmentInvoiceDetail_Delete");
                    tb.HasTrigger("ShipmentInvoiceDetail_Update");
                });

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnType("money");
            entity.Property(e => e.AmountTax).HasColumnType("money");
            entity.Property(e => e.AssignId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BasedId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BookingId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Docs).HasMaxLength(250);
            entity.Property(e => e.ExAmount).HasColumnType("money");
            entity.Property(e => e.ExAmountTax).HasColumnType("money");
            entity.Property(e => e.ExProfitUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExProfitVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExSaleUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExSaleVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExTotalAmount).HasColumnType("money");
            entity.Property(e => e.ExTotalAmountTax).HasColumnType("money");
            entity.Property(e => e.ExchangeRate).HasColumnType("money");
            entity.Property(e => e.ExchangeRateINV).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.FileId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.ObhId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Payable).HasColumnType("money");
            entity.Property(e => e.PaymentCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentRequestDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentRequestId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PmTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.Receivable).HasColumnType("money");
            entity.Property(e => e.SaleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SettlementNo).HasMaxLength(250);
            entity.Property(e => e.ShipmentFeeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentInvoiceCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentInvoiceDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentInvoiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Tax).HasColumnType("money");
            entity.Property(e => e.TotalAmount).HasColumnType("money");
            entity.Property(e => e.TotalAmountTax).HasColumnType("money");
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vat).HasColumnType("money");
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VoucherId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ShipmentSI>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("ShipmentSI_Seq"));

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BookingNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CBM).HasColumnType("money");
            entity.Property(e => e.ClauseId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConsigneeDes).HasMaxLength(500);
            entity.Property(e => e.ConsigneeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ConsigneeIdText).HasMaxLength(500);
            entity.Property(e => e.ContainerText).HasMaxLength(500);
            entity.Property(e => e.DeliveryTermId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DetailsGoods).HasMaxLength(500);
            entity.Property(e => e.FinalDestinationId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GW).HasColumnType("money");
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MblTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.NotifyPartyDes).HasMaxLength(500);
            entity.Property(e => e.NotifyPartyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NotifyPartyIdText).HasMaxLength(500);
            entity.Property(e => e.Package).HasMaxLength(500);
            entity.Property(e => e.PaymentTermId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Pic).HasMaxLength(250);
            entity.Property(e => e.PlaceDeliveryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PodId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PolId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RealConsigneeDes).HasMaxLength(500);
            entity.Property(e => e.RealConsigneeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RealConsigneeIdText).HasMaxLength(500);
            entity.Property(e => e.RealShipperDes).HasMaxLength(500);
            entity.Property(e => e.RealShipperId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RealShipperIdText).HasMaxLength(500);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipperDes).HasMaxLength(500);
            entity.Property(e => e.ShippingMarks).HasMaxLength(500);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vessel)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VesselVoy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ShipmentTask>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.JobName).HasMaxLength(250);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.Id }).HasName("PK_HangFire_State");

            entity.ToTable("State", "HangFire");

            entity.HasIndex(e => e.CreatedAt, "IX_HangFire_State_CreatedAt");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(100);

            entity.HasOne(d => d.Job).WithMany(p => p.State)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_HangFire_State_Job");
        });

        modelBuilder.Entity<TableName>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Duplicate).HasMaxLength(500);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.TableTrigger).HasMaxLength(500);
            entity.Property(e => e.TanentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Tanent>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CompanyName).HasMaxLength(250);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Password).HasMaxLength(250);
            entity.Property(e => e.TanentCode).HasMaxLength(250);
            entity.Property(e => e.TaxCode).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TaskNotification>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AssignedId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(2500);
            entity.Property(e => e.EntityId).HasMaxLength(250);
            entity.Property(e => e.Icon)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RecordId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(2500);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Terminal>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("Terminal_Seq");
                    tb.HasTrigger("Tr_Terminal_update");
                });

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Address).HasMaxLength(600);
            entity.Property(e => e.Code).HasMaxLength(150);
            entity.Property(e => e.CodeMn)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CountryId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(400);
            entity.Property(e => e.ServiceText).HasMaxLength(400);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("User_Partners");
                    tb.HasTrigger("User_Seq");
                });

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccName).HasMaxLength(255);
            entity.Property(e => e.AccNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.Avatar).HasMaxLength(250);
            entity.Property(e => e.BankId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Code).HasMaxLength(250);
            entity.Property(e => e.CompanyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DepartmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(250);
            entity.Property(e => e.FullName).HasMaxLength(250);
            entity.Property(e => e.GenderId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IdentityCard)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsuranceAmount).HasColumnType("money");
            entity.Property(e => e.Knowledge).HasMaxLength(150);
            entity.Property(e => e.NickName).HasMaxLength(150);
            entity.Property(e => e.PartnerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PassEmail).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(250);
            entity.Property(e => e.PhoneNumber).HasMaxLength(250);
            entity.Property(e => e.PlaceIssue).HasMaxLength(150);
            entity.Property(e => e.PositionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Recover).HasMaxLength(500);
            entity.Property(e => e.RoleIds)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RoleIdsText).HasMaxLength(500);
            entity.Property(e => e.SalaryAmount).HasColumnType("money");
            entity.Property(e => e.SalaryCoefficient).HasColumnType("money");
            entity.Property(e => e.Salt).HasMaxLength(250);
            entity.Property(e => e.Ssn).HasMaxLength(250);
            entity.Property(e => e.TargetLocalCurr).HasColumnType("money");
            entity.Property(e => e.TargetUSDCurr).HasColumnType("money");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TeamId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TypeBonusId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TypeContractId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserName).HasMaxLength(250);
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccessToken)
                .HasMaxLength(1500)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IpAddress).HasMaxLength(255);
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(1500)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.created_by).HasMaxLength(255);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.created_by).HasMaxLength(255);
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComponentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FeatureId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("Voucher_Seq");
                    tb.HasTrigger("Voucher_Update");
                });

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdvanceAmount).HasColumnType("money");
            entity.Property(e => e.Amount).HasColumnType("money");
            entity.Property(e => e.AmountText).HasMaxLength(500);
            entity.Property(e => e.AttachedFile).HasMaxLength(500);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DepartmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FormatChat).HasMaxLength(500);
            entity.Property(e => e.ForwardId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentMethodId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PositionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RemainingAmount).HasColumnType("money");
            entity.Property(e => e.SettlementAmount).HasColumnType("money");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserApprovedIds).HasMaxLength(500);
            entity.Property(e => e.UserCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserViewIds).HasMaxLength(500);
            entity.Property(e => e.VoucherId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VoucherNo)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VoucherDetail>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdvanceRequestTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnType("money");
            entity.Property(e => e.AmountTax).HasColumnType("money");
            entity.Property(e => e.BasedId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DescriptionId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Docs).HasMaxLength(250);
            entity.Property(e => e.ExProfitUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExProfitVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExSaleUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExSaleVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRate).HasColumnType("money");
            entity.Property(e => e.ExchangeRateINV).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateUSD).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.ExchangeRateVND).HasColumnType("decimal(30, 24)");
            entity.Property(e => e.FileId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceIssuerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.ObhId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ParentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PartnerId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PmTypeId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("money");
            entity.Property(e => e.SaleId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SeriNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SettelementNo).HasMaxLength(250);
            entity.Property(e => e.ShipmentId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ShipmentInvoiceDetailId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Tax).HasColumnType("money");
            entity.Property(e => e.TotalAmount).HasColumnType("money");
            entity.Property(e => e.TotalAmountTax).HasColumnType("money");
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Vat).HasColumnType("money");
            entity.Property(e => e.VendorId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VoucherId)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<WebConfig>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Key).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Value).HasMaxLength(250);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

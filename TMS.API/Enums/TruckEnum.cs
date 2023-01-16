namespace TMS.API.Enums
{
    public enum TruckTypeEnum
    {
        Truck = 65,
        Tractor = 66,
        Trailer = 1572,
    }

    public enum ContainerMovingTypeEnum
    {
        Import = 2696,
        Export = 2697,
        PortTransferEmpty = 2698,
        D2D = 2874,
        PortTransferCont = 2885,
        WareHouse = 2699,
    }

    public enum TerminalTypeEnum
    {
        Depot = 3035,
        Fac = 3036,
        ICD = 3034,
        Port = 3033,
    }

    public enum CoordinationTypeEnum
    {
        Assembly = 2710,
    }

    public enum StatusPayslipEnum
    {
        CloseToBookBank = 571
    }

    public enum CoorDetailReportTypeEnum
    {
        Day = 1,
        Month = 2,
        Year = 3
    }

    public enum StopType
    {
        AnchorTruck = 3042,
        AnchorTrailer = 3043,
        UnAnchorTruck = 3044,
        UnAnchorTrailer = 3045,
        CutTrailer = 3059,
    }
}

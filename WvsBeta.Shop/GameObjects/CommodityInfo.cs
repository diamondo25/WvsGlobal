namespace WvsBeta.Shop
{
    public enum StockState
    {
        InStock = -1,
        DefaultState = 0,
        OutOfStock = 1,
        NotAvailable = 2,
    }

    public class CommodityInfo
    {
        public int SerialNumber { get; set; }
        public int ItemID { get; set; }
        public short Count { get; set; }
        public short Period { get; set; }
        public bool OnSale { get; set; }
        public int Price { get; set; }
        public CommodityGenders Gender { get; set; }

        public StockState StockState { get; set; } = StockState.DefaultState;
    }
}
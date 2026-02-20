using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShoesShop.Classes
{
    public class ProductViewModel
    {
        public int ProductID { get; set; }
        public string Article { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string Supplier { get; set; }
        public string Unit { get; set; }
        public string QuantityText { get; set; }
        public string Price { get; set; }
        public string OldPrice { get; set; }
        public Visibility OldPriceVisibility { get; set; }
        public string SaleText { get; set; }
        public Visibility SaleVisibility { get; set; }
        public Brush RowBackground { get; set; }
        public BitmapImage ImagePath { get; set; }
        public Visibility EditButtonVisibility { get; set; }
    }
}
using ShoesShop.Classes;
using ShoesShop.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShoesShop.Windows
{
    public partial class MainWindow : Window
    {
        private readonly DemShoesShopEntities context = new DemShoesShopEntities();
        private readonly Users currentUser;
        private List<ProductViewModel> allProducts;
        private bool editWindowOpen = false;

        public MainWindow(Users user)
        {
            InitializeComponent();
            currentUser = user;
            SetupUIByRole();
            LoadProducts();
            LoadSupplierFilter();
        }

        private void SetupUIByRole()
        {
            if (currentUser != null)
            {
                if (currentUser.Roles.Name == "Администратор")
                    AddProductButton.Visibility = Visibility.Visible;

                if (currentUser.Roles.Name == "Администратор" || currentUser.Roles.Name == "Менеджер")
                    OrdersButton.Visibility = Visibility.Visible;
            }

            UserFioText.Text = currentUser == null ? "Гость" :
                $"{currentUser.Surname} {currentUser.Name} {currentUser.MiddleName}";
        }

        private void LoadProducts()
        {
            allProducts = new List<ProductViewModel>();
            var products = context.Products.ToList();

            foreach (var p in products)
            {
                bool hasSale = p.Sale > 0;
                bool bigSale = p.Sale > 15;
                bool outOfStock = p.Quantity == 0;

                Brush rowColor = Brushes.White;
                if (bigSale) rowColor = (Brush)Application.Current.Resources["SaleBigBrush"];
                else if (outOfStock) rowColor = Brushes.LightBlue;

                decimal finalPrice = hasSale ? p.Cost - (p.Cost * p.Sale / 100) : p.Cost;

                string imagePath = string.IsNullOrEmpty(p.Photo) ? "pack://application:,,,/Resources/picture.png" : p.Photo;
                BitmapImage image;
                try
                {
                    image = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                }
                catch
                {
                    image = new BitmapImage(new Uri("Resources/picture.png", UriKind.Relative));
                }

                Visibility editVisibility = (currentUser != null && currentUser.Roles.Name == "Администратор")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                allProducts.Add(new ProductViewModel
                {
                    ProductID = p.ProductID,
                    Category = p.Categories.Name,
                    Name = p.ProductName.Name,
                    Description = "Описание товара: " + p.Description,
                    Manufacturer = "Производитель: " + p.Manufacturer.Name,
                    Supplier = "Поставщик: " + p.Suppliers.Name,
                    Unit = "Ед. изм.: " + p.Units.Name,
                    QuantityText = "Количество на складе: " + p.Quantity,
                    Price = $"Цена: {finalPrice} ₽",
                    OldPrice = $"Цена: {p.Cost} ₽",
                    OldPriceVisibility = hasSale ? Visibility.Visible : Visibility.Collapsed,
                    SaleText = $"-{p.Sale}%",
                    SaleVisibility = hasSale ? Visibility.Visible : Visibility.Collapsed,
                    RowBackground = rowColor,
                    ImagePath = image,
                    EditButtonVisibility = editVisibility
                });
            }

            ProductsItemsControl.ItemsSource = allProducts;
        }

        private void LoadSupplierFilter()
        {
            SupplierFilterComboBox.Items.Clear();
            SupplierFilterComboBox.Items.Add("Все поставщики");
            var suppliers = context.Suppliers.Select(s => s.Name).ToList();
            foreach (var s in suppliers)
                SupplierFilterComboBox.Items.Add(s);
            SupplierFilterComboBox.SelectedIndex = 0;
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (editWindowOpen)
            {
                MessageBox.Show("Закройте текущее окно редактирования перед открытием нового.", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProductEditWindow editWindow = new ProductEditWindow(null);
            editWindow.ProductSaved += EditWindow_ProductSaved;
            editWindowOpen = true;
            editWindow.ShowDialog();
            editWindowOpen = false;
        }

        private void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (editWindowOpen)
            {
                MessageBox.Show("Закройте текущее окно редактирования перед открытием нового.", 
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Button btn = sender as Button;
            ProductViewModel pvm = btn.DataContext as ProductViewModel;
            ProductEditWindow editWindow = new ProductEditWindow(pvm.ProductID);
            editWindow.ProductSaved += EditWindow_ProductSaved;
            editWindowOpen = true;
            editWindow.ShowDialog();
            editWindowOpen = false;
        }

        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            ProductViewModel pvm = btn.DataContext as ProductViewModel;
            var product = context.Products.Find(pvm.ProductID);

            if (product.Orders.Any())
            {
                MessageBox.Show("Невозможно удалить товар, который присутствует в заказах.", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Вы действительно хотите удалить этот товар?", 
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(product.Photo) && File.Exists(product.Photo))
                        File.Delete(product.Photo);

                    context.Products.Remove(product);
                    context.SaveChanges();
                    LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении товара: {ex.Message}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditWindow_ProductSaved(object sender, EventArgs e)
        {
            LoadProducts();
        }


        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private void SupplierFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void ApplyFilters()
        {
            string search = SearchTextBox.Text.ToLower();
            string supplierFilter = SupplierFilterComboBox.SelectedItem?.ToString();

            var filtered = allProducts.Where(p =>
                (string.IsNullOrEmpty(search) ||
                 p.Name.ToLower().Contains(search) ||
                 p.Description.ToLower().Contains(search) ||
                 p.Category.ToLower().Contains(search) ||
                 p.Manufacturer.ToLower().Contains(search) ||
                 p.Supplier.ToLower().Contains(search))
                &&
                (supplierFilter == "Все поставщики" || p.Supplier.Contains(supplierFilter))
            ).ToList();

            if (SortComboBox.SelectedIndex == 0)
                filtered = filtered.OrderBy(p => int.Parse(p.QuantityText.Split(':')[1].Trim())).ToList();
            else if (SortComboBox.SelectedIndex == 1)
                filtered = filtered.OrderByDescending(p => int.Parse(p.QuantityText.Split(':')[1].Trim())).ToList();

            ProductsItemsControl.ItemsSource = filtered;
        }
        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
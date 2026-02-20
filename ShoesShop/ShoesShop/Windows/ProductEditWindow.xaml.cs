using ShoesShop.Model;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ShoesShop.Windows
{
    public partial class ProductEditWindow : Window
    {
        private readonly DemShoesShopEntities context = new DemShoesShopEntities();
        private readonly int? productId;
        private string selectedImagePath;

        public event EventHandler ProductSaved;
        public ProductEditWindow(int? productId)
        {
            InitializeComponent();
            this.productId = productId;
            LoadComboboxes();
            if (productId.HasValue)
                LoadProduct();
        }
        private void LoadComboboxes()
        {
            CategoryComboBox.ItemsSource = context.Categories.Select(c => c.Name).ToList();
            ManufacturerComboBox.ItemsSource = context.Manufacturer.Select(m => m.Name).ToList();
            SupplierComboBox.ItemsSource = context.Suppliers.Select(s => s.Name).ToList();
            UnitComboBox.ItemsSource = context.Units.Select(u => u.Name).ToList();
        }

        private void LoadProduct()
        {
            var product = context.Products.Find(productId.Value);
            if (product == null) return;

            ArticleTextBox.Text = product.Article;
            NameTextBox.Text = product.ProductName.Name;
            CategoryComboBox.SelectedItem = product.Categories.Name;
            DescriptionTextBox.Text = product.Description;
            ManufacturerComboBox.SelectedItem = product.Manufacturer.Name;
            SupplierComboBox.SelectedItem = product.Suppliers.Name;
            PriceTextBox.Text = product.Cost.ToString();
            UnitComboBox.SelectedItem = product.Units.Name;
            QuantityTextBox.Text = product.Quantity.ToString();
            SaleTextBox.Text = product.Sale.ToString();

            string imagePath = string.IsNullOrEmpty(product.Photo) ? "Resources/picture.png" : product.Photo;   
            ProductImage.Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
        }
        private void SelectPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Изображения (*.png;*.jpg)|*.png;*.jpg";
            if (dlg.ShowDialog() == true)
            {
                selectedImagePath = dlg.FileName;
                ProductImage.Source = new BitmapImage(new Uri(selectedImagePath, UriKind.Absolute));
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string article = ArticleTextBox.Text.Trim();
                string name = NameTextBox.Text.Trim();
                string category = CategoryComboBox.SelectedItem as string;
                string description = DescriptionTextBox.Text.Trim();
                string manufacturer = ManufacturerComboBox.SelectedItem as string;
                string supplier = SupplierComboBox.SelectedItem as string;
                string unit = UnitComboBox.SelectedItem as string;

                // Валидация
                if (string.IsNullOrEmpty(article))
                {
                    MessageBox.Show("Введите артикул товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Введите наименование товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(category))
                {
                    MessageBox.Show("Выберите категорию товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(manufacturer))
                {
                    MessageBox.Show("Выберите производителя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(supplier))
                {
                    MessageBox.Show("Выберите поставщика.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(unit))
                {
                    MessageBox.Show("Выберите единицу измерения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!decimal.TryParse(PriceTextBox.Text.Trim(), out decimal price) || price < 0)
                {
                    MessageBox.Show("Введите корректную цену.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(QuantityTextBox.Text.Trim(), out int quantity) || quantity < 0)
                {
                    MessageBox.Show("Введите корректное количество.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(SaleTextBox.Text.Trim(), out int sale) || sale < 0 || sale > 100)
                {
                    MessageBox.Show("Введите корректную скидку (от 0 до 100).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Получаем объекты из БД
                var categoryObj = context.Categories.FirstOrDefault(c => c.Name == category);
                var manufacturerObj = context.Manufacturer.FirstOrDefault(m => m.Name == manufacturer);
                var supplierObj = context.Suppliers.FirstOrDefault(s => s.Name == supplier);
                var unitObj = context.Units.FirstOrDefault(u => u.Name == unit);

                if (categoryObj == null || manufacturerObj == null || supplierObj == null || unitObj == null)
                {
                    MessageBox.Show("Выберите корректные значения для всех полей.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Products product;
                if (productId.HasValue)
                {
                    product = context.Products.Find(productId.Value);
                    if (product == null) return;

                    product.Article = article;

                    var productNameObj = context.ProductName.FirstOrDefault(pn => pn.Name == name);
                    if (productNameObj == null)
                    {
                        productNameObj = new ProductName { Name = name };
                        context.ProductName.Add(productNameObj);
                    }
                    product.ProductName = productNameObj;
                }
                else
                {
                    product = new Products();
                    product.Article = article;

                    var productNameObj = context.ProductName.FirstOrDefault(pn => pn.Name == name);
                    if (productNameObj == null)
                    {
                        productNameObj = new ProductName { Name = name };
                        context.ProductName.Add(productNameObj);
                    }
                    product.ProductName = productNameObj;

                    context.Products.Add(product);
                }

                product.Categories = categoryObj;
                product.Description = description;
                product.Manufacturer = manufacturerObj;
                product.Suppliers = supplierObj;
                product.Cost = (int)Math.Round(price);
                product.Units = unitObj;
                product.Quantity = quantity;
                product.Sale = sale;

                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    if (!File.Exists(selectedImagePath))
                    {
                        MessageBox.Show("Указанный файл изображения не существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(selectedImagePath));
                    File.Copy(selectedImagePath, destPath, true);
                    if (!string.IsNullOrEmpty(product.Photo) && File.Exists(product.Photo))
                        File.Delete(product.Photo);
                    product.Photo = destPath;
                }

                context.SaveChanges();
                ProductSaved?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException exVal)
            {
                string errors = string.Join("\n", exVal.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(ev => $"{ev.PropertyName}: {ev.ErrorMessage}"));
                MessageBox.Show($"Ошибка при сохранении:\n{errors}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
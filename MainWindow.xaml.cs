using ArduinoCore;
using ArduinoCore.Data;
using ArduinoCore.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ArduinoApp
{
    public partial class MainWindow : Window
    {
        private readonly ArduinoManager _manager;
        private readonly DatabaseService _dbService;


        private Component _currentComponent;

        public MainWindow()
        {
            InitializeComponent();

            _dbService = new DatabaseService();
            _manager = new ArduinoManager(_dbService);

            LoadTopics();
            LoadComponents();
        }

        private void LoadTopics()
        {
            var topics = _manager.GetAllTopics();
            TopicsList.ItemsSource = topics;
            if (topics.Count > 0) TopicsList.SelectedIndex = 0;
        }

        private void LoadComponents()
        {
            var components = _manager.GetAllComponents();
            ComponentsList.ItemsSource = components;
        }

        private void TopicsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TopicsList.SelectedItem is Topic selectedTopic)
            {
                TopicTitle.Text = selectedTopic.Title;
                TopicContent.Text = selectedTopic.Content;
            }
        }

        private void ComponentsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComponentsList.SelectedItem is Component selectedComponent)
            {

                var fullComponent = _manager.FindComponent(selectedComponent.Name);
                if (fullComponent != null)
                {
                    ShowComponentCard(fullComponent);
                }
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) PerformSearch();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            string query = SearchBox.Text;
            try
            {
                var component = _manager.FindComponent(query);
                if (component != null)
                {
                    ShowComponentCard(component);


                    foreach (var item in ComponentsList.Items)
                    {
                        if (item is Component c && c.Name == component.Name)
                        {
                            ComponentsList.SelectedItem = c;
                            break;
                        }
                    }
                }
                else
                {
                    ShowError($"Компонент '{query}' не найден.");
                }
            }
            catch (System.ArgumentException ex)
            {
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Отображает карточку компонента с изображением и данными.
        /// </summary>
        private void ShowComponentCard(Component component)
        {
            _currentComponent = component;

            EmptyStateMessage.Visibility = Visibility.Collapsed;
            ErrorMessage.Visibility = Visibility.Collapsed;
            ComponentCardContainer.Visibility = Visibility.Visible;


            LoadComponentImage(component.ImageFileName);


            CardTitle.Text = component.Name;



            CardDescription.Text = component.Description;

            CardPins.Text = component.PinCount.ToString();
            CardVoltage.Text = $"{component.VoltageMin}V - {component.VoltageMax}V";

            CardSchema.Text = !string.IsNullOrEmpty(component.SchemaInfo)
                ? component.SchemaInfo
                : "Информация о схеме отсутствует.";

            CardCode.Text = !string.IsNullOrEmpty(component.ExampleCode)
                ? component.ExampleCode
                : "// Пример кода отсутствует";
        }

        /// <summary>
        /// Показывает сообщение об ошибке
        /// </summary>
        private void ShowError(string message)
        {
            _currentComponent = null;

            ComponentCardContainer.Visibility = Visibility.Collapsed;
            EmptyStateMessage.Visibility = Visibility.Collapsed;

            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Обработчик кнопки "Копировать код"
        /// </summary>
        private void BtnCopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (_currentComponent != null && !string.IsNullOrEmpty(_currentComponent.ExampleCode))
            {
                Clipboard.SetText(_currentComponent.ExampleCode);
                MessageBox.Show("Код скопирован в буфер обмена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Нет кода для копирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #region Методы работы с изображениями

        private void LoadComponentImage(string fileName)
        {

            string placeholderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "placeholder.png");

            if (!string.IsNullOrEmpty(fileName))
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", fileName);

                if (File.Exists(imagePath))
                {
                    SetImageSource(ComponentImage, imagePath);
                    return;
                }
            }

            if (File.Exists(placeholderPath))
            {
                SetImageSource(ComponentImage, placeholderPath);
            }
            else
            {

                ComponentImage.Source = null;
            }
        }

        private void SetImageSource(Image imageControl, string path)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                imageControl.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения {path}: {ex.Message}");
                imageControl.Source = null;
            }
        }

        #endregion

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            _dbService.Dispose();
        }
    }
}
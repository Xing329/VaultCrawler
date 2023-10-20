using System.Windows;

namespace VaultDataCrawlerNF
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new FileRequestViewModel();
        }
    }
}

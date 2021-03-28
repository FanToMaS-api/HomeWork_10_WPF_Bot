using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;



namespace HomeWork_10_WPF_Bot
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Telegram telegram;
        public MainWindow()
        {
            InitializeComponent();
            telegram = new Telegram(this);
            listBox.ItemsSource = telegram.messages;
        }

        private void SendOne(object sender, RoutedEventArgs e)
        {
            telegram.SendMessage(textForSending.Text, idTextBlock.Text, false);
            textForSending.Clear();
        }

        private void SendAll(object sender, RoutedEventArgs e)
        {
            telegram.SendMessage(textForSending.Text, idTextBlock.Text, true);
            textForSending.Clear();
        }
    }    
}

using System.Windows;
using System.Windows.Input;

namespace eartq
{
    public partial class ModernMessageBox : Window
    {
        public ModernMessageBox(string message, string title = "Bilgi")
        {
            InitializeComponent();
            TxtMessage.Text = message;
            TxtTitle.Text = title;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public static void Show(string message, string title = "Bilgi")
        {
            var msgBox = new ModernMessageBox(message, title);
            msgBox.ShowDialog();
        }
    }
}

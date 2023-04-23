using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Helion
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class GridViewWindow : Window
    {
        #region Konstruktor

        public GridViewWindow()
        {
            InitializeComponent();
        }

        #endregion Konstruktor

        #region Buttons

        private void Btn_Accept_Click(object sender, RoutedEventArgs e)
        {
            // Set the DialogResult property to true, indicating that the dialog was accepted
            DialogResult = true;
        }

        private void Btn_Decline_Click(object sender, RoutedEventArgs e)
        {
            // Set the DialogResult property to false, indicating that the dialog was declined
            DialogResult = false;
        }

        #endregion Buttons
       
    }
}

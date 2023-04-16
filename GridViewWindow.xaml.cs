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
using System.Windows.Shapes;

namespace Helion
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class GridViewWindow : Window
    {

        #region Felder

        private readonly string _AppDir;
        private readonly string _PathToListTXT;
        private readonly string _PathToListCSV;
        private readonly string _PathToAllShowCSV;

        private static GridViewWindow _GUI_GridViewWindow;

        #endregion Felder

        #region Konstruktor

        public GridViewWindow()
        {
            InitializeComponent();
            _GUI_GridViewWindow = this;
            _AppDir = (AppDomain.CurrentDomain.BaseDirectory);
            _PathToListTXT = (_AppDir + "\\list.txt");
            _PathToListCSV = (_AppDir + "\\list.csv");
            _PathToAllShowCSV = (_AppDir + "\\allshows.csv");

        }

        #endregion Konstruktor

        #region Public()

        #endregion Public()

        #region Buttons

        private void Btn_Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Btn_Decline_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        #endregion Buttons

        #region Private()

        #endregion Private()

        #region Events()

        #endregion Events()

    }
}

using System.Windows;

namespace Helion
{
  public partial class GridViewWindow : Window
  {
    #region ()

    public GridViewWindow()
    {
      InitializeComponent();
    }

    #endregion ()

    #region Buttons

    private void BTN_Accept_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }

    private void BTN_Decline_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    #endregion Buttons

  }
}

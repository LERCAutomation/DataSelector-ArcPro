using System.Windows;
using System.Windows.Input;

namespace DataSelector.UI
{
    /// <summary>
    /// Interaction logic for WindowChooseProfileView.xaml
    /// </summary>
    public partial class WindowChooseProfileView : Window
    {
        public WindowChooseProfileView()
        {
            InitializeComponent();
            Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }
}

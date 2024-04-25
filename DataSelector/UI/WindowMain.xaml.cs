using System.Windows;
using System.Windows.Input;

namespace DataSelector.UI
{
    /// <summary>
    /// Interaction logic for WindowMainView.xaml
    /// </summary>
    public partial class WindowMainView : Window
    {
        public WindowMainView()
        {
            InitializeComponent();
            Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void ListBoxTables_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.ButtonLoadColumns.Command.CanExecute(null))
                this.ButtonLoadColumns.Command.Execute(null);
        }
    }
}

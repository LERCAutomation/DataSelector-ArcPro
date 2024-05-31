using System.Windows.Controls;
using System.Windows.Input;

namespace DataSelector.UI
{
    /// <summary>
    /// Interaction logic for PaneHeader2View.xaml
    /// </summary>
    public partial class PaneHeader2View : UserControl
    {
        public PaneHeader2View()
        {
            InitializeComponent();
        }

        private void TextColumns_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.ButtonLoadColumns.Command.CanExecute(null))
                this.ButtonLoadColumns.Command.Execute(null);
        }
    }
}
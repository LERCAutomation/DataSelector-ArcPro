using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSelector.UI
{
    internal abstract class PanelViewModelBase : PropertyChangedBase
    {
        #region Properties

        public abstract string DisplayName { get; }

        #endregion Properties
    }
}

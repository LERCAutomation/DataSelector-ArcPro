using ArcGIS.Desktop.Framework;
using DataTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Text.RegularExpressions;
using DataSelector.Properties;
using System.Windows;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;
using System.Windows.Media;

namespace DataSelector.UI
{
    internal class PaneHeader1ViewModel : PanelViewModelBase, INotifyPropertyChanged
    {

        #region Fields

        private DockpaneMainViewModel _dockPane;

        private String _displayName = "DataSelector";

        private string _configFile = null;

        #endregion

        #region ViewModelBase Members

        //public override string DisplayName
        //{
        //    get { return "Select XML Profile"; }
        //}

        public override string DisplayName
        {
            get { return _displayName; }
            //set { _displayName = value; }
        }

        #endregion

        #region Creator

        /// <summary>
        /// Set the global variables
        /// </summary>
        /// <param name="xmlFilesList"></param>
        /// <param name="defaultXMLFile"></param>
        public PaneHeader1ViewModel(DockpaneMainViewModel dockPane)
        {
            _dockPane = dockPane;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Get the app XML config file path and name from settings.
            _xmlFolder = Settings.Default.XMLFolder;

            // Open the app XML config file and determine if the user will
            // choose which tool XML config file to load or if the default
            // file will be used.
            LaunchConfig launchConfig;
            launchConfig = new(_xmlFolder, _displayName, false);

            //// If the app config file can't be found.
            //if (!launchConfig.XMLFound)
            //{
            //    MessageBox.Show("XML file 'DataSelector.xml' not found in folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return false;
            //}
            //// If the app config file hasn't been loaded.
            //else if (!launchConfig.XMLLoaded)
            //{
            //    MessageBox.Show("Error loading XML File 'DataSelector.xml.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return false;
            //}

            // If the app config file can't be found or loaded.
            if (!launchConfig.XMLFound || !launchConfig.XMLLoaded)
                return;

            List<string> xmlFilesList = new();
            bool blOnlyDefault = false;
            bool blDefaultFound = false;

            // If the user is allowed to choose the XML profile then
            // check if there are multiple profiles to choose from.
            if (launchConfig.ChooseConfig)
            {
                // Get a list of all of the valid XML profiles in the folder.
                GetValidXMLFiles(_xmlFolder, launchConfig.DefaultXML, ref xmlFilesList, ref blDefaultFound, ref blOnlyDefault);

                if (xmlFilesList is null || xmlFilesList.Count() == 0)
                {
                    //MessageBox.Show("No valid XML files found in the XML directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // If the user is allowed to choose the XML profile and there are
            // more then just the default profile in the folder, load the
            // list of files for the user to choose.
            if (launchConfig.ChooseConfig && !blOnlyDefault)
            {
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = launchConfig.DefaultXML;
            }
            else
            {
                // If the user isn't allowed to choose, or if there is only the
                // default XML file in the directory, then use the default.
                _configFile = _xmlFolder + @"\" + launchConfig.DefaultXML;

                // Check the default XML file exists.
                if (!FileFunctions.FileExists(_configFile))
                {
                    //MessageBox.Show("The default XML file '" + launchConfig.DefaultXML + "' was not found in the XML directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Add the default file to the list and select it.
                xmlFilesList = new();
                xmlFilesList.Add(launchConfig.DefaultXML);
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = launchConfig.DefaultXML;
            }

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(XMLFolder));
            OnPropertyChanged(nameof(AvailableXMLFiles));
            OnPropertyChanged(nameof(SelectedXMLProfile));

            OnPropertyChanged(nameof(CanSelectXMLPath));
            OnPropertyChanged(nameof(CanOpenXML));

            // Cancel if no XML config file has been selected.
            if (_configFile == null) return;

            // Load the default profile.
            LoadConfig(_configFile);

            // Exit if the XML wasn't loaded.
            if (!XMLLoaded)
                return;
        }

        #endregion

        #region SelectXMLPath Command

        private ICommand _selectXMLPathCommand;

        /// <summary>
        /// Create SelectXMLPath button command
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand SelectXMLPathCommand
        {
            get
            {
                if (_selectXMLPathCommand == null)
                {
                    Action<object> selectXMLAction = new Action<object>(this.SelectXMLPathCommandClick);
                    _selectXMLPathCommand = new RelayCommand(selectXMLAction, param => this.CanSelectXMLPath);
                }

                return _selectXMLPathCommand;
            }
        }

        /// <summary>
        /// Handles event when SelectXMLPath button is clicked
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private void SelectXMLPathCommandClick(object param)
        {
            // Cancel if unable to get the name of the tool XML
            // config file to load.
            if (!GetConfigFileName()) return;

            // Cancel if no XML config file has been selected.
            if (_configFile == null) return;

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(XMLFolder));
            OnPropertyChanged(nameof(AvailableXMLFiles));
            OnPropertyChanged(nameof(SelectedXMLProfile));

            OnPropertyChanged(nameof(CanSelectXMLPath));
            OnPropertyChanged(nameof(CanOpenXML));

            //// Load the default profile.
            //LoadConfig(_configFile);
        }

        /// <summary>
        /// Can the SelectXMLPath button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CanSelectXMLPath { get { return true; } }

        #endregion

        #region Select XML Profile

        private List<string> _availableXMLFiles;

        /// <summary>
        /// List of valid XML profiles that the user can choose from.
        /// </summary>
        public List<string> AvailableXMLFiles
        {
            get
            {
                return _availableXMLFiles;
            }
            set => SetProperty(ref _availableXMLFiles, value);
        }

        private String _selectedXMLProfile;

        /// <summary>
        /// The XML profile that the user has chosen.
        /// </summary>
        public string SelectedXMLProfile
        {
            get
            {
                return _selectedXMLProfile;
            }
            set => SetProperty(ref _selectedXMLProfile, value);
        }

        /// <summary>
        /// Can the user select an XML profile?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CanSelectXMLProfile
        {
            get
            {
                return (!String.IsNullOrEmpty(XMLFolder));
            }
        }

        #endregion

        #region Open XML Command

        private ICommand _openXMLCommand;

        /// <summary>
        /// Create Open XML button command
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand OpenXMLCommand
        {
            get
            {
                if (_openXMLCommand == null)
                {
                    Action<object> openXMLAction = new Action<object>(this.OpenXMLCommandClick);
                    _openXMLCommand = new RelayCommand(openXMLAction, param => this.CanOpenXML);
                }

                return _openXMLCommand;
            }
        }

        /// <summary>
        /// Handles event when Open XML button is clicked
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private void OpenXMLCommandClick(object param)
        {
            // Skip if no profile selected (shouldn't be possible).
            if (SelectedXMLProfile == null)
                return;

            // Set the full path for the profile file.
            string configFile = _xmlFolder + @"\" + SelectedXMLProfile;

            // Check the file (still) exists.
            if (!FileFunctions.FileExists(configFile))
            {
                MessageBox.Show("The selected XML file '" + SelectedXMLProfile + "' was not found in the XML directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Load the selected profile.
            LoadConfig(configFile);

            // Exit if the XML wasn't loaded.
            if (!XMLLoaded)
                return;

            // Initialise the query pane.
            if (!_dockPane.InitialiseQueryPane()) return;

            // Select the query pane.
            _dockPane.SelectedPanelHeaderIndex = 1;
        }

        /// <summary>
        /// Can the Open XML button be pressed (has a profile been selected)?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool CanOpenXML { get { return !String.IsNullOrEmpty(SelectedXMLProfile); } }

        #endregion

        #region Properties

        private SelectorToolConfig _toolConfig;

        public SelectorToolConfig ToolConfig
        {
            get
            {
                return _toolConfig;
            }
        }

        private bool _xmlLoaded = false;

        public bool XMLLoaded
        {
            get
            {
                return _xmlLoaded;
            }
        }

        private string _xmlFolder = null;

        public string XMLFolder
        {
            get
            {
                return _xmlFolder;
            }
            set => SetProperty(ref _xmlFolder, value);
        }

        private ImageSource _imageFolderOpen;

        public ImageSource CmdFolderOpenImg
        {
            get
            {
                if (_imageFolderOpen == null)
                    _imageFolderOpen = System.Windows.Application.Current.Resources["FolderOpenState16"] as ImageSource;
                return _imageFolderOpen;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Read the app config file to see if a default XML file is
        /// found or if the user will be prompted to choose one, and
        /// if the user is allowed to choose the XML profile.
        /// </summary>
        private bool GetConfigFileName()
        {
            // Open the app XML config file and determine if the user will
            // choose which tool XML config file to load or if the default
            // file will be used.
            LaunchConfig launchConfig;
            launchConfig = new(_xmlFolder, _displayName, true);

            // If the user didn't select a folder when prompted.
            if (launchConfig.SelectCancelled)
                return false;

            // If the app config file can't be found.
            if (!launchConfig.XMLFound)
            {
                MessageBox.Show("XML file 'DataSelector.xml' not found in folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            // If the app config file hasn't been loaded.
            else if (!launchConfig.XMLLoaded)
            {
                MessageBox.Show("Error loading XML File 'DataSelector.xml.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Set the folder path containing the app config file.
            _xmlFolder = launchConfig.XMLFolder;

            // As the app config file exists and has loaded then
            // save the folder path to settings for the future.
            Settings.Default.XMLFolder = _xmlFolder;
            Settings.Default.Save();

            List<string> xmlFilesList = new();
            bool blOnlyDefault = false;
            bool blDefaultFound = false;

            // If the user is allowed to choose the XML profile then
            // check if there are multiple profiles to choose from.
            if (launchConfig.ChooseConfig)
            {
                // Get a list of all of the valid XML profiles in the folder.
                GetValidXMLFiles(_xmlFolder, launchConfig.DefaultXML, ref xmlFilesList, ref blDefaultFound, ref blOnlyDefault);

                if (xmlFilesList is null || xmlFilesList.Count() == 0)
                {
                    MessageBox.Show("No valid XML files found in the XML directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            // If the user is allowed to choose the XML profile and there are
            // more then just the default profile in the folder, load the
            // list of files for the user to choose.
            if (launchConfig.ChooseConfig && !blOnlyDefault)
            {
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = launchConfig.DefaultXML;
            }
            else
            {
                // If the user isn't allowed to choose, or if there is only the
                // default XML file in the directory, then use the default.
                _configFile = _xmlFolder + @"\" + launchConfig.DefaultXML;

                // Check the default XML file exists.
                if (!FileFunctions.FileExists(_configFile))
                {
                    MessageBox.Show("The default XML file '" + launchConfig.DefaultXML + "' was not found in the XML directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Add the default file to the list and select it.
                xmlFilesList = new();
                xmlFilesList.Add(launchConfig.DefaultXML);
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = launchConfig.DefaultXML;
            }

            return true;
        }

        /// <summary>
        /// Get a list of valid XML files from the specified folder, and check
        /// if any of the files is the default profile and if only the
        /// </summary>
        /// <param name="strXMLFolder"></param>
        /// <param name="strDefaultXMLName"></param>
        /// <param name="xmlFilesList"></param>
        /// <param name="blDefaultFound"></param>
        /// <param name="blOnlyDefault"></param>
        private void GetValidXMLFiles(string strXMLFolder, string strDefaultXMLName, ref List<string> xmlFilesList, ref bool blDefaultFound, ref bool blOnlyDefault)
        {
            blDefaultFound = false;
            blOnlyDefault = true;

            // Get a list of all of the files in the XML directory.
            List<string> allFilesList = FileFunctions.GetAllFilesInDirectory(strXMLFolder);

            // Loop through the list for valid XML files.
            xmlFilesList = new();
            foreach (string strFile in allFilesList)
            {
                // Add if it's not the app XML file.
                string strFileName = FileFunctions.GetFileName(strFile);
                if (FileFunctions.GetFileNameWithoutExtension(strFileName).ToLower() != _displayName.ToLower()
                && FileFunctions.GetExtension(strFile).ToLower() == ".xml")
                {
                    // Add file to list of XML files.
                    xmlFilesList.Add(strFileName);
                    if (strFileName.ToLower() == strDefaultXMLName.ToLower())
                        blDefaultFound = true;
                    else
                        blOnlyDefault = false;
                }
            }

            // Sort the list of XML files.
            xmlFilesList.Sort();
        }

        public void LoadConfig(string configFile)
        {
            _configFile = configFile;

            // Read the selected XML config file.
            _toolConfig = new(_configFile);

            // If the XML config file can't be found then close
            // the app.
            if (!_toolConfig.XMLFound)
            {
                MessageBox.Show(String.Format("XML file '{0}' not found; form cannot load.", _configFile), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
            }
            // If the tool XML config file hasn't been loaded.
            else if (!_toolConfig.XMLLoaded)
            {
                MessageBox.Show(String.Format("Error loading XML File '{0}'; form cannot load.", _configFile), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
            }

            // Indicate the XML has been loaded.
            _xmlLoaded = true;
        }

        #endregion

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        internal virtual void OnPropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChangedEventArgs e = new(propertyName);
                handler(this, e);
            }
        }

        #endregion

    }
}

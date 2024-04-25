using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using DataSelector.Properties;
using DataTools;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;

namespace DataSelector.UI
{
    internal class WindowChooseProfileViewModel : ViewModelBase
    {

        #region Fields

        private List<string> _availableXMLFiles;
        private String _selectedXMLProfile;
        private ICommand _okCommand;
        private ICommand _cancelCommand;

        #endregion

        #region Creator

        /// <summary>
        /// Set the global variables
        /// </summary>
        /// <param name="xmlFilesList"></param>
        /// <param name="defaultXMLFile"></param>
        public WindowChooseProfileViewModel(List<string> xmlFilesList, String defaultXMLFile)
        {
            if (xmlFilesList != null)
            {
                _availableXMLFiles = xmlFilesList;
                _selectedXMLProfile = defaultXMLFile;
            }
        }

        #endregion

        #region RequestClose

        public delegate void RequestCloseEventHandler(bool chosen, String chosenXMLFile);

        public event RequestCloseEventHandler RequestClose;

        #endregion

        #region Ok Command

        /// <summary>
        /// Create Ok button command
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand OkCommand
        {
            get
            {
                if (_okCommand == null)
                {
                    Action<object> okAction = new Action<object>(this.OkCommandClick);
                    _okCommand = new RelayCommand(okAction, param => this.CanOk);
                }

                return _okCommand;
            }
        }

        /// <summary>
        /// Handles event when Ok button is clicked
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private void OkCommandClick(object param)
        {
            this.RequestClose(true, _selectedXMLProfile);
        }

        /// <summary>
        /// Can the Ok button be pressed (has a profile been selected)?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool CanOk { get { return !String.IsNullOrEmpty(SelectedXMLProfile); } }

        #endregion

        #region Cancel Command

        /// <summary>
        /// Create Cancel button command
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                {
                    Action<object> cancelAction = new Action<object>(this.CancelCommandClick);
                    _cancelCommand = new RelayCommand(cancelAction, param => this.CanCancel);
                }
                return _cancelCommand;
            }
        }

        /// <summary>
        /// Handles event when Cancel button is clicked
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private void CancelCommandClick(object param)
        {
            this.RequestClose(false, null);
        }

        /// <summary>
        /// Can the cancel button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool CanCancel { get { return true; } }

        #endregion

        #region Properties

        /// <summary>
        /// List of valid XML profiles that the user can choose from.
        /// </summary>
        public List<string> AvailableXMLFiles
        {
            get
            {
                return _availableXMLFiles;
            }
            set { }
        }

        /// <summary>
        /// The XML profile that the user has chosen.
        /// </summary>
        public string SelectedXMLProfile
        {
            get
            {
                return _selectedXMLProfile;
            }
            set { _selectedXMLProfile = value; }
        }

        #endregion

    }

    internal class WindowMain_ShowButton : ArcGIS.Desktop.Framework.Contracts.Button
    {
        #region Fields

        private WindowChooseProfileView _windowChooseProfile;
        private WindowChooseProfileViewModel _viewModelChooseProfile;

        private WindowMainView _windowMain;
        private WindowMainViewModel _viewModelMain;

        readonly string _toolName = "DataSelector";

        private string _xmlFolder = null;
        private string _configFile = null;

        private bool canOpenForm; // This tracks all the way through whether the form is initialising correctly.

        #endregion

        #region OnClick

        /// <summary>
        /// When the add-in button has been clicked.
        /// </summary>
        protected override void OnClick()
        {
            canOpenForm = true;

            // Cancel if unable to get the name of the tool XML
            // config file to load.
            if (!GetConfigFileName()) return;

            // Cancel if no XML config file has been selected.
            if (_configFile == null) return;

            // Read the selected XML config file.
            SelectorToolConfig toolConfig;
            toolConfig = new(_configFile);

            // If the XML config file can't be found then close
            // the app.
            if (!toolConfig.XMLFound)
            {
                MessageBox.Show(String.Format("XML file '{0}' not found; form cannot load.", _configFile), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                canOpenForm = false;
            }
            // If the tool XML config file hasn't been loaded.
            else if (!toolConfig.XMLLoaded)
            {
                MessageBox.Show(String.Format("Error loading XML File '{0}'; form cannot load.", _configFile), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                canOpenForm = false;
            }

            // Launch main window.
            if (canOpenForm)
            {
                // Open the main window.
                _windowMain = new()
                {
                    //_windowMain.Owner = DataSelector.Current.MainWindow;
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                };

                WindowMainViewModel viewModelWindowMain = new(toolConfig);
                _viewModelMain = viewModelWindowMain;
                _viewModelMain.RequestClose +=
                    new WindowMainViewModel.RequestCloseEventHandler(ViewModelMain_RequestClose);

                // Trigger getting the SQL Server table names but don't wait for the response.
                _viewModelMain.GetTableNames();

                // Show the main window.
                _windowMain.DataContext = _viewModelMain;
                _windowMain.ShowDialog();
            }
        }

        /// <summary>
        /// Read the app config file to see if a default XML file is
        /// found or if the user will be prompted to choose one, and
        /// if the user is allowed to choose the XML profile.
        /// </summary>
        private bool GetConfigFileName()
        {
            // Get the app XML config file path and name from settings.
            string xmlFile = Settings.Default.XMLFile;

            // Open the app XML config file and determine if the user will
            // choose which tool XML config file to load or if the default
            // file will be used.
            LaunchConfig launchConfig;
            launchConfig = new(xmlFile, _toolName);

            // If the app config file can't be found then close
            // the app.
            if (!launchConfig.XMLFound)
            {
                MessageBox.Show("XML file 'DataSelector.xml' not found; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            // If the app config file hasn't been loaded.
            else if (!launchConfig.XMLLoaded)
            {
                MessageBox.Show("Error loading XML File 'DataSelector.xml'; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // As the app config file exists and has loaded then
            // save the path and name to settings for the future.
            Settings.Default.XMLFile = xmlFile;
            Settings.Default.Save();

            List<string> xmlFilesList = new();
            bool blOnlyDefault = false;
            bool blDefaultFound = false;

            // Get the folder name containing the app config file.
            _xmlFolder = FileFunctions.GetDirectoryName(Settings.Default.XMLFile);

            // If the user is allowed to choose the XML profile then
            // check if there are multiple profiles to choose from.
            if (launchConfig.ChooseConfig)
            {
                // Get a list of all of the valid XML profiles in the folder.
                GetValidXMLFiles(_xmlFolder, launchConfig.DefaultXML, ref xmlFilesList, ref blDefaultFound, ref blOnlyDefault);
            }

            // If the to user is allowed to choose the XML profile and there are
            // more then just the default profile in the folder, prompt the user
            // choose.
            if (launchConfig.ChooseConfig && !blOnlyDefault)
            {
                // Prompt the user has to choose the XML profile to use.
                _windowChooseProfile = new()
                {
                    //_windowChooseProfile.Owner = DataSelector.Current.MainWindow;
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                };

                _viewModelChooseProfile = new(xmlFilesList, launchConfig.DefaultXML);
                _viewModelChooseProfile.RequestClose +=
                    new WindowChooseProfileViewModel.RequestCloseEventHandler(ViewModelChooseProfile_RequestClose);

                _windowChooseProfile.DataContext = _viewModelChooseProfile;
                _windowChooseProfile.ShowDialog();
            }
            else
            {
                // If the user isn't allowed to choose, or if there is only the
                // default XML file in the directory, then use the default.
                _configFile = _xmlFolder + @"\" + launchConfig.DefaultXML;

                // Check the default XML file exists.
                if (!FileFunctions.FileExists(_configFile))
                {
                    MessageBox.Show("The default XML file '" + launchConfig.DefaultXML + "' was not found in the XML directory; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Handle the choose profile window close request.
        /// </summary>
        /// <param name="xmlChosen"></param>
        /// <param name="chosenXMLFile"></param>
        void ViewModelChooseProfile_RequestClose(bool xmlChosen, String chosenXMLFile)
        {
            // Close the form.
            _viewModelChooseProfile.RequestClose -= ViewModelChooseProfile_RequestClose;
            _windowChooseProfile.Close();

            // If a profile has been chosen then set the file path and name.
            if ((xmlChosen) && !(String.IsNullOrEmpty(chosenXMLFile)))
            {
                _configFile = _xmlFolder + @"\" + chosenXMLFile;
            }
            else
            {
                MessageBox.Show("No XML file was chosen; form cannot load.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                canOpenForm = false;
            }
        }

        /// <summary>
        /// Handle the main window close request.
        /// </summary>
        void ViewModelMain_RequestClose()
        {
            _viewModelMain.RequestClose -= ViewModelMain_RequestClose;
            _windowMain.Close();
        }

        #endregion

        #region Methods

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
                if (FileFunctions.GetFullNameWithoutExtension(strFileName).ToLower() != _toolName.ToLower()
                && FileFunctions.GetExtension(strFile).ToLower() == "xml")
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

        #endregion

    }
}

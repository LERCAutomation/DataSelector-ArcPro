// The Data tools are a suite of ArcGIS Pro addins used to extract
// and manage biodiversity information from ArcGIS Pro and SQL Server
// based on pre-defined or user specified criteria.
//
// Copyright © 2024 Andy Foy Consulting.
//
// This file is part of DataSelector.
//
// DataSelector is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// DataSelector is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with DataSelector.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Windows.Forms;
using System.Xml;
using System.Windows;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

//This configuration file reader loads all of the variables to
// be used by the tool. Some are mandatory, the remainder optional.

namespace DataSelector
{
    /// <summary>
    /// This class reads the config XML file and stores the results.
    /// </summary>
    class SelectorToolConfig
    {

        #region Fields

        // Initialise component to read XML
        private XmlElement _xmlDataSelector;

        #endregion

        #region Constructor

        public SelectorToolConfig(string xmlFile)
        {

            // The user has specified the xmlFile and we've checked it exists.
            _xmlFound = true;
            _xmlLoaded = true;

            // Load the XML file into memory.
            XmlDocument xmlConfig = new XmlDocument();
            try
            {
                xmlConfig.Load(xmlFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading XML file. System error message: " + ex.Message, "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
                return;
            }

            // Get the InitialConfig node (the first node).
            XmlNode currNode = xmlConfig.DocumentElement.FirstChild;
            _xmlDataSelector = (XmlElement)currNode;

            if (_xmlDataSelector == null)
            {
                MessageBox.Show("Error loading XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
                return;
            }

            // Get the mandatory variables.
            if (!GetMandatoryVariables())
            {
                _xmlLoaded = false;
                return;
            }

            // Get the optional variables.
            GetOptionalVariables();
        }

        public bool GetMandatoryVariables()
        {
            // The existing file location where log files will be saved with output messages.
            try
            {
                _logFilePath = _xmlDataSelector["LogFilePath"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'LogFilePath' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // The location of the SDE file that specifies which SQL Server database to connect to.
            try
            {
                _sdeName = _xmlDataSelector["SDEFile"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'SDEFile' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Stored procedure to execute selection in SQL Server.
            try
            {
                _selectStoredProcedure = _xmlDataSelector["SelectStoredProcedure"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'SelectStoredProcedure' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Stored procedure to clear selection in SQL Server.
            try
            {
                _clearStoredProcedure = _xmlDataSelector["ClearStoredProcedure"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'ClearStoredProcedure' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // The schema used in the SQL Server database.
            try
            {
                _databaseSchema = _xmlDataSelector["DatabaseSchema"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'DatabaseSchema' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // The Include wildcard for table names to list all the species tables
            // in SQL Server that can be selected by the user to extract from.
            try
            {
                _includeWildcard = _xmlDataSelector["IncludeWildcard"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'IncludeWildcard' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // The Exclude wildcard for table names that should NOT be used
            // for species tables in SQL Server that can be selected by the
            // user to extract from.
            try
            {
                _excludeWildcard = _xmlDataSelector["ExcludeWildcard"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'ExcludeWildcard' in the XML file.", "XML Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // All mandatory variables were loaded successfully.
            return true;
        }

        public void GetOptionalVariables()
        {
            string strRawText;

            // The existing file location where extracts will be saved by default.
            try
            {
                _defaultExtractPath = _xmlDataSelector["DefaultExtractPath"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'DefaultExtractPath' in the XML file.", "XML Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // The existing file location where queries will be saved and loaded by default.
            try
            {
                _defaultQueryPath = _xmlDataSelector["DefaultQueryPath"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'DefaultQueryPath' in the XML file.", "XML Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // The default format of the output files to be created.
            try
            {
                _defaultFormat = _xmlDataSelector["DefaultFormat"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'DefaultFormat' in the XML file.", "XML Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // The default for whether the symbology should be set for feature classes or not.
            try
            {
                _defaultSetSymbology = false;
                strRawText = _xmlDataSelector["DefaultSetSymbology"].InnerText;
                if (strRawText.ToLower() == "yes" || strRawText.ToLower() == "y")
                    _defaultSetSymbology = true;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'DefaultSetSymbology' in the XML file.", "XML Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // The location of layer files.
            try
            {
                _layerLocation = _xmlDataSelector["LayerLocation"].InnerText;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'LayerLocation' in the XML file.", "XML Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // The default for whether an existing log file should be cleared by default.
            try
            {
                _defaultClearLogFile = false;
                strRawText = _xmlDataSelector["DefaultClearLogFile"].InnerText;
                if (strRawText.ToLower() == "yes" || strRawText.ToLower() == "y")
                    _defaultClearLogFile = true;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'DefaultClearLogFile' in the XML file.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // The default for whether the log file should be opened by default.
            try
            {
                _defaultOpenLogFile = false;
                strRawText = _xmlDataSelector["DefaultOpenLogFile"].InnerText;
                if (strRawText.ToLower() == "yes" || strRawText.ToLower() == "y")
                    _defaultOpenLogFile = true;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'DefaultOpenLogFile' in the XML file.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Whether to validate the SQL before running.
            try
            {
                _validateSQL = false;
                strRawText = _xmlDataSelector["ValidateSQL"].InnerText;
                if (strRawText.ToLower() == "yes" || strRawText.ToLower() == "y")
                    _validateSQL = true;
            }
            catch
            {
                MessageBox.Show("Could not locate item 'ValidateSQL' in the XML file.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        #endregion

        #region Members

        private bool _xmlFound;

        /// <summary>
        /// Has the XML file been found.
        /// </summary>
        public bool XMLFound
        {
            get
            {
                return _xmlFound;
            }
        }

        private bool _xmlLoaded;

        /// <summary>
        ///  Has the XML file been loaded.
        /// </summary>
        public bool XMLLoaded
        {
            get
            {
                return _xmlLoaded;
            }
        }

        private string _sdeName;

        public string GetSDEName
        {
            get
            {
                return _sdeName;
            }
        }

        private string _selectStoredProcedure;

        public string GetSelectStoredProcedure
        {
            get
            {
                return _selectStoredProcedure;
            }
        }

        private string _clearStoredProcedure;

        public string GetClearStoredProcedure
        {
            get
            {
                return _clearStoredProcedure;
            }
        }

        private string _logFilePath;

        public string GetLogFilePath
        {
            get
            {
                return _logFilePath;
            }
        }

        private string _defaultExtractPath;

        public string GetDefaultExtractPath
        {
            get
            {
                return _defaultExtractPath;
            }
        }

        private string _defaultQueryPath;

        public string GetDefaultQueryPath
        {
            get
            {
                return _defaultQueryPath;
            }
        }

        private string _defaultFormat;

        public string GetDefaultFormat
        {
            get
            {
                return _defaultFormat;
            }
        }

        private string _databaseSchema;

        public string GetDatabaseSchema
        {
            get
            {
                return _databaseSchema;
            }
        }

        private string _includeWildcard;

        public string GetIncludeWildcard
        {
            get
            {
                return _includeWildcard;
            }
        }

        private string _excludeWildcard;

        public string GetExcludeWildcard
        {
            get
            {
                return _excludeWildcard;
            }
        }

        private bool _defaultSetSymbology;

        public bool GetDefaultSetSymbology
        {
            get
            {
                return _defaultSetSymbology;
            }
        }

        private string _layerLocation;

        public string GetLayerLocation
        {
            get
            {
                return _layerLocation;
            }
        }

        private bool _defaultClearLogFile;

        public bool GetDefaultClearLogFile
        {
            get
            {
                return _defaultClearLogFile;
            }
        }

        private bool _defaultOpenLogFile;

        public bool GetDefaultOpenLogFile
        {
            get
            {
                return _defaultOpenLogFile;
            }
        }

        private bool _validateSQL;

        public bool GetValidateSQL
        {
            get
            {
                return _validateSQL;
            }
        }

        #endregion

    }
}
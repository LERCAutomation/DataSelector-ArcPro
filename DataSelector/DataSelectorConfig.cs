﻿// The DataTools are a suite of ArcGIS Pro addins used to extract, sync
// and manage biodiversity information from ArcGIS Pro and SQL Server
// based on pre-defined or user specified criteria.
//
// Copyright © 2024-25 Andy Foy Consulting.

//
// This file is part of DataTools suite of programs.
//
// DataTools are free software: you can redistribute it and/or modify
// them under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// DataTools are distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with with program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Windows;
using System.Xml;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

//This configuration file reader loads all of the variables to
// be used by the tool. Some are mandatory, the remainder optional.

namespace DataSelector
{
    /// <summary>
    /// This class reads the config XML file and stores the results.
    /// </summary>
    internal class DataSelectorConfig
    {
        #region Fields

        private static string _toolName;

        // Initialise component to read XML
        private readonly XmlElement _xmlDataSelector;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Load the XML profile and read the variables.
        /// </summary>
        /// <param name="xmlFile"></param>
        public DataSelectorConfig(string xmlFile, string toolName, bool msgErrors)
        {
            _toolName = toolName;

            // The user has specified the xmlFile and we've checked it exists.
            _xmlFound = true;
            _xmlLoaded = true;

            // Load the XML file into memory.
            XmlDocument xmlConfig = new();
            try
            {
                xmlConfig.Load(xmlFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading XML file. " + ex.Message, _toolName, MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
                return;
            }

            // Get the InitialConfig node (the first node).
            XmlNode currNode = xmlConfig.DocumentElement.FirstChild;
            _xmlDataSelector = (XmlElement)currNode;

            if (_xmlDataSelector == null)
            {
                MessageBox.Show("Error loading XML file.", _toolName, MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
                return;
            }

            // Get the mandatory variables.
            try
            {
                if (!GetMandatoryVariables())
                    return;
            }
            catch (Exception ex)
            {
                // Only report message if user was prompted for the XML
                // file (i.e. the user interface has already loaded).
                if (msgErrors)
                    MessageBox.Show("Error loading XML file. " + ex.Message, _toolName, MessageBoxButton.OK, MessageBoxImage.Error);
                _xmlLoaded = false;
                return;
            }

            // Get the optional variables.
            try
            {
                GetOptionalVariables();
            }
            catch (Exception ex)
            {
                // Only report message if user was prompted for the XML
                // file (i.e. the user interface has already loaded).
                if (msgErrors)
                    MessageBox.Show("Warning loading XML file. " + ex.Message, _toolName, MessageBoxButton.OK, MessageBoxImage.Warning);
                _xmlLoaded = false;
                return;
            }
        }

        #endregion Constructor

        #region Get Mandatory Variables

        /// <summary>
        /// Get the mandatory variables from the XML file.
        /// </summary>
        /// <returns></returns>
        public bool GetMandatoryVariables()
        {
            // The existing file location where log files will be saved with output messages.
            try
            {
                _logFilePath = _xmlDataSelector["LogFilePath"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'LogFilePath' in the XML profile.");
            }

            // The location of the SDE file that specifies which SQL Server database to connect to.
            try
            {
                _sdeFile = _xmlDataSelector["SDEFile"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'SDEFile' in the XML profile.");
            }

            // Stored procedure to execute selection in SQL Server.
            try
            {
                _selectStoredProcedure = _xmlDataSelector["SelectStoredProcedure"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'SelectStoredProcedure' in the XML profile.");
            }

            // Stored procedure to clear selection in SQL Server.
            try
            {
                _clearStoredProcedure = _xmlDataSelector["ClearStoredProcedure"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'ClearStoredProcedure' in the XML profile.");
            }

            // The schema used in the SQL Server database.
            try
            {
                _databaseSchema = _xmlDataSelector["DatabaseSchema"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'DatabaseSchema' in the XML profile.");
            }

            // The table used to list the spatial objects in the SQL Server database.
            try
            {
                _objectsTable = _xmlDataSelector["ObjectsTable"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'ObjectsTable' in the XML profile.");
            }

            // The Include wildcard for table names to list all the species tables
            // in SQL Server that can be selected by the user to extract from.
            try
            {
                _includeWildcard = _xmlDataSelector["IncludeWildcard"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'IncludeWildcard' in the XML profile.");
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
                throw new("Could not locate item 'ExcludeWildcard' in the XML profile.");
            }

            // All mandatory variables were loaded successfully.
            return true;
        }

        #endregion Get Mandatory Variables

        #region Get Optional Variables

        /// <summary>
        /// Get the optional variables from the XML file.
        /// </summary>
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
                throw new("Could not locate item 'DefaultExtractPath' in the XML profile.");
            }

            // The existing file location where queries will be saved and loaded by default.
            try
            {
                _defaultQueryPath = _xmlDataSelector["DefaultQueryPath"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'DefaultQueryPath' in the XML profile.");
            }

            // The default format of the output files to be created.
            try
            {
                _defaultFormat = _xmlDataSelector["DefaultFormat"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'DefaultFormat' in the XML profile.");
            }

            // The default for whether the symbology should be set for feature classes or not.
            try
            {
                _defaultSetSymbology = false;
                strRawText = _xmlDataSelector["DefaultSetSymbology"].InnerText;
                if (strRawText.ToLower(System.Globalization.CultureInfo.CurrentCulture) is "yes" or "y")
                    _defaultSetSymbology = true;
            }
            catch
            {
                throw new("Could not locate item 'DefaultSetSymbology' in the XML profile.");
            }

            // The location of layer files.
            try
            {
                _layerLocation = _xmlDataSelector["LayerLocation"].InnerText;
            }
            catch
            {
                throw new("Could not locate item 'LayerLocation' in the XML profile.");
            }

            // The default for whether an existing log file should be cleared by default.
            try
            {
                _defaultClearLogFile = false;
                strRawText = _xmlDataSelector["DefaultClearLogFile"].InnerText;
                if (strRawText.ToLower(System.Globalization.CultureInfo.CurrentCulture) is "yes" or "y")
                    _defaultClearLogFile = true;
            }
            catch
            {
                throw new("Could not locate item 'DefaultClearLogFile' in the XML profile.");
            }

            // The default for whether the log file should be opened by default.
            try
            {
                _defaultOpenLogFile = false;
                strRawText = _xmlDataSelector["DefaultOpenLogFile"].InnerText;
                if (strRawText.ToLower(System.Globalization.CultureInfo.CurrentCulture) is "yes" or "y")
                    _defaultOpenLogFile = true;
            }
            catch
            {
                throw new("Could not locate item 'DefaultOpenLogFile' in the XML profile.");
            }

            // Whether to validate the SQL before running.
            try
            {
                _validateSQL = false;
                strRawText = _xmlDataSelector["ValidateSQL"].InnerText;
                if (strRawText.ToLower(System.Globalization.CultureInfo.CurrentCulture) is "yes" or "y")
                    _validateSQL = true;
            }
            catch
            {
                throw new("Could not locate item 'ValidateSQL' in the XML profile.");
            }

            // The timeout in seconds for the SQL verify.
            try
            {
                strRawText = _xmlDataSelector["SQLTimeout"].InnerText;
                bool blResult = int.TryParse(strRawText, out int i);
                if (blResult)
                    _sqlTimeout = (int)i;
                else
                {
                    throw new("The entry for 'SQLTimeout' in the XML document is not a number.");
                }
            }
            catch
            {
                // This is an optional node
                _sqlTimeout = 60;
            }

            // Whether to load the columns for the selected table as a vertical list.
            try
            {
                _loadColumnsVertically = false;
                strRawText = _xmlDataSelector["LoadColumnsVertically"].InnerText;
                if (strRawText.ToLower(System.Globalization.CultureInfo.CurrentCulture) is "yes" or "y")
                    _loadColumnsVertically = true;
            }
            catch
            {
                throw new("Could not locate item 'LoadColumnsVertically' in the XML profile.");
            }
        }

        #endregion Get Optional Variables

        #region Members

        private readonly bool _xmlFound;

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

        private readonly bool _xmlLoaded;

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

        #endregion Members

        #region Variables

        private string _sdeFile;

        public string SDEFile
        {
            get
            {
                return _sdeFile;
            }
        }

        private string _selectStoredProcedure;

        public string SelectStoredProcedure
        {
            get
            {
                return _selectStoredProcedure;
            }
        }

        private string _clearStoredProcedure;

        public string ClearStoredProcedure
        {
            get
            {
                return _clearStoredProcedure;
            }
        }

        private string _logFilePath;

        public string LogFilePath
        {
            get
            {
                return _logFilePath;
            }
        }

        private string _defaultExtractPath;

        public string DefaultExtractPath
        {
            get
            {
                return _defaultExtractPath;
            }
        }

        private string _defaultQueryPath;

        public string DefaultQueryPath
        {
            get
            {
                return _defaultQueryPath;
            }
        }

        private string _defaultFormat;

        public string DefaultFormat
        {
            get
            {
                return _defaultFormat;
            }
        }

        private string _databaseSchema;

        public string DatabaseSchema
        {
            get
            {
                return _databaseSchema;
            }
        }

        private string _objectsTable;

        public string ObjectsTable
        {
            get
            {
                return _objectsTable;
            }
        }

        private string _includeWildcard;

        public string IncludeWildcard
        {
            get
            {
                return _includeWildcard;
            }
        }

        private string _excludeWildcard;

        public string ExcludeWildcard
        {
            get
            {
                return _excludeWildcard;
            }
        }

        private bool _defaultSetSymbology;

        public bool DefaultSetSymbology
        {
            get
            {
                return _defaultSetSymbology;
            }
        }

        private string _layerLocation;

        public string LayerLocation
        {
            get
            {
                return _layerLocation;
            }
        }

        private bool _defaultClearLogFile;

        public bool DefaultClearLogFile
        {
            get
            {
                return _defaultClearLogFile;
            }
        }

        private bool _defaultOpenLogFile;

        public bool DefaultOpenLogFile
        {
            get
            {
                return _defaultOpenLogFile;
            }
        }

        private bool _validateSQL;

        public bool ValidateSQL
        {
            get
            {
                return _validateSQL;
            }
        }

        private int _sqlTimeout;

        public int SQLTimeout
        {
            get
            {
                return _sqlTimeout;
            }
        }

        private bool _loadColumnsVertically;

        public bool LoadColumnsVertically
        {
            get
            {
                return _loadColumnsVertically;
            }
        }

        #endregion Variables
    }
}
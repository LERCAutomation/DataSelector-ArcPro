// The DataTools are a suite of ArcGIS Pro addins used to extract, sync
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

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Internal.Framework.Controls;
using ArcGIS.Desktop.Mapping;
using DataTools;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace DataSelector.UI
{
    internal class PaneHeader2ViewModel : PanelViewModelBase, INotifyPropertyChanged
    {
        #region Fields

        private readonly DockpaneMainViewModel _dockPane;

        private string _sdeFileName;

        private bool _selectErrors;
        private string _successMessage;

        private string _logFile;
        private bool _validateSQL;
        private int _sqlTimeout;
        private bool _loadColumnsVertically;

        private string _logFilePath;
        private string _includeWildcard;
        private string _excludeWildcard;
        private string _defaultFormat;
        private string _defaultSchema;
        private string _objectsTable;
        private string _defaultQueryPath;
        private string _defaultExtractPath;
        private string _selectStoredProcedure;
        private string _clearStoredProcedure;

        private string _queryName;

        private long _tableCount = 0;
        private long _polyCount = 0;
        private long _pointCount = 0;

        private const string _displayName = "DataSelector";

        private readonly DataSelectorConfig _toolConfig;
        private MapFunctions _mapFunctions;
        private SQLServerFunctions _sqlFunctions;

        #endregion Fields

        #region ViewModelBase Members

        public override string DisplayName
        {
            get { return _displayName; }
        }

        #endregion ViewModelBase Members

        #region Creator

        /// <summary>
        /// Set the global variables.
        /// </summary>
        /// <param name="xmlFilesList"></param>
        /// <param name="defaultXMLFile"></param>
        public PaneHeader2ViewModel(DockpaneMainViewModel dockPane, DataSelectorConfig toolConfig)
        {
            _dockPane = dockPane;

            // Return if no config object passed.
            if (toolConfig == null) return;

            // Set the config object.
            _toolConfig = toolConfig;

            InitializeComponent();
        }

        /// <summary>
        /// Initialise the query pane.
        /// </summary>
        private void InitializeComponent()
        {
            // Set the SDE file name.
            _sdeFileName = _toolConfig.SDEFile;

            // Create a new map functions object.
            _mapFunctions = new();

            // Create a new SQL functions object.
            _sqlFunctions = new(_sdeFileName);

            _outputFormats = [
                            "Geodatabase",
                            "Shapefile",
                            "CSV file (comma delimited)",
                            "Text file (tab delimited)"];

            // Get the relevant config file settings.
            _logFilePath = _toolConfig.LogFilePath;
            _includeWildcard = _toolConfig.IncludeWildcard;
            _excludeWildcard = _toolConfig.ExcludeWildcard;
            _defaultFormat = _toolConfig.DefaultFormat;
            _defaultSchema = _toolConfig.DatabaseSchema;
            _objectsTable = _toolConfig.ObjectsTable;
            _defaultQueryPath = _toolConfig.DefaultQueryPath;
            _defaultExtractPath = _toolConfig.DefaultExtractPath;
            _clearLogFile = _toolConfig.DefaultClearLogFile;
            _openLogFile = _toolConfig.DefaultOpenLogFile;
            _setSymbology = _toolConfig.DefaultSetSymbology;
            //_layerLocation = _toolConfig.LayerLocation;
            _validateSQL = _toolConfig.ValidateSQL;
            _sqlTimeout = _toolConfig.SQLTimeout;
            _loadColumnsVertically = _toolConfig.LoadColumnsVertically;
            _selectStoredProcedure = _toolConfig.SelectStoredProcedure;
            _clearStoredProcedure = _toolConfig.ClearStoredProcedure;

            // Set the window properties.
            _selectedOutputFormat = _defaultFormat;
        }

        #endregion Creator

        #region Controls Enabled

        /// <summary>
        /// Is the tables list enabled?
        /// </summary>
        public bool TablesListEnabled
        {
            get
            {
                return ((_dockPane.ProcessStatus == null)
                    && (_tablesList != null));
            }
        }

        /// <summary>
        /// Is the hidden load columns button enabled.
        /// </summary>
        public bool LoadColumnsEnabled
        {
            get
            {
                return (_dockPane.ProcessStatus == null);
            }
        }

        /// <summary>
        /// Can the Clear button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool ClearButtonEnabled
        {
            get
            {
                return ((_dockPane.ProcessStatus == null)
                    && (_selectedTable != null
                    || !string.IsNullOrEmpty(ColumnsText)
                    || !string.IsNullOrEmpty(WhereText)
                    || !string.IsNullOrEmpty(GroupByText)
                    || !string.IsNullOrEmpty(OrderByText)));
            }
        }

        /// <summary>
        /// Can the Load button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool LoadButtonEnabled
        {
            get
            {
                return (_dockPane.ProcessStatus == null);
            }
        }

        /// <summary>
        /// Can the Save button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool SaveButtonEnabled
        {
            get
            {
                return ((_dockPane.ProcessStatus == null)
                    && (!string.IsNullOrEmpty(ColumnsText)
                    || !string.IsNullOrEmpty(WhereText)
                    || !string.IsNullOrEmpty(GroupByText)
                    || !string.IsNullOrEmpty(OrderByText)));
            }
        }

        /// <summary>
        /// Can the verify button be pressed?
        /// </summary>
        public bool VerifyButtonEnabled
        {
            get
            {
                return ((_dockPane.ProcessStatus == null)
                    && (_tablesList != null)
                    && ((_selectedTable != null
                    || !string.IsNullOrEmpty(WhereText) && WhereText.Length > 5 && WhereText.Substring(0, 5).Equals("from ", StringComparison.OrdinalIgnoreCase)))
                    && (!string.IsNullOrEmpty(ColumnsText)));
            }
        }

        /// <summary>
        /// Can the run button be pressed?
        /// </summary>
        public bool RunButtonEnabled
        {
            get
            {
                return ((_dockPane.ProcessStatus == null)
                    && (_tablesList != null)
                    && ((_selectedTable != null)
                    || (!string.IsNullOrEmpty(WhereText) && WhereText.Length > 5 && WhereText.Substring(0, 5).Equals("from ", StringComparison.OrdinalIgnoreCase)))
                    && (!string.IsNullOrEmpty(ColumnsText))
                    && (!string.IsNullOrEmpty(_selectedOutputFormat)));
            }
        }

        #endregion Controls Enabled

        #region Controls Visibility

        /// <summary>
        /// Is the Columns Text expand button visible.
        /// </summary>
        public Visibility ColumnsTextExpandButtonVisibility
        {
            get
            {
                if (string.IsNullOrEmpty(_columnsText))
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;
            }
        }

        /// <summary>
        /// Is the Where Text expand button visible.
        /// </summary>
        public Visibility WhereTextExpandButtonVisibility
        {
            get
            {
                if (string.IsNullOrEmpty(_whereText))
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;
            }
        }

        /// <summary>
        /// Is the Group By Text expand button visible.
        /// </summary>
        public Visibility GroupByTextExpandButtonVisibility
        {
            get
            {
                if (string.IsNullOrEmpty(_groupByText))
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;
            }
        }

        /// <summary>
        /// Is the Order By Text expand button visible.
        /// </summary>
        public Visibility OrderByTextExpandButtonVisibility
        {
            get
            {
                if (string.IsNullOrEmpty(_orderByText))
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;
            }
        }

        #endregion Controls Visibility

        #region Message

        private string _message;

        /// <summary>
        /// The message to display on the form.
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged(nameof(HasMessage));
                OnPropertyChanged(nameof(Message));
            }
        }

        private MessageType _messageLevel;

        /// <summary>
        /// The type of message; Error, Warning, Confirmation, Information
        /// </summary>
        public MessageType MessageLevel
        {
            get
            {
                return _messageLevel;
            }
            set
            {
                _messageLevel = value;
                OnPropertyChanged(nameof(MessageLevel));
            }
        }

        /// <summary>
        /// Is there a message to display?
        /// </summary>
        public Visibility HasMessage
        {
            get
            {
                if (_dockPane.ProcessStatus != null
                || string.IsNullOrEmpty(_message))
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public void ShowMessage(string msg, MessageType messageLevel)
        {
            MessageLevel = messageLevel;
            Message = msg;
        }

        /// <summary>
        /// Clear the form messages.
        /// </summary>
        public void ClearMessage()
        {
            Message = "";
        }

        #endregion Message

        #region Clear Command

        private ICommand _clearCommand;

        /// <summary>
        /// Create Clear button command.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand ClearCommand
        {
            get
            {
                if (_clearCommand == null)
                {
                    Action<object> clearAction = new(ClearCommandClick);
                    _clearCommand = new RelayCommand(clearAction, param => ClearButtonEnabled);
                }
                return _clearCommand;
            }
        }

        /// <summary>
        /// Handles event when Clear button is clicked.
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private void ClearCommandClick(object param)
        {
            // Clear all of the fields.
            ColumnsText = null;
            WhereText = null;
            GroupByText = null;
            OrderByText = null;

            // Clear the saved/loaded query name.
            _queryName = null;

            // Update the fields and buttons in the form.
            UpdateFormControls();
        }

        #endregion Clear Command

        #region Save Command

        private ICommand _saveCommand;

        /// <summary>
        /// Create Save button command.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    Action<object> saveAction = new(SaveCommandClick);
                    _saveCommand = new RelayCommand(saveAction, param => SaveButtonEnabled);
                }
                return _saveCommand;
            }
        }

        /// <summary>
        /// Handles event when Save button is clicked.
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void SaveCommandClick(object param)
        {
            await SaveQueryAsync();
        }

        /// <summary>
        /// Save current query to file specified by user.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SaveQueryAsync()
        {
            // Create up Save file dialog.
            SaveFileDialog saveFileDialog = new()
            {
                Title = "Save Query As...",
                Filter = "Query files (*.qsf)|*.qsf",
                InitialDirectory = _defaultQueryPath,
                OkRequiresInteraction = true,
                FileName = _queryName
            };

            // Loop until a valid save file name has been selected.
            bool blDone = false;
            string saveFileName = "";
            while (blDone == false)
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    saveFileName = saveFileDialog.FileName;

                    // Get the file name without the extension (last 4 chars).
                    string saveFileNameWithoutExtension = FileFunctions.GetFullNameWithoutExtension(saveFileName);

                    // Get only the extension (last 4 chars).
                    string saveFileNameExtension = FileFunctions.GetExtension(saveFileName);

                    // Check if a file extension has been added.
                    if (!saveFileNameWithoutExtension.Contains('.'))
                        saveFileName = saveFileNameWithoutExtension + ".qsf";
                    else
                    {
                        // Check the file extension is valid.
                        if (!saveFileNameExtension.Equals("qsf", StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("File name has incorrect extension. Save cancelled.", _displayName, MessageBoxButton.OK, MessageBoxImage.Error);
                            saveFileDialog.Dispose();
                            return false;
                        }
                    }
                    saveFileDialog.Dispose();
                    blDone = true;
                }
                else // User pressed Cancel.
                {
                    saveFileDialog.Dispose();
                    return false;
                }
            }

            // Save the query.
            await Task.Run(() =>
            {
                // Save the query name ready for future saves.
                _queryName = FileFunctions.GetFileName(saveFileName);

                // Create a new stream reader for the file.
                StreamWriter qryFile = File.CreateText(saveFileName);

                // Save the parameters.
                string columnNames = ColumnsText == null ? "" : ColumnsText.Replace("\r\n", " ");
                string fromClause = WhereText == null ? "" : SelectedTable;
                string whereClause = WhereText == null ? "" : WhereText.Replace("\r\n", " ");
                string groupClause = GroupByText == null ? "" : GroupByText.Replace("\r\n", " ");
                string orderClause = OrderByText == null ? "" : OrderByText.Replace("\r\n", " ");
                string outputFormat = SelectedOutputFormat == null ? "" : SelectedOutputFormat;

                // Replace carriage return and line feeds in the constituent parts.
                string columns = string.Format("Fields {0}{1}{2}", "{", columnNames, "}");
                string from = string.Format("From {0}{1}{2}", "{", fromClause, "}");
                string where = string.Format("Where {0}{1}{2}", "{", whereClause, "}");
                string groupBy = string.Format("Group By {0}{1}{2}", "{", groupClause, "}");
                string orderBy = string.Format("Order By {0}{1}{2}", "{", orderClause, "}");
                string format = string.Format("Format {0}{1}{2}", "{", outputFormat, "}");

                // Write the constituent parts to the file.
                qryFile.WriteLine(columns);
                qryFile.WriteLine(from);
                qryFile.WriteLine(where);
                qryFile.WriteLine(groupBy);
                qryFile.WriteLine(orderBy);
                qryFile.WriteLine(format);

                // Close and dispose of the stream reader.
                qryFile.Close();
                qryFile.Dispose();
            });

            return true;
        }

        #endregion Save Command

        #region Load Command

        private ICommand _loadCommand;

        /// <summary>
        /// Create Load button command.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand LoadCommand
        {
            get
            {
                if (_loadCommand == null)
                {
                    Action<object> loadAction = new(LoadCommandClick);
                    _loadCommand = new RelayCommand(loadAction, param => LoadButtonEnabled);
                }
                return _loadCommand;
            }
        }

        /// <summary>
        /// Handles event when Load button is clicked.
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void LoadCommandClick(object param)
        {
            await LoadQueryAsync();
        }

        /// <summary>
        /// Loads the query file chosen by the user.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> LoadQueryAsync()
        {
            // Create open file dialog.
            OpenFileDialog openFileDialog = new()
            {
                Title = "Load Query...",
                Filter = "Query files (*.qsf)|*.qsf",
                OkRequiresInteraction = true,
                InitialDirectory = _defaultQueryPath
            };

            string saveFileName;

            // If a file has not been chosen.
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                openFileDialog.Dispose();
                return false;
            }

            // Load the query.
            await Task.Run(() =>
            {
                // Clear the form.
                ColumnsText = "";
                WhereText = "";
                GroupByText = "";
                OrderByText = "";

                // Get the name of the file to open.
                saveFileName = openFileDialog.FileName;

                // Save the query name ready for future saves.
                _queryName = FileFunctions.GetFileName(saveFileName);

                // Create a new stream reader for the file.
                using StreamReader qryFile = new(saveFileName);

                // Loop through the lines in the query, separating them
                // into their constituent parts.
                string qryLine = "";
                while ((qryLine = qryFile.ReadLine()) != null)
                {
                    if (qryLine.Length > 7 && qryLine.Substring(0, 8).Equals("FIELDS {", StringComparison.OrdinalIgnoreCase)
                    && !qryLine.Equals("FIELDS {}", StringComparison.OrdinalIgnoreCase))
                    {
                        qryLine = qryLine.Substring(8, qryLine.Length - 9);
                        ColumnsText = qryLine.Replace("$$", "\r\n");
                    }
                    else if (qryLine.Length > 5 && qryLine.Substring(0, 6).Equals("FROM {", StringComparison.OrdinalIgnoreCase)
                    && !qryLine.Equals("FROM {}", StringComparison.OrdinalIgnoreCase))
                    {
                        qryLine = qryLine.Substring(6, qryLine.Length - 7);
                        SelectedTable = qryLine;
                        OnPropertyChanged(nameof(SelectedTable));
                    }
                    else if (qryLine.Length > 6 && qryLine.Substring(0, 7).Equals("WHERE {", StringComparison.OrdinalIgnoreCase)
                    && !qryLine.Equals("WHERE {}", StringComparison.OrdinalIgnoreCase))
                    {
                        qryLine = qryLine.Substring(7, qryLine.Length - 8);
                        WhereText = qryLine.Replace("$$", "\r\n");
                    }
                    else if (qryLine.Length > 9 && qryLine.Substring(0, 10).Equals("GROUP BY {", StringComparison.OrdinalIgnoreCase)
                    && !qryLine.Equals("GROUP BY {}", StringComparison.OrdinalIgnoreCase))
                    {
                        qryLine = qryLine.Substring(10, qryLine.Length - 11);
                        GroupByText = qryLine.Replace("$$", "\r\n");
                    }
                    else if (qryLine.Length > 9 && qryLine.Substring(0, 10).Equals("ORDER BY {", StringComparison.OrdinalIgnoreCase)
                    && !qryLine.Equals("ORDER BY {}", StringComparison.OrdinalIgnoreCase))
                    {
                        qryLine = qryLine.Substring(10, qryLine.Length - 11);
                        OrderByText = qryLine.Replace("$$", "\r\n");
                    }
                    else if (qryLine.Length > 7 && qryLine.Substring(0, 8).Equals("FORMAT {", StringComparison.OrdinalIgnoreCase)
                    && !qryLine.Equals("FORMAT {}", StringComparison.OrdinalIgnoreCase))
                    {
                        qryLine = qryLine.Substring(8, qryLine.Length - 9);
                        SelectedOutputFormat = qryLine;
                        OnPropertyChanged(nameof(SelectedOutputFormat));
                    }
                }

                // Close the stream reader.
                qryFile.Close();

                // Update the fields and buttons in the form.
                UpdateFormControls();
            });

            openFileDialog.Dispose();
            return true;
        }

        #endregion Load Command

        #region Verify Command

        private ICommand _verifyCommand;

        /// <summary>
        /// Create Verify button command.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand VerifyCommand
        {
            get
            {
                if (_verifyCommand == null)
                {
                    Action<object> verifyAction = new(VerifyCommandClick);
                    _verifyCommand = new RelayCommand(verifyAction, param => VerifyButtonEnabled);
                }

                return _verifyCommand;
            }
        }

        /// <summary>
        /// Handles event when Verify button is clicked.
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private void VerifyCommandClick(object param)
        {
            // Save the parameters.
            string tableName = SelectedTable;
            string columnNames = ColumnsText == null ? "" : ColumnsText.Replace("\r\n", " ");
            string whereClause = WhereText == null ? "" : WhereText.Replace("\r\n", " ");
            string groupClause = GroupByText == null ? "" : GroupByText.Replace("\r\n", " ");
            string orderClause = OrderByText == null ? "" : OrderByText.Replace("\r\n", " ");

            // Set a temp table name if the user entered their own
            // in the WHERE clause.
            if (string.IsNullOrEmpty(tableName))
                tableName = "TempTable";

            // Validate the sql command.
            if (!VerifyQuery(tableName, columnNames, whereClause, groupClause, orderClause))
                ShowMessage("SQL is invalid.", MessageType.Warning);
            else
                ShowMessage("SQL is valid.", MessageType.Information);
        }

        #endregion Verify Command

        #region Run Command

        /// <summary>
        /// Runs the query.
        /// </summary>
        public async void RunQueryAsync()
        {
            // Validate the parameters.
            if (!ValidateParameters())
                return;

            // Replace any illegal characters in the user name string.
            string userID = StringFunctions.StripIllegals(Environment.UserName, "_", false);
            // Replace hyphen with underscore.
            userID = userID.Replace('-', '_');

            // User ID should be something at least.
            if (string.IsNullOrEmpty(userID))
            {
                userID = "Temp";
            }

            // Set the destination log file path.
            _logFile = _logFilePath + @"\DataSelector_" + userID + ".log";

            // Clear the log file if required.
            if (ClearLogFile)
            {
                bool blDeleted = FileFunctions.DeleteFile(_logFile);
                if (!blDeleted)
                {
                    MessageBox.Show("Cannot delete log file. Please make sure it is not open in another window.", _displayName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // If userid is temp.
            if (userID == "Temp")
            {
                FileFunctions.WriteLine(_logFile, "User ID not found. User ID used will be 'Temp'");
            }

            // Clear any messages.
            ClearMessage();

            // Update the fields and buttons in the form.
            UpdateFormControls();
            _dockPane.RefreshPanel1Buttons();

            // Perform the selection.
            bool success = await ExecuteSelectionAsync(userID);

            // Run the stored procedure to clear the selection.
            if (success || _selectErrors)
                await ClearSelectionAsync(_defaultSchema, SelectedTable, userID);

            // Indicate that the search process has completed (successfully or not).
            string image = "Error";
            if (success && !_selectErrors)
            {
                if (String.IsNullOrEmpty(_successMessage))
                    _successMessage = "Process complete!";
                image = "Success";
            }
            else if (success && _selectErrors)
            {
                if (String.IsNullOrEmpty(_successMessage))
                    _successMessage = "Process complete!";
                image = "Warning";
            }
            else if (!success && !_selectErrors)
            {
                // No error so skip.
            }
            else if (_selectErrors)
            {
                if (String.IsNullOrEmpty(_successMessage))
                    _successMessage = "Process complete with errors!";
                image = "Error";
            }
            else
            {
                _successMessage = "Process ended unexpectedly!";
                image = "Error";
            }

            // Indicate that the process has stopped.
            if (success || _selectErrors)
                StopSelection(_successMessage, image);

            // Update the fields and buttons in the form.
            UpdateFormControls();
            _dockPane.RefreshPanel1Buttons();
        }

        /// <summary>
        /// Validate the form parameters.
        /// </summary>
        /// <returns></returns>
        private bool ValidateParameters()
        {
            // Check the user entered parameters.
            if (string.IsNullOrEmpty(ColumnsText))
            {
                ShowMessage("Please specify which columns you wish to select", MessageType.Warning);
                return false;
            }

            // Table name should always be selected.
            if (string.IsNullOrEmpty(SelectedTable)
                    && (WhereText.Length <= 5 || !WhereText.Substring(0, 5).Equals("from ", StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessage("Please select a table to query from", MessageType.Warning);
                return false;
            }

            // Output format should always be selected.
            if (string.IsNullOrEmpty(SelectedOutputFormat))
            {
                ShowMessage("Please select an output format", MessageType.Warning);
                return false;
            }

            ClearMessage();
            return true;
        }

        #endregion Run Command

        #region ColumnsTextExpand Command

        private ICommand _columnsTextExpandCommand;

        /// <summary>
        /// Create ColumnsText Expand button command.
        /// </summary>
        /// <value></value>
        public ICommand ColumnsTextExpandCommand
        {
            get
            {
                if (_columnsTextExpandCommand == null)
                {
                    Action<object> expandColumnsTextAction = new(ColumnsTextExpandCommandClick);
                    _columnsTextExpandCommand = new RelayCommand(expandColumnsTextAction, param => true);
                }
                return _columnsTextExpandCommand;
            }
        }

        /// <summary>
        /// Handles event when ColumnsText button is pressed.
        /// </summary>
        /// <param name="param"></param>
        private void ColumnsTextExpandCommandClick(object param)
        {
            if (_columnsTextHeight == null)
                _columnsTextHeight = 235;
            else
                _columnsTextHeight = null;

            OnPropertyChanged(nameof(ColumnsTextHeight));
            OnPropertyChanged(nameof(ColumnsTextExpandButtonContent));
        }

        #endregion ColumnsTextExpand Command

        #region WhereTextExpand Command

        private ICommand _whereTextExpandCommand;

        /// <summary>
        /// Create WhereText Expand button command.
        /// </summary>
        /// <value></value>
        public ICommand WhereTextExpandCommand
        {
            get
            {
                if (_whereTextExpandCommand == null)
                {
                    Action<object> expandWhereTextAction = new(WhereTextExpandCommandClick);
                    _whereTextExpandCommand = new RelayCommand(expandWhereTextAction, param => true);
                }
                return _whereTextExpandCommand;
            }
        }

        /// <summary>
        /// Handles event when WhereText button is pressed.
        /// </summary>
        /// <param name="param"></param>
        private void WhereTextExpandCommandClick(object param)
        {
            if (_whereTextHeight == null)
                _whereTextHeight = 55;
            else
                _whereTextHeight = null;

            OnPropertyChanged(nameof(WhereTextHeight));
            OnPropertyChanged(nameof(WhereTextExpandButtonContent));
        }

        #endregion WhereTextExpand Command

        #region GroupByTextExpand Command

        private ICommand _groupByTextExpandCommand;

        /// <summary>
        /// Create GroupByText Expand button command.
        /// </summary>
        /// <value></value>
        public ICommand GroupByTextExpandCommand
        {
            get
            {
                if (_groupByTextExpandCommand == null)
                {
                    Action<object> expandGroupByTextAction = new(GroupByTextExpandCommandClick);
                    _groupByTextExpandCommand = new RelayCommand(expandGroupByTextAction, param => true);
                }
                return _groupByTextExpandCommand;
            }
        }

        /// <summary>
        /// Handles event when GroupByText button is pressed.
        /// </summary>
        /// <param name="param"></param>
        private void GroupByTextExpandCommandClick(object param)
        {
            if (_groupByTextHeight == null)
                _groupByTextHeight = 55;
            else
                _groupByTextHeight = null;

            OnPropertyChanged(nameof(GroupByTextHeight));
            OnPropertyChanged(nameof(GroupByTextExpandButtonContent));
        }

        #endregion GroupByTextExpand Command

        #region OrderByTextExpand Command

        private ICommand _orderByTextExpandCommand;

        /// <summary>
        /// Create OrderByText Expand button command.
        /// </summary>
        /// <value></value>
        public ICommand OrderByTextExpandCommand
        {
            get
            {
                if (_orderByTextExpandCommand == null)
                {
                    Action<object> expandOrderByTextAction = new(OrderByTextExpandCommandClick);
                    _orderByTextExpandCommand = new RelayCommand(expandOrderByTextAction, param => true);
                }
                return _orderByTextExpandCommand;
            }
        }

        /// <summary>
        /// Handles event when OrderByText button is pressed.
        /// </summary>
        /// <param name="param"></param>
        private void OrderByTextExpandCommandClick(object param)
        {
            if (_orderByTextHeight == null)
                _orderByTextHeight = 55;
            else
                _orderByTextHeight = null;

            OnPropertyChanged(nameof(OrderByTextHeight));
            OnPropertyChanged(nameof(OrderByTextExpandButtonContent));
        }

        #endregion OrderByTextExpand Command

        #region Tables List

        private ICommand _refreshCommand;

        /// <summary>
        /// Create the RefreshTablesList button command.
        /// </summary>
        public ICommand RefreshTablesListCommand
        {
            get
            {
                if (_refreshCommand == null)
                {
                    Action<object> refreshAction = new(RefreshTablesListCommandClick);
                    _refreshCommand = new RelayCommand(refreshAction, param => TablesListEnabled);
                }
                return _refreshCommand;
            }
        }

        /// <summary>
        /// Handles the event when the refresh tables list button is clicked.
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void RefreshTablesListCommandClick(object param)
        {
            // Get the table names from SQL Server (and wait).
            await GetTableNamesAsync(true);
        }

        #endregion Tables List

        #region LoadColumns Command

        private ICommand _loadColumnsCommand;

        /// <summary>
        /// Create the LoadColumns button command.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ICommand LoadColumnsCommand
        {
            get
            {
                if (_loadColumnsCommand == null)
                {
                    Action<object> loadColumnsAction = new(LoadColumnsCommandClick);
                    _loadColumnsCommand = new RelayCommand(loadColumnsAction, param => LoadColumnsEnabled);
                }
                return _loadColumnsCommand;
            }
        }

        /// <summary>
        /// Handles the event when the LoadColumns button is clicked.
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void LoadColumnsCommandClick(object param)
        {
            await LoadColumnsAsync(_selectedTable);
        }

        #endregion LoadColumns Command

        #region Properties

        private string _columnsText;

        /// <summary>
        /// Get/Set the SQL columns clause.
        /// </summary>
        public string ColumnsText
        {
            get
            {
                return _columnsText;
            }
            set
            {
                _columnsText = value;

                // Update the buttons in the form.
                UpdateFormButtons();

                // Clear the message field.
                ClearMessage();

                OnPropertyChanged(nameof(ColumnsTextExpandButtonVisibility));
                OnPropertyChanged(nameof(ColumnsTextExpandButtonContent));
            }
        }

        /// <summary>
        /// Get the columns list tool tip.
        /// </summary>
        public string ColumnsTooltip
        {
            get
            {
                if (_dockPane.TableListLoading
                || _selectedTable == null)
                    return null;
                else
                    return "Double-click to populate with list of columns from the selected table";
            }
        }

        private double? _columnsTextHeight = 235;

        /// <summary>
        /// Get the height of the columns text.
        /// </summary>
        public double? ColumnsTextHeight
        {
            get
            {
                if (string.IsNullOrEmpty(_columnsText))
                    return 235;
                else
                    return _columnsTextHeight;
            }
        }

        /// <summary>
        /// Get the content of the columns text expand button.
        /// </summary>
        public string ColumnsTextExpandButtonContent
        {
            get
            {
                if (_columnsTextHeight == null)
                    return "-";
                else
                    return "+";
            }
        }

        private string _whereText;

        /// <summary>
        /// Get/Set the SQL where clause.
        /// </summary>
        public string WhereText
        {
            get
            {
                return _whereText;
            }
            set
            {
                _whereText = value;

                // Update the buttons in the form.
                UpdateFormButtons();

                // Clear the message field.
                ClearMessage();

                OnPropertyChanged(nameof(WhereTextExpandButtonVisibility));
                OnPropertyChanged(nameof(WhereTextExpandButtonContent));
            }
        }

        private double? _whereTextHeight = 55;

        /// <summary>
        /// Get the height of the where text.
        /// </summary>
        public double? WhereTextHeight
        {
            get
            {
                if (string.IsNullOrEmpty(_whereText))
                    return 55;
                else
                    return _whereTextHeight;
            }
        }

        /// <summary>
        /// Get the content of the where text expand button.
        /// </summary>
        public string WhereTextExpandButtonContent
        {
            get
            {
                if (_whereTextHeight == null)
                    return "-";
                else
                    return "+";
            }
        }

        private string _groupByText;

        /// <summary>
        /// Get/Set the SQL group by clause.
        /// </summary>
        public string GroupByText
        {
            get
            {
                return _groupByText;
            }
            set
            {
                _groupByText = value;

                // Update the buttons in the form.
                UpdateFormButtons();

                // Clear the message field.
                ClearMessage();

                OnPropertyChanged(nameof(GroupByTextExpandButtonVisibility));
                OnPropertyChanged(nameof(GroupByTextExpandButtonContent));
            }
        }

        private double? _groupByTextHeight = 55;

        /// <summary>
        /// Get the height of the groupBy text.
        /// </summary>
        public double? GroupByTextHeight
        {
            get
            {
                if (string.IsNullOrEmpty(_groupByText))
                    return 55;
                else
                    return _groupByTextHeight;
            }
        }

        /// <summary>
        /// Get the content of the groupBy text expand button.
        /// </summary>
        public string GroupByTextExpandButtonContent
        {
            get
            {
                if (_groupByTextHeight == null)
                    return "-";
                else
                    return "+";
            }
        }

        private string _orderByText;

        /// <summary>
        /// Get/Set the SQL order by clause.
        /// </summary>
        public string OrderByText
        {
            get
            {
                return _orderByText;
            }
            set
            {
                _orderByText = value;

                // Update the buttons in the form.
                UpdateFormButtons();

                // Clear the message field.
                ClearMessage();

                OnPropertyChanged(nameof(OrderByTextExpandButtonVisibility));
                OnPropertyChanged(nameof(OrderByTextExpandButtonContent));
            }
        }

        private double? _orderByTextHeight = 55;

        /// <summary>
        /// Get the height of the orderBy text.
        /// </summary>
        public double? OrderByTextHeight
        {
            get
            {
                if (string.IsNullOrEmpty(_orderByText))
                    return 55;
                else
                    return _orderByTextHeight;
            }
        }

        /// <summary>
        /// Get the content of the orderBy text expand button.
        /// </summary>
        public string OrderByTextExpandButtonContent
        {
            get
            {
                if (_orderByTextHeight == null)
                    return "-";
                else
                    return "+";
            }
        }

        private ObservableCollection<string> _tablesList;

        /// <summary>
        /// Get the list of SQL tables.
        /// </summary>
        public ObservableCollection<string> TablesList
        {
            get
            {
                return _tablesList;
            }
        }

        private string _selectedTable;

        /// <summary>
        /// Get/Set the selected SQL table.
        /// </summary>
        public string SelectedTable
        {
            get
            {
                return _selectedTable;
            }
            set
            {
                _selectedTable = value;

                // Update the fields and buttons in the form.
                OnPropertyChanged(nameof(ColumnsTooltip));
                UpdateFormButtons();

                // Clear the message field.
                ClearMessage();
            }
        }

        /// <summary>
        /// Get the image for the TablesListRefresh button.
        /// </summary>
        public static ImageSource ButtonTablesListRefreshImg
        {
            get
            {
                var imageSource = System.Windows.Application.Current.Resources["GenericRefresh16"] as ImageSource;
                return imageSource;
            }
        }

        /// <summary>
        /// Get the image for the Run button.
        /// </summary>
        public static ImageSource ButtonRunImg
        {
            get
            {
                var imageSource = System.Windows.Application.Current.Resources["GenericRun16"] as ImageSource;
                return imageSource;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Update the fields and buttons in the form.
        /// </summary>
        private void UpdateFormControls()
        {
            UpdateFormFields();
            UpdateFormButtons();
        }

        /// <summary>
        /// Update the fields in the form.
        /// </summary>
        private void UpdateFormFields()
        {
            OnPropertyChanged(nameof(ColumnsText));
            OnPropertyChanged(nameof(ColumnsTooltip));
            OnPropertyChanged(nameof(WhereText));
            OnPropertyChanged(nameof(GroupByText));
            OnPropertyChanged(nameof(OrderByText));
            OnPropertyChanged(nameof(TablesList));
            OnPropertyChanged(nameof(TablesListEnabled));
            OnPropertyChanged(nameof(SelectedTable));
            OnPropertyChanged(nameof(LoadColumnsEnabled));
            OnPropertyChanged(nameof(Message));
        }

        /// <summary>
        /// Update the buttons in the form.
        /// </summary>
        private void UpdateFormButtons()
        {
            OnPropertyChanged(nameof(ClearButtonEnabled));
            OnPropertyChanged(nameof(SaveButtonEnabled));
            OnPropertyChanged(nameof(LoadButtonEnabled));
            OnPropertyChanged(nameof(VerifyButtonEnabled));

                // Check if the run button is now enabled/disabled.
                _dockPane.CheckRunButton();
        }

        /// <summary>
        /// Called on double-click of the Columns textbox. The columns belonging
        /// to the selected table name are filled in to the Columns textbox.
        /// </summary>
        /// <param name="selectedTable"></param>
        /// <returns></returns>
        public async Task LoadColumnsAsync(string selectedTable)
        {
            // Check if a table was selected.
            if (selectedTable == null)
                return;

            if (!string.IsNullOrEmpty(_columnsText))
            {
                MessageBoxResult dlResult = MessageBox.Show("Overwrite text in the Columns field?", _displayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (dlResult == MessageBoxResult.No)
                    return; //User clicked by accident; leave routine.
            }

            // Indicate column list is loading.
            _dockPane.ProgressUpdate("Loading list of columns from the selected table ...", -1, -1);

            // Get the field names and wait for the task to finish.
            List<string> columnsList = await _sqlFunctions.GetFieldNamesListAsync(SelectedTable);

            // Convert the field names to a single string.
            string columnNamesText = "";
            foreach (string columnName in columnsList)
            {
                // Concatenate the field names vertically or horizontally.
                if (_loadColumnsVertically)
                    columnNamesText = columnNamesText + columnName + ",\r\n";
                else
                    columnNamesText = columnNamesText + columnName + ", ";
            }

            if (_loadColumnsVertically)
                columnNamesText = columnNamesText.Substring(0, columnNamesText.Length - 3);
            else
                columnNamesText = columnNamesText.Substring(0, columnNamesText.Length - 2);

            // Replace the text box value with the field names.
            ColumnsText = columnNamesText;

            // Indicate the load has finished.
            _dockPane.ProgressUpdate(null, -1, -1);

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(ColumnsText));
            UpdateFormButtons();
        }

        private List<string> _outputFormats;

        /// <summary>
        /// Get the list of available output formats.
        /// </summary>
        public List<string> OutputFormats
        {
            get
            {
                return _outputFormats;
            }
        }

        private string _selectedOutputFormat;

        /// <summary>
        /// Get/Set the selected output format.
        /// </summary>
        public string SelectedOutputFormat
        {
            get
            {
                return _selectedOutputFormat;
            }
            set
            {
                _selectedOutputFormat = value;

                // Check if the run button is now enabled/disabled.
                _dockPane.CheckRunButton();
            }
        }

        private bool _clearLogFile;

        /// <summary>
        /// Is the log file to be cleared before running the query?
        /// </summary>
        public bool ClearLogFile
        {
            get
            {
                return _clearLogFile;
            }
            set
            {
                _clearLogFile = value;
            }
        }

        private bool _openLogFile;

        /// <summary>
        /// Is the log file to be opened after running the query?
        /// </summary>
        public bool OpenLogFile
        {
            get
            {
                return _openLogFile;
            }
            set
            {
                _openLogFile = value;
            }
        }

        private bool _setSymbology;

        /// <summary>
        /// Is the symbology to be set for the output layer(s)?
        /// </summary>
        public bool SetSymbology
        {
            get
            {
                return _setSymbology;
            }
            set
            {
                _setSymbology = value;
            }
        }

        #endregion Methods

        #region SQL

        /// <summary>
        /// Get a list of the table names from the SQL Server.
        /// </summary>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public async Task GetTableNamesAsync(bool refresh)
        {
            // Indicate table is loading.
            _dockPane.TableListLoading = true;
            if (refresh)
                _dockPane.ProgressUpdate("Refreshing tables list ...", -1, -1);
            else
                _dockPane.ProgressUpdate("Loading tables list ...", -1, -1);

            // Preset the tables list.
            _tablesList = ["Loading tables ..."];
            SelectedTable = "Loading tables ...";
            OnPropertyChanged(nameof(TablesList));
            OnPropertyChanged(nameof(SelectedTable));

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(TablesList));
            OnPropertyChanged(nameof(TablesListEnabled));
            OnPropertyChanged(nameof(LoadColumnsEnabled));
            UpdateFormButtons();
            _dockPane.RefreshPanel1Buttons();

            // Get the full list of feature classes and tables from SQL Server.
            if (!await _sqlFunctions.GetTableNamesAsync(_defaultSchema + "." + _objectsTable))
                ShowMessage("Error: Unable to get table names.", MessageType.Warning);

            // If no tables were found.
            if (_sqlFunctions.TableNames.Count == 0)
            {
                // Clear the tables list.
                _tablesList = [];

                // Indicate the refresh has finished.
                _dockPane.TableListLoading = false;
                _dockPane.ProgressUpdate(null, -1, -1);

                // Update the fields and buttons in the form.
                OnPropertyChanged(nameof(TablesList));
                OnPropertyChanged(nameof(TablesListEnabled));
                OnPropertyChanged(nameof(SelectedTable));
                OnPropertyChanged(nameof(LoadColumnsEnabled));
                UpdateFormButtons();
                _dockPane.RefreshPanel1Buttons();

                // Inform the user no tables found in SQL Server.
                if (_sqlFunctions.TableNames.Count == 0)
                    ShowMessage("No tables found in SQL Server", MessageType.Warning);

                return;
            }

            // Get the list of tables returned from SQL Server.
            List<string> tabList = _sqlFunctions.TableNames;

            // Get the include and exclude wildcard settings.
            string includeWC = _includeWildcard;
            string excludeWC = _excludeWildcard;

            // Filter the SQL table names and add them to a list.
            List<string> filteredTableList = FilterTableNames(tabList, _defaultSchema, includeWC, excludeWC, false);

            // Set the tables list in sort order.
            _tablesList = new(filteredTableList.OrderBy(t => t));

            // Select the first item in the list if there is only one.
            if (_tablesList.Count == 1)
                SelectedTable = _tablesList[0];
            else
                SelectedTable = null;

            // Indicate the refresh has finished.
            _dockPane.TableListLoading = false;
            _dockPane.ProgressUpdate(null, -1, -1);

            // Update the fields and buttons in the form.
            OnPropertyChanged(nameof(TablesList));
            OnPropertyChanged(nameof(TablesListEnabled));
            OnPropertyChanged(nameof(SelectedTable));
            OnPropertyChanged(nameof(LoadColumnsEnabled));
            UpdateFormButtons();
            _dockPane.RefreshPanel1Buttons();

            // Inform the user no filtered tables found.
            if (_tablesList.Count == 0)
                ShowMessage("No tables found matching wildcard criteria", MessageType.Warning);
        }

        /// <summary>
        /// Filter the list of the table names base on the include and exclude wildcard criteria.
        /// </summary>
        /// <param name="inputNames"></param>
        /// <param name="schema"></param>
        /// <param name="includeWildcard"></param>
        /// <param name="excludeWildcard"></param>
        /// <param name="includeFullName"></param>
        /// <returns></returns>
        internal static List<string> FilterTableNames(List<string> inputNames, string schema, string includeWildcard, string excludeWildcard,
                              bool includeFullName = false)
        {
            // Define the wildcards as case insensitive
            Wildcard theInclude = new(includeWildcard, schema, RegexOptions.IgnoreCase);
            Wildcard theExclude = new(excludeWildcard, schema, RegexOptions.IgnoreCase);

            List<string> theStringList = [];

            foreach (string inName in inputNames)
            {
                string tableName = inName;
                // Does the name conform to the includeWildcard?
                if (theInclude.IsMatch(tableName))
                {
                    if (!theExclude.IsMatch(tableName))
                    {
                        if (includeFullName)
                        {
                            theStringList.Add(tableName);
                        }
                        else
                        {
                            tableName = tableName.Split('.')[1];
                            theStringList.Add(tableName);
                        }
                    }
                }
            }

            return theStringList;
        }

        /// <summary>
        /// Check if the table contains a spatial column in the columns text.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnsText"></param>
        /// <returns></returns>
        internal async Task<string> IsTableSpatialAsync(string tableName, string columnsText)
        {
            string[] geometryFields = ["SP_GEOMETRY", "Shape"]; // Expand as required.

            // Get the list of field names in the selected table.
            List<string> fieldsList = await _sqlFunctions.GetFieldNamesListAsync(tableName);

            // Loop through the geometry fields looking for a match.
            foreach (string geomField in geometryFields)
            {
                // If the columns text contains the geometry field.
                if (columnsText.Contains(geomField, StringComparison.OrdinalIgnoreCase))
                {
                    return geomField;
                }
                // If "*" is used check for the existence of the geometry field in the table.
                else if (columnsText.Equals("*", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string fieldName in fieldsList)
                    {
                        // If the column text contains the geometry field.
                        if (fieldName.Equals(geomField, StringComparison.OrdinalIgnoreCase))
                            return geomField;
                    }
                }
            }

            // No geometry column found.
            return null;
        }

        /// <summary>
        /// Get the name of the target output file.
        /// </summary>
        /// <param name="isSpatial"></param>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        internal async Task<string> GetOutputFileNameAsync(bool isSpatial, string outputFormat)
        {
            // Get the output file name taking account of adjusted output formats.
            string outputFile = null;
            bool blDone = false;
            bool blHasExtension = false;
            while (!blDone)
            {
                // Prompt the user to specify an output file in the required format.
                outputFile = ArcGISFunctions.GetOutputFileName(outputFormat, _defaultExtractPath);
                if (outputFile != null)
                {
                    // firstly check extensions are as should be.
                    string fileExtensionTest = outputFile.Substring(outputFile.Length - 4, 4).Substring(0, 1);
                    if (fileExtensionTest == ".") blHasExtension = true;

                    // if there isn't, add one.
                    if (outputFormat.Contains("CSV file") && !blHasExtension)
                    {
                        outputFile += ".csv";
                    }
                    else if (outputFormat.Contains("Text file") & !blHasExtension)
                    {
                        outputFile += ".txt";
                    }
                    else if (outputFormat == "Shapefile" && !blHasExtension)
                    {
                        outputFile += ".shp";
                    }
                    else if (outputFormat.Contains("Geodatabase") && (blHasExtension || !outputFile.Contains(".gdb"))) // It is a geodatabase file and should not have an extension.
                    {
                        MessageBox.Show("Please select a file geodatabase output file.", _displayName, MessageBoxButton.OK, MessageBoxImage.Information);

                        return null;
                    }
                    else if (!outputFormat.Contains("Geodatabase") && outputFile.Contains(".gdb")) // Trying to store a non-geoDB in a gdb
                    {
                        MessageBox.Show("Cannot store '" + outputFormat + "' inside a geodatabase. Please choose a different output location.", _displayName, MessageBoxButton.OK, MessageBoxImage.Information);

                        return null;
                    }
                }
                else
                    blDone = true; // user pressed cancel.

                if (isSpatial && blDone != true)
                {
                    // Check if the outputfile_point or outputfile_poly already exists. For text output the dialog does its own check.

                    string pointFile = "";
                    string polyFile = "";
                    if (outputFormat.Contains("Geodatabase"))
                    {
                        pointFile = outputFile + "_Point";
                        polyFile = outputFile + "_Poly";
                    }
                    else if (outputFormat == "Shapefile")
                    {
                        string fileExtensionTest1 = outputFile.Substring(outputFile.Length - 4, 4).Substring(0, 1);
                        if (fileExtensionTest1 == ".")
                        {
                            pointFile = string.Concat(outputFile.AsSpan(0, outputFile.Length - 4), "_Point.shp");
                            polyFile = string.Concat(outputFile.AsSpan(0, outputFile.Length - 4), "_Poly.shp");
                        }
                        else
                        {
                            pointFile = outputFile + "_Point.shp";
                            polyFile = outputFile + "_Poly.shp";
                        }
                    }
                    if (outputFormat.Contains("Geodatabase") || outputFormat == "Shapefile")
                    {
                        bool pointExists = await ArcGISFunctions.FeatureClassExistsAsync(pointFile);
                        bool polyExists = await ArcGISFunctions.FeatureClassExistsAsync(polyFile);
                        if (pointExists || polyExists)
                        {
                            MessageBoxResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", _displayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (dlResult1 == MessageBoxResult.Yes)
                                blDone = true;
                        }
                        else
                            blDone = true;
                    }
                    else
                    {
                        // Check for CSV
                        if (outputFormat.Contains("CSV file") || outputFormat.Contains("Text file")) //& !blHasExtension
                        // Basically if the user chose a text file with an extension, the dialog will already have given her feedback and we don't need to do this again.
                        {
                            if (FileFunctions.FileExists(outputFile))
                            {
                                MessageBoxResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", _displayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                                if (dlResult1 == MessageBoxResult.Yes)
                                    blDone = true;
                            }
                            else
                                blDone = true;
                        }
                        else
                            blDone = true; // Text file; already checked by dialog.
                    }
                }
                else if (blDone != true) // non-isSpatial, not done yet.
                {
                    // Test for the types of flat output.
                    if (outputFormat.Contains("Geodatabase"))
                    {
                        if (await ArcGISFunctions.TableExistsAsync(outputFile))
                        {
                            MessageBoxResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", _displayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (dlResult1 == MessageBoxResult.Yes)
                                blDone = true;
                        }
                        else
                            blDone = true;
                    }
                    else if ((outputFormat.Contains("CSV file") || outputFormat.Contains("Text file")) & !blHasExtension)
                    // Basically if the user chose a text file, the dialog will already have given her feedback and we don't need to do this again.
                    {
                        if (FileFunctions.FileExists(outputFile))
                        {
                            MessageBoxResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", _displayName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (dlResult1 == MessageBoxResult.Yes)
                                blDone = true;
                        }
                        else
                            blDone = true;
                    }
                    else
                        blDone = true; // Text file; already checked by dialog.
                }
            }

            return outputFile;
        }

        /// <summary>
        /// Perform the selection by running the query via a
        /// stored procedure.
        /// </summary>
        /// <param name="isSpatial"></param>
        /// <param name="isSplit"></param>
        /// <param name="schema"></param>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="whereClause"></param>
        /// <param name="groupClause"></param>
        /// <param name="orderClause"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        internal async Task<bool> PerformSelectionAsync(bool isSpatial, bool isSplit, string schema, string tableName,
                                  string columnNames, string whereClause, string groupClause, string orderClause,
                                  string userID)
        {
            bool success;

            // Get the name of the stored procedure to execute selection in SQL Server.
            string storedProcedureName = _selectStoredProcedure;

            // Write the parameters to the log file.
            FileFunctions.WriteLine(_logFile, string.Format("Source table is '{0}'", tableName));
            FileFunctions.WriteLine(_logFile, string.Format("Column names are '{0}'", columnNames));
            if (whereClause != null && whereClause.Length > 0)
                FileFunctions.WriteLine(_logFile, string.Format("Where clause is '{0}'", whereClause));
            else
                FileFunctions.WriteLine(_logFile, "No where clause was used");
            if (groupClause != null && groupClause.Length > 0)
                FileFunctions.WriteLine(_logFile, string.Format("Group by clause is '{0}'", groupClause));
            else
                FileFunctions.WriteLine(_logFile, "No group by clause was used");
            if (orderClause != null && orderClause.Length > 0)
                FileFunctions.WriteLine(_logFile, string.Format("Order by clause is '{0}'", orderClause));
            else
                FileFunctions.WriteLine(_logFile, "No order by clause was used");

            // Set up the SQL command.
            StringBuilder sqlCmd = new();

            // Double-up single quotes so they parse in SQL command correctly.
            if (whereClause != null && whereClause.Contains('\''))
                whereClause = whereClause.Replace("'", "''");

            // Build the SQL command to execute the stored procedure.
            sqlCmd = sqlCmd.Append(string.Format("EXECUTE {0}", storedProcedureName));
            sqlCmd.Append(string.Format(" '{0}'", schema));
            sqlCmd.Append(string.Format(", '{0}'", tableName));
            sqlCmd.Append(string.Format(", '{0}'", columnNames));
            sqlCmd.Append(string.Format(", '{0}'", whereClause));
            sqlCmd.Append(string.Format(", '{0}'", groupClause));
            sqlCmd.Append(string.Format(", '{0}'", orderClause));
            sqlCmd.Append(string.Format(", '{0}'", userID));
            sqlCmd.Append(string.Format(", {0}", isSplit ? "1" : "0"));

            // Set the SQL output file names.
            string pointFeatureClass = schema + "." + tableName + "_point_" + userID;
            string polyFeatureClass = schema + "." + tableName + "_poly_" + userID;
            string flatTable = schema + "." + tableName + "_" + userID;

            // Reset the counters.
            _pointCount = 0;
            _polyCount = 0;
            _tableCount = 0;

            try
            {
                FileFunctions.WriteLine(_logFile, "Performing selection ...");

                // Execute the stored procedure.
                await _sqlFunctions.ExecuteSQLOnGeodatabaseAsync(sqlCmd.ToString());

                // If the result is expected to be spatial it should be split into points and polys.
                if (isSpatial)
                {
                    // Check if the point or polygon feature class exists.
                    success = await _sqlFunctions.FeatureClassExistsAsync(pointFeatureClass);
                    if (!success)
                        success = await _sqlFunctions.FeatureClassExistsAsync(polyFeatureClass);
                }
                else
                {
                    // Check if the table exists.
                    success = await _sqlFunctions.TableExistsAsync(flatTable);
                }

                // If the result table(s) exist.
                if (success)
                {
                    if (isSpatial)
                    {
                        // Count the number of rows in the point feature count.
                        _pointCount = await _sqlFunctions.GetFeaturesCountAsync(pointFeatureClass);

                        // Count the number of rows in the poly feature count.
                        _polyCount = await _sqlFunctions.GetFeaturesCountAsync(polyFeatureClass);
                    }
                    else
                    {
                        // Count the number of rows in the table.
                        _tableCount = await _sqlFunctions.GetTableRowCountAsync(flatTable);
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                FileFunctions.WriteLine(_logFile, "Error: Executing the stored procedure: " + ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clear the SQL output tables by running a stored procedure.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="tableName"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        internal async Task<bool> ClearSelectionAsync(string schema, string tableName, string userID)
        {
            // Set up the SQL command.
            StringBuilder sqlCmd = new();

            // Get the name of the stored procedure to clear selection in SQL Server.
            string storedProcedureName = _clearStoredProcedure;

            // Build the SQL command to execute the stored procedure.
            sqlCmd = sqlCmd.Append(string.Format("EXECUTE {0}", storedProcedureName));
            sqlCmd.Append(string.Format(" '{0}'", schema));
            sqlCmd.Append(string.Format(", '{0}'", tableName));
            sqlCmd.Append(string.Format(", '{0}'", userID));

            try
            {
                FileFunctions.WriteLine(_logFile, "Deleting temporary tables ...");

                // Execute the stored procedure.
                await _sqlFunctions.ExecuteSQLOnGeodatabaseAsync(sqlCmd.ToString());
            }
            catch (Exception ex)
            {
                FileFunctions.WriteLine(_logFile, "Error: Deleting the temporary tables: " + ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Export the spatial results to a feature class, shapefile,
        /// test file or CSV file.
        /// </summary>
        /// <param name="outputFormat"></param>
        /// <param name="inPoints"></param>
        /// <param name="inPolys"></param>
        /// <param name="outPoints"></param>
        /// <param name="outPolys"></param>
        /// <param name="outFile"></param>
        /// <param name="createMap"></param>
        /// <returns></returns>
        internal async Task<bool> ExportSpatialResultsAsync(string outputFormat,
                                  string inPoints, string inPolys,
                                  string outPoints, string outPolys,
                                  string outFile,
                                  bool createMap)
        {
            bool result;

            // If the data is to be exported to a spatial dataset
            if (outputFormat == "Geodatabase FC" || outputFormat == "Shapefile")
            {
                // Set process step counters.
                int processStep = 1;
                int processStepsMax = 1;
                if (_pointCount > 0)
                    processStepsMax += 1;
                if (_polyCount > 0)
                    processStepsMax += 1;
                if (createMap)
                    processStepsMax += 2;

                FileFunctions.WriteLine(_logFile, "Output is spatial and will be split into a point and a polygon layer");

                if (_pointCount > 0)
                {
                    // Indicate the export has started.
                    _dockPane.ProgressUpdate("Saving point results ...", processStep, processStepsMax);
                    processStep += 1;

                    FileFunctions.WriteLine(_logFile, string.Format("Copying point results as '{0}' ...", outputFormat));
                    result = await ArcGISFunctions.CopyFeaturesAsync(inPoints, outPoints, !createMap);

                    if (!result)
                    {
                        FileFunctions.WriteLine(_logFile, string.Format("Error: Copying point results to '{0}'", outPoints));

                        return false;
                    }

                    FileFunctions.WriteLine(_logFile, string.Format("Point results saved"));
                }
                if (_polyCount > 0)
                {
                    // Indicate the export has started.
                    _dockPane.ProgressUpdate("Saving polygon results ...", processStep, processStepsMax);
                    processStep += 1;

                    FileFunctions.WriteLine(_logFile, string.Format("Copying polygon results as '{0}' ...", outputFormat));
                    result = await ArcGISFunctions.CopyFeaturesAsync(inPolys, outPolys, !createMap);

                    if (!result)
                    {
                        FileFunctions.WriteLine(_logFile, string.Format("Error: Copying polygon results to '{0}'", outPolys));

                        return false;
                    }

                    FileFunctions.WriteLine(_logFile, string.Format("Polygon results saved"));
                }

                // Exit if nothing was exported.
                if (_pointCount == 0 && _polyCount == 0)
                    return true;

                // If there is no active map create one, ready to add the results to.
                // Otherwise the data will have been added to an existing map
                // automatically.
                if (createMap)
                {
                    // Indicate the map is being created.
                    _dockPane.ProgressUpdate("Creating map ...", processStep, processStepsMax);
                    processStep += 1;

                    await _mapFunctions.CreateMapAsync("DataMap");

                    // Indicate the data is being added to the map.
                    _dockPane.ProgressUpdate("Adding results to map ...", processStep, processStepsMax);

                    if (_pointCount > 0)
                        await _mapFunctions.AddLayerToMapAsync(outPoints);

                    if (_polyCount > 0)
                        await _mapFunctions.AddLayerToMapAsync(outPolys);
                }

                return true;
            }
            else if (outputFormat.Contains("Text file") || outputFormat.Contains("CSV file"))
            {
                // Set process step counters.
                int processStep = 1;
                int processStepsMax = 1;
                if (_pointCount > 0)
                    processStepsMax += 1;
                if (_polyCount > 0)
                    processStepsMax += 1;
                if (!createMap)
                    processStepsMax += 1;

                string fileType;
                if (outputFormat.Contains("Text file"))
                    fileType = "TXT";
                else
                    fileType = "CSV";

                // Not a spatial export, but it is a spatial layer so there are two files.
                // which are appended back together again.
                FileFunctions.WriteLine(_logFile, "Output is non-spatial and will not be split");

                // If schema.ini file exists delete it.
                string strIniFile = FileFunctions.GetDirectoryName(outFile) + "\\schema.ini";
                if (FileFunctions.FileExists(strIniFile))
                    FileFunctions.DeleteFile(strIniFile); // Not checking for success at the moment.

                bool blAppend = false;
                if (_pointCount > 0)
                {
                    // Indicate the export has started.
                    _dockPane.ProgressUpdate("Saving point results ...", processStep, processStepsMax);
                    processStep += 1;

                    FileFunctions.WriteLine(_logFile, string.Format("Exporting point results as '{0}' ...", fileType));

                    if (outputFormat.Contains("Text file"))
                        result = await _sqlFunctions.CopyToTabAsync(inPoints, outFile, true, false);
                    else
                        result = await _sqlFunctions.CopyToCSVAsync(inPoints, outFile, true, false);

                    if (!result)
                    {
                        FileFunctions.WriteLine(_logFile, string.Format("Error: Exporting point results"));

                        return false;
                    }

                    blAppend = true;
                }
                // Also export the second table - append if necessary
                if (_polyCount > 0)
                {
                    // Indicate the export has started.
                    _dockPane.ProgressUpdate("Saving polygon results ...", processStep, processStepsMax);
                    processStep += 1;

                    FileFunctions.WriteLine(_logFile, string.Format("Exporting polygon results as '{0}' ...", fileType));

                    if (outputFormat.Contains("Text file"))
                        result = await _sqlFunctions.CopyToTabAsync(inPolys, outFile, true, blAppend);
                    else
                        result = await _sqlFunctions.CopyToCSVAsync(inPolys, outFile, true, blAppend);

                    if (!result)
                    {
                        FileFunctions.WriteLine(_logFile, string.Format("Error: Exporting polygon results"));
                    }
                }

                if (_pointCount == 0 && _polyCount == 0)
                    return false;

                // If there is an active map then add the results to.
                if (!createMap)
                {
                    // Indicate the data is being added to the map.
                    _dockPane.ProgressUpdate("Adding results to map ...", processStep, processStepsMax);

                    // Add the output to the map as it won't be added
                    // automatically.
                    await _mapFunctions.AddTableToMapAsync(outFile);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Export the non-spatial results to a geodatabase table,
        /// test file or CSV file.
        /// </summary>
        /// <param name="outputFormat"></param>
        /// <param name="inTable"></param>
        /// <param name="outFile"></param>
        /// <param name="addToMap"></param>
        /// <returns></returns>
        internal async Task<bool> ExportNonSpatialResultsAsync(string outputFormat,
                                  string inTable,
                                  string outFile,
                                  bool addToMap)
        {
            bool result;

            int processStep = 1;
            int processStepsMax = 2;
            if (addToMap)
                processStepsMax += 1;

            FileFunctions.WriteLine(_logFile, "Output is non-spatial and will not be split");

            // Indicate the export has started.
            _dockPane.ProgressUpdate("Saving results ...", processStep, processStepsMax);
            processStep += 1;

            if (outputFormat.Contains("Text file"))
            {
                // We are exporting a non-isSpatial output to text file.
                FileFunctions.WriteLine(_logFile, "Copying results to text file ...");
                result = await ArcGISFunctions.ExportFeaturesAsync(inTable, outFile);

                if (!result)
                {
                    FileFunctions.WriteLine(_logFile, "Error copying results to text file '" + outFile + "'");

                    return false;
                }

                FileFunctions.WriteLine(_logFile, string.Format("Results copied to text file"));
            }
            else if (outputFormat.Contains("CSV file"))
            {
                // We are exporting a non-isSpatial output to CSV file.
                FileFunctions.WriteLine(_logFile, "Copying results to CSV file ...");
                result = await ArcGISFunctions.ExportFeaturesAsync(inTable, outFile, addToMap);

                if (!result)
                {
                    FileFunctions.WriteLine(_logFile, "Error copying results to CSV file '" + outFile + "'");

                    return false;
                }

                FileFunctions.WriteLine(_logFile, string.Format("Results copied to CSV file"));
            }
            else
            {
                // We are exporting any non-spatial output to a geodatabase.
                FileFunctions.WriteLine(_logFile, "Copying results to geodatabase table ...");
                result = await ArcGISFunctions.CopyTableAsync(inTable, outFile, addToMap);

                if (!result)
                {
                    FileFunctions.WriteLine(_logFile, "Error copying results to geodatabase table '" + outFile + "'");

                    return false;
                }

                FileFunctions.WriteLine(_logFile, string.Format("Results copied to geodatabase table"));
            }

            if (!addToMap)
                return true;

            // Indicate the data is being added to the map.
            _dockPane.ProgressUpdate("Adding results to map ...", processStep, processStepsMax);

            // Add the output to the map as it won't be added
            // automatically.
            await _mapFunctions.AddTableToMapAsync(outFile);

            return true;
        }

        /// <summary>
        /// Verify the SQL query by executing it for a single row.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="whereClause"></param>
        /// <param name="groupClause"></param>
        /// <param name="orderClause"></param>
        /// <returns></returns>
        private bool VerifyQuery(string tableName, string columnNames, string whereClause, string groupClause, string orderClause)
        {
            // Build the sql command.
            StringBuilder sqlCommand = new();

            // Append the "Set NOEXEC ON" statement to stop the query being executed.
            sqlCommand.Append("SET NOEXEC ON;SELECT ");
            sqlCommand.Append(columnNames);

            if (string.IsNullOrEmpty(whereClause))
            {
                sqlCommand.Append(" FROM " + tableName);
            }
            else
            {
                if (whereClause.Substring(0, 5).Equals("from ", StringComparison.OrdinalIgnoreCase))
                {
                    sqlCommand.Append(" " + whereClause);
                }
                else
                {
                    sqlCommand.Append(" FROM " + tableName);
                    sqlCommand.Append(" WHERE " + whereClause);
                }
            }

            if (!string.IsNullOrEmpty(groupClause))
                sqlCommand.Append(" GROUP BY " + groupClause);

            if (!string.IsNullOrEmpty(orderClause))
                sqlCommand.Append(" ORDER BY " + orderClause);

            // Append the "Set NOEXEC OFF" statement to enable execution again.
            sqlCommand.Append(";SET NOEXEC OFF");

            try
            {
                // Get the connection properties from the SDE file.
                DatabaseConnectionFile connectionFile = new(new Uri(_sdeFileName));
                DatabaseConnectionProperties connectionProperties = DatabaseClient.GetDatabaseConnectionProperties(connectionFile);

                // Build a new SQL connection string using the connection properties.
                SqlConnectionStringBuilder connBuilder = new()
                {
                    DataSource = connectionProperties.Instance,
                    InitialCatalog = connectionProperties.Database,
                    PersistSecurityInfo = false,
                    IntegratedSecurity = true,
                    TrustServerCertificate = true
                };

                // Using a new SQL connection.
                using SqlConnection conn = new(connBuilder.ConnectionString);

                // Open the connection.
                conn.Open();

                // Using a new SQL command.
                using SqlCommand command = conn.CreateCommand();

                // Execute the SQL command.
                command.CommandType = System.Data.CommandType.Text;
                command.CommandTimeout = _sqlTimeout;
                command.CommandText = sqlCommand.ToString();
                command.ExecuteNonQuery();

                return true;
            }
            catch (SqlException ex)
            {
                // Remove any appended 'SET NOEXEC' statements.
                string msg = ex.Message;
                msg = msg.Replace(";SET NOEXEC OFF", "").Replace("SET NOEXEC ON;", "");

                MessageBox.Show("SQL is invalid:" + "\r\n" + msg, _displayName, MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            catch (Exception ex)
            {
                // Remove any appended 'SET NOEXEC' statements.
                string msg = ex.Message;
                msg = msg.Replace(";SET NOEXEC OFF", "").Replace("SET NOEXEC ON;", "");

                MessageBox.Show("SQL is invalid:" + "\r\n" + msg, _displayName, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Execute the SQL query.
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        private async Task<bool> ExecuteSelectionAsync(string userID)
        {
            // Reset selecting errors flag and success message.
            _selectErrors = false;
            _successMessage = null;

            // Save the parameters.
            string tableName = SelectedTable;
            string columnNames = ColumnsText == null ? "" : ColumnsText.Replace("\r\n", " ");
            string whereClause = WhereText == null ? "" : WhereText.Replace("\r\n", " ");
            string groupClause = GroupByText == null ? "" : GroupByText.Replace("\r\n", " ");
            string orderClause = OrderByText == null ? "" : OrderByText.Replace("\r\n", " ");
            string outputFormat = SelectedOutputFormat;
            string outputFile;

            // Set a temp table name if the user entered their own
            // in the WHERE clause.
            if (string.IsNullOrEmpty(tableName))
                tableName = "TempTable";

            // Validate the sql command if required.
            if (_validateSQL)
            {
                // Validate the sql command.
                if (!VerifyQuery(tableName, columnNames, whereClause, groupClause, orderClause))
                {
                    ShowMessage("SQL is invalid.", MessageType.Warning);
                    _successMessage = null;
                    _selectErrors = false;
                    return false;
                }
            }

            // Check if there is a geometry field in the input
            // table that will be returned.
			string spatialColumn = await IsTableSpatialAsync(tableName, columnNames);

            // Set if the output will be spatial.
            bool isSpatial = spatialColumn != null;
            bool isSplit = false;

            // Set/adjust the output formats as necessary.
            if (isSpatial)
            {
                isSplit = true;
                if (outputFormat == "Geodatabase")
                    outputFormat = "Geodatabase FC";
            }
            else
            {
                if (outputFormat == "Geodatabase")
                    outputFormat = "Geodatabase Table";
                else if (outputFormat == "Shapefile") // Override format for non-spatial data.
                    outputFormat = "CSV file (comma delimited)";
            }

            // Prompt the user for the output file path and name.
            outputFile = await GetOutputFileNameAsync(isSpatial, outputFormat);

            // Exit if no output file path was entered/selected.
            if (outputFile == null)
            {
                ShowMessage("No output file selected.", MessageType.Information);
                _successMessage = null;
                _selectErrors = false;
                return false;
            }

            FileFunctions.WriteLine(_logFile, "-----------------------------------------------------------------------");
            FileFunctions.WriteLine(_logFile, "Process started");
            FileFunctions.WriteLine(_logFile, "-----------------------------------------------------------------------");

            if (isSpatial)
                FileFunctions.WriteLine(_logFile, "Data is spatial. Spatial outputs (Shapefile, Geodatabase) will be split into _point and _poly components");
            else
                FileFunctions.WriteLine(_logFile, "Data is not spatial");

            FileFunctions.WriteLine(_logFile, string.Format("Output format is '{0}'", outputFormat));
            FileFunctions.WriteLine(_logFile, string.Format("Output file is '{0}'", outputFile));

            // Indicate the selection has started.
            _dockPane.QueryRunning = true;
            _dockPane.ProgressUpdate("Performing selection ...", 0, 2);

            // Run the stored procedure to perform the selection.
            bool success = await PerformSelectionAsync(isSpatial, isSplit, _defaultSchema, tableName, columnNames,
                                 whereClause, groupClause, orderClause, userID);

            // Exit if the stored procedure failed.
            if (!success)
            {
                _successMessage = "Process complete with errors!";
                _selectErrors = true;
                return false;
            }

            // Exit if the stored procedure returned no results.
            if (_pointCount == 0 && _polyCount == 0 && _tableCount == 0)
            {
                FileFunctions.WriteLine(_logFile, "No output returned");

                _successMessage = "Process complete. No output returned by the query!";
                _selectErrors = true;
                return true;
            }

            // Log the results of the stored procedure.
            if (isSpatial)
                FileFunctions.WriteLine(_logFile, "Selection returned " + _pointCount.ToString("N0") + " point and " + _polyCount.ToString("N0") +
                    " polygon records");
            else
                FileFunctions.WriteLine(_logFile, "Selection returned " + _tableCount.ToString("N0") + " records");

            // Prepare to export the results.
            string pointFeatureClass = _defaultSchema + "." + tableName + "_point_" + userID;
            string polyFeatureClass = _defaultSchema + "." + tableName + "_poly_" + userID;
            string flatTable = _defaultSchema + "." + tableName + "_" + userID;

            string inPoints = _sdeFileName + @"\" + pointFeatureClass;
            string inPolys = _sdeFileName + @"\" + polyFeatureClass;
            string flatInTable = _sdeFileName + @"\" + flatTable;

            string outPoints = null;
            string outPolys = null;

            // Set the output names depending on the output type.
            switch (outputFormat)
            {
                case "Geodatabase FC":
                    outPoints = outputFile + "_Point";
                    outPolys = outputFile + "_Poly";
                    break;

                case "Geodatabase Table":
                    break;

                case "Shapefile":
                    outputFile = FileFunctions.GetFullNameWithoutExtension(outputFile);
                    outPoints = outputFile + "_Point.shp";
                    outPolys = outputFile + "_Poly.shp";
                    break;

                case "CSV file (comma delimited)":
                    break;

                case "Text file (tab delimited)":
                    break;

                default:
                    break;
            }

            // Check if the map has closed or changed.
            if (_mapFunctions == null || _mapFunctions.MapName == null || (_mapFunctions.MapName != null && MapView.Active == null))
            {
                // Create a new map functions object.
                _mapFunctions = new();
            }

            // Check if there is an active map.
            bool createMap = _mapFunctions.MapName == null;

            // Get the map name of the active map.
            //string mapName = _mapFunctions.MapName;  // Not used at the moment.

            // Export the results in the required format
            bool exportSuccess;
            if (isSpatial)
                exportSuccess = await ExportSpatialResultsAsync(outputFormat, inPoints, inPolys, outPoints, outPolys, outputFile, createMap);
            else
                exportSuccess = await ExportNonSpatialResultsAsync(outputFormat, flatInTable, outputFile, !createMap);

            if (!exportSuccess)
            {
                _successMessage = null;
                _selectErrors = true;
                return false;
            }

            // Inform the user if no results generated.
            if (isSpatial && _pointCount == 0 && _polyCount == 0
                || !isSpatial && _tableCount == 0)
            {
                FileFunctions.WriteLine(_logFile, "No output file(s) generated");

                _successMessage = null;
                _selectErrors = true;
                return false;
            }

            //// Set the layer name(s) for use later.
            //string outputLayerName = FileFunctions.GetFileName(outputFile);
            //if (!outputFormat.Contains("Geodatabase"))
            //    outputLayerName = FileFunctions.GetFileNameWithoutExtension(outputLayerName);

            // Set the symbology for the layers in the map (but only if spatial).
            if (isSpatial && SetSymbology)
            {
                if (_pointCount > 0)
                {
                    //string pointsLayerName = FileFunctions.GetFileName(outPoints);
                    //if (!outputFormat.Contains("Geodatabase"))
                    //    pointsLayerName = FileFunctions.GetFileNameWithoutExtension(pointsLayerName);

                    // Set the symbology for the new layer.
                    //await _mapFunctions.ApplySymbologyFromLayerFileAsync(pointsLayerName, _layerLocation);
                }

                if (_polyCount > 0)
                {
                    //string polysLayerName = FileFunctions.GetFileName(outPolys);
                    //if (!outputFormat.Contains("Geodatabase"))
                    //    polysLayerName = FileFunctions.GetFileNameWithoutExtension(polysLayerName);

                    // Set the symbology for the new layer.
                    //await _mapFunctions.ApplySymbologyFromLayerFileAsync(polysLayerName, _layerLocation);
                }
            }

            // Notify user of completion.
            if (isSpatial || !createMap)
                _successMessage = "Process complete. Output(s) have been added to map";
            else
                _successMessage = "Process complete. Output(s) have been generated";

            return true;
        }

        /// <summary>
        /// Indicate that the process has stopped (either
        /// successfully or otherwise).
        /// </summary>
        /// <param name="message"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        private void StopSelection(string message, string image)
        {
            FileFunctions.WriteLine(_logFile, "---------------------------------------------------------------------------");
            FileFunctions.WriteLine(_logFile, message);
            FileFunctions.WriteLine(_logFile, "---------------------------------------------------------------------------");

            // Indicate processing has finished.
            _dockPane.QueryRunning = false;
            _dockPane.ProgressUpdate(null, -1, -1);

            string imageSource = string.Format("pack://application:,,,/DataSelector;component/Images/{0}32.png", image);

            // Notify user of completion.
            Notification notification = new()
            {
                Title = "Data Selector",
                Severity = Notification.SeverityLevel.High,
                Message = message,
                ImageSource = new BitmapImage(new Uri(imageSource)) as ImageSource
            };
            FrameworkApplication.AddNotification(notification);

            // Open the log file (if required).
            if (OpenLogFile || _selectErrors)
                Process.Start("notepad.exe", _logFile);
        }

        #endregion SQL

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

                if (ThrowOnInvalidPropertyName)
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

        #endregion Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public new event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        internal virtual void OnPropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChangedEventArgs e = new(propertyName);
                handler(this, e);
            }
        }

        #endregion INotifyPropertyChanged Members
    }
}
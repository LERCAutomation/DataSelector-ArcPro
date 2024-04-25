using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using DataTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Ink;
using System.Windows.Input;
using System.Text.RegularExpressions;

using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ActiproSoftware.Windows.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;

namespace DataSelector.UI
{
    class WindowMainViewModel : ViewModelBase, INotifyPropertyChanged
    {

        #region Fields

        //private List<string> _availableXMLFiles;
        //private String _selectedXMLProfile;

        private string _sdeFileName;

        private String _columnsText;
        private String _whereText;
        private String _groupByText;
        private String _orderByText;
        private ObservableCollection<String> _tablesList;
        private String _selectedTable;
        private List<String> _outputFormats;
        private String _selectedOutputFormat;
        private bool _clearLogFile;
        private bool _setSymbology;
        private string _layerLocation;
        private String _windowStatus;
        private string _logFile;

        string _includeWildcard;
        string _excludeWildcard;
        string _defaultFormat;
        string _defaultSchema;

        private String _queryName;

        private ICommand _saveCommand;
        private ICommand _loadCommand;
        private ICommand _okCommand;
        private ICommand _cancelCommand;
        private ICommand _loadColumnsCommand;

        long _tableCount = 0;
        long _polyCount = 0;
        long _pointCount = 0;

        Task<List<String>> _columnsTask;

        private String _displayName = "DataSelector";

        private System.Windows.Forms.Cursor _windowCursor = System.Windows.Forms.Cursors.Arrow;

        private SelectorToolConfig _toolConfig;

        private SQLServerFunctions _sqlFunctions;
        private ArcGISFunctions _gisFunctions;
        private MapFunctions _mapFunctions;


        Task _initialise;

        #endregion

        #region ViewModelBase Members

        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        public string WindowTitle
        {
            get
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return String.Format("{0} - {1}.{2}.{3}", DisplayName, version.Major,
                    version.MajorRevision,
                    version.Minor);
            }
        }

        public System.Windows.Forms.Cursor WindowCursor
        {
            get
            {
                return _windowCursor;
            }
        }

        #endregion

        #region Creator

        /// <summary>
        /// Initialise the window properties.
        /// </summary>
        /// <param name="xmlFilesList"></param>
        /// <param name="defaultXMLFile"></param>
        public WindowMainViewModel(SelectorToolConfig configFile)
        {
            if (configFile == null) return;

            _toolConfig = configFile;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _sdeFileName = _toolConfig.GetSDEName;

            // Check if the SDE file exists.
            if (!FileFunctions.FileExists(_sdeFileName))
            {
                MessageBox.Show("ArcSDE connection file " + _sdeFileName + " not found. Cannot load Data Selector.", "DataSelector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //blOpenForm = false;
                // Close the form.
            }

            // Open an connection to SQL Server
            //IWorkspace wsSQLWorkspace = null;
            try
            {
                //wsSQLWorkspace = ArcSDEFunctions.OpenArcSDEConnection(sdeFileName);
                //_ = ArcSDEFunctions.ObtainingDefinitionsFromGeodatabase(sdeFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot open ArcSDE connection " + _sdeFileName + ". Error is " + ex.Message, "DataSelector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //blOpenForm = false;
                // Close the form.
            }

            _sqlFunctions = new(_sdeFileName);
            _gisFunctions = new();
            _mapFunctions = new();


            _outputFormats = new() {
                            "Geodatabase",
                            "Shapefile",
                            "Text file (tab delimited)",
                            "CSV file (comma delimited)"};

            // Get the relevant config file settings.
            _includeWildcard = _toolConfig.GetIncludeWildcard;
            _excludeWildcard = _toolConfig.GetExcludeWildcard;
            _defaultFormat = _toolConfig.GetDefaultFormat;
            _defaultSchema = _toolConfig.GetDatabaseSchema;
            _clearLogFile = _toolConfig.GetDefaultClearLogFile;
            _setSymbology = _toolConfig.GetDefaultSetSymbology;
            _layerLocation = _toolConfig.GetLayerLocation;

            // Set the window properties.
            _selectedOutputFormat = _defaultFormat;
        }

        #endregion

        #region RequestClose

        public delegate void RequestCloseEventHandler();

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
                    Action<object> okAction = new(OkCommandClick);
                    _okCommand = new RelayCommand(okAction, param => CanOk);
                }

                return _okCommand;
            }
        }

        /// <summary>
        /// Handles event when Ok button is clicked
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void OkCommandClick(object param)
        {
            // Replace any illegal characters in the user name string.
            string userID = StringFunctions.StripIllegals(Environment.UserName, "_", false);

            // User ID should be something at least.
            if (string.IsNullOrEmpty(userID))
            {
                userID = "Temp";
                FileFunctions.WriteLine(_logFile, "User ID not found. User ID used will be 'Temp'");
            }

            // Set the destination log file path.
            _logFile = _toolConfig.GetLogFilePath + @"\DataSelector_" + userID + ".log";

            // Clear the log file if required.
            if (ClearLogFile)
            {
                bool blDeleted = FileFunctions.DeleteFile(_logFile);
                if (!blDeleted)
                {
                    MessageBox.Show("Cannot delete log file. Please make sure it is not open in another window.", "DataSelector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //myFileFuncs.CreateLogFile(_logFile);
            }

            // Check the user entered parameters.
            if (string.IsNullOrEmpty(ColumnsText))
            {
                MessageBox.Show("Please specify which columns you wish to select.", "DataSelector", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //this.BringToFront();
                //this.Cursor = Cursors.Default;
                return;
            }

            // Table name should always be selected.
            if (string.IsNullOrEmpty(SelectedTable))
            {
                MessageBox.Show("Please select a table to query from", "DataSelector.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //this.BringToFront();
                //this.Cursor = Cursors.Default;
                return;
            }

            // Output format should always be selected.
            if (string.IsNullOrEmpty(SelectedOutputFormat))
            {
                MessageBox.Show("Please select an output format.", "DataSelector", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //this.BringToFront();
                //this.Cursor = Cursors.Default;
                return;
            }

            // Perform the selection.
            bool success = await ExecuteSelectionAsync(userID);

            //// Close the main window when done.
            //DialogResult dlResult = MessageBox.Show("Process complete. Do you wish to close the form?", "Data Selector", MessageBoxButtons.YesNo);
            //if (dlResult == DialogResult.Yes)
            //    this.RequestClose();
        }

        /// <summary>
        /// Can the Ok button be pressed (has a profile been selected)?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool CanOk { get { return true; } }

        private async Task<bool> ExecuteSelectionAsync(string userID)
        {
            // Save the parameters.
            string tableName = SelectedTable;
            string columnNames = ColumnsText.Replace("\r\n", " ");
            string whereClause = WhereText.Replace("\r\n", " ");
            string groupClause = GroupByText.Replace("\r\n", " ");
            string orderClause = OrderByText.Replace("\r\n", " ");
            string outputFormat = SelectedOutputFormat;
            string outputFile;

            // Set the window status.
            _windowStatus = "Executing selection ...";
            OnPropertyChanged(nameof(WindowStatus));

            FileFunctions.WriteLine(_logFile, "-----------------------------------------------------------------------");
            FileFunctions.WriteLine(_logFile, "Process started");
            FileFunctions.WriteLine(_logFile, "-----------------------------------------------------------------------");

            //TODO: No need to pause drawing at this stage.
            //myArcMapFuncs.ToggleDrawing();
            //myArcMapFuncs.ToggleTOC();

            //SqlConnection dbConn = mySQLServerFuncs.CreateSQLConnection(myConfig.GetConnectionString());

            // Check if there is a geometry field in the returned data
            // and select the stored procedure accordingly
            bool isSpatial = await IsQuerySpatial(tableName, columnNames);

            // Set the temporary table names and the stored procedure names
            bool isSplit = false;

            // Set/adjust the output formats as necessary.
            if (isSpatial)
            {
                isSplit = true;
                if (outputFormat == "Geodatabase")
                    outputFormat = "Geodatabase FC";

                FileFunctions.WriteLine(_logFile, "Output is spatial. Output (Shapefile, Geodatabase) will be split into _point and _poly components");
            }
            else
            {
                if (outputFormat == "Geodatabase")
                    outputFormat = "Geodatabase Table";
                else if (outputFormat == "Shapefile")
                    outputFormat = "CSV file (comma delimited)";

                FileFunctions.WriteLine(_logFile, "Output is not spatial. Output file type is " + outputFormat);
            }

            // Prompt the user for the output file path and name.
            outputFile = await GetOutputFileNameAsync(isSpatial, outputFormat);

            //this.BringToFront();

            if (outputFile == null)
            {
                // User has pressed Cancel. Bring original menu to the front.
                MessageBox.Show("Please select an output file");

                //myArcMapFuncs.ToggleDrawing();
                //myArcMapFuncs.ToggleTOC();

                //this.Cursor = Cursors.Default;

                return false;
            }

            FileFunctions.WriteLine(_logFile, "Output format is " + outputFormat);
            FileFunctions.WriteLine(_logFile, "Output file is " + outputFile);

            // Run the stored procedure to perform the selection.
            bool success = await PerformSelection(isSpatial, isSplit, _defaultSchema, tableName, columnNames,
                                 whereClause, groupClause, orderClause, userID);
            if (!success)
            {
                return false;
            }

            if (isSpatial)
            {
                FileFunctions.WriteLine(_logFile, "Procedure returned " + _pointCount.ToString() + " point and " + _polyCount.ToString() +
                    " polygon records");
            }
            else
            {
                FileFunctions.WriteLine(_logFile, "Procedure returned " + _tableCount.ToString() + " records");
            }

            // Check if there is an active map.
            bool addToMap = (_mapFunctions.MapName == null);

            // If there is no active map then create one, ready to add the results to.
            if (addToMap)
            {
                await _mapFunctions.CreateMapAsync("DataMap");
            }

            // Get the map name of the active map.
            string mapName = _mapFunctions.MapName;

            // Prepare to export the results.
            string polyFeatureClass = _defaultSchema + "." + tableName + "_poly_" + userID;
            string pointFeatureClass = _defaultSchema + "." + tableName + "_point_" + userID;
            string flatTable = _defaultSchema + "." + tableName + "_" + userID;

            string inPoints = _toolConfig.GetSDEName + @"\" + pointFeatureClass;
            string inPolys = _toolConfig.GetSDEName + @"\" + polyFeatureClass;
            string flatInTable = _toolConfig.GetSDEName + @"\" + flatTable;

            string outPoints = null;
            string outPolys = null;

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

            // Export the results in the required format.
            bool result = await ExportResults(isSpatial, outputFormat, inPoints, inPolys, flatInTable, outPoints, outPolys, outputFile, false);

            //// Set the layer name(s) for use later.
            //string outputLayerName = FileFunctions.GetFileName(outputFile);
            //if (!outputFormat.Contains("Geodatabase"))
            //    outputLayerName = FileFunctions.GetFileNameWithoutExtension(outputLayerName);

            // Add the result to the map (but only if spatial).
            if (isSpatial)
            {
                if (_pointCount > 0)
                {
                    if (addToMap)
                    {
                        // Add the layer to the map.
                        await _mapFunctions.AddLayerToMap(outPoints);
                    }

                    if (SetSymbology)
                    {
                        string pointsLayerName = FileFunctions.GetFileName(outPoints);
                        if (!outputFormat.Contains("Geodatabase"))
                            pointsLayerName = FileFunctions.GetFileNameWithoutExtension(pointsLayerName);

                        // Set the symbology for the new layer.
                        await _mapFunctions.ApplySymbologyFromLayerFileAsync(pointsLayerName, _layerLocation);
                    }
                }

                if (_polyCount > 0)
                {
                    if (addToMap)
                    {
                        // Add the layer to the map.
                        await _mapFunctions.AddLayerToMap(outPolys);
                    }

                    if (SetSymbology)
                    {
                        string polysLayerName = FileFunctions.GetFileName(outPolys);
                        if (!outputFormat.Contains("Geodatabase"))
                            polysLayerName = FileFunctions.GetFileNameWithoutExtension(polysLayerName);


                        // Set the symbology for the new layer.
                        await _mapFunctions.ApplySymbologyFromLayerFileAsync(polysLayerName, _layerLocation);
                    }
                }
            }

            //// Refresh the catalog.
            //ArcGISFunctions.RefreshFolders();

            // Run the stored procedure to clear the selection.
            success = await ClearSelection(_defaultSchema, tableName, userID);
            if (!success)
            {
                return false;
            }




            FileFunctions.WriteLine(_logFile, "---------------------------------------------------------------------------");
            FileFunctions.WriteLine(_logFile, "Process complete");
            FileFunctions.WriteLine(_logFile, "---------------------------------------------------------------------------");



            // Open the log file.
            Process.Start("notepad.exe", _logFile);

            // Tidy up.
            //sqlCmd.Dispose();
            //sqlCmd2.Dispose();
            //dbConn.Dispose();

            return true;
        }


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
                    Action<object> cancelAction = new(CancelCommandClick);
                    _cancelCommand = new RelayCommand(cancelAction, param => CanCancel);
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
            // Close the main window.
            this.RequestClose();
        }

        /// <summary>
        /// Can the Cancel button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool CanCancel { get { return true; } }

        #endregion

        #region Save Command

        /// <summary>
        /// Create Save button command
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
                    _saveCommand = new RelayCommand(saveAction, param => CanSave);
                }
                return _saveCommand;
            }
        }

        /// <summary>
        /// Handles event when Save button is clicked
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void SaveCommandClick(object param)
        {
            bool saved = await SaveQueryAsync();

            //// Inform the user the load was successful.
            //if (saved)
            //    MessageBox.Show("Query saved.", "Save query", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Filter = "Query files (*.qsf)|*.qsf",
                InitialDirectory = _toolConfig.GetDefaultQueryPath,
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
                        if (saveFileNameExtension.ToLower() != "qsf")
                        {
                            MessageBox.Show("File name has incorrect extension. Save cancelled.", "Save query", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    blDone = true;
                }
                else // User pressed Cancel.
                {
                    MessageBox.Show("Please select an output file.", "Save query", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
            }

            await Task.Run(() =>
            {
                // Set the window status.
                _windowStatus = "Saving query ...";
                OnPropertyChanged(nameof(WindowStatus));

                // Save the query name ready for future saves.
                _queryName = FileFunctions.GetFileName(saveFileName);

                // Create a new stream reader for the file.
                StreamWriter qryFile = File.CreateText(saveFileName);

                // Replace carriage return and line feeds in the consituent parts.
                string columns = "Fields {" + ColumnsText.Replace("\r\n", "$$") + "}";
                string where = "Where {" + WhereText.Replace("\r\n", "$$") + "}";
                string groupBy = "Group By {" + GroupByText.Replace("\r\n", "$$") + "}";
                string orderBy = "Order By {" + OrderByText.Replace("\r\n", "$$") + "}";

                // Write the constituent parts to the file.
                qryFile.WriteLine(columns);
                qryFile.WriteLine(where);
                qryFile.WriteLine(groupBy);
                qryFile.WriteLine(orderBy);

                // Close the displose of the stream reader.
                qryFile.Close();
                qryFile.Dispose();
            });

            // Reset the window status.
            _windowStatus = null;
            OnPropertyChanged(nameof(WindowStatus));

            return true;
        }

        /// <summary>
        /// Can the Save button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool CanSave { get { return true; } }

        #endregion

        #region Load Command

        /// <summary>
        /// Create Load button command
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
                    _loadCommand = new RelayCommand(loadAction, param => CanLoad);
                }
                return _loadCommand;
            }
        }

        /// <summary>
        /// Handles event when Load button is clicked
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void LoadCommandClick(object param)
        {
            bool loaded = await LoadQueryAsync();

            //// Inform the user the load was successful.
            //if (loaded)
            //    MessageBox.Show("Query loaded.", "Load query", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Filter = "Query files (*.qsf)|*.qsf",
                InitialDirectory = _toolConfig.GetDefaultQueryPath
            };

            string saveFileName;

            // If a file has been chosen.
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                await Task.Run(() =>
                {
                    // Set the window status.
                    _windowStatus = "Loading query ...";
                    OnPropertyChanged(nameof(WindowStatus));

                    // Clear the form.
                    _columnsText = "";
                    _whereText = "";
                    _groupByText = "";
                    _orderByText = "";

                    // Get the name of the file to open.
                    saveFileName = openFileDialog.FileName;

                    // Save the query name ready for future saves.
                    _queryName = FileFunctions.GetFileName(saveFileName);

                    // Create a new stream reader for the file.
                    StreamReader qryFile = new(saveFileName);

                    // Loop through the lines in the query, separating them
                    // into their constituent parts.
                    string qryLine = "";
                    while ((qryLine = qryFile.ReadLine()) != null)
                    {
                        if (qryLine.Length > 7 && qryLine.Substring(0, 8).ToUpper() == "FIELDS {" && qryLine.ToUpper() != "FIELDS {}")
                        {
                            qryLine = qryLine.Substring(8, qryLine.Length - 9);
                            _columnsText = qryLine.Replace("$$", "\r\n");
                        }
                        else if (qryLine.Length > 6 && qryLine.Substring(0, 7).ToUpper() == "WHERE {" && qryLine.ToUpper() != "WHERE {}")
                        {
                            qryLine = qryLine.Substring(7, qryLine.Length - 8);
                            _whereText = qryLine.Replace("$$", "\r\n");
                        }
                        else if (qryLine.Length > 9 && qryLine.Substring(0, 10).ToUpper() == "GROUP BY {" && qryLine.ToUpper() != "GROUP BY {}")
                        {
                            qryLine = qryLine.Substring(10, qryLine.Length - 11);
                            _groupByText = qryLine.Replace("$$", "\r\n");
                        }
                        else if (qryLine.Length > 9 && qryLine.Substring(0, 10).ToUpper() == "ORDER BY {" && qryLine.ToUpper() != "ORDER BY {}")
                        {
                            qryLine = qryLine.Substring(10, qryLine.Length - 11);
                            _orderByText = qryLine.Replace("$$", "\r\n");
                        }
                    }

                    // Close the displose of the stream reader.
                    qryFile.Close();
                    qryFile.Dispose();

                    // Update the fields in the form.
                    OnPropertyChanged(nameof(ColumnsText));
                    OnPropertyChanged(nameof(WhereText));
                    OnPropertyChanged(nameof(GroupByText));
                    OnPropertyChanged(nameof(OrderByText));
                });

                // Reset the window status.
                _windowStatus = null;
                OnPropertyChanged(nameof(WindowStatus));

                return true;
            }
            else
            {
                // Reset the window status.
                _windowStatus = null;
                OnPropertyChanged(nameof(WindowStatus));

                return false;
            }
        }

        /// <summary>
        /// Can the Load button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool CanLoad { get { return true; } }

        #endregion

        #region LoadColumns Command

        /// <summary>
        /// Create LoadColumns button command
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
                    _loadColumnsCommand = new RelayCommand(loadColumnsAction, param => CanLoadColumns);
                }
                return _loadColumnsCommand;
            }
        }

        /// <summary>
        /// Handles event when LoadColumns button is clicked
        /// </summary>
        /// <param name="param"></param>
        /// <remarks></remarks>
        private async void LoadColumnsCommandClick(object param)
        {
            await LoadColumnsAsync(_selectedTable);
        }

        /// <summary>
        /// Can the LoadColumns button be pressed?
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool CanLoadColumns { get { return true; } }

        #endregion

        #region Properties

        /// <summary>
        ///
        /// </summary>
        public String ColumnsText
        {
            get
            {
                return _columnsText;
            }
            set
            {
                _columnsText = value;
            }
        }

        /// <summary>
        ///
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
            }
        }

        /// <summary>
        ///
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
            }
        }

        /// <summary>
        ///
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
            }
        }

        public ObservableCollection<String> TablesList
        {
            get
            {
                return _tablesList;
            }
        }

        public String SelectedTable
        {
            get
            {
                return _selectedTable;
            }
            set
            {
                _selectedTable = value;
            }
        }

        public String WindowStatus
        {
            get
            {
                return _windowStatus;
            }
        }

        /// <summary>
        /// Called on double-click of the TablesList. The columns belonging
        /// to the table name that was double clicked are filled in to the
        /// Columns textbox.
        /// </summary>
        /// <param name="selectedTable"></param>
        /// <returns></returns>
        public async Task LoadColumnsAsync(string selectedTable)
        {
            // If a table was selected (which it must have been with a double-click.
            if (selectedTable != null)
            {
                if (!String.IsNullOrEmpty(_columnsText))
                {
                    DialogResult dlResult = MessageBox.Show("There is already text in the Columns field. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dlResult == System.Windows.Forms.DialogResult.No)
                        return; //User clicked by accident; leave routine.
                }

                // Set the window status.
                _windowStatus = "Getting table names ...";
                OnPropertyChanged(nameof(WindowStatus));

                // Get the field names and wait for the task to finish.
                List<String> columnsList = await _sqlFunctions.GetFieldNamesListAsync(SelectedTable);

                // Convert the field names to a single string.
                string columnNamesText = "";
                foreach (string columnName in columnsList)
                {
                    columnNamesText = columnNamesText + columnName + ",\r\n";
                }
                columnNamesText = columnNamesText.Substring(0, columnNamesText.Length - 3);

                // Replace the text box value with the field names.
                _columnsText = columnNamesText;

                // Update the field in the form.
                OnPropertyChanged(nameof(ColumnsText));

                // Reset the window status
                _windowStatus = null;
                OnPropertyChanged(nameof(WindowStatus));
            }
        }

        public List<String> OutputFormats
        {
            get
            {
                return _outputFormats;
            }
        }

        public String SelectedOutputFormat
        {
            get
            {
                return _selectedOutputFormat;
            }
            set
            {
                _selectedOutputFormat = value;
            }
        }

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

        #endregion

        #region SQL

        /// <summary>
        /// Get a list of the table names from the SQL Server.
        /// </summary>
        public async void GetTableNames()
        {
            _windowStatus = "Getting table names ...";
            OnPropertyChanged(nameof(WindowStatus));

            //
            Task getTableNamesTask = _sqlFunctions.GeodatabaseGetTableNamesAsync();
            await getTableNamesTask;

            List<String> tabList = _sqlFunctions.TableNames;

            string includeWC = String.Format("{0}.{1}", _defaultSchema, _includeWildcard);
            string excludeWC = String.Format("{0}.{1}", _defaultSchema, _excludeWildcard);

            // Get the SQL table names and add them to a list.
            ObservableCollection<String> tablesList = new();
            List<string> tableList = FilterTableNames(tabList, includeWC, excludeWC, false);
            foreach (string strItem in tableList)
            {
                //_tablesList.Add(strItem);
                tablesList.Add(strItem);
            }

            //_tablesList = new ObservableCollection<string>(_tablesList.OrderBy(t => t));
            _tablesList = new(tablesList.OrderBy(t => t));
            OnPropertyChanged(nameof(TablesList));

            _windowStatus = null;
            OnPropertyChanged(nameof(WindowStatus));
        }

        /// <summary>
        /// Filter the list of the table names base on the include and exclude wildcard critera.
        /// </summary>
        /// <param name="includeWildcard"></param>
        /// <param name="excludeWildcard"></param>
        /// <param name="includeFullName"></param>
        /// <returns></returns>
        internal static List<string> FilterTableNames(List<String> inputNames, string includeWildcard, string excludeWildcard,
                              bool includeFullName = false)
        {
            // Define the wildcards as case insensitive
            Wildcard theInclude = new(includeWildcard, RegexOptions.IgnoreCase);
            Wildcard theExclude = new(excludeWildcard, RegexOptions.IgnoreCase);

            List<string> theStringList = new();

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
        /// <returns></returns>
        internal async Task<bool> IsQuerySpatial(string tableName, string columnsText)
        {
            string[] geometryFields = { "SP_GEOMETRY", "Shape" }; // Expand as required.

            // Get the list of field names in the selected table.
            List<String> fieldsList = await _sqlFunctions.GetFieldNamesListAsync(tableName);

            // Loop through the geometry fields looking for a match.
            foreach (string geomField in geometryFields)
            {
                // If the columns text contains the geometry field.
                if (columnsText.ToLower().Contains(geomField.ToLower()))
                {
                    return true;
                }
                // If "*" is used check for the existence of the geometry field in the table.
                else if (columnsText.ToLower().Contains('*'))
                {
                    foreach (string fieldName in fieldsList)
                    {
                        // If the column text contains the geometry field.
                        if (fieldName.ToLower() == geomField.ToLower())
                            return true;
                    }
                }
            }

            // No geometry column found.
            return false;
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
                outputFile = ArcGISFunctions.GetOutputFileName(outputFormat, _toolConfig.GetDefaultExtractPath);
                if (outputFile != null)
                {
                    // firstly check extensions are as should be.
                    string fileExtensionTest = outputFile.Substring(outputFile.Length - 4, 4).Substring(0, 1);
                    if (fileExtensionTest == ".") blHasExtension = true;

                    // if there isn't, put one one.
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
                    else if ((outputFormat.Contains("Geodatabase")) && (blHasExtension || !outputFile.Contains(".gdb"))) // It is a geodatabase file and should not have an extension.
                    {
                        MessageBox.Show("Please select a file geodatabase output file");
                        //myArcMapFuncs.ToggleDrawing();
                        //myArcMapFuncs.ToggleTOC();
                        //this.Cursor = Cursors.Default;
                        //this.BringToFront();
                        return null;
                    }
                    else if ((!outputFormat.Contains("Geodatabase")) && (outputFile.Contains(".gdb"))) // Trying to store a non-geoDB in a gdb
                    {
                        MessageBox.Show("Cannot store " + outputFormat + " inside a geodatabase. Please choose a different output location");
                        //myArcMapFuncs.ToggleDrawing();
                        //myArcMapFuncs.ToggleTOC();
                        //this.Cursor = Cursors.Default;
                        //this.BringToFront();
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
                            DialogResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                            if (dlResult1 == System.Windows.Forms.DialogResult.Yes)
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
                                DialogResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                                if (dlResult1 == System.Windows.Forms.DialogResult.Yes)
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
                        if (ArcGISFunctions.TableExists(outputFile))
                        {
                            DialogResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                            if (dlResult1 == System.Windows.Forms.DialogResult.Yes)
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
                            DialogResult dlResult1 = MessageBox.Show("The output file already exists. Do you want to overwrite it?", "Data Selector", MessageBoxButtons.YesNo);
                            if (dlResult1 == System.Windows.Forms.DialogResult.Yes)
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

        internal async Task<bool> PerformSelection(bool isSpatial, bool isSplit, string schema, string tableName,
                                  string columnNames, string whereClause, string groupClause, string orderClause,
                                  string userID)
        {
            bool success = false;

            string storedProcedureName = "AFSelectSppSubset";

            // Hard-code the parameters (for testing).
            //schema = "dbo";
            //tableName = "Spp_Poly_NoNames";
            //columnNames = "*";
            //whereClause = "TaxonGroup LIKE 'Higher%'";
            //groupClause = "";
            //orderClause = "";
            //userID = "Test";
            //isSplit = "0";

            // Write the parameters to the log file.
            FileFunctions.WriteLine(_logFile, "Species table is " + tableName);
            FileFunctions.WriteLine(_logFile, "Column names are " + columnNames);
            if (whereClause != null && whereClause.Length > 0)
                FileFunctions.WriteLine(_logFile, "Where clause is " + whereClause);
            else
                FileFunctions.WriteLine(_logFile, "No where clause was used");
            if (groupClause != null && groupClause.Length > 0)
                FileFunctions.WriteLine(_logFile, "Group by clause is " + groupClause);
            else
                FileFunctions.WriteLine(_logFile, "No group by clause was used");
            if (orderClause != null && orderClause.Length > 0)
                FileFunctions.WriteLine(_logFile, "Order by clause is " + orderClause);
            else
                FileFunctions.WriteLine(_logFile, "No order by clause was used");
            if (isSplit)
                FileFunctions.WriteLine(_logFile, "Data is spatial and will be split into a point and a polygon layer");
            else
                FileFunctions.WriteLine(_logFile, "Data is not spatial and will not be split");

            // Set up the SQL command.
            StringBuilder sqlCmd = new();

            // Double-up single quotes so they parse in SQL command correctly.
            if (whereClause != null && whereClause.Contains('\''))
                whereClause = whereClause.Replace("'", "''");

            // Build the SQL command to execute the stored procedure.
            sqlCmd = sqlCmd.Append(String.Format("EXECUTE {0}", storedProcedureName));
            sqlCmd.Append(string.Format(" '{0}'", schema));
            sqlCmd.Append(string.Format(", '{0}'", tableName));
            sqlCmd.Append(string.Format(", '{0}'", columnNames));
            sqlCmd.Append(string.Format(", '{0}'", whereClause));
            sqlCmd.Append(string.Format(", '{0}'", groupClause));
            sqlCmd.Append(string.Format(", '{0}'", orderClause));
            sqlCmd.Append(string.Format(", '{0}'", userID));
            sqlCmd.Append(string.Format(", {0}", isSplit ? "1" : "0"));

            string polyFeatureClass = schema + "." + tableName + "_poly_" + userID;
            string pointFeatureClass = schema + "." + tableName + "_point_" + userID;
            string flatTable = schema + "." + tableName + "_" + userID;

            try
            {
                FileFunctions.WriteLine(_logFile, "Performing selection");

                // Execute the stored procedure.
                await _sqlFunctions.ExecuteSQLOnGeodatabase(sqlCmd.ToString());

                // If the result is isSpatial it should be split into points and polys.
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
                        _pointCount = await _sqlFunctions.FeatureClassCountRowsAsync(pointFeatureClass);

                        // Count the number of rows in the poly feature count.
                        _polyCount = await _sqlFunctions.FeatureClassCountRowsAsync(polyFeatureClass);
                    }
                    else
                    {
                        // Count the number of rows in the table.
                        _tableCount = await _sqlFunctions.TableCountRowsAsync(flatTable);
                    }
                }
            }
            catch (Exception ex)
            {
                FileFunctions.WriteLine(_logFile, "Error executing the stored procedure. System returned the following message: " +
                    ex.Message);
                MessageBox.Show("Error executing the stored procedure. System returned the following message: " +
                    ex.Message);
            }

            return success;
        }

        internal async Task<bool> ClearSelection(string schema, string tableName, string userID)
        {
            bool success = false;

            // Set up the SQL command.
            StringBuilder sqlCmd = new();

            string storedProcedureName = "AFClearSppSubset";

            // Build the SQL command to execute the stored procedure.
            sqlCmd = sqlCmd.Append(String.Format("EXECUTE {0}", storedProcedureName));
            sqlCmd.Append(string.Format(" '{0}'", schema));
            sqlCmd.Append(string.Format(", '{0}'", tableName));
            sqlCmd.Append(string.Format(", '{0}'", userID));

            try
            {
                FileFunctions.WriteLine(_logFile, "Deleting temporary tables");

                // Execute the stored procedure.
                await _sqlFunctions.ExecuteSQLOnGeodatabase(sqlCmd.ToString());

                success = true;
            }
            catch (Exception ex)
            {
                FileFunctions.WriteLine(_logFile, "Error deleting the temporary tables. System returned the following message: " +
                    ex.Message);
                MessageBox.Show("Error deleting the temporary tables. System returned the following message: " +
                    ex.Message);
            }

            return success;
        }

        internal async Task<bool> ExportResults(bool isSpatial, string outputFormat,
                                  string inPoints, string inPolys, string inTable,
                                  string outPoints, string outPolys, string outFile,
                                  bool addToMap)
        {
            bool isFlatTable = !isSpatial;
            bool result = false;

            // Export the points, polygons and/or flat table.
            if (isSpatial)
            {
                // How is the data to be exported?
                if (outputFormat == "Geodatabase FC")
                {
                    // Easy, export without further ado.
                    if (_pointCount > 0)
                    {
                        FileFunctions.WriteLine(_logFile, "Copying point results to point geodatabase file");
                        result = await ArcGISFunctions.CopyFeaturesAsync(inPoints, outPoints);

                        if (!result)
                        {
                            FileFunctions.WriteLine(_logFile, "Error exporting point geodatabase file");
                            MessageBox.Show("Error exporting point geodatabase file");

                            //this.Cursor = Cursors.Default;
                            //_gisFunctions.ToggleDrawing();
                            //_gisFunctions.ToggleTOC();
                            //this.BringToFront();

                            return false;
                        }

                        // Add the result to the map.
                        if (addToMap)
                            await _mapFunctions.AddLayerToMap(outPoints);
                    }
                    if (_polyCount > 0)
                    {
                        result = await ArcGISFunctions.CopyFeaturesAsync(inPolys, outPolys);
                        FileFunctions.WriteLine(_logFile, "Copying polygon results to polygon geodatabase file");

                        if (!result)
                        {
                            FileFunctions.WriteLine(_logFile, "Error exporting polygon geodatabase file");
                            MessageBox.Show("Error exporting polygon geodatabase file");

                            //this.Cursor = Cursors.Default;
                            //myArcMapFuncs.ToggleDrawing();
                            //myArcMapFuncs.ToggleTOC();
                            //this.BringToFront();

                            return false;
                        }

                        // Add the result to the map.
                        if (addToMap)
                            await _mapFunctions.AddLayerToMap(outPolys);
                    }

                    return true;

                }
                else if (outputFormat == "Shapefile")
                {
                    // Create file names first.
                    //outFile = FileFunctions.GetFullNameWithoutExtension(outFile);

                    if (_pointCount > 0)
                    {
                        FileFunctions.WriteLine(_logFile, "Copying point results to point shapefile");
                        result = await ArcGISFunctions.CopyFeaturesAsync(inPoints, outPoints);

                        if (!result)
                        {
                            FileFunctions.WriteLine(_logFile, "Error exporting point shapefile");
                            MessageBox.Show("Error exporting point shapefile");

                            //this.Cursor = Cursors.Default;
                            //myArcMapFuncs.ToggleDrawing();
                            //myArcMapFuncs.ToggleTOC();
                            //this.BringToFront();

                            return false;
                        }

                        // Add the result to the map.
                        if (addToMap)
                            await _mapFunctions.AddLayerToMap(outPoints);
                    }
                    if (_polyCount > 0)
                    {
                        FileFunctions.WriteLine(_logFile, "Copying polygon results to polygon shapefile");
                        result = await ArcGISFunctions.CopyFeaturesAsync(inPolys, outPolys);

                        if (!result)
                        {
                            FileFunctions.WriteLine(_logFile, "Error exporting polygon shapefile");
                            MessageBox.Show("Error exporting polygon shapefile");

                            //this.Cursor = Cursors.Default;
                            //myArcMapFuncs.ToggleDrawing();
                            //myArcMapFuncs.ToggleTOC();
                            //this.BringToFront();

                            return false;
                        }

                        // Add the result to the map.
                        if (addToMap)
                            await _mapFunctions.AddLayerToMap(outPolys);
                    }

                    return true;
                }
                else if (outputFormat.Contains("Text file"))
                {
                    // Not a isSpatial export, but it is a isSpatial layer so there are two files.
                    // Function pulls them back together again.

                    // if schema.ini file exists delete it.
                    string strIniFile = FileFunctions.GetDirectoryName(outFile) + "\\schema.ini";
                    if (FileFunctions.FileExists(strIniFile))
                    {
                        bool blDeleted = FileFunctions.DeleteFile(strIniFile); // Not checking for success at the moment.
                    }

                    isFlatTable = true;
                    bool blAppend = false;
                    if (_pointCount > 0)
                    {
                        FileFunctions.WriteLine(_logFile, "Copying point results to text file");
                        result = await _sqlFunctions.CopyToTabAsync(inPoints, outFile, isSpatial, false, true);
                        //result = await _gisFunctions.CopyToTextFileAsync(inPoints, outFile, true, false, true);

                        if (!result)
                        {
                            FileFunctions.WriteLine(_logFile, "Error exporting output table to text file " + outFile);
                            MessageBox.Show("Error exporting output table to text file " + outFile);

                            //this.Cursor = Cursors.Default;
                            //myArcMapFuncs.ToggleDrawing();
                            //myArcMapFuncs.ToggleTOC();
                            //this.BringToFront();

                            return false;
                        }
                        blAppend = true;
                    }
                    // Also export the second table - append if necessary
                    if (_polyCount > 0)
                    {
                        FileFunctions.WriteLine(_logFile, "exporting polygon results to text file");
                        result = await _sqlFunctions.CopyToTabAsync(inPolys, outFile, isSpatial, blAppend, true);
                        //result = await _gisFunctions.CopyToTextFileAsync(inPolys, outFile, true, blAppend, true);

                        if (!result)
                        {
                            FileFunctions.WriteLine(_logFile, "Error appending output table to text file " + outFile);
                            MessageBox.Show("Error appending output table to text file " + outFile);

                            //this.Cursor = Cursors.Default;
                            //myArcMapFuncs.ToggleDrawing();
                            //myArcMapFuncs.ToggleTOC();
                            //this.BringToFront();
                        }
                    }

                    // Add the output to ArcMap
                    //FileFunctions.WriteLine(_logFile, "Adding output to ArcMap view");
                    //_gisFunctions.AddTableLayerFromString(outFile, layerName);

                    return true;
                }
                else if (outputFormat.Contains("CSV file"))
                {
                    // Not a isSpatial export, but it is a isSpatial layer so there are two files.
                    // Function pulls them back together again.

                    // if schema.ini file exists delete it.
                    string strIniFile = FileFunctions.GetDirectoryName(outFile) + "\\schema.ini";
                    if (FileFunctions.FileExists(strIniFile))
                    {
                        bool blDeleted = FileFunctions.DeleteFile(strIniFile); // Not checking for success at the moment.
                    }

                    isFlatTable = true;
                    string sFinalFile = "";

                    bool blAppend = false;
                    if (_pointCount > 0)
                    {
                        FileFunctions.WriteLine(_logFile, "Copying point results to CSV file");
                        result = await _sqlFunctions.CopyToCSVAsync(inPoints, outFile, isSpatial, false, true);
                        //result = await ArcGISFunctions.ExportFeaturesAsync(inPoints, outPoints);
                        //result = await _gisFunctions.CopyToCSVAsync(inPoints, outFile, true, false, true);

                        if (!result)
                        {
                            FileFunctions.WriteLine(_logFile, "Error exporting output table to CSV file " + outFile);
                            MessageBox.Show("Error exporting output table to CSV file " + outFile);

                            //this.Cursor = Cursors.Default;
                            //myArcMapFuncs.ToggleDrawing();
                            //myArcMapFuncs.ToggleTOC();
                            //this.BringToFront();

                            return false;
                        }
                        blAppend = true;
                    }

                    // Also export the second table - append if necessary.
                    if (_polyCount > 0)
                    {
                        FileFunctions.WriteLine(_logFile, "Appending polygon results to CSV file");
                        result = await _sqlFunctions.CopyToTabAsync(inPolys, outFile, isSpatial, blAppend, true);
                        //result = await ArcGISFunctions.ExportFeaturesAsync(inPolys, outPolys);
                        //result = await _gisFunctions.CopyToCSVAsync(inPolys, outFile, true, blAppend, true);

                        if (!result)
                        {
                            FileFunctions.WriteLine(_logFile, "Error appending output table to CSV file " + outFile);
                            MessageBox.Show("Error appending output table to CSV file " + outFile);

                            //this.Cursor = Cursors.Default;
                            //myArcMapFuncs.ToggleDrawing();
                            //myArcMapFuncs.ToggleTOC();
                            //this.BringToFront();
                        }
                    }

                    return true;

                }

                return false;
            }
            else // Non-isSpatial query, successfully run.
            {
                if (outputFormat.Contains("CSV file"))
                {
                    // We are exporting a non-isSpatial output to CSV file.
                    FileFunctions.WriteLine(_logFile, "Copying results to CSV file");
                    result = await ArcGISFunctions.ExportFeaturesAsync(inTable, outFile);
                    //result = await _gisFunctions.CopyToCSVAsync(inTable, outFile, false, false, true);

                    if (!result)
                    {
                        FileFunctions.WriteLine(_logFile, "Error exporting output table to CSV file " + outFile);
                        MessageBox.Show("Error exporting output table to CSV file " + outFile);

                        //this.Cursor = Cursors.Default;
                        //myArcMapFuncs.ToggleDrawing();
                        //myArcMapFuncs.ToggleTOC();
                        //this.BringToFront();

                        return false;
                    }

                    //FileFunctions.WriteLine(_logFile, "Adding output to ArcMap view");
                    //_gisFunctions.AddTableLayerFromString(outFile, layerName);
                }

                else if (outputFormat.Contains("Text file"))
                {
                    // We are exporting a non-isSpatial output to text file.
                    FileFunctions.WriteLine(_logFile, "Copying results to text file");
                    result = await ArcGISFunctions.ExportFeaturesAsync(inTable, outFile);
                    //result = await _gisFunctions.CopyToTextFileAsync(inTable, outFile, false, false, true);

                    if (!result)
                    {
                        FileFunctions.WriteLine(_logFile, "Error exporting output table to text file " + outFile);
                        MessageBox.Show("Error exporting output table to text file " + outFile);

                        //this.Cursor = Cursors.Default;
                        //myArcMapFuncs.ToggleDrawing();
                        //myArcMapFuncs.ToggleTOC();
                        //this.BringToFront();

                        return false;
                    }

                    //FileFunctions.WriteLine(_logFile, "Adding output to ArcMap view");
                    //_gisFunctions.AddTableLayerFromString(outFile, layerName);
                }
                else
                {
                    // We are exporting any non-spatial output to a geodatabase.
                    result = await ArcGISFunctions.CopyFeaturesAsync(inTable, outFile);
                    if (!result)
                    {
                        FileFunctions.WriteLine(_logFile, "Error exporting output table");
                        MessageBox.Show("Error exporting output table");

                        //this.Cursor = Cursors.Default;
                        //myArcMapFuncs.ToggleDrawing();
                        //myArcMapFuncs.ToggleTOC();
                        //this.BringToFront();

                        return false;
                    }
                }

                return true;
            }
        }

        internal static bool CreateEmptyOutput()
        {
            //if (outputFormat == "CSV file")
            //{
            //    if (columnNames == "*")
            //    {
            //        //dbConn.Open();
            //        string[] columnNames = SQLServerFunctions.GetFieldNamesListAsync(ref dbConn, tableName);
            //        //dbConn.Close();
            //        columnNames = "";
            //        foreach (string columnName in columnNames)
            //        {
            //            columnNames = columnNames + columnName + ",";
            //        }

            //        // Remove last comma
            //        columnNames = columnNames.Substring(0, columnNames.Length - 1);
            //        _gisFunctions.WriteEmptyCSV(outFile, columnNames);

            //        FileFunctions.WriteLine(_logFile, "There were no results for the query. An empty text file has been created");
            //        MessageBox.Show("There were no results for the query. An empty text file has been created");
            //    }
            //}
            //else
            //{
            //    FileFunctions.WriteLine(_logFile, "There were no results for the query. No output has been created");
            //    MessageBox.Show("There were no results for this query. No output has been created");
            //}

            return true;
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

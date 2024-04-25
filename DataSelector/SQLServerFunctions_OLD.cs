//// DataSelector is an ArcGIS add-in used to extract biodiversity
//// information from SQL Server based on any selection criteria.
////
//// Copyright © 2016-2017 SxBRC, 2017-2018 TVERC
////
//// This file is part of DataSelector.
////
//// DataSelector is free software: you can redistribute it and/or modify
//// it under the terms of the GNU General Public License as published by
//// the Free Software Foundation, either version 3 of the License, or
//// (at your option) any later version.
////
//// DataSelector is distributed in the hope that it will be useful,
//// but WITHOUT ANY WARRANTY; without even the implied warranty of
//// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//// GNU General Public License for more details.
////
//// You should have received a copy of the GNU General Public License
//// along with DataSelector.  If not, see <http://www.gnu.org/licenses/>.


//using ArcGIS.Core.Geometry;



//using System;
//using System.IO;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;

////using ESRI.ArcGIS.Geodatabase;
////using ESRI.ArcGIS.ArcMapUI;
////using ESRI.ArcGIS.Carto;

//using System.Data;
//using System.Data.SqlClient;
//using System.Text.RegularExpressions;
//using ArcGIS.Core.Data;
//using ArcGIS.Desktop.Framework;
//using System.Threading.Tasks;
//using ArcGIS.Desktop.Framework.Threading.Tasks;
//using ArcGIS.Desktop.Catalog;
//using System.Collections.ObjectModel;
//using System.Windows;
//using System.Windows.Input;
//using System.Windows.Data;
//using ActiproSoftware.Windows.Extensions;

//using MessageBox = System.Windows.Forms.MessageBox;
//using Index = ArcGIS.Core.Data.Index;


//namespace DataTools
//{


//    public enum EnumDatastoreType
//    {
//        /// <summary>
//        /// Datastore is File Geodatabase
//        /// </summary>
//        FileGDB,
//        /// <summary>
//        /// Datastore is Enterprise Geodatabase
//        /// </summary>
//        EnterpriseGDB,
//        /// <summary>
//        /// Datastore is Web Geodatabase
//        /// </summary>
//        WebGDB,
//        /// <summary>
//        /// Datastore is Enterprise Database
//        /// </summary>
//        EnterpriseDB,
//        /// <summary>
//        /// Datastore is SQLITE Database
//        /// </summary>
//        SqliteDB,
//        /// <summary>
//        /// Datastore is Shape File
//        /// </summary>
//        ShapeFile
//    }

//    /// <summary>
//    /// Encapsulates the meta data definition for a Dataset
//    /// </summary>
//    public class DatasetInfo
//    {
//        /// <summary>
//        /// Dataset name
//        /// </summary>
//        public string Name { get; set; }

//        /// <summary>
//        /// Definition for a given dataset
//        /// </summary>
//        public Definition DatasetDefinition { get; set; }
//    }


//    public class DatasetTypeCategory
//    {
//        /// <summary>
//        /// Ctor
//        /// </summary>
//        /// <param name="name"></param>
//        /// <param name="datasetType"></param>
//        public DatasetTypeCategory(string name, DatasetType datasetType)
//        {
//            Name = name;
//            DatasetType = datasetType;
//        }

//        /// <summary>
//        /// Datastore Category description
//        /// </summary>
//        public string Name { get; set; }
//        /// <summary>
//        /// DatasetType
//        /// </summary>
//        public DatasetType DatasetType { get; set; }
//    }


//    public class DatastoreCategory
//    {
//        /// <summary>
//        /// Ctor
//        /// </summary>
//        /// <param name="type"></param>
//        /// <param name="name"></param>
//        /// <param name="pathCaption"></param>
//        /// <param name="openDlgFilter"></param>
//        /// <param name="openDlgTitle"></param>
//        public DatastoreCategory(EnumDatastoreType type,
//            string name, string pathCaption,
//            string openDlgFilter, string openDlgTitle)
//        {
//            Name = name;
//            PathCaption = pathCaption;
//            Type = type;
//            OpenDlgFilter = openDlgFilter;
//            OpenDlgTitle = openDlgTitle;
//        }

//        /// <summary>
//        /// datastore category name
//        /// </summary>
//        public string Name { get; internal set; }
//        /// <summary>
//        /// Caption to be shown on button to query for data path entry
//        /// </summary>
//        public string PathCaption { get; internal set; }
//        /// <summary>
//        /// Type of datastore
//        /// </summary>
//        public EnumDatastoreType Type { get; internal set; }
//        /// <summary>
//        /// Filter used for openItem dialog
//        /// </summary>
//        public string OpenDlgFilter { get; internal set; }
//        /// <summary>
//        /// Title used for openItem dialog
//        /// </summary>
//        public string OpenDlgTitle { get; internal set; }

//        /// <summary>
//        /// Method opens a 'datastore' using the given path, then Datatypes is populated with options for the given store
//        /// </summary>
//        /// <param name="path"></param>
//        /// <param name="dataTypes"></param>
//        /// <returns></returns>
//        public Task<Datastore> OpenDatastore(Uri path, ObservableCollection<DatasetTypeCategory> dataTypes)
//        {
//            Task<Datastore> datastore = null;
//            switch (Type)
//            {
//                case EnumDatastoreType.FileGDB:
//                    PopulateFileEnterpriseDatasetTypes(dataTypes);
//                    datastore = QueuedTask.Run<Datastore>(() =>
//                    {
//                        return new Geodatabase(new FileGeodatabaseConnectionPath(path));
//                    });
//                    break;
//                case EnumDatastoreType.EnterpriseGDB:
//                    PopulateFileEnterpriseDatasetTypes(dataTypes);
//                    datastore = QueuedTask.Run<Datastore>(() =>
//                    {
//                        return new Geodatabase(new DatabaseConnectionFile(path));
//                    });
//                    break;
//                case EnumDatastoreType.WebGDB:
//                    PopulateServiceGeodatabaseDatabaseShapeFileDataTypes(dataTypes);
//                    datastore = QueuedTask.Run<Datastore>(() =>
//                    {
//                        return new Geodatabase(new ServiceConnectionProperties(path));
//                    });
//                    break;
//                //case EnumDatastoreType.EnterpriseDB:
//                //	PopulateServiceGeodatabaseDatabaseShapeFileDataTypes(dataTypes);
//                //	datastore = QueuedTask.Run<Datastore>(() =>
//                //	{
//                //		return new Database(new DatabaseConnectionFile(path));
//                //	});
//                //	break;
//                case EnumDatastoreType.SqliteDB:
//                    PopulateServiceGeodatabaseDatabaseShapeFileDataTypes(dataTypes);
//                    datastore = QueuedTask.Run<Datastore>(() =>
//                    {
//                        return new Database(new SQLiteConnectionPath(path));
//                    });
//                    break;
//                case EnumDatastoreType.ShapeFile:
//                    PopulateServiceGeodatabaseDatabaseShapeFileDataTypes(dataTypes);
//                    datastore = QueuedTask.Run<Datastore>(() =>
//                    {
//                        return new FileSystemDatastore(new FileSystemConnectionPath(path, FileSystemDatastoreType.Shapefile));
//                    });
//                    break;
//            }
//            return datastore;
//        }

//        /// <summary>
//        /// called to validate if the DataPath works for the given type of datastore
//        /// </summary>
//        /// <param name="DataPath"></param>
//        /// <returns>Validation Error message or null if ok</returns>
//        public string ValidateDataPath(string DataPath)
//        {
//            var ext = System.IO.Path.GetExtension(DataPath).ToLower();
//            switch (Type)
//            {
//                case EnumDatastoreType.FileGDB:
//                    if (ext != ".gdb"
//                          || System.IO.Directory.Exists(DataPath) == false)
//                    {
//                        return $@"The path: {DataPath} is not a valid File Geodatabase path";
//                    }
//                    break;
//                case EnumDatastoreType.EnterpriseGDB:
//                    if (ext != ".sde")
//                    {
//                        return $@"The selection: {DataPath} is not a valid .SDE connection file";
//                    }
//                    break;
//                case EnumDatastoreType.WebGDB:
//                    if (!DataPath.ToLower().StartsWith("http"))
//                    {
//                        return $@"The URL: {DataPath} is not a valid.  Please specify the ArcGIS Server URL for your feature service.";
//                    }
//                    break;
//                //case EnumDatastoreType.EnterpriseDB:
//                //	break;
//                case EnumDatastoreType.SqliteDB:
//                    if (!ext.Contains(".sqlite")
//                            || System.IO.Directory.Exists(DataPath) == false)
//                    {
//                        return $@"The path: {DataPath} is not a valid sqlite database path.  "".Sqlite"" in the folder path name is expected.";
//                    }
//                    break;
//                case EnumDatastoreType.ShapeFile:
//                    if (System.IO.Directory.Exists(DataPath) == false)
//                    {
//                        return $@"The path: {DataPath} is not a Folder that contains shape files.";
//                    }
//                    break;
//            }
//            return null;
//        }



//        #region Static Members

//        private static List<DatastoreCategory> LstDatastoreTypes = new List<DatastoreCategory>();

//        /// <summary>
//        /// List of all Datastore Types supported 
//        /// </summary>
//        public static List<DatastoreCategory> AllDatastoreCategories
//        {
//            get
//            {
//                if (LstDatastoreTypes.Count == 0)
//                {
//                    LstDatastoreTypes.Add(new DatastoreCategory(EnumDatastoreType.FileGDB, "File Geodatabase", "GDB Path", ItemFilters.Geodatabases, "Select a File GeoDatabase"));
//                    LstDatastoreTypes.Add(new DatastoreCategory(EnumDatastoreType.EnterpriseGDB, "Enterprise Geodatabase", "Enterprise GDB", ItemFilters.Databases, "Select a database connection file"));
//                    LstDatastoreTypes.Add(new DatastoreCategory(EnumDatastoreType.WebGDB, "Feature Service", "", ItemFilters.Folders, "Select a folder"));
//                    //LstDatastoreTypes.Add(new DatastoreCategory(EnumDatastoreType.EnterpriseDB, "Enterprise Database", "Enterprise DB", ItemFilters.Folders, "Select a folder"));
//                    LstDatastoreTypes.Add(new DatastoreCategory(EnumDatastoreType.SqliteDB, "Sqlite Database", "SQL Lite DB", ItemFilters.Folders, "Select a folder"));
//                    LstDatastoreTypes.Add(new DatastoreCategory(EnumDatastoreType.ShapeFile, "Shape File", "Shape Folder", ItemFilters.Folders, "Select a folder"));
//                }
//                return LstDatastoreTypes;
//            }
//        }

//        /// <summary>
//        /// Called to update Observable collection of DataType Categories
//        /// </summary>
//        /// <param name="dataTypeCategories"></param>
//        public static void PopulateServiceGeodatabaseDatabaseShapeFileDataTypes(ObservableCollection<DatasetTypeCategory> dataTypeCategories)
//        {
//            dataTypeCategories.Clear();
//            dataTypeCategories.Add(new DatasetTypeCategory("Table", DatasetType.Table));
//            dataTypeCategories.Add(new DatasetTypeCategory("Feature Class", DatasetType.FeatureClass));
//        }

//        /// <summary>
//        /// Called to update Observable collection of DataType Categories
//        /// </summary>
//        /// <param name="dataTypeCategories"></param>
//        public static void PopulateFileEnterpriseDatasetTypes(ObservableCollection<DatasetTypeCategory> dataTypeCategories)
//        {
//            dataTypeCategories.Clear();
//            dataTypeCategories.Add(new DatasetTypeCategory("Table", DatasetType.Table));
//            dataTypeCategories.Add(new DatasetTypeCategory("Feature Class", DatasetType.FeatureClass));
//            dataTypeCategories.Add(new DatasetTypeCategory("Feature Dataset", DatasetType.FeatureDataset));
//            dataTypeCategories.Add(new DatasetTypeCategory("Relationship Class", DatasetType.RelationshipClass));
//            dataTypeCategories.Add(new DatasetTypeCategory("Attributed Relationship Class", DatasetType.AttributedRelationshipClass));
//        }

//        #endregion Static Members

//    }






//    public class ArcSDEFunctions
//    {
//        /// <summary>
//        /// Open an ArcSDE connection using the supplied connection file.
//        /// </summary>
//        /// <param name="connectionFile"></param>
//        /// <returns></returns>
//        //public static IWorkspace OpenArcSDEConnection(String connectionFile)
//        //{
//        //    // For example, connectionFile = @"C:\myData\Connection to Kona.sde".
//        //    Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.SdeWorkspaceFactory");
//        //    IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
//        //    return workspaceFactory.OpenFromFile(connectionFile, 0);
//        //}

//        private Datastore _datastore;
//        private readonly object _lockCollection = new object();
//        private DatasetInfo _selectedDatasetInfo;
//        private string _dataPath;
//        private DatasetTypeCategory _datasetTypeCategory;
//        private DatastoreCategory _datastoreCategory;
//        private Visibility _cmdDataPathVisible;
//        private string _cmdDataPathContent;
//        private ICommand _cmdDataPath;
//        private ICommand _cmdLoadData;


//        private static bool IsOnUiThread => FrameworkApplication.TestMode || System.Windows.Application.Current.Dispatcher.CheckAccess();


//        public ObservableCollection<DatasetInfo> Datasets { get; set; }

//        public ObservableCollection<string> DefinitionDetails { get; set; }

//        public DatasetInfo Dataset
//        {
//            get { return _selectedDatasetInfo; }
//            set
//            {
//                //SetProperty(ref _selectedDatasetInfo, value, () => Dataset);
//                //if (_selectedDatasetInfo == null) return;
//                _selectedDatasetInfo = value;

//#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//                SetupDefinitionDetailsAsync();
//#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//            }
//        }

//        public ObservableCollection<DatasetTypeCategory> DatasetTypeCategories { get; set; }

//        public DatasetTypeCategory DatasetTypeCategory
//        {
//            get
//            { return _datasetTypeCategory; }
//            set
//            {
//                //SetProperty(ref _datasetTypeCategory, value, () => DatasetTypeCategory);
//                //if (_datasetTypeCategory == null) return;
//                _datasetTypeCategory = value;

//#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//                SetupDefinitionAsync();
//#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//            }
//        }

//        private DatasetInfo CreateDataSetInfo(Definition definition)
//        {
//            return new DatasetInfo
//            {
//                Name = definition.GetName(),
//                DatasetDefinition = definition
//            };
//        }





//        private async Task SetupDefinitionAsync()
//        {
//            try
//            {
//                var lstDefinitions = await QueuedTask.Run<List<DatasetInfo>>(() =>
//                {
//                    List<DatasetInfo> definitions = new List<DatasetInfo>();
//                    if (_datastore is Geodatabase)
//                    {
//                        var geodatabase = _datastore as Geodatabase;
//                        switch (DatasetTypeCategory.DatasetType)
//                        {
//                            case DatasetType.Table:
//                                definitions = geodatabase.GetDefinitions<TableDefinition>().Select(CreateDataSetInfo).ToList();
//                                break;
//                            case DatasetType.FeatureClass:
//                                definitions = geodatabase.GetDefinitions<FeatureClassDefinition>().Select(CreateDataSetInfo).ToList();
//                                break;
//                            case DatasetType.FeatureDataset:
//                                definitions = geodatabase.GetDefinitions<FeatureDatasetDefinition>().Select(CreateDataSetInfo).ToList();
//                                break;
//                            case DatasetType.RelationshipClass:
//                                definitions = geodatabase.GetDefinitions<RelationshipClassDefinition>().Select(CreateDataSetInfo).ToList();
//                                break;
//                            case DatasetType.AttributedRelationshipClass:
//                                definitions = geodatabase.GetDefinitions<AttributedRelationshipClassDefinition>().Select(CreateDataSetInfo).ToList();
//                                break;
//                        }
//                    }
//                    else if (_datastore is Database)
//                    {
//                        var database = _datastore as Database;
//                        IReadOnlyList<string> tableNames = database.GetTableNames();
//                        foreach (string tableName in tableNames)
//                        {
//                            QueryDescription queryDescription = database.GetQueryDescription(tableName);
//                            TableDefinition tableDefinition = database.GetDefinition(queryDescription);
//                            if (DatasetTypeCategory.DatasetType == DatasetType.Table || DatasetTypeCategory.DatasetType == DatasetType.FeatureClass)
//                            {
//                                definitions.Add(new DatasetInfo
//                                {
//                                    Name = tableDefinition.GetName(),
//                                    DatasetDefinition = tableDefinition
//                                });
//                            }
//                        }
//                    }
//                    else if (_datastore is FileSystemDatastore)
//                    {
//                        var shapefile = _datastore as FileSystemDatastore;
//                        FileSystemConnectionPath shapefileConnectionPath = (FileSystemConnectionPath)shapefile.GetConnector();
//                        DirectoryInfo directoryInfo = new DirectoryInfo(shapefileConnectionPath.Path.LocalPath);

//                        if (DatasetTypeCategory.DatasetType == DatasetType.FeatureClass)
//                        {
//                            FileInfo[] filesWithShpExtension = directoryInfo.GetFiles("*.shp");

//                            foreach (FileInfo file in filesWithShpExtension)
//                            {
//                                definitions.Add(CreateDataSetInfo(shapefile.GetDefinition<FeatureClassDefinition>(file.Name)));
//                            }
//                        }
//                        if (DatasetTypeCategory.DatasetType == DatasetType.Table)
//                        {
//                            FileInfo[] filesWithDbfExtension = directoryInfo.GetFiles("*.dbf");

//                            foreach (FileInfo file in filesWithDbfExtension)
//                            {
//                                definitions.Add(CreateDataSetInfo(shapefile.GetDefinition<TableDefinition>(file.Name)));
//                            }
//                        }
//                    }
//                    return definitions;
//                });
//                Datasets.Clear();
//                Datasets.AddRange(lstDefinitions);
//                DefinitionDetails.Clear();
//            }
//            catch (Exception exObj)
//            {
//                MessageBox.Show(exObj.Message, "Error");
//            }
//        }

//        private async Task SetupDefinitionDetailsAsync()
//        {
//            DefinitionDetails.Clear();
//            try
//            {
//                var lstDefs = await QueuedTask.Run<List<string>>(() =>
//                {
//                    Definition datasetDefinition = Dataset.DatasetDefinition;
//                    List<string> lstDefDetails = new List<string>();
//                    if (datasetDefinition is TableDefinition)
//                    {
//                        TableDefinition tableDefinition = datasetDefinition as TableDefinition;
//                        lstDefDetails.Add($"Object ID Field: {tableDefinition.GetObjectIDField()}");
//                        StringBuilder stringBuilder = new StringBuilder();

//                        if (!(_datastore is FileSystemDatastore))
//                        {
//                            lstDefDetails.Add($"Alias Name: {tableDefinition.GetAliasName()}");
//                            lstDefDetails.Add($"CreatedAt Field: {tableDefinition.GetCreatedAtField()}");
//                            lstDefDetails.Add($"Creator Field: {tableDefinition.GetCreatorField()}");
//                            lstDefDetails.Add($"Subtype Field: {tableDefinition.GetSubtypeField()}");
//                            lstDefDetails.Add($"Default Subtype Code: {tableDefinition.GetDefaultSubtypeCode()}");
//                            lstDefDetails.Add($"EditedAt Field: {tableDefinition.GetEditedAtField()}");
//                            lstDefDetails.Add($"Editor Field: {tableDefinition.GetEditorField()}");
//                            lstDefDetails.Add($"Global ID Field: {tableDefinition.GetGlobalIDField()}");
//                            lstDefDetails.Add($"Model Name: {tableDefinition.GetModelName()}");
//                            foreach (var subtype in tableDefinition.GetSubtypes())
//                            {
//                                stringBuilder.Append(subtype.GetCode()).Append(": ").Append(subtype.GetName()).Append(Environment.NewLine);
//                            }
//                            lstDefDetails.Add($"Subtypes: {stringBuilder}");
//                        }
//                        stringBuilder = new StringBuilder();
//                        foreach (Index index in tableDefinition.GetIndexes())
//                        {
//                            stringBuilder.Append(index.GetName()).Append(",");
//                            string order = index.IsAscending() ? "Ascending" : "Descending";
//                            stringBuilder.Append(order).Append(", ");
//                            string unique = index.IsUnique() ? "Unique" : "Not Unique";
//                            stringBuilder.Append(unique);
//                        }
//                        lstDefDetails.Add($"Indexes: {stringBuilder}");
//                    }

//                    if (datasetDefinition is FeatureClassDefinition)
//                    {
//                        FeatureClassDefinition featureClassDefinition = datasetDefinition as FeatureClassDefinition;
//                        if (!(_datastore is FileSystemDatastore))
//                        {
//                            lstDefDetails.Add($"Area Field: {featureClassDefinition.GetAreaField()}");
//                            lstDefDetails.Add($"Length Field: {featureClassDefinition.GetLengthField()}");
//                        }
//                        lstDefDetails.Add($"Shape Field: {featureClassDefinition.GetShapeField()}");
//                        lstDefDetails.Add($"Shape Type: {featureClassDefinition.GetShapeType()}");
//                        lstDefDetails.Add($"Spatial Reference Name: {featureClassDefinition.GetSpatialReference().Name}");
//                        Envelope extent = featureClassDefinition.GetExtent();
//                        lstDefDetails.Add($"Extent Details: XMin-{extent.XMin} XMax-{extent.XMax} YMin-{extent.YMin} YMax-{extent.YMax}");
//                    }

//                    if (datasetDefinition is FeatureDatasetDefinition)
//                    {
//                        FeatureDatasetDefinition featureDatasetDefinition = datasetDefinition as FeatureDatasetDefinition;
//                        lstDefDetails.Add($"Spatial Reference Name: {featureDatasetDefinition.GetSpatialReference().Name}");
//                        try
//                        {
//                            Envelope extent = featureDatasetDefinition.GetExtent();
//                            lstDefDetails.Add($"Extent Details: XMin-{extent.XMin} XMax-{extent.XMax} YMin-{extent.YMin} YMax-{extent.YMax}");
//                        }
//                        catch (Exception)
//                        {
//                            lstDefDetails.Add("Could not get extent");
//                        }
//                    }

//                    if (datasetDefinition is RelationshipClassDefinition)
//                    {
//                        RelationshipClassDefinition relationshipClassDefinition = datasetDefinition as RelationshipClassDefinition;
//                        lstDefDetails.Add($"Alias Name: {relationshipClassDefinition.GetAliasName()}");
//                        lstDefDetails.Add($"Cardinality: {relationshipClassDefinition.GetCardinality()}");
//                        lstDefDetails.Add($"Origin Class: {relationshipClassDefinition.GetOriginClass()}");
//                        lstDefDetails.Add($"Destination Class: {relationshipClassDefinition.GetDestinationClass()}");
//                        lstDefDetails.Add($"Origin Primary Key: {relationshipClassDefinition.GetOriginKeyField()}");
//                        lstDefDetails.Add($"Origin Foreign Key: {relationshipClassDefinition.GetOriginForeignKeyField()}");
//                        lstDefDetails.Add($"Is Attachement?: {relationshipClassDefinition.IsAttachmentRelationship()}");
//                        lstDefDetails.Add($"Is Composite Relationship?: {relationshipClassDefinition.IsComposite()}");
//                    }

//                    if (datasetDefinition is AttributedRelationshipClassDefinition)
//                    {
//                        AttributedRelationshipClassDefinition relationshipClassDefinition = datasetDefinition as AttributedRelationshipClassDefinition;
//                        lstDefDetails.Add($"Destination Key: {relationshipClassDefinition.GetDestinationKeyField()}");
//                        lstDefDetails.Add($"Destination Foreign Key: {relationshipClassDefinition.GetDestinationForeignKeyField()}");
//                        lstDefDetails.Add($"Object ID Field: {relationshipClassDefinition.GetObjectIDField()}");
//                    }
//                    return lstDefDetails;
//                });
//                DefinitionDetails.AddRange(lstDefs);
//            }
//            catch (Exception exObj)
//            {
//                MessageBox.Show(exObj.Message, "Error");
//            }
//        }


//        public async Task OpenEnterpriseGeodatabaseUsingSDEFilePath(string connectionFile)
//        {
//            await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
//            {
//                using (Geodatabase geodatabase = new Geodatabase(new DatabaseConnectionFile(new Uri(connectionFile))))
//                {
//                    // Use the geodatabase.
//                }
//            });
//        }
        
//        public static async Task ObtainingDefinitionsFromGeodatabase(string connectionFile)
//        {


//            Datasets = new ObservableCollection<DatasetInfo>();
//            DatasetTypeCategories = new ObservableCollection<DatasetTypeCategory>();
//            DefinitionDetails = new ObservableCollection<string>();
//            RunOnUiThread(() =>
//            {
//                BindingOperations.EnableCollectionSynchronization(Datasets, _lockCollection);
//                BindingOperations.EnableCollectionSynchronization(DatasetTypeCategories, _lockCollection);
//                BindingOperations.EnableCollectionSynchronization(DefinitionDetails, _lockCollection);
//            });



//            //List<TableDefinition> definitions = new List<TableDefinition>();

//            //try
//            //{
//            //    var lstDefinitions = await QueuedTask.Run<List<Definition>>(() =>
//            //    {
//            //            Geodatabase geodatabase = new Geodatabase(new DatabaseConnectionFile(new Uri(connectionFile)));

//            //            definitions = geodatabase.GetDefinitions<TableDefinition>().Select(CreateDataSetInfo).ToList();

//            //            //definitions.Add(new Definition { })
//            //    return definitions;
//            //});
//            //}
//            //catch (Exception ex)
//            //{
//            //    MessageBox.Show(ex.Message);
//            //}




//            //try
//            //{
//            //await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
//            //{
//            //    using (Geodatabase geodatabase = new Geodatabase(new DatabaseConnectionFile(new Uri(connectionFile))))
//            //    {
//            //        IReadOnlyList<FeatureClassDefinition> enterpriseDefinitions = geodatabase.GetDefinitions<FeatureClassDefinition>();
//            //        IEnumerable<Definition> featureClassesHavingGlobalID = enterpriseDefinitions.Where(definition => definition.HasGlobalID());

//            //        IReadOnlyList<FeatureDatasetDefinition> featureDatasetDefinitions = geodatabase.GetDefinitions<FeatureDatasetDefinition>();
//            //        bool electionRelatedFeatureDatasets = featureDatasetDefinitions.Any(definition => definition.GetName().Contains("GiGL"));

//            //        //IReadOnlyList<AttributedRelationshipClassDefinition> attributedRelationshipClassDefinitions = geodatabase.GetDefinitions<AttributedRelationshipClassDefinition>();

//            //        //IReadOnlyList<RelationshipClassDefinition> relationshipClassDefinitions = geodatabase.GetDefinitions<RelationshipClassDefinition>();
//            //    }
//            //});
//            //}
//            //catch (Exception ex)
//            //{
//            //    MessageBox.Show(ex.Message);
//            //}
//        }



//        private TableDefinition CreateDataSetInfo(TableDefinition definition)
//        {
//            return definition;
//        }



//        /// <summary>
//        /// Get a list of the table names from the specified workspace.
//        /// </summary>
//        /// <param name="aWorkspace"></param>
//        /// <param name="IncludeWildcard"></param>
//        /// <param name="ExcludeWildcard"></param>
//        /// <param name="IncludeFullName"></param>
//        /// <returns></returns>
//        //public static List<string> GetTableNames(IWorkspace aWorkspace, string IncludeWildcard, string ExcludeWildcard, bool IncludeFullName = false)
//        //{
//        //    // Define the wildcards as case insensitive
//        //    Wildcard theInclude = new Wildcard(IncludeWildcard, RegexOptions.IgnoreCase);
//        //    Wildcard theExclude = new Wildcard(ExcludeWildcard, RegexOptions.IgnoreCase);

//        //    List<string> theStringList = new List<string>();
//        //    IEnumDatasetName enumDatasetName = aWorkspace.get_DatasetNames(esriDatasetType.esriDTAny);
//        //    IDatasetName datasetName = enumDatasetName.Next();
//        //    while (datasetName != null)
//        //    {
//        //        string strName = datasetName.Name;
//        //        // Does the name conform to the IncludeWildcard?
//        //        if (theInclude.IsMatch(strName))
//        //        {
//        //            if (!theExclude.IsMatch(strName))
//        //            {
//        //                if (IncludeFullName)
//        //                {
//        //                    theStringList.Add(strName);
//        //                }
//        //                else
//        //                {
//        //                    strName = strName.Split('.')[2];
//        //                    theStringList.Add(strName);
//        //                }
//        //            }
//        //        }
//        //        datasetName = enumDatasetName.Next();
//        //    }
//        //    // Tidy up.
//        //    enumDatasetName = null;
//        //    datasetName = null;
//        //    return theStringList;
//        //}

//    }

//    public class SQLServerFunctions
//    {

//        #region SQLConnection

//        /// <summary>
//        /// Create a SQL connection using the connection string.
//        /// </summary>
//        /// <param name="connectionString"></param>
//        /// <returns></returns>
//        public SqlConnection CreateSQLConnection(string connectionString)
//        {
//            SqlConnection con = new SqlConnection(connectionString);
//            return con;
//        }

//        #endregion

//        #region SQLCommand

//        /// <summary>
//        /// Create a SQL command using the connection provided.
//        /// </summary>
//        /// <param name="aConnection"></param>
//        /// <param name="aName"></param>
//        /// <param name="aCommandType"></param>
//        /// <param name="TimeOutSeconds"></param>
//        /// <returns></returns>
//        public SqlCommand CreateSQLCommand(ref SqlConnection aConnection, string aName, System.Data.CommandType aCommandType, int TimeOutSeconds = 4000)
//        {
//            SqlCommand myCmd = new SqlCommand(aName, aConnection);
//            myCmd.CommandType = aCommandType;
//            myCmd.CommandTimeout = TimeOutSeconds;
//            return myCmd;
//        }

//        #endregion

//        #region AddSQLParameter

//        /// <summary>
//        /// Add a string SQL parameter.
//        /// </summary>
//        /// <param name="aCommand"></param>
//        /// <param name="aName"></param>
//        /// <param name="aValue"></param>
//        /// <returns></returns>
//        public bool AddSQLParameter(ref SqlCommand aCommand, string aName, string aValue)
//        {
//            // Note we are passing the value as a string as this will eventually become an overloaded method which will accept
//            // different types of data. For the moment we only need string.
//            SqlParameter myParameter = aCommand.Parameters.Add(aName, System.Data.SqlDbType.VarChar);
//            myParameter.Value = aValue;
//            return true;
//        }

//        /// <summary>
//        /// Add a integer SQL parameter.
//        /// </summary>
//        /// <param name="aCommand"></param>
//        /// <param name="aName"></param>
//        /// <param name="aValue"></param>
//        /// <returns></returns>
//        public bool AddSQLParameter(ref SqlCommand aCommand, string aName, int aValue)
//        {
//            SqlParameter myParameter = aCommand.Parameters.Add(aName, System.Data.SqlDbType.BigInt);
//            myParameter.Value = aValue;
//            return true;
//        }

//        /// <summary>
//        /// Add a bool SQL parameter.
//        /// </summary>
//        /// <param name="aCommand"></param>
//        /// <param name="aName"></param>
//        /// <param name="aValue"></param>
//        /// <returns></returns>
//        public bool AddSQLParameter(ref SqlCommand aCommand, string aName, bool aValue)
//        {
//            SqlParameter myParameter = aCommand.Parameters.Add(aName, System.Data.SqlDbType.Bit);
//            myParameter.Value = 0;
//            if (aValue) myParameter.Value = 1;
//            return true;
//        }

//        #endregion

//        #region Rows

//        public int CountRows(ref SqlConnection aConnection, string aTableName)
//        {
//            string strQuery = "SELECT COUNT(*) FROM " + aTableName;
//            SqlCommand objCommand = new SqlCommand(strQuery, aConnection);
//            int aCount = (int)objCommand.ExecuteScalar();

//            return aCount;
//        }

//        #endregion

//        #region Fields

//        public bool FieldExists(ref SqlConnection aConnection, string aTableName, string aColumnName)
//        {
//            string strQuery = "SELECT TOP 1 * FROM " + aTableName;
//            SqlCommand objCommand = new SqlCommand(strQuery, aConnection);
//            bool blColExists = false;
//            SqlDataReader objReader = objCommand.ExecuteReader();
//            for (int col = 0; col < objReader.FieldCount; col++)
//            {
//                if (objReader.GetName(col).ToString() == aColumnName) blColExists = true;
//            }
//            objCommand.Dispose();
//            objReader.Dispose();
//            return blColExists;
//        }

//        public string[] GetFieldNames(ref SqlConnection aConnection, string aTableName)
//        {
//            List<string> strFieldNames = new List<string>();

//            string strQuery = "SELECT TOP 1 * FROM " + aTableName;
//            SqlCommand objCommand = new SqlCommand(strQuery, aConnection);

//            SqlDataReader objReader = objCommand.ExecuteReader();
//            for (int col = 0; col < objReader.FieldCount; col++)
//            {
//                strFieldNames.Add(objReader.GetName(col).ToString());
//            }
//            objCommand.Dispose();
//            objReader.Dispose();
//            return strFieldNames.ToArray();
//        }

//        #endregion

//        #region Tables

//        public bool TableExists(ref SqlConnection aConnection, string aTableName)
//        {
//            bool blTableExists = false;
//            List<string> tables = new List<string>();
//            DataTable dt = aConnection.GetSchema("Tables");
//            foreach (DataRow row in dt.Rows)
//            {
//                string tablename = (string)row[2];
//                if (tablename == aTableName)
//                    blTableExists = true;
//            }
//            dt.Dispose();
//            return blTableExists;
//        }

//        public bool TableHasRows(ref SqlConnection aConnection, string aTableName)
//        {
//            bool blHasRows = false;
//            System.Data.SqlClient.SqlCommand CheckNone = new System.Data.SqlClient.SqlCommand(
//                "IF EXISTS(SELECT * from " + aTableName + ") SELECT 1 ELSE SELECT 0", aConnection);
//            int result = (int)CheckNone.ExecuteScalar();
//            if (result == 1)
//                blHasRows = true;

//            CheckNone.Dispose();
//            return blHasRows;
//        }

//        #endregion

//    }
//}

// DataSelector is an ArcGIS add-in used to extract biodiversity
// information from SQL Server based on any selection criteria.
//
// Copyright © 2016-2017 SxBRC, 2017-2018 TVERC
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

using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
//using ArcGIS.Core.CIM;
using System;
using System.IO;
using System.Threading.Tasks;
using ArcGIS.Core.Data.Exceptions;
using System.Linq;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Contracts;
//using ArcGIS.Core.Internal.CIM;

namespace DataTools
{
    class MapFunctions
    {

        #region Fields

        Map _map;
        MapView _mapView;

        #endregion

        #region Constructor

        // Class constructor.
        public MapFunctions()
        {
            // Get the active map view (if there is one).
            _mapView = GetActiveMapView();

            // Set the map currently displayed in the active map view.
            if (_mapView != null)
                _map = _mapView.Map;
            else
                _map = null;
        }

        #endregion

        #region Map

        internal MapView GetActiveMapView()
        {
            // Get the active map view.
            MapView mapView = MapView.Active;
            if (mapView == null)
                return null;

            return mapView;
        }

        public async Task<string> CreateMapAsync(String mapName)
        {
            _map = null;
            _mapView = null;

            await QueuedTask.Run(() =>
            {
                try
                {
                    // Create a new map without a basemap.
                    _map = MapFactory.Instance.CreateMap(mapName, basemap:Basemap.None);

                    // Create and activate new map.
                    ProApp.Panes.CreateMapPaneAsync(_map, MapViewingMode.Map);
                    //var paneTask = ProApp.Panes.CreateMapPaneAsync(_map, MapViewingMode.Map);
                    //paneTask.Wait();

                    // Get the active map view;
                    //_mapView = GetActiveMapView();

                    //Pane pane = ProApp.Panes.ActivePane;
                    //pane.Activate();
                }
                catch
                {
                    // CreateMap throws an exception if the map view wasn't created.
                    // CreateMapPaneAsync throws an excrption if the map isn't created.
                }
            });

            // Get the active map view;
            _mapView = GetActiveMapView();

            return _map.Name;
        }

        public string MapName
        {
            get
            {
                if (_map == null)
                    return null;
                else
                    return _map.Name;
            }
        }

        public async Task AddLayerToMap(string url)
        {
            try
            {
                await QueuedTask.Run(() =>
                {
                    Uri uri = new(url);

                    //var layerParams = new FeatureLayerCreationParams(fc);
                    var layer = LayerFactory.Instance.CreateLayer(uri, _map);
                });

                // Redraw the active map.
                //_mapView.Redraw(false);
            }
            catch (Exception ex)
            {
            }
        }

        #endregion

        #region Layers

        internal Layer FindLayer(String layerName)
        {
            //Finds layers by name and returns a read only list of Layers
            IReadOnlyList<Layer> layers = _map.FindLayers(layerName, true);

            while (layers.Count > 0)
            {
                Layer layer = layers.First();

                if (layer.Map.Name == _map.Name)
                    return layer;
            }

            return null;
        }

        #endregion

        #region Symbology

        public async Task<bool> ApplySymbologyFromLayerFileAsync(string layerName, string layerFile)
        {
            // Check the layer file exists.
            if (!FileFunctions.FileExists(layerFile))
                return false;

            // Find the layer in the active map.
            Layer layer = FindLayer(layerName);

            if (layer is FeatureLayer featureLayer)
            {

                // Apply the layer file symbology to the feature layer.
                try
                {
                    await QueuedTask.Run(() =>
                    {
                        // Get the Layer Document from the lyrx file.
                        var lyrDocFromLyrxFile = new LayerDocument(layerFile);
                        var cimLyrDoc = lyrDocFromLyrxFile.GetCIMLayerDocument();

                        // Get the renderer from the layer file.
                        var rendererFromLayerFile = ((CIMFeatureLayer)cimLyrDoc.LayerDefinitions[0]).Renderer as CIMUniqueValueRenderer;

                        // Apply the renderer to the feature layer.
                        featureLayer?.SetRenderer(rendererFromLayerFile);
                    });
                }
                catch (GeodatabaseNotFoundOrOpenedException exception)
                {
                    // Handle Exception.
                    return false;
                }

            }

            return true;
        }

        #endregion

        //    public IMxDocument GetIMXDocument()
        //    {
        //        ESRI.ArcGIS.ArcMapUI.IMxDocument mxDocument = ((ESRI.ArcGIS.ArcMapUI.IMxDocument)(thisApplication.Document));
        //        return mxDocument;
        //    }

        //    public ESRI.ArcGIS.Carto.IMap GetMap()
        //    {
        //        if (thisApplication == null)
        //        {
        //            return null;
        //        }
        //        ESRI.ArcGIS.ArcMapUI.IMxDocument mxDocument = ((ESRI.ArcGIS.ArcMapUI.IMxDocument)(thisApplication.Document)); // Explicit Cast
        //        ESRI.ArcGIS.Carto.IActiveView activeView = mxDocument.ActiveView;
        //        ESRI.ArcGIS.Carto.IMap map = activeView.FocusMap;

        //        return map;
        //    }

        //    public void RefreshTOC()
        //    {
        //        IMxDocument theDoc = GetIMXDocument();
        //        theDoc.CurrentContentsView.Refresh(null);
        //    }
    }

    class ArcGISFunctions
    {
        #region Constructor

        // Class constructor.
        public ArcGISFunctions()
        {
        }

        #endregion

        //    public IWorkspaceFactory GetWorkspaceFactory(string filePath, bool aTextFile = false, bool messages = false)
        //    {
        //        // This function decides what type of feature workspace factory would be best for this file.
        //        // it is up to the user to decide whether the file path and file names exist (or should exist).

        //        // Reworked 18/05/2016 to deal with the singleton issue.

        //        IWorkspaceFactory pWSF;
        //        // What type of output file it it? This defines what kind of workspace factory will be returned.
        //        if (filePath.Substring(filePath.Length - 4, 4) == ".gdb")
        //        {
        //            // It is a file geodatabase file.
        //            Type t = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
        //            System.Object obj = Activator.CreateInstance(t);
        //            pWSF = obj as IWorkspaceFactory;
        //        }
        //        else if (filePath.Substring(filePath.Length - 4, 4) == ".mdb")
        //        {
        //            // Personal geodatabase.
        //            Type t = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
        //            System.Object obj = Activator.CreateInstance(t);
        //            pWSF = obj as IWorkspaceFactory;
        //        }
        //        else if (filePath.Substring(filePath.Length - 4, 4) == ".sde")
        //        {
        //            // ArcSDE connection
        //            Type t = Type.GetTypeFromProgID("esriDataSourcesGDB.SdeWorkspaceFactory");
        //            System.Object obj = Activator.CreateInstance(t);
        //            pWSF = obj as IWorkspaceFactory;
        //        }
        //        else if (aTextFile == true)
        //        {
        //            // Text file
        //            //Type t = Type.GetTypeFromProgID("esriDataSourcesOleDB.TextFileWorkspaceFactory");
        //            //System.Object obj = Activator.CreateInstance(t);
        //            pWSF = new TextFileWorkspaceFactory();
        //        }
        //        else // Shapefile
        //        {
        //            Type t = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
        //            System.Object obj = Activator.CreateInstance(t);
        //            pWSF = obj as IWorkspaceFactory;
        //        }
        //        return pWSF;
        //    }


        #region Feature Class

        /// <summary>
        /// Check if the feature class exists in the file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<bool> FeatureClassExistsAsync(string filePath, string fileName)
        {
            if (fileName.Substring(fileName.Length - 4, 1) == ".")
            {
                // It's a file.
                if (FileFunctions.FileExists(filePath + @"\" + fileName))
                    return true;
                else
                    return false;
            }
            else if (filePath.Substring(filePath.Length - 3, 3) == "sde")
            {
                // It's an SDE class
                // Not handled. We know the table exists.
                return true;
            }
            else // it is a geodatabase class.
            {
                try
                {
                    bool exists = await FeatureClassExistsGDBAsync(filePath, fileName);

                    return exists;
                }
                catch
                {
                    // GetDefinition throws an exception if the definition doesn't exist
                    return false;
                }
            }
        }

        /// <summary>
        /// Check if the feature class exists.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static async Task<bool> FeatureClassExistsAsync(string fullPath)
        {
            return await FeatureClassExistsAsync(FileFunctions.GetDirectoryName(fullPath), FileFunctions.GetFileName(fullPath));
        }

        /// <summary>
        /// Check if the feature class exists in the file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<bool> FeatureClassExistsAsyncOLD(string filePath, string fileName)
        {
            if (fileName.Substring(fileName.Length - 4, 1) == ".")
            {
                // It's a file.
                if (FileFunctions.FileExists(filePath + @"\" + fileName))
                    return true;
                else
                    return false;
            }
            else if (filePath.Substring(filePath.Length - 3, 3) == "sde")
            {
                // It's an SDE class
                // Not handled. We know the table exists.
                return true;
            }
            else // it is a geodatabase class.
            {
                try
                {
                    bool exists = await FeatureClassExistsGDBAsync(filePath, fileName);

                    return exists;
                }
                catch
                {
                    // GetDefinition throws an exception if the definition doesn't exist
                    return false;
                }
            }
        }

        #endregion

        #region Geodatabase

        public static async Task<bool> FeatureClassExistsGDBAsync(string filePath, string fileName)
        {
            bool exists = false;

            try
            {
                await QueuedTask.Run(() =>
                {
                    // Open the file geodatabase. This will open the geodatabase if the folder exists and contains a valid geodatabase.
                    using Geodatabase geodatabase = new(new FileGeodatabaseConnectionPath(new Uri(filePath)));

                    using FeatureClassDefinition featureClassDefinition = geodatabase.GetDefinition<FeatureClassDefinition>(fileName);

                    exists = true;
                    //if (featureClassDefinition != null)
                    //    exists = true;

                    //featureClassDefinition.Dispose();  // FeatureClass found, dispose of object
                    //geodatabase.Dispose();
                });
            }
            catch (GeodatabaseNotFoundOrOpenedException exception)
            {
                // Handle Exception.
                return false;
            }

            return exists;
        }

        #endregion

        //    #region GetFeatureClass

        //    public IFeatureClass GetFeatureClass(string filePath, string fileName, bool messages = false)
        //    // This is incredibly quick.
        //    {
        //        // Check input first.
        //        string aTestPath = filePath;
        //        if (filePath.Contains(".sde"))
        //        {
        //            aTestPath = FileFunctions.GetDirectoryName(filePath);
        //        }
        //        if (FileFunctions.DirExists(aTestPath) == false || fileName == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("Please provide valid input", "Get Featureclass");
        //            return null;
        //        }


        //        IWorkspaceFactory pWSF = GetWorkspaceFactory(filePath);
        //        IFeatureWorkspace pWS = (IFeatureWorkspace)pWSF.OpenFromFile(filePath, 0);
        //        if (FeatureClassExists(filePath, fileName))
        //        {
        //            IFeatureClass pFC = pWS.OpenFeatureClass(fileName);
        //            return pFC;
        //        }
        //        else
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("The file " + fileName + " doesn't exist in this location", "Open Feature Class from Disk");
        //            return null;
        //        }

        //    }


        //    public IFeatureClass GetFeatureClass(string aFullPath, bool messages = false)
        //    {
        //        string filePath = FileFunctions.GetDirectoryName(aFullPath);
        //        string fileName = FileFunctions.GetFileName(aFullPath);
        //        IFeatureClass pFC = GetFeatureClass(filePath, fileName, messages);
        //        return pFC;
        //    }

        //    #endregion

        //    public IFeatureLayer GetFeatureLayerFromString(string aFeatureClassName, bool messages = false)
        //    {
        //        // as far as I can see this does not work for geodatabase files.
        //        // firstly get the Feature Class
        //        // Does it exist?
        //        if (!FileFunctions.FileExists(aFeatureClassName))
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("The featureclass " + aFeatureClassName + " does not exist");
        //            }
        //            return null;
        //        }
        //        string filePath = FileFunctions.GetDirectoryName(aFeatureClassName);
        //        string aFCName = FileFunctions.GetFileName(aFeatureClassName);

        //        IFeatureClass myFC = GetFeatureClass(filePath, aFCName);
        //        if (myFC == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Cannot open featureclass " + aFeatureClassName);
        //            }
        //            return null;
        //        }

        //        // Now get the Feature Layer from this.
        //        FeatureLayer pFL = new FeatureLayer();
        //        pFL.FeatureClass = myFC;
        //        pFL.Name = myFC.AliasName;
        //        return pFL;
        //    }

        //    public ILayer GetLayer(string aName, bool messages = false)
        //    {
        //        // Gets existing layer in map.
        //        // Check there is input.
        //       if (aName == null)
        //       {
        //           if (messages)
        //           {
        //                System.Windows.Forms.MessageBox.Show("Please pass a valid layer name", "Find Layer By Name");
        //           }
        //           return null;
        //        }

        //        // Get map, and layer names.
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("No map found", "Find Layer By Name");
        //            }
        //            return null;
        //        }
        //        IEnumLayer pLayers = pMap.Layers;
        //        Boolean blFoundit = false;
        //        ILayer pTargetLayer = null;

        //        ILayer pLayer = pLayers.Next();

        //        // Look through the layers and carry on until found,
        //        // or we have reached the end of the list.
        //        while ((pLayer != null) && !blFoundit)
        //        {
        //            if (!(pLayer is ICompositeLayer))
        //            {
        //                if (pLayer.Name == aName)
        //                {
        //                    pTargetLayer = pLayer;
        //                    blFoundit = true;
        //                }
        //            }
        //            pLayer = pLayers.Next();
        //        }

        //        if (pTargetLayer == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("The layer " + aName + " doesn't exist", "Find Layer");
        //            return null;
        //        }
        //        return pTargetLayer;
        //    }

        //    public bool FieldExists(string filePath, string fileName, string aFieldName, bool messages = false)
        //    {
        //        // This function returns true if a field (or a field alias) exists, false if it doesn (or the dataset doesn't)
        //        IFeatureClass myFC = GetFeatureClass(filePath, fileName);
        //        ITable myTab;
        //        if (myFC == null)
        //        {
        //            myTab = GetTable(filePath, fileName);
        //            if (myTab == null) return false; // Dataset doesn't exist.
        //        }
        //        else
        //        {
        //            myTab = (ITable)myFC;
        //        }

        //        int aTest;
        //        IFields theFields = myTab.Fields;
        //        aTest = theFields.FindField(aFieldName);
        //        if (aTest == -1)
        //        {
        //            aTest = theFields.FindFieldByAliasName(aFieldName);
        //        }

        //        if (aTest == -1) return false;
        //        return true;
        //    }

        //    public bool AddLayerFromFClass(IFeatureClass theFeatureClass, bool messages = false)
        //    {
        //        // Check we have input
        //        if (theFeatureClass == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Please pass a feature class", "Add Layer From Feature Class");
        //            }
        //            return false;
        //        }
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("No map found", "Add Layer From Feature Class");
        //            }
        //            return false;
        //        }
        //        FeatureLayer pFL = new FeatureLayer();
        //        pFL.FeatureClass = theFeatureClass;
        //        pFL.Name = theFeatureClass.AliasName;
        //        pMap.AddLayer(pFL);

        //        return true;
        //    }

        //    public bool AddFeatureLayerFromString(string aFeatureClassName, bool messages = false)
        //    {
        //        // firstly get the Feature Class
        //        // Does it exist?
        //        if (!FileFunctions.FileExists(aFeatureClassName))
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("The featureclass " + aFeatureClassName + " does not exist");
        //            }
        //            return false;
        //        }
        //        string filePath = FileFunctions.GetDirectoryName(aFeatureClassName);
        //        string aFCName = FileFunctions.GetFileName(aFeatureClassName);

        //        IFeatureClass myFC = GetFeatureClass(filePath, aFCName);
        //        if (myFC == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Cannot open featureclass " + aFeatureClassName);
        //            }
        //            return false;
        //        }

        //        // Now add it to the view.
        //        bool blResult = AddLayerFromFClass(myFC);
        //        if (blResult)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Cannot add featureclass " + aFeatureClassName);
        //            }
        //            return false;
        //        }
        //    }

        #region TableExists

        /// <summary>
        /// Check the table exists in the file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool TableExists(string filePath, string fileName)
        {

            if (fileName.Substring(fileName.Length - 4, 1) == ".")
            {
                // It's a file.
                if (FileFunctions.FileExists(filePath + @"\" + fileName))
                    return true;
                else
                    return false;
            }
            else if (filePath.Substring(filePath.Length - 3, 3) == "sde")
            {
                // It's an SDE class
                // Not handled. We know the table exists.
                return true;
            }
            else // it is a geodatabase class.
            {
                //IWorkspaceFactory pWSF = GetWorkspaceFactory(filePath);
                //IWorkspace2 pWS = (IWorkspace2)pWSF.OpenFromFile(filePath, 0);
                //if (pWS.get_NameExists(ESRI.ArcGIS.Geodatabase.esriDatasetType.esriDTTable, fileName))
                //    return true;
                //else
                //    return false;
                return false;
            }
        }

        /// <summary>
        /// Check if the table exists.
        /// </summary>
        /// <param name="aFullPath"></param>
        /// <returns></returns>
        public static bool TableExists(string aFullPath)
        {
            return TableExists(FileFunctions.GetDirectoryName(aFullPath), FileFunctions.GetFileName(aFullPath));
        }

        #endregion

        //public static void RefreshFolders()
        //{
        //    foreach (var item in Project.Current.Items.OfType<FolderConnectionProjectItem>())
        //    {
        //        string itemName = item.Name;
        //        ((ArcGIS.Desktop.Internal.Core.IItemInternal)item).RefreshChildren();
        //    }
        //}



        //    #region GetTable

        //    public ITable GetTable(string filePath, string fileName, bool messages = false)
        //    {
        //        // Check input first.
        //        string aTestPath = filePath;
        //        if (filePath.Contains(".sde"))
        //        {
        //            aTestPath = FileFunctions.GetDirectoryName(filePath);
        //        }
        //        if (FileFunctions.DirExists(aTestPath) == false || fileName == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("Please provide valid input", "Get Table");
        //            return null;
        //        }
        //        bool blText = false;
        //        string strExt = fileName.Substring(fileName.Length - 4, 4);
        //        if (strExt == ".txt" || strExt == ".csv" || strExt == ".tab")
        //        {
        //            blText = true;
        //        }

        //        IWorkspaceFactory pWSF = GetWorkspaceFactory(filePath, blText);

        //        if (pWSF == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Workspace factory is null");
        //                return null;
        //            }
        //        }

        //        IFeatureWorkspace pWS = (IFeatureWorkspace)pWSF.OpenFromFile(filePath, 0);

        //        if (pWS == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Workspace is null");
        //                return null;
        //            }
        //        }

        //        ITable pTable = null;
        //        try
        //        {
        //            pTable = pWS.OpenTable(fileName);
        //            if (pTable == null)
        //            {
        //                if (messages) System.Windows.Forms.MessageBox.Show("The file " + fileName + " doesn't exist in this location", "Open Table from Disk");
        //                return null;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            return null;
        //        }
        //        return pTable;
        //    }

        //    public ITable GetTable(string aTableLayer, bool messages = false)
        //    {
        //        IMap pMap = GetMap();
        //        IStandaloneTableCollection pColl = (IStandaloneTableCollection)pMap;
        //        IStandaloneTable pThisTable = null;

        //        for (int I = 0; I < pColl.StandaloneTableCount; I++)
        //        {
        //            pThisTable = pColl.StandaloneTable[I];
        //            if (pThisTable.Name == aTableLayer)
        //            {
        //                ITable myTable = pThisTable.Table;
        //                return myTable;
        //            }
        //        }
        //        if (messages)
        //        {
        //            System.Windows.Forms.MessageBox.Show("The table layer " + aTableLayer + " could not be found in this map");
        //        }
        //        return null;
        //    }
        //    #endregion

        //public static bool AddTableLayerFromString(string aTableName, string aLayerName, bool messages = false)
        //{
        //    // Check the table exists.
        //    if (!FileFunctions.FileExists(aTableName))
        //    {
        //        if (messages)
        //        {
        //            System.Windows.Forms.MessageBox.Show("The table " + aTableName + " does not exist");
        //        }
        //        return false;
        //    }
        //    string filePath = FileFunctions.GetDirectoryName(aTableName);
        //    string fileName = FileFunctions.GetFileName(aTableName);

        //    ITable myTable = GetTable(filePath, fileName);
        //    if (myTable == null)
        //    {
        //        if (messages)
        //        {
        //            System.Windows.Forms.MessageBox.Show("Cannot open table " + aTableName);
        //        }
        //        return false;
        //    }

        //    // Now add it to the view.
        //    bool blResult = AddLayerFromTable(myTable, aLayerName);
        //    if (blResult)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        if (messages)
        //        {
        //            System.Windows.Forms.MessageBox.Show("Cannot add table " + fileName);
        //        }
        //        return false;
        //    }
        //}

        //    public bool AddLayerFromTable(ITable theTable, string aName, bool messages = false)
        //    {
        //        // check we have nput
        //        if (theTable == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Please pass a table", "Add Layer From Table");
        //            }
        //            return false;
        //        }
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("No map found", "Add Layer From Table");
        //            }
        //            return false;
        //        }
        //        IStandaloneTableCollection pStandaloneTableCollection = (IStandaloneTableCollection)pMap;
        //        IStandaloneTable pTable = new StandaloneTable();
        //        IMxDocument mxDoc = GetIMXDocument();

        //        pTable.Table = theTable;
        //        pTable.Name = aName;

        //        // Remove if already exists
        //        if (TableLayerExists(aName))
        //            RemoveStandaloneTable(aName);

        //        mxDoc.UpdateContents();

        //        pStandaloneTableCollection.AddStandaloneTable(pTable);
        //        mxDoc.UpdateContents();
        //        return true;
        //    }

        //    public bool TableLayerExists(string aLayerName, bool messages = false)
        //    {
        //        // Check there is input.
        //        if (aLayerName == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("Please pass a valid layer name", "Find Layer By Name");
        //            return false;
        //        }

        //        // Get map, and layer names.
        //        IMxDocument mxDoc = GetIMXDocument();
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("No map found", "Find Layer By Name");
        //            return false;
        //        }

        //        IStandaloneTableCollection pColl = (IStandaloneTableCollection)pMap;
        //        IStandaloneTable pThisTable = null;
        //        for (int I = 0; I < pColl.StandaloneTableCount; I++)
        //        {
        //            pThisTable = pColl.StandaloneTable[I];
        //            if (pThisTable.Name == aLayerName)
        //            {
        //                return true;
        //                //pColl.RemoveStandaloneTable(pThisTable);
        //               // mxDoc.UpdateContents();
        //                //break; // important: get out now, the index is no longer valid
        //            }
        //        }
        //        return false;
        //    }

        //    public bool RemoveStandaloneTable(string aTableName, bool messages = false)
        //    {
        //        // Check there is input.
        //        if (aTableName == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("Please pass a valid layer name", "Find Layer By Name");
        //            return false;
        //        }

        //        // Get map, and layer names.
        //        IMxDocument mxDoc = GetIMXDocument();
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("No map found", "Find Layer By Name");
        //            return false;
        //        }

        //        IStandaloneTableCollection pColl = (IStandaloneTableCollection)pMap;
        //        IStandaloneTable pThisTable = null;
        //        for (int I = 0; I < pColl.StandaloneTableCount; I++)
        //        {
        //            pThisTable = pColl.StandaloneTable[I];
        //            if (pThisTable.Name == aTableName)
        //            {
        //                try
        //                {
        //                    pColl.RemoveStandaloneTable(pThisTable);
        //                    mxDoc.UpdateContents();
        //                    return true; // important: get out now, the index is no longer valid
        //                }
        //                catch (Exception ex)
        //                {
        //                    System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //                    return false;
        //                }
        //            }
        //        }
        //        return false;
        //    }


        //    public bool LayerExists(string aLayerName, bool messages = false)
        //    {
        //        // Check there is input.
        //        if (aLayerName == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("Please pass a valid layer name", "Find Layer By Name");
        //            return false;
        //        }

        //        // Get map, and layer names.
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("No map found", "Find Layer By Name");
        //            return false;
        //        }
        //        IEnumLayer pLayers = pMap.Layers;

        //        ILayer pLayer = pLayers.Next();

        //        // Look through the layers and carry on until found,
        //        // or we have reached the end of the list.
        //        while (pLayer != null)
        //        {
        //            if (!(pLayer is IGroupLayer))
        //            {
        //                if (pLayer.Name == aLayerName)
        //                {
        //                    return true;
        //                }

        //            }
        //            pLayer = pLayers.Next();
        //        }
        //        return false;
        //    }

        //    public bool GroupLayerExists(string aGroupLayerName, bool messages = false)
        //    {
        //        // Check there is input.
        //        if (aGroupLayerName == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("Please pass a valid layer name", "Find Layer By Name");
        //            return false;
        //        }

        //        // Get map, and layer names.
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("No map found", "Find Layer By Name");
        //            return false;
        //        }
        //        IEnumLayer pLayers = pMap.Layers;

        //        ILayer pLayer = pLayers.Next();

        //        // Look through the layers and carry on until found,
        //        // or we have reached the end of the list.
        //        while (pLayer != null)
        //        {
        //            if (pLayer is IGroupLayer)
        //            {
        //                if (pLayer.Name == aGroupLayerName)
        //                {
        //                    return true;
        //                }

        //            }
        //            pLayer = pLayers.Next();
        //        }
        //        return false;
        //    }

        //    public ILayer GetGroupLayer(string aGroupLayerName, bool messages = false)
        //    {
        //        // Check there is input.
        //        if (aGroupLayerName == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("Please pass a valid layer name", "Find Layer By Name");
        //            return null;
        //        }

        //        // Get map, and layer names.
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("No map found", "Find Layer By Name");
        //            return null;
        //        }
        //        IEnumLayer pLayers = pMap.Layers;

        //        ILayer pLayer = pLayers.Next();

        //        // Look through the layers and carry on until found,
        //        // or we have reached the end of the list.
        //        while (pLayer != null)
        //        {
        //            if (pLayer is IGroupLayer)
        //            {
        //                if (pLayer.Name == aGroupLayerName)
        //                {
        //                    return pLayer;
        //                }

        //            }
        //            pLayer = pLayers.Next();
        //        }
        //        return null;
        //    }

        //    public bool MoveToGroupLayer(string theGroupLayerName, ILayer aLayer,  bool messages = false)
        //    {
        //        bool blExists = false;
        //        IGroupLayer myGroupLayer = new GroupLayer();
        //        // Does the group layer exist?
        //        if (GroupLayerExists(theGroupLayerName))
        //        {
        //            myGroupLayer = (IGroupLayer)GetGroupLayer(theGroupLayerName);
        //            blExists = true;
        //        }
        //        else
        //        {
        //            myGroupLayer.Name = theGroupLayerName;
        //        }
        //        string theOldName = aLayer.Name;

        //        // Remove the original instance, then add it to the group.
        //        RemoveLayer(aLayer);
        //        myGroupLayer.Add(aLayer);

        //        if (!blExists)
        //        {
        //            // Add the layer to the map.
        //            IMap pMap = GetMap();
        //            pMap.AddLayer(myGroupLayer);
        //        }
        //        RefreshTOC();
        //        return true;
        //    }

        //    #region RemoveLayer

        //    public bool RemoveLayer(string aLayerName, bool messages = false)
        //    {
        //        // Check there is input.
        //        if (aLayerName == null)
        //        {
        //            System.Windows.Forms.MessageBox.Show("Please pass a valid layer name", "Find Layer By Name");
        //            return false;
        //        }

        //        // Get map, and layer names.
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("No map found", "Find Layer By Name");
        //            return false;
        //        }
        //        IEnumLayer pLayers = pMap.Layers;

        //        ILayer pLayer = pLayers.Next();

        //        // Look through the layers and carry on until found,
        //        // or we have reached the end of the list.
        //        while (pLayer != null)
        //        {
        //            if (!(pLayer is IGroupLayer))
        //            {
        //                if (pLayer.Name == aLayerName)
        //                {
        //                    pMap.DeleteLayer(pLayer);
        //                    return true;
        //                }

        //            }
        //            pLayer = pLayers.Next();
        //        }
        //        return false;
        //    }

        //    public bool RemoveLayer(ILayer aLayer, bool messages = false)
        //    {
        //        // Check there is input.
        //        if (aLayer == null)
        //        {
        //            System.Windows.Forms.MessageBox.Show("Please pass a valid layer ", "Remove Layer");
        //            return false;
        //        }

        //        // Get map, and layer names.
        //        IMap pMap = GetMap();
        //        if (pMap == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("No map found", "Remove Layer");
        //            return false;
        //        }
        //        pMap.DeleteLayer(aLayer);
        //        return true;
        //    }

        //    #endregion

        /// <summary>
        /// Prompt the user to specify an output file in the required format.
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="initialDirectory"></param>
        /// <returns></returns>
        public static string GetOutputFileName(string fileType, string initialDirectory = @"C:\")
        {
            BrowseProjectFilter bf;

            //string saveItemDlg;
            switch (fileType)
            {
                case "Geodatabase FC":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_featureClasses_all");
                    break;
                case "Geodatabase Table":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_geodatabaseItems_tables");
                    break;
                case "Shapefile":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_shapefiles");
                    break;
                case "CSV file (comma delimited)":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_textFiles_csv");
                    break;
                case "Text file (tab delimited)":
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_textFiles_txt");
                    break;
                default:
                    bf = BrowseProjectFilter.GetFilter("esri_browseDialogFilters_all");
                    break;
            }

            // Display the saveItemDlg in an Open Item dialog.
            SaveItemDialog saveItemDlg = new()
            {
                Title = "Save Output As...",
                InitialLocation = initialDirectory,
                //Filter = ItemFilters.Files_All,
                OverwritePrompt = true,
                BrowseFilter = bf
            };

            bool? ok = saveItemDlg.ShowDialog();

            string strOutFile = null;
            if (ok.HasValue)
                strOutFile = saveItemDlg.FilePath;

            return strOutFile; // Null if user pressed exit
        }

        #region CopyFeatures

        /// <summary>
        /// Copy the input feature class to the output feature class.
        /// </summary>
        /// <param name="inFeatureClass"></param>
        /// <param name="outFeatureClass"></param>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public static async Task<bool> CopyFeaturesAsync(string inFeatureClass, string outFeatureClass, bool Messages = false)
        {
            //// prepare input parameter values to CopyFeaturesAsync tool
            //string input_data = @"C:\data\california.gdb\ca_highways";
            //string out_workspace = ArcGIS.Desktop.Core.Project.Current.DefaultGeodatabasePath;
            //string out_data = System.IO.Path.Combine(out_workspace, "ca_highways2");

            // Make a value array of strings to be passed to the tool.
            var parameters = Geoprocessing.MakeValueArray(inFeatureClass, outFeatureClass);

            // Make a value array of the environments to be passed to the tool.
            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            // Execute the tool.
            try
            {
                IGPResult gp_result = await Geoprocessing.ExecuteToolAsync("management.CopyFeatures", parameters, environments);

                if (gp_result.IsFailed)
                {
                    var messages = gp_result.Messages;
                    var errMessages = gp_result.ErrorMessages;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return true;
        }

        /// <summary>
        /// Export the input table to the output table.
        /// </summary>
        /// <param name="InTable"></param>
        /// <param name="OutFile"></param>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public static async Task<bool> ExportFeaturesAsync(string inTable, string outTable, bool Messages = false)
        {
            //// prepare input parameter values to CopyFeaturesAsync tool
            //string input_data = @"C:\data\california.gdb\ca_highways";
            //string out_workspace = ArcGIS.Desktop.Core.Project.Current.DefaultGeodatabasePath;
            //string out_data = System.IO.Path.Combine(out_workspace, "ca_highways2");

            // Make a value array of strings to be passed to the tool.
            var parameters = Geoprocessing.MakeValueArray(inTable, outTable);

            // Make a value array of the environments to be passed to the tool.
            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            // Execute the tool.
            try
            {
                IGPResult gp_result = await Geoprocessing.ExecuteToolAsync("conversion.ExportTable", parameters, environments);

                if (gp_result.IsFailed)
                {
                    var messages = gp_result.Messages;
                    var errMessages = gp_result.ErrorMessages;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return true;
        }

        /// <summary>
        /// Copy the input dataset name to the output feature class.
        /// </summary>
        /// <param name="InWorkspace"></param>
        /// <param name="InDatasetName"></param>
        /// <param name="OutFeatureClass"></param>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public static async Task<bool> CopyFeaturesAsync(string InWorkspace, string InDatasetName, string OutFeatureClass, bool Messages = false)
        {
            string inFeatureClass = InWorkspace + @"\" + InDatasetName;
            return await CopyFeaturesAsync(inFeatureClass, OutFeatureClass, Messages);
        }

        /// <summary>
        /// Copy the input dataset to the output dataset.
        /// </summary>
        /// <param name="InWorkspace"></param>
        /// <param name="InDatasetName"></param>
        /// <param name="OutWorkspace"></param>
        /// <param name="OutDatasetName"></param>
        /// <param name="Messages"></param>
        /// <returns></returns>
        public static async Task<bool> CopyFeaturesAsync(string InWorkspace, string InDatasetName, string OutWorkspace, string OutDatasetName, bool Messages = false)
        {
            string inFeatureClass = InWorkspace + @"\" + InDatasetName;
            string outFeatureClass = OutWorkspace + @"\" + OutDatasetName;
            return await CopyFeaturesAsync(inFeatureClass, outFeatureClass, Messages);
        }

        #endregion

        //    public bool CopyTable(string inTable, string outFile, bool messages = false)
        //    {
        //        // This works absolutely fine for geodatabase but does not export to CSV.

        //        // Note the csv export already removes the geometry field; in this case it is not necessary to check again.

        //        ESRI.ArcGIS.Geoprocessor.Geoprocessor gp = new ESRI.ArcGIS.Geoprocessor.Geoprocessor();
        //        gp.OverwriteOutput = true;

        //        IGeoProcessorResult myresult = new GeoProcessorResultClass();

        //        // Create a variant array to hold the parameter values.
        //        IVariantArray parameters = new VarArrayClass();

        //        // Populate the variant array with parameter values.
        //        parameters.Add(inTable);
        //        parameters.Add(outFile);

        //        // Execute the tool.
        //        try
        //        {
        //            myresult = (IGeoProcessorResult)gp.Execute("CopyRows_management", parameters, null);

        //            // Wait until the execution completes.
        //            while (myresult.Status == esriJobStatus.esriJobExecuting)
        //                Thread.Sleep(1000);
        //                // Wait for 1 second.


        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Process complete");
        //            }
        //            gp = null;
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            gp = null;
        //            return false;
        //        }
        //    }

        //    public bool AlterFieldAliasName(string fileName, string aFieldName, string theAliasName, bool messages = false)
        //    {
        //        // This script changes the field alias of a the named field in the layer.
        //        IObjectClass myObject = (IObjectClass)GetFeatureClass(fileName);
        //        IClassSchemaEdit myEdit = (IClassSchemaEdit)myObject;
        //        try
        //        {
        //            myEdit.AlterFieldAliasName(aFieldName, theAliasName);
        //            myObject = null;
        //            myEdit = null;
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            myObject = null;
        //            myEdit = null;
        //            return false;
        //        }
        //    }

        //    public IField getFCField(string InputDirectory, string FeatureclassName, string FieldName, bool messages = false)
        //    {
        //        IFeatureClass featureClass = GetFeatureClass(InputDirectory, FeatureclassName);
        //        // Find the index of the requested field.
        //        int fieldIndex = featureClass.FindField(FieldName);

        //        // Get the field from the feature class's fields collection.
        //        if (fieldIndex > -1)
        //        {
        //            IFields fields = featureClass.Fields;
        //            IField field = fields.get_Field(fieldIndex);
        //            return field;
        //        }
        //        else
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("The field " + FieldName + " was not found in the featureclass " + FeatureclassName);
        //            }
        //            return null;
        //        }
        //    }

        //    public IField getTableField(string TableName, string FieldName, bool messages = false)
        //    {
        //        ITable theTable = GetTable(FileFunctions.GetDirectoryName(TableName), FileFunctions.GetFileName(TableName), messages);
        //        int fieldIndex = theTable.FindField(FieldName);

        //        // Get the field from the feature class's fields collection.
        //        if (fieldIndex > -1)
        //        {
        //            IFields fields = theTable.Fields;
        //            IField field = fields.get_Field(fieldIndex);
        //            return field;
        //        }
        //        else
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("The field " + FieldName + " was not found in the table " + FileFunctions.GetFileName(TableName));
        //            }
        //            return null;
        //        }
        //    }

        //    public bool AppendTable(string inTable, string TargetTable, bool messages = false)
        //    {
        //        ESRI.ArcGIS.Geoprocessor.Geoprocessor gp = new ESRI.ArcGIS.Geoprocessor.Geoprocessor();
        //        gp.OverwriteOutput = true;

        //        IGeoProcessorResult myresult = new GeoProcessorResultClass();

        //        // Create a variant array to hold the parameter values.
        //        IVariantArray parameters = new VarArrayClass();


        //        // Populate the variant array with parameter values.
        //        parameters.Add(inTable);
        //        parameters.Add(TargetTable);

        //        // Execute the tool. Note this only works with geodatabase tables.
        //        try
        //        {
        //            myresult = (IGeoProcessorResult)gp.Execute("Append_management", parameters, null);

        //            // Wait until the execution completes.
        //            while (myresult.Status == esriJobStatus.esriJobExecuting)
        //                Thread.Sleep(1000);
        //            // Wait for 1 second.
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Process complete");
        //            }
        //            gp = null;
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            gp = null;
        //            return false;
        //        }
        //    }

        //    public bool CopyToCSV(string inTable, string outFile, bool isSpatial, bool append, bool messages = false)
        //    {
        //        // This sub copies the input table to CSV.
        //        string filePath = FileFunctions.GetDirectoryName(inTable);
        //        string fileName = FileFunctions.GetFileName(inTable);

        //        ICursor myCurs = null;
        //        IFields fldsFields = null;
        //        if (isSpatial)
        //        {

        //            IFeatureClass myFC = GetFeatureClass(filePath, fileName, true);
        //            myCurs = (ICursor)myFC.Search(null, false);
        //            fldsFields = myFC.Fields;
        //        }
        //        else
        //        {
        //            ITable myTable = GetTable(filePath, fileName, true);

        //            if (myTable == null)
        //            {
        //                if (messages)
        //                {
        //                    System.Windows.Forms.MessageBox.Show("Table not found " + inTable);
        //                }
        //                return false;
        //            }

        //            myCurs = myTable.Search(null, false);
        //            fldsFields = myTable.Fields;
        //        }

        //        if (myCurs == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Cannot open table " + inTable);
        //            }
        //            return false;
        //        }

        //        // Open output file.
        //        StreamWriter theOutput = new StreamWriter(outFile, append);

        //        string field = null;
        //        string header = "";
        //        int intFieldCount = fldsFields.FieldCount;
        //        int ignoreField = -1;

        //        // iterate through the fields in the collection to create header.
        //        for (int i = 0; i < intFieldCount; i++)
        //        {
        //            // Get the field at the given index.
        //            field = fldsFields.get_Field(i).Name;
        //            if (field == "SP_GEOMETRY" || field == "Shape")
        //                ignoreField = i;
        //            else
        //                header = header + field + ",";
        //        }
        //        if (!append)
        //        {
        //            // Write the header.
        //            header = header.Substring(0, header.Length - 1);
        //            theOutput.WriteLine(header);
        //        }
        //        // Now write the file.
        //        IRow row = myCurs.NextRow();
        //        //MessageBox.Show("Writing ...");
        //        while (row != null)
        //        {
        //            string strRow = "";
        //            for (int i = 0; i < intFieldCount; i++)
        //            {
        //                if (i != ignoreField)
        //                {
        //                    var rowValue = row.get_Value(i);
        //                    // Wrap value if quotes if it is a string that contains a comma
        //                    if ((rowValue is string) &&
        //                       (rowValue.ToString().Contains(","))) rowValue = "\"" + rowValue.ToString() + "\"";
        //                    strRow = strRow + rowValue.ToString();
        //                    if (i < intFieldCount - 1) strRow = strRow + ",";
        //                }
        //            }
        //            theOutput.WriteLine(strRow);
        //            row = myCurs.NextRow();
        //        }

        //        theOutput.Close();
        //        theOutput.Dispose();
        //        myCurs = null;
        //        row = null;
        //        return true;
        //    }


        //    public bool WriteEmptyTabDelimitedFile(string outFile, string tableHeaders)
        //    {
        //        // Open output file.
        //        StreamWriter theOutput = new StreamWriter(outFile, false);
        //        tableHeaders.Replace(",", "\t");
        //        theOutput.Write(tableHeaders);
        //        theOutput.Close();
        //        theOutput.Dispose();
        //        return true;
        //    }

        //    public void ShowTable(string aTableName, bool messages = false)
        //    {
        //        if (aTableName == null)
        //        {
        //            if (messages) System.Windows.Forms.MessageBox.Show("Please pass a table name", "Show Table");
        //            return;
        //        }

        //        ITable myTable = GetTable(aTableName);
        //        if (myTable == null)
        //        {
        //            if (messages)
        //            {
        //                System.Windows.Forms.MessageBox.Show("Table " + aTableName + " not found in map");
        //                return;
        //            }
        //        }

        //        ITableWindow myWin = new TableWindow();
        //        myWin.Table = myTable;
        //        myWin.Application = thisApplication;
        //        myWin.Show(true);
        //    }

        //    public void ToggleTOC()
        //    {
        //        IApplication m_app = thisApplication;

        //        IDockableWindowManager pDocWinMgr = m_app as IDockableWindowManager;
        //        UID uid = new UIDClass();
        //        uid.Value = "{368131A0-F15F-11D3-A67E-0008C7DF97B9}";
        //        IDockableWindow pTOC = pDocWinMgr.GetDockableWindow(uid);
        //        if (pTOC.IsVisible())
        //            pTOC.Show(false);
        //        else pTOC.Show(true);
        //        IMxApplication2 thisApp = (IMxApplication2)thisApplication;
        //        thisApp.Display.Invalidate(null, true, -1);
        //        IActiveView activeView = GetActiveView();
        //        activeView.Refresh();
        //        thisApplication.RefreshWindow();
        //    }

        //    public void ToggleDrawing()
        //    {
        //        IMxApplication2 thisApp = (IMxApplication2)thisApplication;
        //        thisApp.PauseDrawing = !thisApp.PauseDrawing;
        //        thisApp.Display.Invalidate(null, true, -1);
        //        IActiveView activeView = GetActiveView();
        //        activeView.Refresh();
        //        thisApplication.RefreshWindow();
        //    }

        //    public void ZoomToLayer(string aLayerName, bool messages = false)
        //    {
        //        if (!LayerExists(aLayerName))
        //        {
        //            if (messages)
        //                System.Windows.Forms.MessageBox.Show("The layer " + aLayerName + " does not exist in the map");
        //            return;
        //        }
        //        IActiveView activeView = GetActiveView();
        //        ILayer pLayer = GetLayer(aLayerName);
        //        IEnvelope pEnv = pLayer.AreaOfInterest;
        //        pEnv.Expand(1.05, 1.05, true);
        //        activeView.Extent = pEnv;
        //        activeView.Refresh();
        //    }

        //    public void ZoomToFullExtent()
        //    {
        //        IActiveView activeView = GetActiveView();
        //        activeView.Extent = activeView.FullExtent;
        //        activeView.Refresh();
        //    }

        //    public IActiveView GetActiveView()
        //    {
        //        IMxDocument mxDoc = GetIMXDocument();
        //        return mxDoc.ActiveView;
        //    }
        //}
    }
}

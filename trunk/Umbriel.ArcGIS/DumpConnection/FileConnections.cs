﻿

namespace DumpConnection
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Text;
    using System.Diagnostics;
    using ESRI.ArcGIS.esriSystem;
    using ESRI.ArcGIS.Carto;
    using ESRI.ArcGIS.Framework;
    using ESRI.ArcGIS.DataSourcesGDB;
    using ESRI.ArcGIS.Geodatabase;
    using ConnectionStringList = System.Collections.Generic.List<string>;

    public class FileConnections
    {
        private const string LayerConnectionString = "LayerName={0},{1},{2}";

        public const string WildcardExtensionSearch = "*{0}";

        internal static string SearchPath { get; set; }

        internal static bool Recurse { get; set; }

        internal static ConnectionStringList GetConnectionString(ILayer layer, string filepath)
        {
            ConnectionStringList connectionStrings = new ConnectionStringList();

            if (layer is IFeatureLayer)
            {
                IFeatureLayer featureLayer = layer as IFeatureLayer;

                IDataset dataset = featureLayer.FeatureClass as IDataset;

                if (dataset != null)
                {
                    object propertyNames;
                    object propertyValues;

                    dataset.Workspace.ConnectionProperties.GetAllProperties(out propertyNames, out propertyValues);

                    connectionStrings.Add(
                        string.Format(LayerConnectionString, layer.Name, MakeConnectionString(propertyNames, propertyValues), filepath));
                }
                else
                {
                    connectionStrings.Add(string.Format(LayerConnectionString, layer.Name, "No Dataset Information", filepath));
                }


            }
            else if (layer is IGroupLayer)
            {
                ICompositeLayer compositeLayer = layer as ICompositeLayer;

                for (int i = 0; i < compositeLayer.Count; i++)
                {
                    ILayer subLayer = compositeLayer.Layer[i];

                    connectionStrings.AddRange(GetConnectionString(subLayer, filepath));
                }
            }
            else
            {
            }

            return connectionStrings;
        }

        internal static string MakeConnectionString(object propnames, object propvalues)
        {
            ConnectionStringList connstrings = new ConnectionStringList();

            string kvp = "{0}={1}";

            System.Array propNameArray = (System.Array)propnames;
            System.Array propValuesArray = (System.Array)propvalues;

            for (int i = 0; i < propValuesArray.Length; i++)
            {
                connstrings.Add(string.Format(kvp, propNameArray.GetValue(i), propValuesArray.GetValue(i)));
            }

            return string.Join(",", connstrings.ToArray());
        }

        internal static IPropertySet GetConnectionProperties(string path)
        {
            return null;

        }

        internal static IMapDocument OpenMapDocument(string path)
        {
            IMapDocument mapdoc = new MapDocumentClass();
            mapdoc.Open(path);

            return mapdoc;



        }

        internal static ILayer OpenLayerFile(string path)
        {
            // Create a new GxLayer
            ESRI.ArcGIS.Catalog.IGxLayer gxLayer = new ESRI.ArcGIS.Catalog.GxLayerClass();

            ESRI.ArcGIS.Catalog.IGxFile gxFile = (ESRI.ArcGIS.Catalog.IGxFile)gxLayer; //Explicit Cast

            // Set the path for where the layerfile is located on disk
            gxFile.Path = path;


            if (!(gxLayer.Layer == null))
            {
                return gxLayer.Layer;
            }
            else
            {
                return null;
            }

        }

        internal static ConnectionStringList ReadFile(string path)
        {
            ConnectionStringList list = new ConnectionStringList();

            FileInfo file = new FileInfo(path);

            if (file.Exists)
            {
                if (System.IO.Path.GetExtension(file.FullName).Equals(".lyr"))
                {
                    list = GetConnectionString(OpenLayerFile(file.FullName), file.FullName);
                }
                else if (System.IO.Path.GetExtension(file.FullName).Equals(".mxd"))
                {
                    IMapDocument mapdoc = OpenMapDocument(file.FullName);

                    for (int i = 0; i < mapdoc.MapCount; i++)
                    {
                        IMap map = mapdoc.Map[i];
                        if (map != null)
                        {
                            IEnumLayer layers = map.get_Layers(null, true);
                            ILayer maplayer = null;

                            while ((maplayer = layers.Next()) != null)
                            {
                                if (maplayer != null)
                                {
                                    list.AddRange(GetConnectionString(maplayer, file.FullName));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine(string.Format("'{0}' does not exist.", path));
            }

            return list;
        }

    }
}

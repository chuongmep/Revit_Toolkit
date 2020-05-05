/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using Autodesk.Revit.DB;
using BH.Adapter.Revit;
using BH.Engine.Adapters.Revit;
using BH.oM.Adapter;
using BH.oM.Adapters.Revit;
using BH.oM.Adapters.Revit.Enums;
using BH.oM.Adapters.Revit.Settings;
using BH.oM.Base;
using BH.oM.Data.Requests;
using BH.oM.Geometry;
using BH.oM.Graphics;
using BH.UI.Revit.Engine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BH.UI.Revit.Adapter
{
    public partial class RevitUIAdapter
    {
        /***************************************************/
        /****             Protected Methods             ****/
        /***************************************************/

        protected override IEnumerable<IBHoMObject> Read(IRequest request, ActionConfig actionConfig = null)
        {
            Autodesk.Revit.UI.UIDocument uiDocument = this.UIDocument;
            Document document = this.Document;

            if (request == null)
            {
                BH.Engine.Reflection.Compute.RecordError("BHoM objects could not be read because provided IRequest is null.");
                return new List<IBHoMObject>();
            }

            RevitPullConfig pullConfig = actionConfig as RevitPullConfig;
            if (pullConfig == null)
            {
                BH.Engine.Reflection.Compute.RecordError("BHoM objects could not be read because provided actionConfig is not a valid RevitPullConfig.");
                return new List<IBHoMObject>();
            }

            Discipline? requestDiscipline = request.Discipline(pullConfig.Discipline);
            if (requestDiscipline == null)
            {
                BH.Engine.Reflection.Compute.RecordError("Conflicting disciplines have been detected.");
                return new List<IBHoMObject>();
            }

            IEnumerable<ElementId> worksetPrefilter = null;
            if (!pullConfig.IncludeClosedWorksets)
                worksetPrefilter = document.ElementIdsByWorksets(document.OpenWorksetIds().Union(document.SystemWorksetIds()).ToList());

            IEnumerable<ElementId> elementIds = request.IElementIds(uiDocument, worksetPrefilter).RemoveGridSegmentIds(document);
            if (elementIds == null)
                return new List<IBHoMObject>();

            Discipline discipline = requestDiscipline.Value;
            if (discipline == Discipline.Undefined)
                discipline = Discipline.Physical;

            RevitSettings revitSettings = RevitSettings.DefaultIfNull();

            //TODO: this should happen somewhere else!
            MapSettings mapSettings = RevitSettings.MapSettings;
            if (mapSettings.TypeMaps == null || mapSettings.TypeMaps.Count == 0)
                mapSettings = BH.Engine.Adapters.Revit.Query.DefaultMapSettings();

            PullGeometryConfig geometryConfig = pullConfig.GeometryConfig;
            if (geometryConfig == null)
                geometryConfig = new PullGeometryConfig();

            PullRepresentationConfig representationConfig = pullConfig.RepresentationConfig;
            if (representationConfig == null)
                representationConfig = new PullRepresentationConfig();

            Options geometryOptions = BH.UI.Revit.Engine.Create.Options(ViewDetailLevel.Fine, geometryConfig.IncludeNonVisible, false);
            Options meshOptions = BH.UI.Revit.Engine.Create.Options(geometryConfig.MeshDetailLevel.ViewDetailLevel(), geometryConfig.IncludeNonVisible, false);
            Options renderMeshOptions = BH.UI.Revit.Engine.Create.Options(representationConfig.DetailLevel.ViewDetailLevel(), representationConfig.IncludeNonVisible, false);

            List<IBHoMObject> result = new List<IBHoMObject>();
            Dictionary<string, List<IBHoMObject>> refObjects = new Dictionary<string, List<IBHoMObject>>();
            
            foreach (ElementId id in elementIds)
            {
                Element element = document.GetElement(id);
                if (element == null)
                    continue;
                
                IEnumerable<IBHoMObject> iBHoMObjects = Read(element, discipline, revitSettings, refObjects);
                if (iBHoMObjects != null && iBHoMObjects.Count() != 0)
                { 
                    if (geometryConfig.PullEdges)
                    {
                        List<ICurve> edges = element.Curves(geometryOptions, revitSettings);
                        foreach (IBHoMObject iBHoMObject in iBHoMObjects)
                        {
                            iBHoMObject.CustomData[BH.Engine.Adapters.Revit.Convert.Edges] = edges;
                        }
                    }

                    if (geometryConfig.PullSurfaces)
                    {
                        List<ISurface> surfaces = element.Surfaces(geometryOptions, revitSettings);
                        foreach (IBHoMObject iBHoMObject in iBHoMObjects)
                        {
                            iBHoMObject.CustomData[BH.Engine.Adapters.Revit.Convert.Surfaces] = surfaces;
                        }
                    }

                    if (geometryConfig.PullMeshes)
                    {
                        List<IGeometry> meshes = element.Meshes(meshOptions, revitSettings);
                        foreach (IBHoMObject iBHoMObject in iBHoMObjects)
                        {
                            iBHoMObject.CustomData[BH.Engine.Adapters.Revit.Convert.Meshes] = meshes;
                        }
                    }

                    if (representationConfig.PullRenderMesh)
                    {
                        List<IGeometry> meshes = element.Meshes(renderMeshOptions, revitSettings);
                        for (int i = 0; i < meshes.Count; i++)
                        {
                            if (meshes[i] is BH.oM.Geometry.Mesh)
                            {
                                BH.oM.Geometry.Mesh mesh = ((BH.oM.Geometry.Mesh)meshes[i]);
                                meshes[i] = new BH.oM.Graphics.RenderMesh() { Faces = mesh.Faces, Vertices = mesh.Vertices.Select(pt => (Vertex)pt).ToList() };
                            }
                        }
                        foreach (IBHoMObject iBHoMObject in iBHoMObjects)
                        {
                            iBHoMObject.CustomData[BH.Engine.Adapters.Revit.Convert.RenderMesh] = meshes;
                        }
                    }

                    result.AddRange(iBHoMObjects);
                }
            }

            bool[] activePulls = new bool[] { geometryConfig.PullEdges, geometryConfig.PullSurfaces, geometryConfig.PullMeshes, representationConfig.PullRenderMesh };
            if (activePulls.Count(x => x == true) > 1)
                BH.Engine.Reflection.Compute.RecordWarning("Pull of more than one geometry/representation type has been specified in RevitPullConfig. Please consider this can be time consuming due to the amount of conversions.");

            return result;
        }


        /***************************************************/
        /****               Public Methods              ****/
        /***************************************************/

        public static IEnumerable<IBHoMObject> Read(Element element, Discipline discipline, RevitSettings settings, Dictionary<string, List<IBHoMObject>> refObjects)
        {
            if (element == null || !element.IsValidObject)
                return new List<IBHoMObject>();

            object obj = null;
            try
            {
                obj = element.IFromRevit(discipline, settings, refObjects);
            }
            catch (Exception exception)
            {
                BH.Engine.Reflection.Compute.RecordError(string.Format("BHoM object could not be properly converted. Element Id: {0}, Element Name: {1}, Exception Message: {2}", element.Id.IntegerValue, element.Name, exception.Message));
            }

            List<IBHoMObject> result = new List<IBHoMObject>();
            if (obj != null)
            {
                if (obj is IBHoMObject)
                    result.Add((IBHoMObject)obj);
                else if (obj is IEnumerable<IBHoMObject>)
                    result.AddRange((IEnumerable<IBHoMObject>)obj);
            }

            //Assign Tags
            string tagsParameterName = null;
            if (settings != null)
                tagsParameterName = settings.TagsParameterName;

            if (!string.IsNullOrEmpty(tagsParameterName))
            {
                foreach(IBHoMObject o in result)
                {
                    o.SetTags(element, tagsParameterName);
                }
            }

            return result;
        }

        /***************************************************/
    }
}
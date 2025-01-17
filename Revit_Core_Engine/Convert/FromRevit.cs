/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2021, the respective contributors. All rights reserved.
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
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Structure;
using BH.Engine.Architecture;
using BH.Engine.Physical;
using BH.Engine.Revit;
using BH.Engine.Spatial;
using BH.Engine.Structure;
using BH.oM.Adapters.Revit.Enums;
using BH.oM.Adapters.Revit.Settings;
using BH.oM.Base;
using BH.oM.Dimensional;
using BH.oM.Geometry;
using BH.oM.Reflection.Attributes;
using BH.oM.Structure.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Revit.Engine.Core
{
    public static partial class Convert
    {
        /***************************************************/
        /****      Convert Revit elements to BHoM       ****/
        /***************************************************/

        public static IBHoMObject FromRevit(this ProjectInfo projectInfo, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                case Discipline.Environmental:
                    return projectInfo.BuildingFromRevit(settings, refObjects);
                default:
                    return null;
            }
        }

        /***************************************************/

        public static IEnumerable<IBHoMObject> FromRevit(this EnergyAnalysisDetailModel energyAnalysisModel, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                case Discipline.Environmental:
                    return energyAnalysisModel.EnergyAnalysisModelFromRevit(settings, refObjects);
                default:
                    return null;
            }
        }

        /***************************************************/

        public static IEnumerable<IBHoMObject> FromRevit(this FamilyInstance familyInstance, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            if (AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(familyInstance))
            {
                BH.oM.Adapters.Revit.Elements.ModelInstance instance = familyInstance.ModelInstanceFromRevit(settings, refObjects);

                if (instance != null && transform?.IsIdentity == false)
                {
                    TransformMatrix bHoMTransform = transform.FromRevit();
                    instance = instance.Transform(bHoMTransform) as BH.oM.Adapters.Revit.Elements.ModelInstance;
                }

                return new List<IBHoMObject> { instance };
            }
            else
            {
                IEnumerable<IElement> result = null;
                switch (discipline)
                {
                    case Discipline.Structural:
                        if (typeof(Bar).BuiltInCategories().Contains((BuiltInCategory)familyInstance.Category.Id.IntegerValue))
                            result = familyInstance.BarsFromRevit(settings, refObjects);
                        break;
                    case Discipline.Physical:
                    case Discipline.Architecture:
                        if (typeof(BH.oM.Physical.Elements.Window).BuiltInCategories().Contains((BuiltInCategory)familyInstance.Category.Id.IntegerValue))
                            result = new List<IElement> { familyInstance.WindowFromRevit(settings, refObjects) };
                        else if (typeof(BH.oM.Physical.Elements.Door).BuiltInCategories().Contains((BuiltInCategory)familyInstance.Category.Id.IntegerValue))
                            result = new List<IElement> { familyInstance.DoorFromRevit(settings, refObjects) };
                        else if (typeof(BH.oM.Physical.Elements.Column).BuiltInCategories().Contains((BuiltInCategory)familyInstance.Category.Id.IntegerValue) || familyInstance.StructuralType == StructuralType.Column)
                            result = new List<IElement> { familyInstance.ColumnFromRevit(settings, refObjects) };
                        else if (typeof(BH.oM.Physical.Elements.Bracing).BuiltInCategories().Contains((BuiltInCategory)familyInstance.Category.Id.IntegerValue) || familyInstance.StructuralType == StructuralType.Brace)
                            result = new List<IElement> { familyInstance.BracingFromRevit(settings, refObjects) };
                        else if (typeof(BH.oM.Physical.Elements.Beam).BuiltInCategories().Contains((BuiltInCategory)familyInstance.Category.Id.IntegerValue))
                            result = new List<IElement> { familyInstance.BeamFromRevit(settings, refObjects) };
                        break;
                    case Discipline.Environmental:
                        result = new List<IElement> { familyInstance.EnvironmentPanelFromRevit(settings, refObjects) };
                        break;
                    case Discipline.Facade:
                        if (typeof(BH.oM.Facade.Elements.Opening).BuiltInCategories().Contains((BuiltInCategory)familyInstance.Category.Id.IntegerValue))
                            result = new List<IElement> { familyInstance.FacadeOpeningFromRevit(settings, refObjects) };
                        else if (typeof(BH.oM.Facade.Elements.FrameEdge).BuiltInCategories().Contains((BuiltInCategory)familyInstance.Category.Id.IntegerValue))
                            result = new List<IElement> { familyInstance.FrameEdgeFromRevit(settings, refObjects) };
                        break;
                }

                if (result != null && transform?.IsIdentity == false)
                {
                    TransformMatrix bHoMTransform = transform.FromRevit();
                    result = result.Select(x => x?.ITransform(bHoMTransform));
                }

                return result?.Cast<IBHoMObject>().ToList();
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this FamilySymbol familySymbol, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    if (typeof(BH.oM.Spatial.ShapeProfiles.IProfile).BuiltInCategories().Contains((BuiltInCategory)familySymbol.Category.Id.IntegerValue))
                        return familySymbol.ProfileFromRevit(settings, refObjects);
                    else
                        return null;
            }
        }

        /***************************************************/

        public static IEnumerable<IBHoMObject> FromRevit(this Wall wall, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IEnumerable<IElement2D> result = null;
            switch (discipline)
            {
                case Discipline.Environmental:
                    result = wall.EnvironmentPanelsFromRevit(settings, refObjects);
                    break;
                case Discipline.Structural:
                    result = wall.StructuralPanelsFromRevit(settings, refObjects);
                    break;
                case Discipline.Facade:
                    if (wall.CurtainGrid != null)
                        result = new List<IElement2D> { wall.CurtainWallFromRevit(settings, refObjects) };
                    else
                        result = new List<IElement2D> { wall.FacadePanelFromRevit(settings, refObjects) };
                    break;
                case Discipline.Architecture:
                case Discipline.Physical:
                    result = new List<IElement2D> { wall.WallFromRevit(settings, refObjects) };
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.Select(x => x.ITransform(bHoMTransform));
            }

            return result?.Cast<IBHoMObject>().ToList();
        }

        /***************************************************/

        public static IEnumerable<IBHoMObject> FromRevit(this Ceiling ceiling, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IEnumerable<IElement2D> result = null;
            switch (discipline)
            {
                case Discipline.Environmental:
                    result = ceiling.EnvironmentPanelsFromRevit(settings, refObjects);
                    break;
                case Discipline.Facade:
                case Discipline.Architecture:
                case Discipline.Physical:
                    result = new List<IElement2D> { ceiling.CeilingFromRevit(settings, refObjects) };
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.Select(x => x.ITransform(bHoMTransform));
            }

            return result?.Cast<IBHoMObject>().ToList();
        }

        /***************************************************/

        public static IEnumerable<IBHoMObject> FromRevit(this Floor floor, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IEnumerable<IElement2D> result = null;
            switch (discipline)
            {
                case Discipline.Environmental:
                    result = floor.EnvironmentPanelsFromRevit(settings, refObjects);
                    break;
                case Discipline.Structural:
                    result = floor.StructuralPanelsFromRevit(settings, refObjects);
                    break;
                case Discipline.Facade:
                case Discipline.Architecture:
                case Discipline.Physical:
                    result = new List<IElement2D> { floor.FloorFromRevit(settings, refObjects) };
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.Select(x => x.ITransform(bHoMTransform));
            }

            return result?.Cast<IBHoMObject>().ToList();
        }

        /***************************************************/

        public static IEnumerable<IBHoMObject> FromRevit(this RoofBase roofBase, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IEnumerable<IElement2D> result = null;
            switch (discipline)
            {
                case Discipline.Environmental:
                    result = roofBase.EnvironmentPanelsFromRevit(settings, refObjects);
                    break;
                case Discipline.Structural:
                    result = roofBase.StructuralPanelsFromRevit(settings, refObjects);
                    break;
                case Discipline.Facade:
                case Discipline.Architecture:
                case Discipline.Physical:
                    result = new List<IElement2D> { roofBase.RoofFromRevit(settings, refObjects) };
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.Select(x => x.ITransform(bHoMTransform));
            }

            return result?.Cast<IBHoMObject>().ToList();
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this HostObjAttributes hostObjAttributes, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                case Discipline.Structural:
                    return hostObjAttributes.SurfacePropertyFromRevit(null, settings, refObjects) as IBHoMObject;
                case Discipline.Facade:
                case Discipline.Architecture:
                case Discipline.Physical:
                case Discipline.Environmental:
                    return hostObjAttributes.ConstructionFromRevit(null, settings, refObjects);
                default:
                    return null;
            }
        }

        /***************************************************/

        [Description("Convert a Revit cable tray into a BHoM cable tray.")]
        [Input("cableTray", "Revit cable tray to be converted.")]
        [Input("discipline", "Engineering discipline based on the BHoM discipline classification.")]
        [Input("settings", "Revit adapter settings.")]
        [Input("refObjects", "A collection of objects processed in the current adapter action, stored to avoid processing the same object more than once.")]
        [Output("cableTrays", "Resulted list of BHoM cable trays converted from a Revit cable trays.")]
        public static IEnumerable<IBHoMObject> FromRevit(this Autodesk.Revit.DB.Electrical.CableTray cableTray, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IEnumerable<IElement1D> result = null;
            switch (discipline)
            {
                case Discipline.Architecture:
                case Discipline.Physical:
                case Discipline.Environmental:
                    result = new List<IElement1D>(cableTray.CableTrayFromRevit(settings, refObjects));
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.Select(x => x.ITransform(bHoMTransform));
            }

            return result?.Cast<IBHoMObject>().ToList();
        }
        
        /***************************************************/

        [Description("Convert a Revit duct into a BHoM duct.")]
        [Input("duct", "Revit duct to be converted.")]
        [Input("discipline", "Engineering discipline based on the BHoM discipline classification.")]
        [Input("settings", "Revit adapter settings.")]
        [Input("refObjects", "A collection of objects processed in the current adapter action, stored to avoid processing the same object more than once.")]
        [Output("ducts", "Resulted list of BHoM ducts converted from a Revit ducts.")]
        public static IEnumerable<IBHoMObject> FromRevit(this Autodesk.Revit.DB.Mechanical.Duct duct, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IEnumerable<IElement1D> result = null;
            switch (discipline)
            {
                case Discipline.Architecture:
                case Discipline.Physical:
                case Discipline.Environmental:
                    result = new List<IElement1D>(duct.DuctFromRevit(settings, refObjects));
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.Select(x => x.ITransform(bHoMTransform));
            }

            return result?.Cast<IBHoMObject>().ToList();
        }

        /***************************************************/
        
        [Description("Convert a Revit pipe into a BHoM pipe.")]
        [Input("pipe", "Revit pipe to be converted.")]
        [Input("discipline", "Engineering discipline based on the BHoM discipline classification.")]
        [Input("settings", "Revit adapter settings.")]
        [Input("refObjects", "A collection of objects processed in the current adapter action, stored to avoid processing the same object more than once.")]
        [Output("pipes", "Resulted list of BHoM MEP pipes converted from a Revit pipes.")]
        public static IEnumerable<IBHoMObject> FromRevit(this Autodesk.Revit.DB.Plumbing.Pipe pipe, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IEnumerable<IElement1D> result = null;
            switch (discipline)
            {
                case Discipline.Architecture:
                case Discipline.Physical:
                case Discipline.Environmental:
                    result = new List<IElement1D>(pipe.PipeFromRevit(settings, refObjects));
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.Select(x => x.ITransform(bHoMTransform));
            }

            return result?.Cast<IBHoMObject>().ToList();
        }

        /***************************************************/

        [Description("Convert a Revit wire into a BHoM wire.")]
        [Input("wire", "Revit wire to be converted.")]
        [Input("discipline", "Engineering discipline based on the BHoM discipline classification.")]
        [Input("settings", "Revit adapter settings.")]
        [Input("refObjects", "A collection of objects processed in the current adapter action, stored to avoid processing the same object more than once.")]
        [Output("wire", "BHoM wire converted from a Revit wire.")]
        public static IBHoMObject FromRevit(this Autodesk.Revit.DB.Electrical.Wire wire, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IElement1D result = null;
            switch (discipline)
            {
                case Discipline.Architecture:
                case Discipline.Physical:
                case Discipline.Environmental:
                    result = wire.WireFromRevit(settings, refObjects);
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.ITransform(bHoMTransform);
            }

            return result as IBHoMObject;
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this Level level, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    BH.oM.Geometry.SettingOut.Level result = level.LevelFromRevit(settings, refObjects);
                    if (result != null && transform?.IsIdentity == false)
                        result.Elevation += transform.Origin.Z.ToSI(UnitType.UT_Length);

                    return result;
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this Grid grid, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IElement1D result = null;
            switch (discipline)
            {
                default:
                    result = grid.GridFromRevit(settings, refObjects);
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.ITransform(bHoMTransform);
            }

            return result as IBHoMObject;
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this MultiSegmentGrid grid, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IElement1D result = null;
            switch (discipline)
            {
                default:
                    result = grid.GridFromRevit(settings, refObjects);
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.ITransform(bHoMTransform);
            }

            return result as IBHoMObject;
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this ElementType elementType, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    return elementType.InstancePropertiesFromRevit(settings, refObjects);
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this GraphicsStyle graphicStyle, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    return graphicStyle.InstancePropertiesFromRevit(settings, refObjects);
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this SpatialElement spatialElement, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IElement2D result = null;
            switch (discipline)
            {
                case Discipline.Environmental:
                    result = spatialElement.SpaceFromRevit(settings, refObjects);
                    break;
                case Discipline.Facade:
                case Discipline.Architecture:
                case Discipline.Physical:
                    result = spatialElement.RoomFromRevit(settings, refObjects);
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.ITransform(bHoMTransform);
            }

            return result as IBHoMObject;
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this EnergyAnalysisSpace energyAnalysisSpace, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IElement2D result = null;
            switch (discipline)
            {
                case Discipline.Facade:
                case Discipline.Environmental:
                case Discipline.Architecture:
                case Discipline.Physical:
                    result = energyAnalysisSpace.SpaceFromRevit(settings, refObjects);
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.ITransform(bHoMTransform);
            }

            return result as IBHoMObject;
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this EnergyAnalysisSurface energyAnalysisSurface, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IElement2D result = null;
            switch (discipline)
            {
                case Discipline.Facade:
                case Discipline.Environmental:
                case Discipline.Architecture:
                case Discipline.Physical:
                    result = energyAnalysisSurface.EnvironmentPanelFromRevit(settings, refObjects);
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.ITransform(bHoMTransform);
            }

            return result as IBHoMObject;
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this EnergyAnalysisOpening energyAnalysisOpening, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            IElement2D result = null;
            switch (discipline)
            {
                case Discipline.Facade:
                case Discipline.Environmental:
                case Discipline.Architecture:
                case Discipline.Physical:
                    result = energyAnalysisOpening.EnvironmentPanelFromRevit(null, settings, refObjects);
                    break;
            }

            if (result != null && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.ITransform(bHoMTransform);
            }

            return result as IBHoMObject;
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this ViewSheet viewSheet, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    return viewSheet.SheetFromRevit(settings, refObjects);
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this Viewport viewport, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    return viewport.ViewportFromRevit(settings, refObjects);
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this ViewPlan viewPlan, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    return viewPlan.ViewPlanFromRevit(settings, refObjects);
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this Material material, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                case Discipline.Environmental:
                    return material.SolidMaterialFromRevit(settings, refObjects);
                case Discipline.Structural:
                    return material.MaterialFragmentFromRevit(null, settings, refObjects);
                case Discipline.Facade:
                case Discipline.Physical:
                    return material.MaterialFromRevit(null, settings, refObjects);
                default:
                    return null;
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this Family family, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    return family.FamilyFromRevit(settings, refObjects);
            }
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this CurveElement curveElement, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            BH.oM.Adapters.Revit.Elements.IInstance result = null;
            switch (discipline)
            {
                default:
                    result = curveElement.InstanceFromRevit(settings, refObjects);
                    break;
            }

            if (result is BH.oM.Adapters.Revit.Elements.ModelInstance && transform?.IsIdentity == false)
            {
                TransformMatrix bHoMTransform = transform.FromRevit();
                result = result.Transform(bHoMTransform);
            }

            return result;
        }

        /***************************************************/

        public static IBHoMObject FromRevit(this FilledRegion filledRegion, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            switch (discipline)
            {
                default:
                    return filledRegion.DraftingInstanceFromRevit(settings, refObjects);
            }
        }


        /***************************************************/
        /****             Fallback Methods              ****/
        /***************************************************/

        public static IBHoMObject FromRevit(this Element element, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            return null;
        }

        /***************************************************/

        public static IGeometry FromRevit(this Location location)
        {
            return null;
        }


        /***************************************************/
        /****             Interface Methods             ****/
        /***************************************************/

        public static object IFromRevit(this Element element, Discipline discipline, Transform transform = null, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null)
        {
            if (element == null)
            {
                BH.Engine.Reflection.Compute.RecordWarning("BHoM object could not be read because Revit element does not exist.");
                return null;
            }

            var result = FromRevit(element as dynamic, discipline, transform, settings, refObjects);
            if (result == null || (typeof(IEnumerable<object>).IsAssignableFrom(result.GetType()) && ((IEnumerable<object>)result).Count(x => x != null) == 0))
            {
                result = element.ObjectFromRevit(discipline, settings, refObjects);
                if (result is BH.oM.Adapters.Revit.Elements.ModelInstance && transform?.IsIdentity == false)
                {
                    TransformMatrix bHoMTransform = transform.FromRevit();
                    result = (result as BH.oM.Adapters.Revit.Elements.IInstance).Transform(bHoMTransform);
                }

                element.NotConvertedWarning(discipline);
            }

            return result;
        }

        /***************************************************/

        public static IGeometry IFromRevit(this Location location)
        {
            if (location == null)
                return null;

            return FromRevit(location as dynamic);
        }

        /***************************************************/
    }
}


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
using Autodesk.Revit.DB.Mechanical;
using BH.Engine.Adapters.Revit;
using BH.oM.Adapters.Revit.Settings;
using BH.oM.Spatial.ShapeProfiles;
using System;
using System.Collections.Generic;
using System.Linq;
using BH.oM.MEP.System.SectionProperties;

namespace BH.Revit.Engine.Core
{
    public static partial class Convert
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public static Autodesk.Revit.DB.Electrical.CableTray ToRevitCableTray(this oM.MEP.System.CableTray cableTray, Document document, RevitSettings settings = null, Dictionary<Guid, List<int>> refObjects = null)
        {
            if (document == null)
                return null;

            // Check valid cableTray object
            if (cableTray == null)
                return null;

            // Construct Revit Cable Tray
            Autodesk.Revit.DB.Electrical.CableTray revitTray = refObjects.GetValue<Autodesk.Revit.DB.Electrical.CableTray>(document, cableTray.BHoM_Guid);
            if (revitTray != null)
                return revitTray;

            // Settings
            settings = settings.DefaultIfNull();

            // CableTray type
            Autodesk.Revit.DB.Electrical.CableTrayType trayType = cableTray.ToRevitElementType(document, new List<BuiltInCategory> { BuiltInCategory.OST_CableTrayRun }, settings, refObjects) as Autodesk.Revit.DB.Electrical.CableTrayType;
            if (trayType == null)
            {
                BH.Engine.Reflection.Compute.RecordError("No valid family has been found in the Revit model. CableTray creation requires the presence of the default CableTray Family Type.");
                return null;
            }

            // End points
            XYZ start = cableTray.StartPoint.Position.ToRevit();
            XYZ end = cableTray.EndPoint.Position.ToRevit();

            // Level
            Level level = document.LevelBelow(Math.Min(start.Z, end.Z), settings);
            if (level == null)
                return null;

            List<SectionProfile> sectionProfile = cableTray.SectionProfile;

            //BoxProfile elementProfile = sectionProfile.ElementProfile as BoxProfile;
            //if (elementProfile == null)
            //    return null;

            revitTray = Autodesk.Revit.DB.Electrical.CableTray.Create(document, trayType.Id, start, end, level.Id);
            if (revitTray == null)
            {
                BH.Engine.Reflection.Compute.RecordError("No Revit CableTray has been created. Please check inputs prior to push attempt.");
                return null;
            }

            // Copy parameters from BHoM object to Revit element
            revitTray.CopyParameters(cableTray, settings);

            // Set Orientation angle
            double orientationAngle = cableTray.OrientationAngle;
            if (Math.Abs(orientationAngle) > settings.AngleTolerance)
            {
                ElementTransformUtils.RotateElement(document, revitTray.Id, Line.CreateBound(start, end), orientationAngle);
            }

            // Set Height
            double profileHeight = cableTray.ElementSize.Height;
            revitTray.SetParameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM, profileHeight);

            // Set Width
            double profileWidth = cableTray.ElementSize.Width;
            revitTray.SetParameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM, profileWidth);

            refObjects.AddOrReplace(cableTray, revitTray);
            return revitTray;
        }

        /***************************************************/
    }
}


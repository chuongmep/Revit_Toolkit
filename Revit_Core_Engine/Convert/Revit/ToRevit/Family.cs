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
using BH.Engine.Adapters.Revit;
using BH.oM.Adapters.Revit.Properties;
using BH.oM.Adapters.Revit.Settings;
using System;
using System.Collections.Generic;

namespace BH.Revit.Engine.Core
{
    public static partial class Convert
    {
        /***************************************************/
        /****               Public Methods              ****/
        /***************************************************/

        public static Family ToRevitFamily(this oM.Adapters.Revit.Elements.Family family, Document document, RevitSettings settings = null, Dictionary<Guid, List<int>> refObjects = null)
        {
            Family revitFamily = refObjects.GetValue<Family>(document, family.BHoM_Guid);
            if (revitFamily != null)
                return revitFamily;

            settings = settings.DefaultIfNull();

            if (family.PropertiesList != null && family.PropertiesList.Count != 0)
            {
                foreach(InstanceProperties instanceProperties in family.PropertiesList)
                    instanceProperties.ToRevitElementType(document, settings, refObjects);
            }

            BuiltInCategory builtInCategory = family.BuiltInCategory(document, settings.FamilyLoadSettings);

            revitFamily = family.Family(document, builtInCategory, settings.FamilyLoadSettings);

            revitFamily.CheckIfNullPush(family);
            if (revitFamily == null)
                return null;

            // Copy parameters from BHoM CustomData to Revit Element
            revitFamily.CopyParameters(family, null);

            refObjects.AddOrReplace(family, revitFamily);
            return revitFamily;
        }

        /***************************************************/
    }
}

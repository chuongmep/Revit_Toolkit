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
using BH.oM.Adapters.Revit.Generic;
using BH.oM.Adapters.Revit.Settings;
using System.Collections.Generic;
using System.Linq;

namespace BH.Revit.Engine.Core
{
    public static partial class Compute
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public static FamilySymbol LoadFamilySymbol(this FamilyLoadSettings familyLoadSettings, Document document, string categoryName, string familyName, string familyTypeName = null)
        {
            if (familyLoadSettings == null || familyLoadSettings.FamilyLibrary == null || document == null)
                return null;

            FamilyLibrary familyLibrary = familyLoadSettings.FamilyLibrary;

            IEnumerable<string> paths = BH.Engine.Adapters.Revit.Query.Paths(familyLibrary, categoryName, familyName, familyTypeName);
            if (paths == null || paths.Count() == 0)
                return null;

            string path = paths.First();

            string typeName = familyTypeName;
            if (string.IsNullOrEmpty(typeName))
            {
                //Look for first available type name if type name not provided

                RevitFilePreview revitFilePreview = BH.Engine.Adapters.Revit.Create.RevitFilePreview(path);
                if (revitFilePreview == null)
                    return null;

                List<string> typeNameList = BH.Engine.Adapters.Revit.Query.FamilyTypeNames(revitFilePreview);
                if (typeNameList == null || typeNameList.Count < 1)
                    return null;

                typeName = typeNameList.First();
            }

            if (string.IsNullOrEmpty(typeName))
                return null;

            FamilySymbol familySymbol = null;

            if (document.LoadFamilySymbol(path, typeName, new FamilyLoadOptions(familyLoadSettings), out familySymbol))
            {
                if (!familySymbol.IsActive)
                    familySymbol.Activate();
            }

            return familySymbol;
        }

        /***************************************************/
    }
}

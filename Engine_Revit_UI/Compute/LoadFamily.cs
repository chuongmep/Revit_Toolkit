/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2018, the respective contributors. All rights reserved.
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

using System.Linq;
using System.Collections.Generic;

using BH.oM.Adapters.Revit.Generic;
using BH.oM.Adapters.Revit.Settings;

using Autodesk.Revit.DB;

namespace BH.UI.Revit.Engine
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static Family LoadFamily(this FamilyLoadSettings FamilyLoadSettings, Document document, string categoryName, string familyName)
        {
            if (FamilyLoadSettings == null || FamilyLoadSettings.FamilyLibrary == null || document == null)
                return null;

            FamilyLibrary aFamilyLibrary = FamilyLoadSettings.FamilyLibrary;

            IEnumerable<string> aPaths = BH.Engine.Adapters.Revit.Query.Paths(aFamilyLibrary, categoryName, familyName, null);
            if (aPaths == null || aPaths.Count() == 0)
                return null;

            string aPath = aPaths.First();

            Family aFamily= null;

            if (document.LoadFamily(aPath, new FamilyLoadOptions(FamilyLoadSettings), out aFamily))
                return aFamily;

            return null;
        }

        /***************************************************/
    }
}
﻿/*
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

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using BH.oM.Base;
using BH.Engine.Adapters.Revit;
using BH.oM.Adapters.Revit;
using BH.oM.Adapters.Revit.Enums;
using BH.oM.Adapters.Revit.Interface;
using BH.oM.Data.Requests;
using System.Collections;

namespace BH.UI.Revit.Engine
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public static IEnumerable<ElementId> ElementIdsByIdObjects(this Document document, IList idObjects, IEnumerable<ElementId> ids = null)
        {
            List<int> elementIds = new List<int>();
            List<string> uniqueIds = new List<string>();
            if (idObjects != null)
            {
                foreach (object obj in idObjects)
                {
                    if (obj is int)
                        elementIds.Add((int)obj);
                    else if (obj is string)
                    {
                        string stringId = (string)obj;
                        int id;
                        if (int.TryParse(stringId, out id))
                            elementIds.Add(id);
                        else
                            uniqueIds.Add(stringId);
                    }
                }
            }

            if (elementIds.Count == 0 && uniqueIds.Count == 0)
                return ids;
            
            HashSet<ElementId> result = new HashSet<ElementId>();
            result.UnionWith(document.ElementIdsByInts(elementIds));
            result.UnionWith(document.ElementIdsByUniqueIds(uniqueIds));
            return result;
        }

        /***************************************************/
    }
}
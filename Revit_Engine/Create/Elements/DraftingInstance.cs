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

using System.ComponentModel;

using BH.oM.Adapters.Revit.Elements;
using BH.oM.Geometry;
using BH.oM.Reflection.Attributes;
using BH.oM.Adapters.Revit.Properties;

namespace BH.Engine.Adapters.Revit
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates DraftingInstance by given Family Name, Type Name, Location and View Name. Drafting Instance defines all view specific 2D elements")]
        [Input("familyName", "Revit Family Name")]
        [Input("familyTypeName", "Revit Family Type Name")]
        [Input("location", "Location of DraftingObject on View")]
        [Input("viewName", "View assigned to DraftingInstance")]
        [Output("DraftingInstance")]
        public static DraftingInstance DraftingInstance(string familyName, string familyTypeName, string viewName, Point location)
        {
            if (string.IsNullOrWhiteSpace(familyName) || string.IsNullOrWhiteSpace(familyTypeName) || string.IsNullOrWhiteSpace(viewName) || location == null)
                return null;

            return DraftingInstance(Create.InstanceProperties(familyName, familyTypeName), viewName, location);
        }

        /***************************************************/

        [Description("Creates DraftingInstance by given Family Name, Type Name, Location and View Name. Drafting Instance defines all view specific 2D elements")]
        [Input("properties", "InstanceProperties of Instance")]
        [Input("location", "Location of DraftingObject on View")]
        [Input("viewName", "View assigned to DraftingInstance")]
        [Output("DraftingInstance")]
        public static DraftingInstance DraftingInstance(InstanceProperties properties, string viewName, Point location)
        {
            if (properties == null || string.IsNullOrWhiteSpace(viewName) || location == null)
                return null;

            DraftingInstance aDraftingInstance = new DraftingInstance()
            {
                Properties = properties,
                Name = properties.Name,
                ViewName = viewName,
                Location = location
            };

            return aDraftingInstance;
        }

        /***************************************************/
    }
}
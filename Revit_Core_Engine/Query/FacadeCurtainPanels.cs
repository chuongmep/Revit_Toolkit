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
using BH.oM.Adapters.Revit.Settings;
using BH.oM.Base;
using BH.oM.Dimensional;
using BH.oM.Geometry;
using BH.oM.Physical.Elements;
using BH.oM.Facade.Elements;
using BH.Engine.Facade;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BH.Revit.Engine.Core
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public static List<BH.oM.Facade.Elements.Opening> FacadeCurtainPanels(this CurtainGrid curtainGrid, Document document, RevitSettings settings = null, Dictionary<string, List<IBHoMObject>> refObjects = null, BH.oM.Adapters.Revit.Enums.Discipline discipline = oM.Adapters.Revit.Enums.Discipline.Physical)
        {
            if (curtainGrid == null)
                return null;

            List<IElement1D> cwMullions = curtainGrid.CurtainWallMullions(document, settings, refObjects).Select(x => x as IElement1D).ToList();

            List<oM.Facade.Elements.Opening> result = new List<oM.Facade.Elements.Opening>();
            List<Element> panels = curtainGrid.GetPanelIds().Select(x => document.GetElement(x)).ToList();
            List<CurtainCell> cells = curtainGrid.GetCurtainCells().ToList();
            if (panels.Count != cells.Count)
                return null;

            for (int i = 0; i < panels.Count; i++)
            {
                FamilyInstance panel = panels[i] as FamilyInstance;
                if (panel == null || panel.get_BoundingBox(null) == null)
                    continue;

                foreach (PolyCurve pc in cells[i].CurveLoops.FromRevit())
                {
                    oM.Facade.Elements.Opening bHoMOpening = panel.FacadeOpeningFromRevit(settings, refObjects);
                    List<FrameEdge> edges = bHoMOpening.Edges;
                    foreach  (FrameEdge edge in edges)
                    {
                        List<FrameEdge> adjEdges = edge.AdjacentElements(cwMullions).OfType<FrameEdge>().ToList() ;
                        if (adjEdges.Count > 0)
                            edge.FrameEdgeProperty = adjEdges[0].FrameEdgeProperty;
                        else
                            edge.FrameEdgeProperty = null;
                    }
                    bHoMOpening.Edges = edges;

                    result.Add(panel.FacadeOpeningFromRevit(settings, refObjects));
                }
            }
            
            return result;
        }

        /***************************************************/
    }
}
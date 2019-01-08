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

using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using BH.UI.Revit.Engine;

using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.oM.Adapters.Revit.Settings;

namespace BH.UI.Revit.Adapter
{
    public partial class RevitUIAdapter : BH.Adapter.Revit.InternalRevitAdapter
    {
        /***************************************************/
        /**** Protected Methods                         ****/
        /***************************************************/

        protected override bool Create<T>(IEnumerable<T> objects, bool replaceAll = false)
        {
            if (Document == null)
            {
                NullDocumentCreateError();
                return false;
            }

            if (objects == null)
            {
                NullObjectsCreateError();
                return false;
            }

            if (objects.Count() < 1)
                return false;

            Document aDocument = Document;

            bool aResult = false;
            if (!aDocument.IsModifiable && !aDocument.IsReadOnly)
            {
                //Transaction has to be opened
                using (Transaction aTransaction = new Transaction(aDocument, "Create"))
                {
                    aTransaction.Start();
                    aResult = Create(objects, aDocument, RevitSettings);
                    aTransaction.Commit();
                }
            }
            else
            {
                //Transaction is already opened
                aResult = Create(objects, aDocument, RevitSettings);
            }

            return aResult; ;
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static bool Create<T>(IEnumerable<T> objects, Document document, RevitSettings revitSettings) where T : IObject
        {
            string aTagsParameterName = revitSettings.GeneralSettings.TagsParameterName;

            PushSettings aPushSettings = new PushSettings()
            {
                Replace = revitSettings.GeneralSettings.Replace,
                ConvertUnits = true,
                CopyCustomData = true,
                FamilyLoadSettings = revitSettings.FamilyLoadSettings

            };

            for (int i = 0; i < objects.Count(); i++)
            {
                IBHoMObject aBHoMObject = objects.ElementAt<T>(i) as IBHoMObject;

                if (aBHoMObject == null)
                {
                    NullObjectCreateError(typeof(IBHoMObject));
                    continue;
                }

                if(aBHoMObject is Bar)
                {
                    ConvertBeforePushError(aBHoMObject, typeof(FramingElement));
                    continue;
                }

                try
                {
                    Element aElement = null;

                    string aUniqueId = BH.Engine.Adapters.Revit.Query.UniqueId(aBHoMObject);
                    if (!string.IsNullOrEmpty(aUniqueId))
                        aElement = document.GetElement(aUniqueId);

                    if (aElement == null)
                    {
                        int aId = BH.Engine.Adapters.Revit.Query.ElementId(aBHoMObject);
                        if (aId != -1)
                            aElement = document.GetElement(new ElementId(aId));
                    }

                    if (revitSettings.GeneralSettings.Replace && aElement != null)
                    {
                        if (aElement.Pinned)
                        {
                            DeletePinnedElementError(aElement);
                            continue;
                        }

                        document.Delete(aElement.Id);
                        aElement = null;
                    }

                    if (aElement == null)
                    {
                        Type aType = aBHoMObject.GetType();

                        if (aType != typeof(BHoMObject))
                        {
                            //if (aBHoMObject is oM.Architecture.Elements.Level || aBHoMObject is BuildingElement || aBHoMObject is BuildingElementProperties)
                            //    aElement = Create(aBHoMObject as dynamic, document, aPushSettings);
                            //else
                            //    aElement = BH.UI.Cobra.Engine.Convert.ToRevit(aBHoMObject as dynamic, document, aPushSettings);

                            aElement = BH.UI.Revit.Engine.Convert.ToRevit(aBHoMObject as dynamic, document, aPushSettings);

                            SetIdentifiers(aBHoMObject, aElement);
                        }

                    }
                    else
                    {
                        aElement = Modify.SetParameters(aElement, aBHoMObject);
                        if (aElement != null && aElement.Location != null)
                        {
                            try
                            {
                                Location aLocation = Modify.Move(aElement.Location, aBHoMObject as dynamic, aPushSettings);
                            }
                            catch(Exception aException)
                            {
                                ObjectNotMovedWarning(aBHoMObject);
                            }

                        }
                    }

                    //Assign Tags
                    if (aElement != null && !string.IsNullOrEmpty(aTagsParameterName))
                    {
                        Modify.SetTags(aElement, aBHoMObject, aTagsParameterName);
                    }
                }
                catch (Exception aException)
                {
                    ObjectNotCreatedCreateError(aBHoMObject);
                }
            }

            return true;
        }

        /***************************************************/

        private static void SetIdentifiers(IBHoMObject bHoMObject, Element element)
        {
            if (bHoMObject == null || element == null)
                return;

            SetCustomData(bHoMObject, BH.Engine.Adapters.Revit.Convert.ElementId, element.Id.IntegerValue);
            SetCustomData(bHoMObject, BH.Engine.Adapters.Revit.Convert.AdapterId, element.UniqueId);

            int aWorksetId = WorksetId.InvalidWorksetId.IntegerValue;
            if (element.Document != null && element.Document.IsWorkshared)
            {
                WorksetId aWorksetId_Revit = element.WorksetId;
                if (aWorksetId_Revit != null)
                    aWorksetId = aWorksetId_Revit.IntegerValue;
            }
            SetCustomData(bHoMObject, BH.Engine.Adapters.Revit.Convert.WorksetId, aWorksetId);

            Parameter aParameter = null;

            aParameter = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
            if (aParameter != null)
                SetCustomData(bHoMObject, BH.Engine.Adapters.Revit.Convert.FamilyName, aParameter.AsValueString());

            aParameter = element.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM);
            if (aParameter != null)
                SetCustomData(bHoMObject, BH.Engine.Adapters.Revit.Convert.FamilyTypeName, aParameter.AsValueString());

            aParameter = element.get_Parameter(BuiltInParameter.ELEM_CATEGORY_PARAM);
            if (aParameter != null)
                SetCustomData(bHoMObject, BH.Engine.Adapters.Revit.Convert.CategoryName, aParameter.AsValueString());
        }

        /***************************************************/

        private static void SetCustomData(IBHoMObject bHoMObject, string customDataName, object value)
        {
            if (bHoMObject == null || string.IsNullOrEmpty(customDataName))
                return;

            if (bHoMObject.CustomData.ContainsKey(customDataName))
                bHoMObject.CustomData[customDataName] = value;
            else
                bHoMObject.CustomData.Add(customDataName, value);
        }

        /***************************************************/

    }
}
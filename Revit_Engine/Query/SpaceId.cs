﻿using System.ComponentModel;

using BH.oM.Base;
using BH.oM.Reflection.Attributes;

namespace BH.Engine.Adapters.Revit
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("gets integer representation of Space ElementId (stored in CustomData) for given BHoMObject.")]
        [Input("bHoMObject", "BHoMObject")]
        [Output("ElementId")]
        public static int SpaceId(this IBHoMObject bHoMObject)
        {
            if (bHoMObject == null)
                return -1;

            object aValue = null;
            if (bHoMObject.CustomData.TryGetValue(Convert.SpaceId, out aValue))
            {
                if (aValue is string)
                {
                    int aInt = -1;
                    if (int.TryParse((string)aValue, out aInt))
                        return aInt;
                }
                else if (aValue is int)
                {
                    return (int)aValue;
                }
                else
                {
                    return -1;
                }
            }

            return -1;
        }

        /***************************************************/
    }
}
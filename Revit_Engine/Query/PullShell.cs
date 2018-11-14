﻿using BH.oM.DataManipulation.Queries;
using BH.oM.Reflection.Attributes;
using System.ComponentModel;

namespace BH.Engine.Adapters.Revit
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Returns true if FilterQuery shall pull shell from Revit Element")]
        [Input("filterQuery", "FilterQuery")]
        [Output("PullShell")]
        public static bool PullShell(this FilterQuery filterQuery)
        {
            if (filterQuery == null)
                return false;

            if (filterQuery.Equalities.ContainsKey(Convert.FilterQuery.PullShell))
            {
                object aObject = filterQuery.Equalities[Convert.FilterQuery.PullShell];
                if (aObject is bool)
                    return (bool)aObject;
            }

            return false;
        }

        /***************************************************/
    }
}
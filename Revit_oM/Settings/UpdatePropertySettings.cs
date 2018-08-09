﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BH.oM.Base;

namespace BH.oM.Adapters.Revit
{
    public class UpdatePropertySettings : BHoMObject
    {
        /***************************************************/
        /**** Public Properties                        ****/
        /***************************************************/

        public string ParameterName { get; set; } = null;

        public object Value { get; set; } = null;

        public bool ConvertUnits { get; set; } = true;

        /***************************************************/

        public static UpdatePropertySettings Default = new UpdatePropertySettings();
    }

    
}
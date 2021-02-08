﻿/*
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
using BH.Engine.Data;
using BH.oM.Adapters.Revit.Requests;
using BH.oM.Data.Requests;
using BH.oM.Reflection.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Adapters.Revit
{
    public static partial class Compute
    {
        /***************************************************/
        /****              Public Methods               ****/
        /***************************************************/

        //[Description("Groups and sorts IRequests by their estimated execution time in order to execute fastest first. Order from slowest to fastest: IParameterRequest, IlogicalRequests, others. ")]
        //[Input("requests", "A collection of IRequests to be sorted.")]
        //[Output("sortedRequests")]
        public static Dictionary<Document, IRequest> SplitRequestTreeByLinks(this IRequest request, Document document)
        {
            List<Document> linkDocs = new FilteredElementCollector(document).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().Select(x=>x.GetLinkDocument()).ToList();

            Dictionary<Document, IRequest> requestsByLinks = new Dictionary<Document, IRequest>();
            List<IRequest> splitPerDoc = request.SplitRequestTreeByType(typeof(FilterByLink));
            foreach (IRequest splitRequest in splitPerDoc)
            {
                if (!splitRequest.TryOrganizeByLink(document, linkDocs, requestsByLinks))
                    return null;
            }

            return requestsByLinks;
        }


        /***************************************************/
        /****              Private Methods              ****/
        /***************************************************/

        private static bool TryOrganizeByLink(this IRequest request, Document mainDocument, List<Document> linkDocuments, Dictionary<Document, IRequest> requestsByLinks)
        {
            if (request == null)
                return false;

            if (request is FilterByLink)
            {
                BH.Engine.Reflection.Compute.RecordError($"It is not allowed to pull from links without any filtering (IRequest of type {nameof(FilterByLink)} needs to be wrapped into a {nameof(LogicalAndRequest)} with at least one more request).");
                return false;
            }
            else if (!request.IsValidToOrganize())
                return false;

            List<IRequest> linkRequests = request.AllRequestsOfType(typeof(FilterByLink));
            if (linkRequests.Count == 0)
            {
                requestsByLinks.AddRequestByLink(request, mainDocument);
                return true;
            }
            else if (linkRequests.Count == 1)
            {
                if (request.AllRequestsOfType(typeof(SelectionRequest)).Count != 0)
                {
                    BH.Engine.Reflection.Compute.RecordError("It is not allowed to combine selection requests with link requests - Revit selection does not work with links.");
                    return false;
                }

                FilterByLink linkRequest = (FilterByLink)linkRequests[0];
                string linkName = linkRequest.LinkName.ToLower(); ;
                if (!linkName.EndsWith(".rvt"))
                {
                    BH.Engine.Reflection.Compute.RecordWarning($"Link name {linkName} inside a link request does not end with .rvt - the suffix has been added.");
                    linkName += ".rvt";
                }

                bool fullPath = linkName.Contains("\\");
                List<Document> validDocs;
                if (fullPath)
                    validDocs = linkDocuments.Where(x => x.PathName.ToLower() == linkName).ToList();
                else
                    validDocs = linkDocuments.Where(x => x.Title.ToLower() == linkName).ToList();

                if (validDocs.Count == 0)
                {
                    if (fullPath)
                        BH.Engine.Reflection.Compute.RecordError($"Active Revit document does not contain link under path {linkName}.");
                    else
                        BH.Engine.Reflection.Compute.RecordError($"Active Revit document does not contain link named {linkName}.");

                    return false;
                }
                else if (validDocs.Count != 1)
                {
                    BH.Engine.Reflection.Compute.RecordError($"There is more than one link document named {linkName} - please use full link path in request instead of link name to pull.");
                    return false;
                }

                Document linkDoc = validDocs[0];

                request.RemoveSubRequest(linkRequest);
                request = request.SimplifyRequestTree();
                requestsByLinks.AddRequestByLink(request, linkDoc);

                return true;
            }
            else
                return false;
        }

        /***************************************************/

        private static void AddRequestByLink(this Dictionary<Document, IRequest> requestsByLinks, IRequest request, Document document)
        {
            if (requestsByLinks.ContainsKey(document))
            {
                IRequest requestByLink = requestsByLinks[document];
                if (requestByLink is LogicalOrFilter)
                    ((LogicalOrRequest)requestByLink).Requests.Add(request);
                else
                {
                    LogicalOrRequest newHead = new LogicalOrRequest();
                    newHead.Requests.Add(requestByLink);
                    newHead.Requests.Add(request);
                    requestsByLinks[document] = newHead;
                }
            }
            else
                requestsByLinks[document] = request;
        }

        /***************************************************/

        private static bool RemoveSubRequest(this IRequest requestTree, IRequest toRemove)
        {
            if (requestTree is LogicalNotRequest)
            {
                LogicalNotRequest logical = (LogicalNotRequest)requestTree;
                if (logical.Request == toRemove)
                {
                    logical.Request = null;
                    return true;
                }
                else
                    return logical.Request.RemoveSubRequest(toRemove);
            }
            else if (requestTree is ILogicalRequest)
            {
                ILogicalRequest logical = (ILogicalRequest)requestTree;
                List<IRequest> subRequests = logical.IRequests();
                if (subRequests.Contains(toRemove))
                {
                    subRequests.Remove(toRemove);
                    return true;
                }
                else
                {
                    foreach (IRequest subRequest in subRequests)
                    {
                        bool removed = subRequest.RemoveSubRequest(toRemove);
                        if (removed)
                            return true;
                    }
                }
            }

            return false;
        }

        /***************************************************/

        private static bool IsValidToOrganize(this IRequest request)
        {
            if (request is LogicalNotRequest)
            {
                IRequest subRequest = ((LogicalNotRequest)request).Request;
                if (subRequest == null)
                    return false;

                if (subRequest.GetType() == typeof(FilterByLink))
                {
                    BH.Engine.Reflection.Compute.RecordError($"It is not allowed to wrap IRequests of type {nameof(FilterByLink)} into a {nameof(LogicalNotRequest)} request.");
                    return false;
                }
                else if (subRequest.AllRequestsOfType(typeof(LogicalNotRequest)).Count != 0)
                {
                    BH.Engine.Reflection.Compute.RecordError($"A chain of nested {nameof(LogicalNotRequest)}s has been detected, which is not allowed.");
                    return false;
                }
                else if (subRequest.AllRequestsOfType(typeof(FilterByLink)).Count != 0)
                {
                    BH.Engine.Reflection.Compute.RecordError($"A {nameof(FilterByLink)} nested in {nameof(LogicalNotRequest)} has been detected, which is not allowed.");
                    return false;
                }
                else
                    return subRequest.IsValidToOrganize();
            }
            else if (request is ILogicalRequest)
            {
                List<IRequest> subRequests = ((ILogicalRequest)request).IRequests();
                if (subRequests.Any(x => x.GetType() == typeof(FilterByLink))
                    && (request is LogicalOrRequest || (request is LogicalAndRequest && subRequests.Count == 1)))
                {
                    BH.Engine.Reflection.Compute.RecordError($"It is not allowed to pull from links without any filtering (IRequest of type {nameof(FilterByLink)} needs to be wrapped into a {nameof(LogicalAndRequest)}).");
                    return false;
                }

                return subRequests.All(x => x.IsValidToOrganize());
            }

            return true;
        }

        /***************************************************/
    }
}
//-----------------------------------------------------------------------
// <copyright file="ResourceAccessHandler.cs" company="Rare Crowds Inc.">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using DataAccessLayer;

namespace ResourceAccess
{
    /// <summary>Class to handle resource access checking.</summary>
    public class ResourceAccessHandler : IResourceAccessHandler
    {
        /// <summary>Initializes a new instance of the <see cref="ResourceAccessHandler"/> class.</summary>
        /// <param name="userAccessRepository">The user access repository.</param>
        /// <param name="entityRepository">The entiry repository.</param>
        public ResourceAccessHandler(IUserAccessRepository userAccessRepository, IEntityRepository entityRepository)
        {
            this.UserAccessRepository = userAccessRepository;
            this.EntityRepository = entityRepository;
        }

        /// <summary>Gets UserAccessRepository.</summary>
        internal IUserAccessRepository UserAccessRepository { get; private set; }

        /// <summary>Gets EntityRepository.</summary>
        internal IEntityRepository EntityRepository { get; private set; }

        /// <summary>Check whether a user has access to a resource.</summary>
        /// <param name="canonicalResource">A canonical resource object.</param>
        /// <param name="userEntityId">An authenticated user entity id.</param>
        /// <returns>True if access is granted.</returns>
        public bool CheckAccess(CanonicalResource canonicalResource, EntityId userEntityId)
        {
            // No user, no access
            if (userEntityId == null)
            {
                return false;
            }

            // TODO: Remove this once the service namespaces have been removed
            // Right now if the resource is not canonical we grant access as long
            // as there is an authenticated user.
            if (!canonicalResource.IsCanonical)
            {
                return true;
            }
            
            // Get the access list from the repository
            var accessList = this.UserAccessRepository.GetUserAccessList(userEntityId).ToList();

            // Check for a match and if it is, validate the resource chain is parent/child hierarchy
            if (this.MatchAccess(canonicalResource, accessList, false) && this.ValidateResourceChain(canonicalResource))
            {
                return true;
            }

            return false;
        }

        /// <summary>Check if a resource has global access rights.</summary>
        /// <param name="canonicalResource">A canonical resource object.</param>
        /// <returns>True if global access is granted.</returns>
        public bool CheckGlobalAccess(CanonicalResource canonicalResource)
        {
            // TODO: load this from config
            var accessList = new List<string>
                {
                    "ROOT:#:GET",
                    "*.html:#:GET:*", // allows user access to any .html page
                    "LogOff.aspx:#:GET:*",
                    "userverification.html:#:GET:*",
                    "home.html:#:GET:*",
                    "ping.html:#:GET:*",
                    "favicon.ico:#:GET:*",
                    "css:*:#:GET:*",
                    "dhtml:*:#:GET:*",
                    "images:*:#:GET:*",
                    "jquery:*:#:GET:*",
                    "scripts:*:#:GET:*",
                    "federationmetadata:*:#:GET:*",
                    "api/data:*:#:GET:*",
                };

            return this.MatchAccess(canonicalResource, accessList, true);
        }

        /// <summary>Return an array of EnityId'sbased on the user's access list</summary>
        /// <param name="userEntityId">User's Entity Id</param>
        /// <returns>Array of Company Ids.</returns>
        public EntityId[] GetUserCompanyByAccessList(EntityId userEntityId)
        {
            var accessList = this.UserAccessRepository.GetUserAccessList(userEntityId).ToList();

            // get the companies from the list
            var accessListCompanies = accessList.Where(c => c.Contains("COMPANY") && !c.Contains("COMPANY:*"));

            if (accessListCompanies.Any())
            {
                var resourceChain = CanonicalResource.ExtractResourceList(accessListCompanies.FirstOrDefault());
                return new EntityId[] { resourceChain[1] };
            }
            else
            {
                return new EntityId[0];
            }
        }

        /// <summary>Check if a resource has access rights.</summary>
        /// <param name="canonicalResource">A canonical resource object.</param>
        /// <param name="accessList">The access list.</param>
        /// <param name="checkGlobalAccess">True if we are only checking for global access.</param>
        /// <returns>True if access is granted.</returns>
        internal bool MatchAccess(CanonicalResource canonicalResource, List<string> accessList, bool checkGlobalAccess)
        {
            // Check for simple exact match first
            if (CheckExactMatch(canonicalResource, accessList))
            {
                return true;
            }

            if (this.CheckInheritedMatch(canonicalResource, accessList, checkGlobalAccess))
            {
                return true;
            }

            return false;
        }

        /// <summary>Determine whether a resource matches a given access descriptor.</summary>
        /// <param name="canonicalResource">The canonical resource to check.</param>
        /// <param name="accessList">The access list.</param>
        /// <returns>True if this is a match.</returns>
        private static bool CheckExactMatch(CanonicalResource canonicalResource, IEnumerable<string> accessList)
        {
            var canonicalDescriptor = canonicalResource.CanonicalDescriptor;
            return accessList.Any(a => IsStringMatch(a, canonicalDescriptor));
        }

        /// <summary>
        /// Given an access descriptor string and a list of resourceDescriptor tokens build a
        /// list of accessDescriptor tokens that is expanded to have a 1:1 correspondance
        /// with the resourceDescriptor tokens for safer comparison.
        /// </summary>
        /// <param name="accessDescriptorTokensIn">The access descriptor tokens.</param>
        /// <param name="resourceDescriptorTokens">The resource descriptor tokens.</param>
        /// <returns>A list of accessDescriptor tokens.</returns>
        private static List<string> ExpandAccessDescriptorTokens(IList<string> accessDescriptorTokensIn, IList<string> resourceDescriptorTokens)
        {
            var accessDescriptorTokens = new string[resourceDescriptorTokens.Count()];
            var wildCardEncountered = false;

            for (var i = 0; i < resourceDescriptorTokens.Count(); i++)
            {
                // By default, an unspecified access descriptor token will be empty string. If this is
                // a resource chain and we have encountered a wildcard fill to end with the wildcard.
                accessDescriptorTokens[i] = wildCardEncountered ? CanonicalResource.WildCard : string.Empty;

                if (accessDescriptorTokensIn.Count <= i)
                {
                    continue;
                }

                accessDescriptorTokens[i] = accessDescriptorTokensIn[i];

                // Toggle as a sticky flag
                if (IsWildcard(accessDescriptorTokensIn[i]))
                {
                    wildCardEncountered = true;
                }
            }

            return accessDescriptorTokens.ToList();
        }

        /// <summary>
        /// Determine if a list of tokens from a canonical resource descriptor is a match for
        /// a list of tokens from an access descriptor.
        /// </summary>
        /// <param name="accessDescriptorTokens">
        /// The access descriptor tokens. Must be the same length as resourceDescriptor list.
        /// </param>
        /// <param name="resourceDescriptorTokens">The resource descriptor tokens.</param>
        /// <returns>True if they are a match.</returns>
        private static bool CompareTokenLists(IList<string> accessDescriptorTokens, IList<string> resourceDescriptorTokens)
        {
            var accessMatch = true;

            for (var i = 0; i < resourceDescriptorTokens.Count(); i++)
            {
                var accessToken = accessDescriptorTokens[i];
                if (IsWildcard(accessToken))
                {
                    // If this is a resource chain we don't need to go any futhur, but that is not the case
                    // for the Action and Message which are independent - so we keep going.
                    continue;
                }

                var resourceToken = resourceDescriptorTokens[i];
                if (CompareTokens(resourceToken, accessToken))
                {
                    // Exact match to this point, keep going.
                    continue;
                }

                if (MatchWithWildcard(resourceToken, accessToken))
                {
                    continue;
                }

                // Match failed
                accessMatch = false;
                break;
            }

            return accessMatch;
        }

        /// <summary>
        /// Determine if a list of tokens from a canonical resource descriptor entity id contains a match from
        /// a list of tokens from an access descriptor.
        /// </summary>
        /// <param name="accessDescriptorTokens">
        /// The access descriptor tokens. Must be the same length as resourceDescriptor list.
        /// </param>
        /// <param name="resourceDescriptorTokens">The resource descriptor tokens.</param>
        /// <returns>True if there is an entity id match.</returns>
        private static bool ComparePartialTokenListsForEntityId(IList<string> accessDescriptorTokens, IList<string> resourceDescriptorTokens)
        {
            for (var i = 0; i < resourceDescriptorTokens.Count(); i++)
            {
                var accessToken = accessDescriptorTokens[i];
                var resourceToken = resourceDescriptorTokens[i];
                var parsedGuid = new Guid();
                if (Guid.TryParse(resourceToken, out parsedGuid) && CompareTokens(resourceToken, accessToken))
                {
                    // Exact match
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if a list of resource tokens from a canonical resource descriptor is a match for
        /// a list of tokens from an access descriptor.
        /// </summary>
        /// <param name="accessResourceTokens">The access descriptor tokens.</param>
        /// <param name="resourceTokens">The resource descriptor tokens.</param>
        /// <returns>True if they are a match.</returns>
        private static bool CompareResourceTokenLists(IList<string> accessResourceTokens, IList<string> resourceTokens)
        {
            // Expand the access resource tokens to align 1:1 with the resource tokens for easier direct comparison
            var expandedAccessResourceTokens = ExpandAccessDescriptorTokens(accessResourceTokens, resourceTokens);

            // Determine if resource chain matches
            return CompareTokenLists(expandedAccessResourceTokens, resourceTokens);
        }

        /// <summary>
        /// Determine if a list of resource tokens from a canonical resource descriptor contains a match for
        /// a list of tokens from an access descriptor.
        /// </summary>
        /// <param name="accessResourceTokens">The access descriptor tokens.</param>
        /// <param name="resourceTokens">The resource descriptor tokens.</param>
        /// <returns>True if there is a match.</returns>
        private static bool IsAccessResourceInDerived(IList<string> accessResourceTokens, IList<string> resourceTokens)
        {
            // Expand the access resource tokens to align 1:1 with the resource tokens for easier direct comparison
            var expandedAccessResourceTokens = ExpandAccessDescriptorTokens(accessResourceTokens, resourceTokens);

            // Determine if an entity id in the resource chain matches
            return ComparePartialTokenListsForEntityId(expandedAccessResourceTokens, resourceTokens);
        }

        /// <summary>Try to get an entity id from a resource id string.</summary>
        /// <param name="resourceId">The resource id.</param>
        /// <returns>An entity id or null if it is not a valid id.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "TryGet pattern.")]
        private static EntityId TryBuildEntityId(string resourceId)
        {
            try
            {
                return new EntityId(resourceId);
            }
            catch
            {
            }

            return null;
        }

        /// <summary>Compare two descriptor tokens</summary>
        /// <param name="resourceToken">The resource token.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns>True if the tokens match.</returns>
        private static bool CompareTokens(string resourceToken, string accessToken)
        {
            return IsStringMatch(accessToken, resourceToken);
        }

        /// <summary>Check if a token is a wildcard.</summary>
        /// <param name="token">The token.</param>
        /// <returns>True if the token is a wildcard.</returns>
        private static bool IsWildcard(string token)
        {
            return IsStringMatch(token, CanonicalResource.WildCard);
        }

        /// <summary>Check if a token matches a value with a wildcard.</summary>
        /// <param name="resourceToken">The resource token.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns>True if the token matches wildcard.</returns>
        private static bool MatchWithWildcard(string resourceToken, string accessToken)
        {
            var regex = new Regex(WildcardToRegex(accessToken), RegexOptions.IgnoreCase);
            return regex.IsMatch(resourceToken);
        }

        /// <summary>
        /// WildcardToRegex returns a regular expression when for the passed in string that has a wildcard star (*)
        /// </summary>
        /// <param name="pattern">pattern to search</param>
        /// <returns>regualr expression to be used for a wildcard match</returns>
        private static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        /// <summary>Compare two strings.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        /// <returns>True if the strings are equal.</returns>
        private static bool IsStringMatch(string a, string b)
        {
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>Determine if an entity id refers to a child of a given entity.</summary>
        /// <param name="candidateChildId">The candidate child id.</param>
        /// <param name="parentEntity">The parent entity.</param>
        /// <returns>True if the entity is a child.</returns>
        private static bool IsChildEntity(EntityId candidateChildId, IEntity parentEntity)
        {
            if (parentEntity.Associations.Any(
                a => a.TargetEntityId == candidateChildId && a.AssociationType == AssociationType.Child))
            {
                return true;
            }

            // TODO: Remove when we start saving as child associations. For now we give it a pass
            if (parentEntity.Associations.Any(
                a => a.TargetEntityId == candidateChildId && a.AssociationType == AssociationType.Relationship))
            {
                return true;
            }

            return false;
        }

        /// <summary>Check if a resource is granted inherited access</summary>
        /// <param name="canonicalResource">The canonical resource to check.</param>
        /// <param name="accessList">The access list.</param>
        /// <param name="checkGlobalAccess">True if we are only checking for global access.</param>
        /// <returns>True if access granted.</returns>
        private bool CheckInheritedMatch(CanonicalResource canonicalResource, IList<string> accessList, bool checkGlobalAccess)
        {
            // Extract the canonical resource components
            var resourceTokens = CanonicalResource.ExtractResourceList(canonicalResource.CanonicalDescriptor);
            var resourceActions = new List<string> { canonicalResource.Action };
            var resourceMessages = new List<string> { canonicalResource.Message };

            // Reduce the accessList by filtering Actions that do not match
            var reducedAccessList = accessList.Where(
                    a => CompareTokenLists(new List<string> { CanonicalResource.ExtractAction(a) }, resourceActions)).ToList();

            // Reduce the accessList by filtering Messages that do not match
            reducedAccessList = reducedAccessList.Where(
                a => CompareTokenLists(new List<string> { CanonicalResource.ExtractMessage(a) }, resourceMessages)).ToList();

            // Convert the access desciptors into a list of lists of access resource tokens
            var accessResourceTokenLists = reducedAccessList.Select(a => CanonicalResource.ExtractResourceList(a)).ToList();

            // Look for a direct match between the access descriptors and the resource chain.
            foreach (var accessResourceTokens in accessResourceTokenLists)
            {
                if (CompareResourceTokenLists(accessResourceTokens, resourceTokens))
                {
                    return true;
                }
            }

            // For global resource checks we don't go further than this.
            if (checkGlobalAccess)
            {
                return false;
            }

            // Look for an indirect match among access descriptors that refer to parents of the resource chain.
            // This is expensive so it is done after all other checks have failed.
            foreach (var accessResourceTokens in accessResourceTokenLists)
            {
                // Construct a resource chain that includes the parent from the access tokens if possible
                var derivedResourceTokens = this.BuildDerivedResourceList(accessResourceTokens, resourceTokens);
                if (derivedResourceTokens == null)
                {
                    continue;
                }

                if (CompareResourceTokenLists(accessResourceTokens, derivedResourceTokens))
                {
                    return true;
                }
                
                if (IsAccessResourceInDerived(accessResourceTokens, derivedResourceTokens))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Given a list of access resource tokens and resource tokens, determine if we can derive
        /// a resource chain leading back to a parent in the access resource tokens.
        /// </summary>
        /// <param name="accessResourceTokens">The access descriptor tokens.</param>
        /// <param name="resourceTokens">The resource descriptor tokens.</param>
        /// <returns>The derived access resource list if found.</returns>
        private List<string> BuildDerivedResourceList(IList<string> accessResourceTokens, IList<string> resourceTokens)
        {
            // For now, only check head entity of the access resource list
            var headAccessResourceEntityId = accessResourceTokens.Select(TryBuildEntityId).FirstOrDefault(e => e != null);

            // Get the first valid entity id in the requested resource chain
            var headResourceEntityId = resourceTokens.Select(TryBuildEntityId).FirstOrDefault(e => e != null);

            if (headAccessResourceEntityId == null || headResourceEntityId == null)
            {
                return null;
            }

            var context = new RequestContext { ExternalCompanyId = headAccessResourceEntityId };
            var headEntity = this.EntityRepository.TryGetEntity(context, headAccessResourceEntityId);

            if (headEntity == null)
            {
                return null;
            }

            // Assume the head entity is a company for purposes of matching with the access descriptor
            if (IsChildEntity(headResourceEntityId, headEntity))
            {
                // Create a new list of derived resource tokens (leaving the old one intact).
                var derivedResourceTokens = resourceTokens.Select(token => token).ToList();
                derivedResourceTokens.Insert(0, headAccessResourceEntityId);
                derivedResourceTokens.Insert(0, "COMPANY");
                return derivedResourceTokens;
            }

            return null;
        }

        /// <summary>Determine that the resource chain has legitimate parent/child associations.</summary>
        /// <param name="canonicalResource">A canonical resource.</param>
        /// <returns>True if the resouce chain is valid.</returns>
        private bool ValidateResourceChain(CanonicalResource canonicalResource)
        {
            var resourceChain = CanonicalResource.ExtractResourceList(canonicalResource.CanonicalDescriptor);

            // TODO: consider making this method part of a mockable class
            // TODO: generalize for longer resource chains
            // Currently this only applies for resource chains of two entities
            if (resourceChain.Count != 4)
            {
                return true;
            }

            // If either resource has a wildcard parent/child is not meaningful
            if (IsWildcard(resourceChain[1]) || IsWildcard(resourceChain[3]))
            {
                return true;
            }

            var companyEntityId = TryBuildEntityId(resourceChain[1]);
            var childEntityId = TryBuildEntityId(resourceChain[3]);
            if (companyEntityId == null || childEntityId == null)
            {
                // Should have been an entity id
                return false;
            }

            var context = new RequestContext { ExternalCompanyId = companyEntityId };
            var companyEntity = this.EntityRepository.TryGetEntity(context, companyEntityId);
            if (companyEntity == null)
            {
                return false;
            }

            return IsChildEntity(childEntityId, companyEntity);
        }
    }
}
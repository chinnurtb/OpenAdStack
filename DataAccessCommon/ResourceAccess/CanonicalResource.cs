//-----------------------------------------------------------------------
// <copyright file="CanonicalResource.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DataAccessLayer;
using Diagnostics;

namespace ResourceAccess
{
    /// <summary>Class to encapsulate a resource Uri in a canonical form.</summary>
    public class CanonicalResource
    {
        /// <summary>WildCard string</summary>
        internal static readonly string WildCard = "*";

        /// <summary>Token delimiter string</summary>
        internal static readonly string TokenDelimiter = ":";

        /// <summary>Descriptor delimiter string</summary>
        internal static readonly string DescriptorDelimiter = ":#:";

        /// <summary>ROOT namespace</summary>
        internal static readonly string RootNamespace = "ROOT";

        /// <summary>Initializes a new instance of the <see cref="CanonicalResource"/> class.</summary>
        /// <param name="resourceUri">The resource uri.</param>
        /// <param name="action">The action being performed with the resource uri.</param>
        public CanonicalResource(Uri resourceUri, string action)
        {
            this.ResourceUri = resourceUri;
            this.Action = action;
        }

        /// <summary>Enum of application types.</summary>
        private enum Application
        {
            /// <summary>Web site uri.</summary>
            Web,

            /// <summary>Api uri.</summary>
            Api
        }

        /// <summary>Gets a value indicating whether this is an api resource.</summary>
        public bool IsApiResource
        {
            get { return this.GetApplication() == Application.Api; }
        }

        /// <summary>Gets Uri.</summary>
        public Uri ResourceUri { get; private set; }

        /// <summary>Gets Action.</summary>
        public string Action { get; private set; }

        /// <summary>Gets the first Message element.</summary>
        public string Message
        {
            get { return this.BuildMessage(); }
        }

        /// <summary>Gets CanonicalDescriptor.</summary>
        public string CanonicalDescriptor
        {
            get { return this.BuildCanonicalDescriptor(); }
        }

        /// <summary>Gets a value indicating whether the resource can be represented canonically.
        /// TODO: Make this go away 
        /// </summary>
        internal bool IsCanonical
        {
            get
            {
                // Non-api resources are considered canonical
                if (!this.IsApiResource)
                {
                    return true;
                }

                // To be a canonical api resource it must include the entity service namespace
                return this.ResourceUri.AbsolutePath.StartsWith("/api/entity", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Factory method for a CanonicalResource.</summary>
        /// <param name="resource">Resource uri string.</param>
        /// <param name="action">The action being performed with the resource uri.</param>
        /// <returns>A Canonical resource or null if resource is not well-formed.</returns>
        public static CanonicalResource BuildCanonicalResource(string resource, string action)
        {
            // TODO: What URI validation is appropriate here given that we were able to get this far?
            if (!Uri.IsWellFormedUriString(resource, UriKind.Absolute))
            {
                var msg = string.Format(
                    CultureInfo.InvariantCulture, "Resource uri is not a well-formed absolute uri: {0}", resource);
                LogManager.Log(LogLevels.Warning, msg);
                return null;
            }

            // TODO: How much validation is needed?
            if (string.IsNullOrEmpty(action))
            {
                var msg = string.Format(
                    CultureInfo.InvariantCulture, "Resource action is invalid: {0}", action);
                LogManager.Log(LogLevels.Warning, msg);
                return null;
            }

            return new CanonicalResource(BuildUri(resource), action);
        }

        /// <summary>Get an entity id associated with a namespace in the resource</summary>
        /// <param name="resourceNamespace">The resource namespace.</param>
        /// <returns>The entity id or null if not found.</returns>
        public EntityId ExtractEntityId(string resourceNamespace)
        {
            var resourceList = ExtractResourceList(this.CanonicalDescriptor);
            var namespaceIndex = resourceList.IndexOf(resourceNamespace);
            if (resourceList.Count() <= namespaceIndex + 1)
            {
                return null;
            }

            var entityId = resourceList[namespaceIndex + 1];
            return !IsEntityId(entityId) ? null : new EntityId(entityId);
        }

        /// <summary>Extracts a list of resource fragment strings from a canonical descriptor.</summary>
        /// <param name="canonicalDescriptor">A canonical descriptor string.</param>
        /// <returns>The list of resource fragments.</returns>
        internal static IList<string> ExtractResourceList(string canonicalDescriptor)
        {
            // The tokens before the :#: define the resource chain
            var resourceDescriptor = SplitDescriptor(canonicalDescriptor, true);
            return SplitTokens(resourceDescriptor);
        }

        /// <summary>Extracts the action from a canonical descriptor string.</summary>
        /// <param name="canonicalDescriptor">A canonical descriptor string.</param>
        /// <returns>The action.</returns>
        internal static string ExtractAction(string canonicalDescriptor)
        {
            // The tokens after the :#: are the action and message
            var modifierDescriptor = SplitDescriptor(canonicalDescriptor, false);

            // The first modifier is the action
            return SplitTokens(modifierDescriptor).First();
        }

        /// <summary>Extracgts the message from a canonical descriptor string.</summary>
        /// <param name="canonicalDescriptor">A canonical descriptor string.</param>
        /// <returns>The message.</returns>
        internal static string ExtractMessage(string canonicalDescriptor)
        {
            // The tokens after the :#: are the action and message
            var modifierDescriptor = SplitDescriptor(canonicalDescriptor, false);

            // The second modifer is the query
            var modifiers = SplitTokens(modifierDescriptor);
            return modifiers.Count <= 1 ? string.Empty : modifiers[1];
        }

        /// <summary>Split a string based on the token delimiter.</summary>
        /// <param name="tokens">The token string.</param>
        /// <returns>The list of delimited elements.</returns>
        private static List<string> SplitTokens(string tokens)
        {
            return tokens.Split(new[] { TokenDelimiter }, StringSplitOptions.None).ToList();
        }

        /// <summary>Split a string based on the descriptor delimiter.</summary>
        /// <param name="descriptor">The descriptor string.</param>
        /// <param name="first">True to return the first half of descriptor.</param>
        /// <returns>The list of elements.</returns>
        private static string SplitDescriptor(string descriptor, bool first)
        {
            if (string.IsNullOrEmpty(descriptor))
            {
                return string.Empty;
            }

            var descriptorParts = descriptor.Split(new[] { DescriptorDelimiter }, StringSplitOptions.None).ToList();

            if (first)
            {
                return descriptorParts[0];
            }

            if (descriptorParts.Count <= 1)
            {
                return string.Empty;
            }

            return descriptorParts[1];
        }

        /// <summary>Build an absolute uri from the resource string.</summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The Uri.</returns>
        private static Uri BuildUri(string resource)
        {
            var uri = new Uri(resource, UriKind.Absolute);
            return uri;
        }

        /// <summary>Split a path delimited by '/' into a list of non-empty elements.</summary>
        /// <param name="path">The path string.</param>
        /// <returns>The list of elements.</returns>
        private static List<string> SplitPath(string path)
        {
            return path.Split('/').Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        }

        /// <summary>Test if the element is an entity id.</summary>
        /// <param name="resourceElement">The resource element.</param>
        /// <returns>True if this is an Id.</returns>
        private static bool IsEntityId(string resourceElement)
        {
            try
            {
                // If this can be parsed as an entity id get the canonical representation.
                return !string.IsNullOrEmpty(new EntityId(resourceElement));
            }
            catch (ArgumentException)
            {
            }

            return false;
        }

        /// <summary>Test if the element is a file.</summary>
        /// <param name="resourceElement">The resource element.</param>
        /// <returns>True if this is a file.</returns>
        private static bool IsFile(string resourceElement)
        {
            // Current rather naive criteria for a file resource is the presence of an extension.
            // Otherwise it will be treated as a namespace
            return resourceElement.Contains('.');
        }

        /// <summary>Test if the element is a namespace.</summary>
        /// <param name="resourceElement">The resource element.</param>
        /// <returns>True if this is a namespace.</returns>
        private static bool IsNamespace(string resourceElement)
        {
            // Right now anything that isn't an id or a file is treated as a namespace (including empty string)
            return !IsEntityId(resourceElement) && !IsFile(resourceElement);
        }
        
        /// <summary>Build a canonical resource path fragment from a raw resource path input.</summary>
        /// <param name="resourcePathFragment">The resource path fragment.</param>
        /// <returns>A canonical resource path fragment.</returns>
        private static string BuildCanonicalResourceFragment(string resourcePathFragment)
        {
            // Entity id's should be changed to canonical form. All others are unchanged.
            if (IsEntityId(resourcePathFragment))
            {
                return new EntityId(resourcePathFragment);
            }

            return resourcePathFragment;
        }

        /// <summary>Build a resource portion of a descriptor for a uri with at most two one sub-namespace.</summary>
        /// <returns>A resource descriptor string.</returns>
        private string BuildResourceDescriptor()
        {
            // Get the resource elements present in the Uri.
            var resourceFragments = SplitPath(this.ExtractResourcePath());

            // Build an list of canonical resource path elements
            var canonicalResourceFragments = resourceFragments.Select(
                resourceInput => BuildCanonicalResourceFragment(resourceInput)).ToList();

            // If the last element in the chain is a namespace, wildcard access is
            // implicitly being requested except for ROOT namespace. ROOT namespace is a special case. 
            // We need the marker to distinguish it from empty string or wildcard.
            if (!canonicalResourceFragments.Any())
            {
                return RootNamespace;
            }

            if (IsNamespace(canonicalResourceFragments.Last()))
            {
                canonicalResourceFragments.Add(WildCard);
            }

            return string.Join(TokenDelimiter, canonicalResourceFragments).ToUpperInvariant();
        }

        /// <summary>Build a canonical descriptor from a resource Uri and Action.</summary>
        /// <returns>A string representing the resource in canonical form.</returns>
        private string BuildCanonicalDescriptor()
        {
            // Start with the resource
            var sb = new StringBuilder(this.BuildResourceDescriptor());

            // Delimit the resource from the action/query
            sb.Append(DescriptorDelimiter);

            // Append the action
            sb.Append(this.Action);

            // Append the query if present
            if (!string.IsNullOrEmpty(this.Message))
            {
                sb.Append(TokenDelimiter);
                sb.Append(this.Message);
            }

            return sb.ToString().ToUpperInvariant();
        }

        /// <summary>Get at most one query element (which will message if present).</summary>
        /// <returns>The message name, or empty string if not found.</returns>
        private string BuildMessage()
        {
            if (string.IsNullOrEmpty(this.ResourceUri.Query))
            {
                return string.Empty;
            }

            // Get the first non-empty thing after the ?
            var queryString = this.ResourceUri.Query.Split('?')
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

            if (string.IsNullOrEmpty(queryString))
            {
                return string.Empty;
            }

            // To correctly extract message will require something of the form
            // ?message=name. However, this should be safe even if it is malformed.
            var messageName = queryString.Split('&').First().Split('=').Last();
            
            return messageName.Trim().ToUpperInvariant();
        }

        /// <summary>Extract the resource component of the uri (without the application).</summary>
        /// <returns>The resource string.</returns>
        private string ExtractResourcePath()
        {
            var pathElements = SplitPath(this.ResourceUri.AbsolutePath);
            if (this.IsApiResource)
            {
                pathElements.RemoveAt(0);

                // TODO: Remove this once we eliminate the entity service namespace.
                // TODO: We handle non-entity service namespaces at all so we wouldn't get this far.
                if (pathElements.Any())
                {
                    if (string.Compare(pathElements[0], "entity", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        pathElements.RemoveAt(0);
                    }
                }
            }

            return string.Join("/", pathElements);
        }

        /// <summary>Get the application type of the resource (e.g. - web or api).</summary>
        /// <returns>An Application enum value.</returns>
        private Application GetApplication()
        {
            if (this.ResourceUri.AbsolutePath.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                return Application.Api;
            }

            return Application.Web;
        }
    }
}
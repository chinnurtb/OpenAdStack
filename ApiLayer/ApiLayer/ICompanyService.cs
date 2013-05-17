// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICompanyService.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ApiLayer
{
    /// <summary>
    /// This is an interface that exposes interaction with company entity
    /// </summary>
    public interface ICompanyService
    {
        /// <summary>
        /// Creates a new company
        /// </summary>
        /// <param name="jsonCompanyValue">json string value containing new company values</param>
        /// <returns>new Company</returns>
        Stream CreateCompany(Stream jsonCompanyValue);

        /// <summary>
        /// Returns an existing company
        /// </summary>
        /// <returns>the Company(s)</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This is not a property, it really is a method.")]
        Stream GetCompany();

        /// <summary>
        /// Return an existing company based on the entityid 
        /// </summary>
        /// <param name="id">entity id of the company</param>
        /// <returns>the company</returns>
        Stream GetCompanyByEntity(string id);

        /// <summary>
        /// Updates an existing company
        /// </summary>
        /// <param name="id">company id</param>
        /// <param name="jsonCompanyValue">json string value containing updated company values</param>
        /// <returns>the updated Company</returns>
        string Update(string id, string jsonCompanyValue);
    }
}
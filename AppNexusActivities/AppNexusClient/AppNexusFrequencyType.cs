//-----------------------------------------------------------------------
// <copyright file="AppNexusFrequencyType.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace AppNexusClient
{
    /// <summary>
    /// Types of impression frequency limits available for AppNexus exports
    /// </summary>
    /// <seealso href="https://wiki.appnexus.com/display/api/Frequency+for+the+API"/>
    /// <seealso href="https://wiki.appnexus.com/display/api/Profile+Service#ProfileService-Frequency"/>
    public enum AppNexusFrequencyType
    {
        /// <summary>
        /// Maximum number of impressions per lifetime
        /// </summary>
        /// <remarks>
        /// AppNexus Profile Value: max_lifetime_imps
        /// AppNexus Default: unlimited (255)
        /// </remarks>
        Lifetime,

        /// <summary>
        /// Maximum number of impressions per session
        /// </summary>
        /// <remarks>
        /// AppNexus Profile Value: max_session_imps
        /// AppNexus Default: unlimited (255)
        /// </remarks>
        Session,

        /// <summary>
        /// Maximum number of impressions per day
        /// </summary>
        /// <remarks>
        /// AppNexus Profile Value: max_day_imps
        /// AppNexus Default: unlimited (255)
        /// </remarks>
        Day,

        /// <summary>
        /// Minimum number of minutes between impressions per person
        /// </summary>
        /// <remarks>
        /// AppNexus Profile Value: min_minutes_per_imp
        /// AppNexus Default: 0
        /// </remarks>
        Minutes
    }
}

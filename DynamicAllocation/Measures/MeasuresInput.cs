// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MeasuresInput.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DynamicAllocation
{
    /// <summary>
    /// represents the measure input expected from the UI
    /// </summary>
    public class MeasuresInput
    {
        /// <summary>
        /// Gets or sets Measure: the measure
        /// </summary>
        public long Measure { get; set; }

        /// <summary>
        /// Gets or sets base Valuation
        /// </summary>
        public int Valuation { get; set; }

        /// <summary>
        /// Gets or sets Group: the OR Group the measure belongs to (may be null if it belongs to its own group)
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the measure is pinned
        /// TODO: someone has to enforce that all the measures in an OR group are pinned if any of them are. is that us?
        /// </summary>
        public bool Pinned { get; set; }

        /// <summary>
        /// implements ==
        /// </summary>
        /// <param name="left">the left</param>
        /// <param name="right">the right</param>
        /// <returns>bool answer</returns>
        public static bool operator ==(MeasuresInput left, MeasuresInput right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// implements !=
        /// </summary>
        /// <param name="left">the left</param>
        /// <param name="right">the right</param>
        /// <returns>bool answer</returns>
        public static bool operator !=(MeasuresInput left, MeasuresInput right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// implements equals
        /// </summary>
        /// <param name="other">other being compared to</param>
        /// <returns>bool answer</returns>
        public bool Equals(MeasuresInput other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other.Measure == this.Measure && Equals(other.Group, this.Group) && other.Pinned.Equals(this.Pinned);
        }

        /// <summary>
        /// implements equals for derived classes
        /// </summary>
        /// <param name="obj">other being compared to</param>
        /// <returns>bool answer</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(MeasuresInput))
            {
                return false;
            }

            return this.Equals((MeasuresInput)obj);
        }

        /// <summary>
        /// implements hash code 
        /// </summary>
        /// <returns>returns hash code</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var result = this.Measure;
                result = (result * 397) ^ (this.Group != null ? this.Group.GetHashCode() : 0);
                result = (result * 397) ^ this.Pinned.GetHashCode();
                return (int)(result % int.MaxValue);
            }
        }
    }
}

﻿using System;
using System.Runtime.Serialization;
using System.Text;

namespace EventService.Models
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public partial class DefaultErrorResponse : IEquatable<DefaultErrorResponse>
    {
        /// <summary>
        /// Gets or Sets Detail
        /// </summary>
        [DataMember(Name = "detail")]
        public string Detail { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class DefaultErrorResponse {\n");
            sb.Append("  Detail: ").Append(Detail).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((DefaultErrorResponse)obj);
        }

        /// <summary>
        /// Returns true if DefaultErrorResponse instances are equal
        /// </summary>
        /// <param name="other">Instance of DefaultErrorResponse to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(DefaultErrorResponse other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    Detail == other.Detail ||
                    Detail != null &&
                    Detail.Equals(other.Detail)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                // Suitable nullity checks etc, of course :)
                if (Detail != null)
                    hashCode = hashCode * 59 + Detail.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
#pragma warning disable 1591

        public static bool operator ==(DefaultErrorResponse left, DefaultErrorResponse right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DefaultErrorResponse left, DefaultErrorResponse right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591
        #endregion Operators
    }
}

// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Management.Internal.Network.Version2017_10_01.Models
{
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Parameters that define a geographic location.
    /// </summary>
    public partial class AzureReachabilityReportLocation
    {
        /// <summary>
        /// Initializes a new instance of the AzureReachabilityReportLocation
        /// class.
        /// </summary>
        public AzureReachabilityReportLocation()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the AzureReachabilityReportLocation
        /// class.
        /// </summary>
        /// <param name="country">The name of the country.</param>
        /// <param name="state">The name of the state.</param>
        /// <param name="city">The name of the city or town.</param>
        public AzureReachabilityReportLocation(string country, string state = default(string), string city = default(string))
        {
            Country = country;
            State = state;
            City = city;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets the name of the country.
        /// </summary>
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets the name of the state.
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the name of the city or town.
        /// </summary>
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Country == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Country");
            }
        }
    }
}

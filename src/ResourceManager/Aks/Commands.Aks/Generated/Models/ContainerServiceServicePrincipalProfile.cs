// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Commands.Aks.Generated.Models
{
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Information about a service principal identity for the cluster to use
    /// for manipulating Azure APIs. Either secret or keyVaultSecretRef must be
    /// specified.
    /// </summary>
    public partial class ContainerServiceServicePrincipalProfile
    {
        /// <summary>
        /// Initializes a new instance of the
        /// ContainerServiceServicePrincipalProfile class.
        /// </summary>
        public ContainerServiceServicePrincipalProfile()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the
        /// ContainerServiceServicePrincipalProfile class.
        /// </summary>
        /// <param name="clientId">The ID for the service principal.</param>
        /// <param name="secret">The secret password associated with the
        /// service principal in plain text.</param>
        /// <param name="keyVaultSecretRef">Reference to a secret stored in
        /// Azure Key Vault.</param>
        public ContainerServiceServicePrincipalProfile(string clientId, string secret = default(string), KeyVaultSecretRef keyVaultSecretRef = default(KeyVaultSecretRef))
        {
            ClientId = clientId;
            Secret = secret;
            KeyVaultSecretRef = keyVaultSecretRef;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets the ID for the service principal.
        /// </summary>
        [JsonProperty(PropertyName = "clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the secret password associated with the service
        /// principal in plain text.
        /// </summary>
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; set; }

        /// <summary>
        /// Gets or sets reference to a secret stored in Azure Key Vault.
        /// </summary>
        [JsonProperty(PropertyName = "keyVaultSecretRef")]
        public KeyVaultSecretRef KeyVaultSecretRef { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (ClientId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "ClientId");
            }
            if (KeyVaultSecretRef != null)
            {
                KeyVaultSecretRef.Validate();
            }
        }
    }
}

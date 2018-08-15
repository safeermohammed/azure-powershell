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
    /// Profile for the container service master.
    /// </summary>
    public partial class ContainerServiceMasterProfile
    {
        /// <summary>
        /// Initializes a new instance of the ContainerServiceMasterProfile
        /// class.
        /// </summary>
        public ContainerServiceMasterProfile()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ContainerServiceMasterProfile
        /// class.
        /// </summary>
        /// <param name="dnsPrefix">DNS prefix to be used to create the FQDN
        /// for the master pool.</param>
        /// <param name="vmSize">Size of agent VMs. Possible values include:
        /// 'Standard_A0', 'Standard_A1', 'Standard_A10', 'Standard_A11',
        /// 'Standard_A1_v2', 'Standard_A2', 'Standard_A2_v2',
        /// 'Standard_A2m_v2', 'Standard_A3', 'Standard_A4', 'Standard_A4_v2',
        /// 'Standard_A4m_v2', 'Standard_A5', 'Standard_A6', 'Standard_A7',
        /// 'Standard_A8', 'Standard_A8_v2', 'Standard_A8m_v2', 'Standard_A9',
        /// 'Standard_D1', 'Standard_D11', 'Standard_D11_v2',
        /// 'Standard_D11_v2_Promo', 'Standard_D12', 'Standard_D12_v2',
        /// 'Standard_D12_v2_Promo', 'Standard_D13', 'Standard_D13_v2',
        /// 'Standard_D13_v2_Promo', 'Standard_D14', 'Standard_D14_v2',
        /// 'Standard_D14_v2_Promo', 'Standard_D15_v2', 'Standard_D16_v3',
        /// 'Standard_D16s_v3', 'Standard_D1_v2', 'Standard_D2',
        /// 'Standard_D2_v2', 'Standard_D2_v2_Promo', 'Standard_D2_v3',
        /// 'Standard_D2s_v3', 'Standard_D3', 'Standard_D3_v2',
        /// 'Standard_D3_v2_Promo', 'Standard_D4', 'Standard_D4_v2',
        /// 'Standard_D4_v2_Promo', 'Standard_D4_v3', 'Standard_D4s_v3',
        /// 'Standard_D5_v2', 'Standard_D5_v2_Promo', 'Standard_D8_v3',
        /// 'Standard_D8s_v3', 'Standard_DS1', 'Standard_DS11',
        /// 'Standard_DS11_v2', 'Standard_DS11_v2_Promo', 'Standard_DS12',
        /// 'Standard_DS12_v2', 'Standard_DS12_v2_Promo', 'Standard_DS13',
        /// 'Standard_DS13_v2', 'Standard_DS13_v2_Promo', 'Standard_DS14',
        /// 'Standard_DS14_v2', 'Standard_DS14_v2_Promo', 'Standard_DS15_v2',
        /// 'Standard_DS1_v2', 'Standard_DS2', 'Standard_DS2_v2',
        /// 'Standard_DS2_v2_Promo', 'Standard_DS3', 'Standard_DS3_v2',
        /// 'Standard_DS3_v2_Promo', 'Standard_DS4', 'Standard_DS4_v2',
        /// 'Standard_DS4_v2_Promo', 'Standard_DS5_v2',
        /// 'Standard_DS5_v2_Promo', 'Standard_E16_v3', 'Standard_E16s_v3',
        /// 'Standard_E2_v3', 'Standard_E2s_v3', 'Standard_E32_v3',
        /// 'Standard_E32s_v3', 'Standard_E4_v3', 'Standard_E4s_v3',
        /// 'Standard_E64_v3', 'Standard_E64s_v3', 'Standard_E8_v3',
        /// 'Standard_E8s_v3', 'Standard_F1', 'Standard_F16', 'Standard_F16s',
        /// 'Standard_F1s', 'Standard_F2', 'Standard_F2s', 'Standard_F4',
        /// 'Standard_F4s', 'Standard_F8', 'Standard_F8s', 'Standard_G1',
        /// 'Standard_G2', 'Standard_G3', 'Standard_G4', 'Standard_G5',
        /// 'Standard_GS1', 'Standard_GS2', 'Standard_GS3', 'Standard_GS4',
        /// 'Standard_GS5', 'Standard_H16', 'Standard_H16m', 'Standard_H16mr',
        /// 'Standard_H16r', 'Standard_H8', 'Standard_H8m', 'Standard_L16s',
        /// 'Standard_L32s', 'Standard_L4s', 'Standard_L8s', 'Standard_M128s',
        /// 'Standard_M64ms', 'Standard_NC12', 'Standard_NC24',
        /// 'Standard_NC24r', 'Standard_NC6', 'Standard_NV12', 'Standard_NV24',
        /// 'Standard_NV6'</param>
        /// <param name="count">Number of masters (VMs) in the container
        /// service cluster. Allowed values are 1, 3, and 5. The default value
        /// is 1.</param>
        /// <param name="osDiskSizeGB">OS Disk Size in GB to be used to specify
        /// the disk size for every machine in this master/agent pool. If you
        /// specify 0, it will apply the default osDisk size according to the
        /// vmSize specified.</param>
        /// <param name="vnetSubnetID">VNet SubnetID specifies the vnet's
        /// subnet identifier. If you specify either master VNet Subnet, or
        /// agent VNet Subnet, you need to specify both. And they have to be in
        /// the same VNet.</param>
        /// <param name="firstConsecutiveStaticIP">FirstConsecutiveStaticIP
        /// used to specify the first static ip of masters.</param>
        /// <param name="storageProfile">Storage profile specifies what kind of
        /// storage used. Choose from StorageAccount and ManagedDisks. Leave it
        /// empty, we will choose for you based on the orchestrator choice.
        /// Possible values include: 'StorageAccount', 'ManagedDisks'</param>
        /// <param name="fqdn">FDQN for the master pool.</param>
        public ContainerServiceMasterProfile(string dnsPrefix, string vmSize, int? count = default(int?), int? osDiskSizeGB = default(int?), string vnetSubnetID = default(string), string firstConsecutiveStaticIP = default(string), string storageProfile = default(string), string fqdn = default(string))
        {
            Count = count;
            DnsPrefix = dnsPrefix;
            VmSize = vmSize;
            OsDiskSizeGB = osDiskSizeGB;
            VnetSubnetID = vnetSubnetID;
            FirstConsecutiveStaticIP = firstConsecutiveStaticIP;
            StorageProfile = storageProfile;
            Fqdn = fqdn;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets number of masters (VMs) in the container service
        /// cluster. Allowed values are 1, 3, and 5. The default value is 1.
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets DNS prefix to be used to create the FQDN for the
        /// master pool.
        /// </summary>
        [JsonProperty(PropertyName = "dnsPrefix")]
        public string DnsPrefix { get; set; }

        /// <summary>
        /// Gets or sets size of agent VMs. Possible values include:
        /// 'Standard_A0', 'Standard_A1', 'Standard_A10', 'Standard_A11',
        /// 'Standard_A1_v2', 'Standard_A2', 'Standard_A2_v2',
        /// 'Standard_A2m_v2', 'Standard_A3', 'Standard_A4', 'Standard_A4_v2',
        /// 'Standard_A4m_v2', 'Standard_A5', 'Standard_A6', 'Standard_A7',
        /// 'Standard_A8', 'Standard_A8_v2', 'Standard_A8m_v2', 'Standard_A9',
        /// 'Standard_D1', 'Standard_D11', 'Standard_D11_v2',
        /// 'Standard_D11_v2_Promo', 'Standard_D12', 'Standard_D12_v2',
        /// 'Standard_D12_v2_Promo', 'Standard_D13', 'Standard_D13_v2',
        /// 'Standard_D13_v2_Promo', 'Standard_D14', 'Standard_D14_v2',
        /// 'Standard_D14_v2_Promo', 'Standard_D15_v2', 'Standard_D16_v3',
        /// 'Standard_D16s_v3', 'Standard_D1_v2', 'Standard_D2',
        /// 'Standard_D2_v2', 'Standard_D2_v2_Promo', 'Standard_D2_v3',
        /// 'Standard_D2s_v3', 'Standard_D3', 'Standard_D3_v2',
        /// 'Standard_D3_v2_Promo', 'Standard_D4', 'Standard_D4_v2',
        /// 'Standard_D4_v2_Promo', 'Standard_D4_v3', 'Standard_D4s_v3',
        /// 'Standard_D5_v2', 'Standard_D5_v2_Promo', 'Standard_D8_v3',
        /// 'Standard_D8s_v3', 'Standard_DS1', 'Standard_DS11',
        /// 'Standard_DS11_v2', 'Standard_DS11_v2_Promo', 'Standard_DS12',
        /// 'Standard_DS12_v2', 'Standard_DS12_v2_Promo', 'Standard_DS13',
        /// 'Standard_DS13_v2', 'Standard_DS13_v2_Promo', 'Standard_DS14',
        /// 'Standard_DS14_v2', 'Standard_DS14_v2_Promo', 'Standard_DS15_v2',
        /// 'Standard_DS1_v2', 'Standard_DS2', 'Standard_DS2_v2',
        /// 'Standard_DS2_v2_Promo', 'Standard_DS3', 'Standard_DS3_v2',
        /// 'Standard_DS3_v2_Promo', 'Standard_DS4', 'Standard_DS4_v2',
        /// 'Standard_DS4_v2_Promo', 'Standard_DS5_v2',
        /// 'Standard_DS5_v2_Promo', 'Standard_E16_v3', 'Standard_E16s_v3',
        /// 'Standard_E2_v3', 'Standard_E2s_v3', 'Standard_E32_v3',
        /// 'Standard_E32s_v3', 'Standard_E4_v3', 'Standard_E4s_v3',
        /// 'Standard_E64_v3', 'Standard_E64s_v3', 'Standard_E8_v3',
        /// 'Standard_E8s_v3', 'Standard_F1', 'Standard_F16', 'Standard_F16s',
        /// 'Standard_F1s', 'Standard_F2', 'Standard_F2s', 'Standard_F4',
        /// 'Standard_F4s', 'Standard_F8', 'Standard_F8s', 'Standard_G1',
        /// 'Standard_G2', 'Standard_G3', 'Standard_G4', 'Standard_G5',
        /// 'Standard_GS1', 'Standard_GS2', 'Standard_GS3', 'Standard_GS4',
        /// 'Standard_GS5', 'Standard_H16', 'Standard_H16m', 'Standard_H16mr',
        /// 'Standard_H16r', 'Standard_H8', 'Standard_H8m', 'Standard_L16s',
        /// 'Standard_L32s', 'Standard_L4s', 'Standard_L8s', 'Standard_M128s',
        /// 'Standard_M64ms', 'Standard_NC12', 'Standard_NC24',
        /// 'Standard_NC24r', 'Standard_NC6', 'Standard_NV12', 'Standard_NV24',
        /// 'Standard_NV6'
        /// </summary>
        [JsonProperty(PropertyName = "vmSize")]
        public string VmSize { get; set; }

        /// <summary>
        /// Gets or sets OS Disk Size in GB to be used to specify the disk size
        /// for every machine in this master/agent pool. If you specify 0, it
        /// will apply the default osDisk size according to the vmSize
        /// specified.
        /// </summary>
        [JsonProperty(PropertyName = "osDiskSizeGB")]
        public int? OsDiskSizeGB { get; set; }

        /// <summary>
        /// Gets or sets vNet SubnetID specifies the vnet's subnet identifier.
        /// If you specify either master VNet Subnet, or agent VNet Subnet, you
        /// need to specify both. And they have to be in the same VNet.
        /// </summary>
        [JsonProperty(PropertyName = "vnetSubnetID")]
        public string VnetSubnetID { get; set; }

        /// <summary>
        /// Gets or sets firstConsecutiveStaticIP used to specify the first
        /// static ip of masters.
        /// </summary>
        [JsonProperty(PropertyName = "firstConsecutiveStaticIP")]
        public string FirstConsecutiveStaticIP { get; set; }

        /// <summary>
        /// Gets or sets storage profile specifies what kind of storage used.
        /// Choose from StorageAccount and ManagedDisks. Leave it empty, we
        /// will choose for you based on the orchestrator choice. Possible
        /// values include: 'StorageAccount', 'ManagedDisks'
        /// </summary>
        [JsonProperty(PropertyName = "storageProfile")]
        public string StorageProfile { get; set; }

        /// <summary>
        /// Gets FDQN for the master pool.
        /// </summary>
        [JsonProperty(PropertyName = "fqdn")]
        public string Fqdn { get; private set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (DnsPrefix == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "DnsPrefix");
            }
            if (VmSize == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "VmSize");
            }
        }
    }
}

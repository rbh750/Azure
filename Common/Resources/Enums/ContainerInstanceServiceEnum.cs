using System.Runtime.Serialization;

namespace Common.Resources.Enums;

public enum ContainerInstanceServiceEnum
{
    [EnumMember(Value = "2GB-2vCPU")] TwoGB_TwovCPU,
    [EnumMember(Value = "2GB-3vCPU")] TwoGB_ThreevCPU,
    [EnumMember(Value = "2GB-4vCPU")] TwoGB_FourvCPU,
    [EnumMember(Value = "4GB-2vCPU")] FourGB_TwovCPU,
    [EnumMember(Value = "4GB-3vCPU")] FourGB_ThreevCPU,
    [EnumMember(Value = "4GB-4vCPU")] FourGB_FourvCPU,
    [EnumMember(Value = "8GB-2vCPU")] EightGB_TwovCPU,
    [EnumMember(Value = "8GB-3vCPU")] EightGB_ThreevCPU,
    [EnumMember(Value = "8GB-4vCPU")] EightGB_FourvCPU,
    [EnumMember(Value = "16GB-2vCPU")] SixteenGB_TwovCPU,
    [EnumMember(Value = "16GB-3vCPU")] SixteenGB_ThreevCPU,
    [EnumMember(Value = "16GB-4vCPU")] SixteenGB_FourvCPU
}

using System.ComponentModel;

namespace AuthService.Domain.Enums
{
    public enum EApiKeyStatus
    {
        [Description("Active")]
        Active = 1,

        [Description("Inactive")]
        Inactive = 2,

        [Description("Deleted")]
        Deleted = 3,

        [Description("Revoked")]
        Revoked = 4
    }
}

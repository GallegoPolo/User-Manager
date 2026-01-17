using AuthService.Domain.Enums;
using System.ComponentModel;
using System.Reflection;

namespace AuthService.Domain.Extensions
{
    public static class EApiKeyStatusExtensions
    {
        public static string GetDescription(this EApiKeyStatus status)
        {
            var field = status.GetType().GetField(status.ToString());

            if (field == null)
                return status.ToString();

            var attribute = field.GetCustomAttribute<DescriptionAttribute>();

            return attribute?.Description ?? status.ToString();
        }
    }
}

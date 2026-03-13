using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Task_Reminder.Wpf.Models;

namespace Task_Reminder.Wpf.Services;

public sealed class LocalCertificateValidator(IHostEnvironment hostEnvironment, IOptions<ClientOptions> options)
{
    public bool Validate(HttpRequestMessage request, X509Certificate2? _, X509Chain? __, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (!hostEnvironment.IsDevelopment() || !options.Value.AllowInvalidLocalCertificatesInDevelopment)
        {
            return false;
        }

        return IsLoopbackHost(request.RequestUri?.Host);
    }

    private static bool IsLoopbackHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Prevueit.Lib.Models.Shared
{
    public class AppSettingsModel
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public Jwt Jwt { get; set; }
        public AzureStorage AzureStorage { get; set; }
        public Email Email { get; set; }
    }

    public class ConnectionStrings
    {
        public string PrevuitContext { get; set; }
    }
    public class Jwt
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
    }

    public class AzureStorage
    {
        public string storageAccountConnectionString { get; set; }
        public string ContainerName { get; set; }
        public string AzureFileURL { get; set; }
        public string AzureSASToken { get; set; }
    }

    public class Email
    {
        public string FromEmailId { get; set; }
        public string FromEmailPassword { get; set; }
        public string VerifyUrl { get; set; }
        public string SendInBlueAPIKey { get; set; }
    }
}

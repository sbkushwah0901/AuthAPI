using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class SpaceConfiguration
    {
        public long ISpaceConfigurationId { get; set; }
        public string UserType { get; set; }
        public string PerFileUploadLimit { get; set; }
        public string TotalSpaceAllowed { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

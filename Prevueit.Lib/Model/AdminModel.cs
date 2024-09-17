using System;
using System.Collections.Generic;
using System.Text;

namespace Prevueit.Lib.Model
{
    public class AllCountModel
    {
        public int TotalUserCount { get; set; }
        public int FreelUserCount { get; set; }
        public int PaidUserCount { get; set; }
        public string TotalSpaceUsed { get; set; }
        public string FreeUserSpace { get; set; }
        public string PaidUserSpace { get; set; }
        public int TodayFreeUser { get; set; }
        public int TodayPaidUser { get; set; }
    }

    public class SpaceConfigModel
    {
        public int iSpaceConfigId { get; set; }
        public string UserType { get; set; }
        public string PerFileUploadLimit { get; set; }
        public string TotalSpaceAllowed { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int UserCount { get; set; }
    }

    public class DateRequestModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}

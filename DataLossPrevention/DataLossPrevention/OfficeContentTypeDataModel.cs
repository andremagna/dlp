using System;

namespace DataLossPrevention
{
    public class OfficeContentTypeDataModel
    {
        public string contentUri { get; set; }
        public string contentId { get; set; }
        public string contentType { get; set; }
        public DateTime contentCreated { get; set; }
        public DateTime contentExpiration { get; set; }
    }
}

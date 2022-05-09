namespace BITool.Models
{
    public class AdminCampaignDto
    {
        public int CampaignID { get; set; }
        public string CampaignName { get; set; }
        public DateTime CampaignDate { get; set; }
        public int Status { get; set; }
        public int? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedON { get; set; }
    }
}

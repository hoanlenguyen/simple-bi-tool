namespace BITool.Models
{
    public class CustomerScoreDto
    {
        public int CustomerScoreID { get; set; }
        public string CustomerMobileNo { get; set; }
        public int ScoreID { get; set; }
        public string DateOccurred { get; set; }
        public int Status { get; set; }
        public int? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedON { get; set; }
    }
}
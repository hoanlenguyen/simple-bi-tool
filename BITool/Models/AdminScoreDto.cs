namespace BITool.Models
{
    public class AdminScoreDto
    {
        public int ScoreID { get; set; }
        public string ScoreCategory { get; set; }
        public string ScoreTitle { get; set; }
        public int Points { get; set; }
        public int Status { get; set; }
        public int LastUpdatedBy { get; set; }
        public DateTime LastUpdatedON { get; set; }
    }
}

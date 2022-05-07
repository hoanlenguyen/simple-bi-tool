namespace BITool.Models
{
    public class CustomerScoreFilterDto
    {
        public string KeyWord { get; set; }
        public int? ScoreId { get; set; }
        public string ScoreCategory { get; set; }
        public DateTime? DateFirstAddedFrom { get; set; }
        public DateTime? DateFirstAddedTo { get; set; }         
    }
}

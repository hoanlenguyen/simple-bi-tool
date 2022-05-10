namespace BITool.Models
{
    public class AssignCampaignToCustomerModel
    {
        public IEnumerable<string> CustomerList { get; set; } = new List<string>();
        public int CampaignID { get; set; }
    }
}

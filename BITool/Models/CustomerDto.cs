namespace BITool.Models
{
    public class CustomerDto
    {
        public int CustomerID { get; set; }
        public string CustomerMobileNo { get; set; }
        public DateTime DateFirstAdded { get; set; } = DateTime.Now;
        public int Status { get; set; } = 1;
    }
}

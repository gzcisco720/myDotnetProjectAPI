namespace myDotnetApp.API.Helpers
{
    public class MessageParams
    {
        private const int MaxPageSize = 100;
        public int PageNumber { get; set; } = 1;
        private int pageSize { get; set; } = 10;
        
        public int PageSize
        {
            get { return pageSize; }
            set { pageSize = (value > MaxPageSize)? MaxPageSize:value;}
        }
        public int UserId { get; set; }
        public string MessageContainer { get; set; } = "Unread";
    }
}
namespace WpfApplication2
{
    public class Host
    {
        private string IP;
        private string Comment;
        private bool Online;

        public Host(string IP, string Comment, bool Online)
        {
            this.IP = IP;
            this.Comment = Comment;
            this.Online = Online;
        }

        public bool getOnline()
        {
            return Online;
        }

        public string getIP()
        {
            return IP;
        }

        public string getComment()
        {
            return Comment;
        }
    }
}

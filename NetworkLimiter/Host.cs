namespace NetworkLimiter
{
    public class Host
    {
        private string IP;
        private string Comment;
        private bool Online;
        private double Download;
        private double Upload;
        private double ReceiveTotal;
        private double SendTotal;

        public Host(string IP, string Comment, bool Online)
        {
            this.IP = IP;
            this.Comment = Comment;
            this.Online = Online;
        }

        public void setSendTotal(double SendTotal)
        {
            this.SendTotal = SendTotal;
        }

        public void setReceiveTotal(double ReceiveTotal)
        {
            this.ReceiveTotal = ReceiveTotal;
        }

        public void setDownload(double Download)
        {
            this.Download = Download;
        }

        public void setUpload(double Upload)
        {
            this.Upload = Upload;
        }

        public double getRecieveTotals()
        {
            return ReceiveTotal;
        }

        public double getSendTotals()
        {
            return SendTotal;
        }

        public double getDownload()
        {
            return Download;
        }

        public double getUpload()
        {
            return Download;
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

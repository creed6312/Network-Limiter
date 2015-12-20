namespace WpfApplication2
{
    public class Queue
    {
        private string ID;
        private string IP;
        private int Down;
        private int Up;
        private string Comment;
        private bool Online;

        public Queue(string ID, string IP)
        {
            this.ID = ID;
            this.IP = IP;
        }

        public Queue(string ID, string IP, int Down, int Up)
        {
            this.ID = ID;
            this.IP = IP;
            this.Down = Down;
            this.Up = Up;
        }

        public void setComment(string Comment)
        {
            this.Comment = Comment;
        }

        public void setOnline(bool Online)
        {
            this.Online = Online;
        }

        public string getIP()
        {
            return IP;
        }

        public string getID()
        {
            return ID;
        }

        public string getComment()
        {
            return Comment;
        }

        public bool getOnline()
        {
            return Online;
        }

        public int getDown()
        {
            return Down;
        }

        public int getUp()
        {
            return Up;
        }
    }

}

using System;
using System.Net;
using netudt;
namespace netudt.packets
{
    public class Destination {

        private int port;

        private IPAddress address;

        //UDT socket ID of the peer
        private long socketID;

        public IPEndPoint EndPoint { get; set; }

        public long SocketID
        {
            get { return socketID; }
            set { socketID = value; }
        }
        public Destination(EndPoint point)
        {
            
            EndPoint = point as IPEndPoint;
            this.address = EndPoint.Address;
            this.port = EndPoint.Port;
        }
        public Destination(IPAddress address, int port) {
            this.address = address;
            this.port = port;
            EndPoint = new IPEndPoint(address, port);
        }
        public Destination(string address, int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(address), port);
            this.address = EndPoint.Address;
            this.port = port;
        }

        public IPAddress GetAddress() {
            return address;
        }

        public int GetPort() {
            return port;
        }

        //public long GetSocketID() {
        //    return socketID;
        //}

        //public void SetSocketID(long socketID) {
        //    this.socketID = socketID;
        //}


        public string toString() {
            return ("Destination [" + address.ToString()+ " port=" + port + " socketID=" + socketID) + "]";
        }


        public int HashCode() {
            int prime = 31;
            int result = 1;
            result = prime * result + ((address == null) ? 0 : address.GetHashCode());
            result = prime * result + port;
            result = prime * result + (int)(socketID ^(socketID.RightMove(32)));
            return result;
        }


        public override bool Equals(object obj) {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
             if(!obj.GetType().Equals(this.GetType()))
            {
                return false;
            }
            Destination other = (Destination)obj;
            if (address == null) {
                if (other.address != null)
                    return false;
            } else if (!address.Equals(other.address))
                return false;
            if (port != other.port)
                return false;
            if (socketID != other.socketID)
                return false;
            return true;
        }
    }
	
}






using System;
/**
* The UDTInputStream receives data blocks from the {@link UDTSocket}
* as they become available, and places them into an ordered, 
* bounded queue (the flow window) for reading by the application
* 
* 
*/
namespace netudt
{
   
        /**
         * used for storing application data and the associated
         * sequence number in the queue in ascending order
         */
        public class AppData : IComparable<AppData>{
		 long sequenceNumber;
		public byte[] data;
		public AppData(long sequenceNumber, byte[]data){
			this.sequenceNumber=sequenceNumber;
			this.data=data;
		}

		
		public int compareTo(AppData o) {
			return (int)(sequenceNumber-o.sequenceNumber);
		}

		
		public string toString(){
			return sequenceNumber+"["+data.Length+"]";
		}

		public long getSequenceNumber(){
			return sequenceNumber;
		}


    public override int GetHashCode()
    {


        int prime = 31;
        int result = 1;
        result = prime * result
        + (int)(sequenceNumber ^ (sequenceNumber.RightMove(32)));
        return result;
    }

		
		public override bool Equals(object obj) {
			if (this == obj)
				return true;
			if (obj == null)
				return false;
		if(!this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
			AppData other = (AppData) obj;
			if (sequenceNumber != other.sequenceNumber)
				return false;
			return true;
		}

    public int CompareTo(AppData other)
    {
        throw new NotImplementedException();
    }
}

}

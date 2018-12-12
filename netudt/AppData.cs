/*********************************************************************************
 * Copyright (c) 2010 Forschungszentrum Juelich GmbH 
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * (1) Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the disclaimer at the end. Redistributions in
 * binary form must reproduce the above copyright notice, this list of
 * conditions and the following disclaimer in the documentation and/or other
 * materials provided with the distribution.
 * 
 * (2) Neither the name of Forschungszentrum Juelich GmbH nor the names of its 
 * contributors may be used to endorse or promote products derived from this 
 * software without specific prior written permission.
 * 
 * DISCLAIMER
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *********************************************************************************/





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

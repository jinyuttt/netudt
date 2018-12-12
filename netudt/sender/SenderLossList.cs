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




using System.Collections.Generic;
/**
* stores the sequence number of the lost packets in increasing order
*/
namespace netudt.sender
{
    public class SenderLossList {

        private List<long> backingList;

        public bool IsEmpty
        {
            get { return backingList.Count == 0; }
        }

        public int Size
        {
            get { return backingList.Count; }
        }

        /**
         * create a new sender lost list
         */
        public SenderLossList() {
            backingList = new List<long>();
        }

        public void Insert(long obj) {
            lock(backingList) {
                if (!backingList.Contains(obj)) {
                    for (int i = 0; i < backingList.Count; i++) {
                        if (obj < backingList[i]) {
                               backingList.Insert(i, obj);
                            return;
                        }
                    }
                    backingList.Add(obj);
                }
            }
        }

        /**
         * retrieves the loss list entry with the lowest sequence number
         */
        public long GetFirstEntry() {
            lock(backingList){
                return backingList[0];
            }
        }

       

        public string toString() {
            lock(backingList) {
                return backingList.ToString();
            }
        }
    }
}

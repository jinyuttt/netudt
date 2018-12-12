#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：netudt
* 项目描述 ：
* 类 名 称 ：UDPUserToken
* 类 描 述 ：
* 命名空间 ：netudt
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2018
* 更新时间 ：2018
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2018. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace netudt
{
    /* ============================================================================== 
    * 功能描述：UDPUserToken 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

   public class UDPUserToken
    {
        private IPEndPoint endPoint;

        public UDPUserToken(IPEndPoint endPoint)
        {
            this.endPoint = endPoint;
            Remote = endPoint;
        }

        public byte[] Buffer { get; set; }

        public int Offset { get; set; }

        public int Length { get; set; }
        public IPEndPoint Remote { get; set; }
    }
}

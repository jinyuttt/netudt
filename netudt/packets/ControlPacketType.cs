#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：netudt.packets
* 项目描述 ：
* 类 名 称 ：ControlPacketType
* 类 描 述 ：
* 命名空间 ：netudt.packets
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
using System.Text;

namespace netudt.packets
{
    /* ============================================================================== 
    * 功能描述：ControlPacketType 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

  public  enum ControlPacketType
    {
        CONNECTION_HANDSHAKE,
        KEEP_ALIVE,
        ACK,
        NAK,
        UNUNSED_1,
        SHUTDOWN,
        ACK2,
        MESSAGE_DROP_REQUEST,
        UNUNSED_2,
        UNUNSED_3,
        UNUNSED_4,
        UNUNSED_5,
        UNUNSED_6,
        UNUNSED_7,
        UNUNSED_8,
        USER_DEFINED,
    }
}

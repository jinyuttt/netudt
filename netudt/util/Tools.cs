
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
namespace netudt.util
{ 
public class Tools {
	public static IPAddress GetLocalHostLANAddress()  {
            try
            {
                IPAddress candidateAddress = null;
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in nics)
                {
                    //判断是否为以太网卡
                    //Wireless80211         无线网卡    Ppp     宽带连接
                    //Ethernet              以太网卡   
                    //这里篇幅有限贴几个常用的，其他的返回值大家就自己百度吧！
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        //获取以太网卡网络接口信息
                        IPInterfaceProperties ip = adapter.GetIPProperties();
                        //获取单播地址集
                        UnicastIPAddressInformationCollection ipCollection = ip.UnicastAddresses;
                        foreach (UnicastIPAddressInformation ipadd in ipCollection)
                        {
                            //InterNetwork    IPV4地址      InterNetworkV6        IPV6地址
                            //Max            MAX 位址
                            if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                                //判断是否为ipv4
                                return ipadd.Address;
                            else if (candidateAddress == null)
                            {
                                candidateAddress = ipadd.Address;
                            }
                        }
                    }
                }
                if (candidateAddress != null)
                {
                    return candidateAddress;
                }
                string hostName = Dns.GetHostName();   //获取本机名
                IPHostEntry localhost = Dns.GetHostByName(hostName);    //方法已过期，可以获取IPv4的地址
                                                                        //IPHostEntry localhost = Dns.GetHostEntry(hostName);   //获取IPv6地址
                IPAddress localaddr = localhost.AddressList[0];
                return localaddr;
            }catch
            {

            }
            return null;
	}
	
	/**
	 * 
	* @Title: getPeerIP
	* @Description: 或者字符串
	* @param @return    参数
	* @return String    返回类型
	 */
	public static string  GetPeerIP()
	{
		string address="127.0.0.1";
		IPAddress addr = null;
		try {
			addr = GetLocalHostLANAddress();
		} catch (Exception e) {
			
			
		}
		if(addr!=null)
		{
			address=addr.ToString();
		
		}
		return address;
	}
	public static long IPToLong(string strIp) {
            //String[]ip = strIp.Split(".");
            //return (long.Parse((ip[0]) << 24)) + (long.Parse(ip[1]) << 16) + (long.Parse(ip[2]) << 8) + long.Parse(ip[3]);
            // IPAddress.Parse(strIp).Address;
            char[] separator = new char[] { '.' };
            string[] items = strIp.Split(separator);
            long dreamduip= long.Parse(items[0]) << 24
                    | long.Parse(items[1]) << 16
                    | long.Parse(items[2]) << 8
                    | long.Parse(items[3]);
          return  IPAddress.HostToNetworkOrder(dreamduip);
            //
            // byte[] byts = IPAddress.Parse(beginIP).GetAddressBytes();
            // Array.Reverse(byts); // 需要倒置一次字节序
            // long beginip = BitConverter.ToUInt32(byts, 0); // 起始地址
            // byts = IPAddress.Parse(endIP).GetAddressBytes();
            // Array.Reverse(byts);// 需要倒置一次字节序
            //return long endip = BitConverter.ToUInt32(byts, 0);   // 终止地址

        }
        public static string LongToIP(long longIp) {
            longIp=IPAddress.NetworkToHostOrder(longIp);
            StringBuilder sb = new StringBuilder();
            sb.Append((longIp >> 24) & 0xFF).Append(".");
            sb.Append((longIp >> 16) & 0xFF).Append(".");
            sb.Append((longIp >> 8) & 0xFF).Append(".");
            sb.Append(longIp & 0xFF);
           //
           
            return sb.ToString();
        }
	public static long[] IP6ToLong(string strIp) {
		long[]ips=new long[4];
            char[] separator = new char[] { ':' };
            string[]ip = strIp.Split(separator);
		//128位，
		ips[3] =(long.Parse(ip[15]) << 24) + (long.Parse(ip[14]) << 16) + (long.Parse(ip[13]) << 8) + long.Parse(ip[12]);
		ips[2] =(long.Parse(ip[11]) << 24) + (long.Parse(ip[10]) << 16) + (long.Parse(ip[9]) << 8) + long.Parse(ip[8]);
		ips[1] =(long.Parse(ip[7]) << 24) + (long.Parse(ip[6]) << 16) + (long.Parse(ip[5]) << 8) + long.Parse(ip[4]);
		ips[0] =(long.Parse(ip[3]) << 24) + (long.Parse(ip[2]) << 16) + (long.Parse(ip[1]) << 8) + long.Parse(ip[0]);
		return ips;
	}


        
        public static string LongToIP6(long[] longIps) {
            StringBuilder sb = new StringBuilder();
		for(int i=0;i<4;i++)
		{
			long longIp=longIps[i];
		// 直接右移24位
		sb.Append(longIp.RightMove(24));
		sb.Append(":");
                // 将高8位置0，然后右移16位
       sb.Append(longIp.RightMove(16));
		sb.Append(":");
                // 将高16位置0，然后右移8位
                sb.Append(longIp.RightMove(8));
                sb.Append(":");
		// 将高24位置0
		sb.Append(longIp & 0x000000FF);
		}
            return sb.ToString();
	}
	
	/**
	 * 
	* @Title: iptopeer
	* @Description: 转换IP
	* @param @param addr
	* @param @return    参数
	* @return long[]    返回类型
	 */
	public static long[] IPtoPeer(string addr)
	{
		long[] ips=new long[4];
		if(addr.Contains("."))
		{
			ips[0]=IPToLong(addr);
		}
		else
		{
			ips=IP6ToLong(addr);
		}
		return ips;
	}
	public static string String2MD5(string inStr){
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] md5Source = Encoding.UTF8.GetBytes(inStr);
            byte[] md5Out = md5.ComputeHash(md5Source);
            string pwd = "";
            for (int i = 0; i < md5Out.Length; i++)
            {
                pwd += md5Out[i].ToString("x2");
            }
            return pwd;


        }  
	 public static string ConvertMD5(string inStr){

            char[] a = inStr.ToCharArray();
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = (char)(a[i] ^ 't');
            }
            string s = new string(a);
            return s;



        }
    }
}

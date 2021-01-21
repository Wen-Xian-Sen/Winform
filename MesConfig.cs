using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MesConfiguration
{
    static class MesConfig
    {
        static public MachineMessage machineMessage;
        static public ReadMes        readMes;
        static public ResponMessage  responMessage;
        static public NetworkMessage networkMessage;
        static public ServerMessage  serverMessage;
        static public ReadNetwork    readNetwork;
        static public ReadServer     readServer;
        static public ReadRtdata     readRtdata;
        static public RealTimeData   realTimeData;
        static public PlcAddrGroup   plcAddrGroup;
        static public ReadPlcAddr    readPlcAddr;
        static MesConfig()
        {
            machineMessage = new MachineMessage();
            readMes = new ReadMes();
            readMes.type = TYPE.READ_MES;
            responMessage = new ResponMessage();
            networkMessage = new NetworkMessage();
            serverMessage = new ServerMessage();
            readNetwork = new ReadNetwork();
            readNetwork.type = TYPE.READ_NET;
            readServer = new ReadServer();
            readServer.type = TYPE.READ_SERVER;
            readRtdata = new ReadRtdata();
            realTimeData = new RealTimeData();
            readRtdata.type = TYPE.READ_RTDATA;
            plcAddrGroup = new PlcAddrGroup();
            readPlcAddr = new ReadPlcAddr();
            readPlcAddr.type = TYPE.READ_PLCADDR;
        }
        public class MachineMessage
        {
            public TYPE type;
            [JsonIgnore] public int errorCode;
            public MachineInfo machineInfo;

            public MachineMessage()
            {
                machineInfo = new MachineInfo();
            }
            public class MachineInfo
            {
                public string mac;
                public Channel[] channel;
                public class Channel
                {
                    public string sn;
                    public double range0;
                    public int v0Type;
                    public double range1;
                    public int v1Type;
                }
                public MachineInfo()
                {
                    channel = new Channel[4];
                    channel[0] = new Channel();
                    channel[1] = new Channel();
                    channel[2] = new Channel();
                    channel[3] = new Channel();
                }
            }
        }

        public class ReadMes
        {
            public TYPE type;
        }

        public class ResponMessage
        {
            public TYPE type;
            [JsonIgnore] public int errorCode;
        }

        public class NetworkMessage
        {
            public TYPE type;
            [JsonIgnore] public int errorCode;
            public NetworkInfo networkInfo;
            public NetworkMessage()
            {
                networkInfo = new NetworkInfo();
            }
            public class NetworkInfo
            {
                public string ssid;
                public string password;
                public string ip;
                public string netMask;
                public string gateway;
                public string dns1;
                public string dns2;
                public int loPort;
            }
        }

        public class ServerMessage
        {
            public TYPE type;
            [JsonIgnore] public int errorCode;
            public ServerInfo serverInfo;
            public ServerMessage()
            {
                serverInfo = new ServerInfo();
            }
            public class ServerInfo
            {
                public string ip;
                public int port;
            }
        }

        public class ReadNetwork
        {
            public TYPE type;
        }

        public class ReadServer
        {
            public TYPE type;
        }

        public class ReadRtdata
        {
            public TYPE type;
        }

        public class RealTimeData
        {
            public TYPE type;
            [JsonIgnore] public int errorCode;
            public Rtdata rtdata;
            public RealTimeData()
            {
                rtdata = new Rtdata();
            }
            public class Rtdata
            {
                public double value1;
                public double value2;
                public double value3;
                public double value4;
                public double value5;
                public double value6;
                public double value7;
                public double value8;
                public int input1;
                public int input2;
                public int input3;
                public int input4;
                public int output1;
                public int output2;
                public int output3;
                public int output4;
            }
        }

        public class PlcAddrGroup //
        {
            public TYPE type;
            [JsonIgnore] public int errorCode;
            public GroupInfo groupInfo;
            public PlcAddrGroup()
            {
                groupInfo = new GroupInfo();
            }
            public class GroupInfo
            {
                public int plcType;
                public int Group1Addr1;
                public int Group1Addr2;
                public int Group2Addr1;
                public int Group2Addr2;
                public int Group3Addr1;
                public int Group3Addr2;
                public int Group4Addr1;
                public int Group4Addr2;
                public int Group5Addr1;
                public int Group5Addr2;
                public int Group6Addr1;
                public int Group6Addr2;
                public int Group7Addr1;
                public int Group7Addr2;
                public int Group8Addr1;
                public int Group8Addr2;
                public int Group9Addr1;
                public int Group9Addr2;
                public int Group10Addr1;
                public int Group10Addr2;
            }
        }

        public class ReadPlcAddr
        {
          public TYPE type;
        }
  }
    enum TYPE
    {
        WRITE_MES = 1,  //1
        WRITE_SERVER,   //2
        WRITE_NET,      //3
        WRITE_PLCADDR,  //4
        ACCEPT,         //5

        READ_MES,       //6
        ACCEPT_MES,     //7

        READ_SERVER,    //8
        ACCEPT_SERVER,  //9

        READ_NET,       //10
        ACCEPT_NET,     //11

        READ_RTDATA,    //12 

        READ_PLCADDR ,  //13
        ACCEPT_PLCADDR, //14
   };
}

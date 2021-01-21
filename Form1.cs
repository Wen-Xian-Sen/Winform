using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MesConfiguration
{
    public delegate void ProcessDelegate();
    public partial class Mes_pc : Form
    {
        private SerialPort serialPort;
        public static byte[] read_buffer;
        public static int buf_size, read_flag = 1;
        private Thread readThread, upThread, displayThread;
        public static string[] portList = null;
        ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        public Mes_pc()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            readThread = new Thread(readPort);
            upThread = new Thread(showPortsList);
            upThread.IsBackground = true;
            upThread.Start();
            readThread.Start();
            read_buffer = new byte[1024];
            EnabledControl(0, false);
            displayThread = new Thread(displayReadRTData);
            displayThread.IsBackground = true;
            displayThread.Start();
            stop_rtdata_btn.Enabled = false;
        }
        private void displayReadRTData()
        {
            string jsonStr;
            read_flag = 0;
            while (true)
            {
                manualResetEvent.WaitOne();
                try
                {
                    jsonStr = JsonConvert.SerializeObject(MesConfig.readRtdata);
                    //MessageBox.Show(jsonStr);
                    writeBuffer(jsonStr);
                    for (int i = 0; i < 10; i++)
                    {
                        if (read_flag == 1)
                        {
                            read_flag = 0;
                            jsonStr = Encoding.ASCII.GetString(read_buffer);
                            MesConfig.realTimeData = JsonConvert.DeserializeObject<MesConfig.RealTimeData>(jsonStr);
                            value1_textBox.Text = MesConfig.realTimeData.rtdata.value1.ToString();
                            value2_textBox.Text = MesConfig.realTimeData.rtdata.value2.ToString();
                            value3_textBox.Text = MesConfig.realTimeData.rtdata.value3.ToString();
                            value4_textBox.Text = MesConfig.realTimeData.rtdata.value4.ToString();
                            value5_textBox.Text = MesConfig.realTimeData.rtdata.value5.ToString();
                            value6_textBox.Text = MesConfig.realTimeData.rtdata.value6.ToString();
                            value7_textBox.Text = MesConfig.realTimeData.rtdata.value7.ToString();
                            value8_textBox.Text = MesConfig.realTimeData.rtdata.value8.ToString();
                            checkbtn(input1_checkBox, MesConfig.realTimeData.rtdata.input1,true);
                            checkbtn(input2_checkBox, MesConfig.realTimeData.rtdata.input2, true);
                            checkbtn(input3_checkBox, MesConfig.realTimeData.rtdata.input3, true);
                            checkbtn(input4_checkBox, MesConfig.realTimeData.rtdata.input4, true);
                            checkbtn(output1_checkBox, MesConfig.realTimeData.rtdata.output1, false);
                            checkbtn(output2_checkBox, MesConfig.realTimeData.rtdata.output2, false);
                            checkbtn(output3_checkBox, MesConfig.realTimeData.rtdata.output3, false);
                            checkbtn(output4_checkBox, MesConfig.realTimeData.rtdata.output4, false);
                            break;
                        }
                        if (i == 9)
                        {
                            MessageBox.Show("读取超时！");
                        }
                        Thread.Sleep(20);
                    }
                    Thread.Sleep(200);
                }
                catch
                {
                    Console.WriteLine("display fail");
                    Thread.Sleep(1000);
                }
            }
        }
        private void checkbtn(CheckBox checkBox,int input,bool command)
        {
            if (command)
            {
                if (input == 1)
                {
                    checkBox.Text = "开";
                    checkBox.Checked = true;
                }
                else
                {
                    checkBox.Text = "关";
                    checkBox.Checked = false;
                }
            }
            else
            {
                if (input == 0)
                {
                    checkBox.Text = "开";
                    checkBox.Checked = true;
                }
                else
                {
                    checkBox.Text = "关";
                    checkBox.Checked = false;
                }
            }
        }
        private void showPortsList()
        {
            Thread.Sleep(1000);
            ProcessDelegate showProcess = new ProcessDelegate(showList);
            while (true)
            {
                portList = SerialPort.GetPortNames();

                PortsComboBox.Invoke(showProcess);

                Thread.Sleep(200);
            }

        }

        public void showList()
        {
            for (int i = 0; i < PortsComboBox.Items.Count; i++)
            {
                string str = (string)PortsComboBox.Items[i];
                if (!portList.Contains(str))
                {
                    PortsComboBox.Items.RemoveAt(i);
                }

            }
            foreach (string i in portList)
            {
                if (!PortsComboBox.Items.Contains(i))
                {
                    PortsComboBox.Items.Add(i);
                }
            }
        }

        private void readPort()
        {

            Int32 Length;
            Thread.Sleep(1000);
            while (true)
            {
                try
                {
                    while (true)
                    {
                        if (read_flag == 1)
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        if (serialPort.ReadByte() != 0xba)
                        {
                            Console.WriteLine("0xba erro");
                            continue;
                        }
                        if (serialPort.ReadByte() != 0xbe)
                        {
                            Console.WriteLine("0xbe erro");
                            continue;
                        }
                        Length = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            Length *= 256;
                            Length += serialPort.ReadByte();

                        }
                        Console.WriteLine(Length);
                        buf_size = 0;
                        Array.Clear(read_buffer, 0, read_buffer.Length);
                        while (buf_size < Length)
                        {
                            buf_size += serialPort.Read(read_buffer, buf_size, Length - buf_size);
                        }
                        serialPort.ReadByte();
                        if (serialPort.ReadByte() != 0xaa)
                        {
                            read_flag = 0;
                            buf_size = 0;
                            Length = 0;
                            Console.WriteLine("0xaa erro");
                            continue;
                        }
                        read_flag = 1;
                        Console.WriteLine("ok");

                    }
                }
                catch
                {
                    Console.WriteLine("read fail");
                    Thread.Sleep(1000);
                }
            }
            //throw new NotImplementedException();

        }

        public void writeBuffer(string text)
        {
            byte[] head = { 0xba, 0xbe, 0x00, 0x00, 0x00, 0x00 };
            byte[] end = { 0x00, 0xaa };
            int len = text.Length;

            for (int i = 0; i < 4; i++)
            {
                head[5 - i] = (byte)(len % 256);
                len /= 256;
            }
            for (int i = 0; i < text.Length; i++)
            {
                end[0] += (byte)text[i];
            }

            try
            {
                serialPort.Write(head, 0, 6);
                serialPort.Write(text);
                serialPort.Write(end, 0, 2);
            }
            catch
            {
                return;
            }
        }

        private void read_btn_Click(object sender, EventArgs e)
        {
            string jsonStr, sonStr;
            EnabledControl(1, false);
            Enabled = false;
            string warmMes = "";
            Mac_textBox.Text = "";
            sn1_textBox.Text = ""; sn1_range0_textBox.Text = ""; sn1_v0_comboBox.Text = ""; sn1_range1_textBox.Text = ""; sn1_v1_comboBox.Text = "";
            sn2_textBox.Text = ""; sn2_range0_textBox.Text = ""; sn2_v0_comboBox.Text = ""; sn2_range1_textBox.Text = ""; sn2_v1_comboBox.Text = "";
            sn3_textBox.Text = ""; sn3_range0_textBox.Text = ""; sn3_v0_comboBox.Text = ""; sn3_range1_textBox.Text = ""; sn3_v1_comboBox.Text = "";
            sn4_textBox.Text = ""; sn4_range0_textBox.Text = ""; sn4_v0_comboBox.Text = ""; sn4_range1_textBox.Text = ""; sn4_v1_comboBox.Text = "";
            wifi_name_textBox.Text = ""; wifi_password_textBox.Text = "";
            ethernet_ip_textBox.Text = ""; netMask_textBox.Text = ""; gateway_textBox.Text = "";
            dns1_textBox.Text = ""; dns2_textBox.Text = ""; modbus_textBox.Text = "";
            server_ip_textBox.Text = ""; server_port_textBox.Text = "";

            jsonStr = JsonConvert.SerializeObject(MesConfig.readMes);
            //MessageBox.Show(jsonStr);
            writeBuffer(jsonStr);
            read_flag = 0;

            for (int i = 0; i < 10; i++)
            {
                if (read_flag == 1)
                {
                    jsonStr = Encoding.ASCII.GetString(read_buffer);
                    MesConfig.machineMessage = JsonConvert.DeserializeObject<MesConfig.MachineMessage>(jsonStr);
                    Mac_textBox.Text = MesConfig.machineMessage.machineInfo.mac;
                    sn1_textBox.Text = MesConfig.machineMessage.machineInfo.channel[0].sn;
                    sn1_range0_textBox.Text = Convert.ToString(MesConfig.machineMessage.machineInfo.channel[0].range0);
                    sn1_v0_comboBox.Text = check_num(MesConfig.machineMessage.machineInfo.channel[0].v0Type);
                    sn1_range1_textBox.Text = Convert.ToString(MesConfig.machineMessage.machineInfo.channel[0].range1);
                    sn1_v1_comboBox.Text = check_num(MesConfig.machineMessage.machineInfo.channel[0].v1Type);

                    sn2_textBox.Text = MesConfig.machineMessage.machineInfo.channel[1].sn;
                    sn2_range0_textBox.Text = Convert.ToString(MesConfig.machineMessage.machineInfo.channel[1].range0);
                    sn2_v0_comboBox.Text = check_num(MesConfig.machineMessage.machineInfo.channel[1].v0Type);
                    sn2_range1_textBox.Text = Convert.ToString(MesConfig.machineMessage.machineInfo.channel[1].range1);
                    sn2_v1_comboBox.Text = check_num(MesConfig.machineMessage.machineInfo.channel[1].v1Type);

                    sn3_textBox.Text = MesConfig.machineMessage.machineInfo.channel[2].sn;
                    sn3_range0_textBox.Text = Convert.ToString(MesConfig.machineMessage.machineInfo.channel[2].range0);
                    sn3_v0_comboBox.Text = check_num(MesConfig.machineMessage.machineInfo.channel[2].v0Type);
                    sn3_range1_textBox.Text = Convert.ToString(MesConfig.machineMessage.machineInfo.channel[2].range1);
                    sn3_v1_comboBox.Text = check_num(MesConfig.machineMessage.machineInfo.channel[2].v1Type);

                    sn4_textBox.Text = MesConfig.machineMessage.machineInfo.channel[3].sn;
                    sn4_range0_textBox.Text = Convert.ToString(MesConfig.machineMessage.machineInfo.channel[3].range0);
                    sn4_v0_comboBox.Text = check_num(MesConfig.machineMessage.machineInfo.channel[3].v0Type);
                    sn4_range1_textBox.Text = Convert.ToString(MesConfig.machineMessage.machineInfo.channel[3].range1);
                    sn4_v1_comboBox.Text = check_num(MesConfig.machineMessage.machineInfo.channel[3].v1Type);
                    break;
                }
                if (i == 9)
                {
                    warmMes += "序列号以及阙值信息读出超时\n";
                }
                Thread.Sleep(100);
            }

            jsonStr = JsonConvert.SerializeObject(MesConfig.readNetwork);
            //MessageBox.Show(jsonStr);
            writeBuffer(jsonStr);
            read_flag = 0;
            for (int i = 0; i < 10; i++)
            {
                if (read_flag == 1)
                {
                    jsonStr = Encoding.ASCII.GetString(read_buffer);
                    MesConfig.networkMessage = JsonConvert.DeserializeObject<MesConfig.NetworkMessage>(jsonStr);
                    wifi_name_textBox.Text = MesConfig.networkMessage.networkInfo.ssid;
                    wifi_password_textBox.Text = MesConfig.networkMessage.networkInfo.password;
                    ethernet_ip_textBox.Text = MesConfig.networkMessage.networkInfo.ip;
                    netMask_textBox.Text = MesConfig.networkMessage.networkInfo.netMask;
                    gateway_textBox.Text = MesConfig.networkMessage.networkInfo.gateway;
                    dns1_textBox.Text = MesConfig.networkMessage.networkInfo.dns1;
                    dns2_textBox.Text = MesConfig.networkMessage.networkInfo.dns2;
                    modbus_textBox.Text = Convert.ToString(MesConfig.networkMessage.networkInfo.loPort);
                    break;
                }
                if (i == 9)
                {
                    warmMes += "网络信息读出超时\n";
                }
                Thread.Sleep(100);
            }

            jsonStr = JsonConvert.SerializeObject(MesConfig.readServer);
            //MessageBox.Show(jsonStr);
            writeBuffer(jsonStr);
            read_flag = 0;
            for (int i = 0; i < 10; i++)
            {
                if (read_flag == 1)
                {
                    jsonStr = Encoding.ASCII.GetString(read_buffer);
                    MesConfig.serverMessage = JsonConvert.DeserializeObject<MesConfig.ServerMessage>(jsonStr);
                    server_ip_textBox.Text = MesConfig.serverMessage.serverInfo.ip;
                    server_port_textBox.Text = Convert.ToString(MesConfig.serverMessage.serverInfo.port);
                    break;
                }
                if (i == 9)
                {
                    warmMes += "服务器信息读出超时\n";
                }
                Thread.Sleep(100);
            }

            jsonStr = JsonConvert.SerializeObject(MesConfig.readPlcAddr);
            //MessageBox.Show(jsonStr);
            writeBuffer(jsonStr);
            read_flag = 0;
            for (int i = 0; i < 10; i++)
            {
              if (read_flag == 1)
              {
                jsonStr = Encoding.ASCII.GetString(read_buffer);
                MesConfig.plcAddrGroup = JsonConvert.DeserializeObject<MesConfig.PlcAddrGroup>(jsonStr);
                plcTypeComboBox.Text = check_plcNum(MesConfig.plcAddrGroup.groupInfo.plcType);
                VBGroup1StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group1Addr1);
                VBGroup1EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group1Addr2);

                VBGroup2StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group2Addr1);
                VBGroup2EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group2Addr2);

                VBGroup3StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group3Addr1);
                VBGroup3EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group3Addr2);

                VBGroup4StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group4Addr1);
                VBGroup4EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group4Addr2);

                VBGroup5StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group5Addr1);
                VBGroup5EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group5Addr2);

                VBGroup6StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group6Addr1);
                VBGroup6EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group6Addr2);

                VBGroup7StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group7Addr1);
                VBGroup7EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group7Addr2);

                VBGroup8StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group8Addr1);
                VBGroup8EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group8Addr2);

                VBGroup9StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group9Addr1);
                VBGroup9EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group9Addr2);

                VBGroup10StartAddr.Text = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group10Addr1);
                VBGroup10EndAddr.Text   = Convert.ToString(MesConfig.plcAddrGroup.groupInfo.Group10Addr2);
                break;
              }
              if (i == 9)
              {
                warmMes += "PLC地址读出超时\n";
              }
              Thread.Sleep(100);
            }


            if (warmMes == "")
            {
                MessageBox.Show("读出成功");
            }
            else
            {
                MessageBox.Show(warmMes);
            }
            EnabledControl(1, true);
            Enabled = true;
        }

        private void write_btn_Click(object sender, EventArgs e)
        {
            Enabled = false;
            bool write_success = false;
            string warmInfo = "", fiInfo = "", erroMes = "";
            if (Mac_textBox.Text == "")
                warmInfo += "mac输入内容有误!\n";
            if (sn1_textBox.Text == "")
                warmInfo += "序列号1输入有误!\n";
            if (sn2_textBox.Text == "")
                warmInfo += "序列号2输入有误!\n";
            if (sn3_textBox.Text == "")
                warmInfo += "序列号3输入有误!\n";
            if (sn4_textBox.Text == "")
                warmInfo += "序列号4输入有误!\n";
            if (sn1_range0_textBox.Text == "" || sn2_range0_textBox.Text == "" || sn3_range0_textBox.Text == "" || sn4_range0_textBox.Text == "")
                warmInfo += "量程0输入有误!\n";
            if (sn1_range1_textBox.Text == "" || sn2_range1_textBox.Text == "" || sn3_range1_textBox.Text == "" || sn4_range1_textBox.Text == "")
                warmInfo += "量程1输入有误!\n";
            if (sn1_v0_comboBox.Text == "" || sn2_v0_comboBox.Text == "" || sn3_v0_comboBox.Text == "" || sn4_v0_comboBox.Text == "")
                warmInfo += "类型0输入有误!\n";
            if (sn1_v1_comboBox.Text == "" || sn2_v1_comboBox.Text == "" || sn3_v1_comboBox.Text == "" || sn4_v1_comboBox.Text == "")
                warmInfo += "类型1输入有误!\n";

            if (wifi_name_textBox.Text == "")
                warmInfo += "wifi账号输入有误！\n";
            if (wifi_password_textBox.Text == "")
                warmInfo += "wifi密码输入有误！\n";
            if (ethernet_ip_textBox.Text == "")
                warmInfo += "ip输入有误！\n";
            if (netMask_textBox.Text == "")
                warmInfo += "掩码输入有误！\n";
            if (gateway_textBox.Text == "")
                warmInfo += "网关输入有误\n";
            if (dns1_textBox.Text == "")
                warmInfo += "DNS1输入有误\n";
            if (dns2_textBox.Text == "")
                warmInfo += "DNS2输入有误\n";
            if (server_ip_textBox.Text == "")
                warmInfo += "服务器地址输入有误\n";
            if (server_port_textBox.Text == "")
                warmInfo += "服务器端口输入有误\n";
            if (warmInfo != "")
            {
                MessageBox.Show(warmInfo, "错误提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                Enabled = true;
                return;
            }


            MesConfig.machineMessage.type = TYPE.WRITE_MES;
            MesConfig.machineMessage.machineInfo.mac = Mac_textBox.Text;
            MesConfig.machineMessage.machineInfo.channel[0].sn = sn1_textBox.Text;
            MesConfig.machineMessage.machineInfo.channel[0].range0 = Convert.ToDouble(sn1_range0_textBox.Text);
            MesConfig.machineMessage.machineInfo.channel[0].v0Type = check_type(sn1_v0_comboBox.Text);
            MesConfig.machineMessage.machineInfo.channel[0].range1 = Convert.ToDouble(sn1_range1_textBox.Text);
            MesConfig.machineMessage.machineInfo.channel[0].v1Type = check_type(sn1_v1_comboBox.Text);
            MesConfig.machineMessage.machineInfo.channel[1].sn = sn2_textBox.Text;
            MesConfig.machineMessage.machineInfo.channel[1].range0 = Convert.ToDouble(sn2_range0_textBox.Text);
            MesConfig.machineMessage.machineInfo.channel[1].v0Type = check_type(sn2_v0_comboBox.Text);
            MesConfig.machineMessage.machineInfo.channel[1].range1 = Convert.ToDouble(sn2_range1_textBox.Text);
            MesConfig.machineMessage.machineInfo.channel[1].v1Type = check_type(sn2_v1_comboBox.Text);
            MesConfig.machineMessage.machineInfo.channel[2].sn = sn3_textBox.Text;
            MesConfig.machineMessage.machineInfo.channel[2].range0 = Convert.ToDouble(sn3_range0_textBox.Text);
            MesConfig.machineMessage.machineInfo.channel[2].v0Type = check_type(sn3_v0_comboBox.Text);
            MesConfig.machineMessage.machineInfo.channel[2].range1 = Convert.ToDouble(sn3_range1_textBox.Text);
            MesConfig.machineMessage.machineInfo.channel[2].v1Type = check_type(sn3_v1_comboBox.Text);
            MesConfig.machineMessage.machineInfo.channel[3].sn = sn4_textBox.Text;
            MesConfig.machineMessage.machineInfo.channel[3].range0 = Convert.ToDouble(sn4_range0_textBox.Text);
            MesConfig.machineMessage.machineInfo.channel[3].v0Type = check_type(sn4_v0_comboBox.Text);
            MesConfig.machineMessage.machineInfo.channel[3].range1 = Convert.ToDouble(sn4_range1_textBox.Text);
            MesConfig.machineMessage.machineInfo.channel[3].v1Type = check_type(sn4_v1_comboBox.Text);
            string jsonStr = JsonConvert.SerializeObject(MesConfig.machineMessage);
            //MessageBox.Show(jsonStr);

            writeBuffer(jsonStr);
            read_flag = 0;
            for (int i = 0; i < 10; i++)
            {
                if (read_flag == 1)
                {
                    MesConfig.responMessage = JsonConvert.DeserializeObject<MesConfig.ResponMessage>(Encoding.ASCII.GetString(read_buffer));
                    if (MesConfig.responMessage.errorCode != 0)
                    {
                        erroMes += "序列号或阙值写入出错：" + Convert.ToString(MesConfig.responMessage.errorCode) + "\n";
                    }
                    else
                    {
                        fiInfo += "序列号和阙值写入成功\n";
                    }
                    break;
                }
                if (i == 9)
                {
                    erroMes += "序列号或阙值写入超时\n";
                }
                Thread.Sleep(200);

            }

            MesConfig.serverMessage.type = TYPE.WRITE_SERVER;
            MesConfig.serverMessage.serverInfo.ip = server_ip_textBox.Text;
            MesConfig.serverMessage.serverInfo.port = Convert.ToInt16(server_port_textBox.Text);
            jsonStr = JsonConvert.SerializeObject(MesConfig.serverMessage);
            //MessageBox.Show(jsonStr);

            writeBuffer(jsonStr);
            read_flag = 0;
            for (int i = 0; i < 10; i++)
            {
                if (read_flag == 1)
                {
                    MesConfig.responMessage = JsonConvert.DeserializeObject<MesConfig.ResponMessage>(Encoding.ASCII.GetString(read_buffer));
                    if (MesConfig.responMessage.errorCode != 0)
                    {
                        erroMes += "服务器写入出错：" + Convert.ToString(MesConfig.responMessage.errorCode) + "\n";
                    }
                    else
                    {
                        fiInfo += "服务器写入成功\n";
                    }
                    break;
                }

                if (i == 9)
                {
                    erroMes += "服务器写入超时\n";
                }
                Thread.Sleep(200);

            }

            MesConfig.networkMessage.type = TYPE.WRITE_NET;
            MesConfig.networkMessage.networkInfo.ssid = wifi_name_textBox.Text;
            MesConfig.networkMessage.networkInfo.password = wifi_password_textBox.Text;
            MesConfig.networkMessage.networkInfo.ip = ethernet_ip_textBox.Text;
            MesConfig.networkMessage.networkInfo.netMask = netMask_textBox.Text;
            MesConfig.networkMessage.networkInfo.gateway = gateway_textBox.Text;
            MesConfig.networkMessage.networkInfo.dns1 = dns1_textBox.Text;
            MesConfig.networkMessage.networkInfo.dns2 = dns2_textBox.Text;
            MesConfig.networkMessage.networkInfo.loPort = Convert.ToInt16(modbus_textBox.Text);
            jsonStr = JsonConvert.SerializeObject(MesConfig.networkMessage);
            //MessageBox.Show(jsonStr);

            writeBuffer(jsonStr);
            read_flag = 0;

            for (int i = 0; i < 10; i++)
            {
                if (read_flag == 1)
                {
                    MesConfig.responMessage = JsonConvert.DeserializeObject<MesConfig.ResponMessage>(Encoding.ASCII.GetString(read_buffer));
                    if (MesConfig.responMessage.errorCode != 0)
                    {
                        erroMes += "网络信息写入出错：" + Convert.ToString(MesConfig.responMessage.errorCode) + "\n";
                    }
                    else
                    {
                        fiInfo += "网络信息写入成功\n";
                    }
                    break;
                }
                if (i == 9)
                {
                    erroMes += "网络信息写入超时\n";
                }
                Thread.Sleep(200);
            }

            MesConfig.plcAddrGroup.type = TYPE.WRITE_PLCADDR;
            MesConfig.plcAddrGroup.groupInfo.plcType = check_plcType(plcTypeComboBox.Text);
            MesConfig.plcAddrGroup.groupInfo.Group1Addr1 = int.Parse(VBGroup1StartAddr.Text);//
            MesConfig.plcAddrGroup.groupInfo.Group1Addr2 = Convert.ToInt32(VBGroup1EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group2Addr1 = Convert.ToInt32(VBGroup2StartAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group2Addr2 = Convert.ToInt32(VBGroup2EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group3Addr1 = Convert.ToInt32(VBGroup3StartAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group3Addr2 = Convert.ToInt32(VBGroup3EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group4Addr1 = Convert.ToInt32(VBGroup4StartAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group4Addr2 = Convert.ToInt32(VBGroup4EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group5Addr1 = Convert.ToInt32(VBGroup5StartAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group5Addr2 = Convert.ToInt32(VBGroup5EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group6Addr1 = Convert.ToInt32(VBGroup6StartAddr.Text);//
            MesConfig.plcAddrGroup.groupInfo.Group6Addr2 = Convert.ToInt32(VBGroup6EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group7Addr1 = Convert.ToInt32(VBGroup7StartAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group7Addr2 = Convert.ToInt32(VBGroup7EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group8Addr1 = Convert.ToInt32(VBGroup8StartAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group8Addr2 = Convert.ToInt32(VBGroup8EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group9Addr1 = Convert.ToInt32(VBGroup9StartAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group9Addr2 = Convert.ToInt32(VBGroup9EndAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group10Addr1 = Convert.ToInt32(VBGroup10StartAddr.Text);
            MesConfig.plcAddrGroup.groupInfo.Group10Addr2 = Convert.ToInt32(VBGroup10EndAddr.Text);
            jsonStr = JsonConvert.SerializeObject(MesConfig.plcAddrGroup);

            writeBuffer(jsonStr);
            read_flag = 0;

            for (int i = 0; i < 10; i++)
            {
                if (read_flag == 1)
                {
                    MesConfig.responMessage = JsonConvert.DeserializeObject<MesConfig.ResponMessage>(Encoding.ASCII.GetString(read_buffer));
                    if (MesConfig.responMessage.errorCode != 0)
                    {
                      erroMes += "PLC地址写入出错：" + Convert.ToString(MesConfig.responMessage.errorCode) + "\n";
                    }
                    else
                    {
                      fiInfo += "PLC地址写入成功\n";
                    }
                    break;
                }
                if (i == 9)
                {
                    erroMes += "PLC地址写入超时\n";
                }
                Thread.Sleep(200);
            }

            if (fiInfo != "")
            { 
                MessageBox.Show(fiInfo);
            }
            if (erroMes != "")
            {
                MessageBox.Show(erroMes, "错误提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            }

            Enabled = true;

        }

        private void server_ip_textBox_TextChanged(object sender, EventArgs e)
        {
            TextBox text = sender as TextBox;
            Regex reg = new Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])$");
            if (reg.IsMatch(text.Text))
            {
                text.BackColor = Color.White;
            }
            else
            {
                if (text.Text != "")
                    text.BackColor = Color.Red;
            }
        }

        private void ethernet_ip_textBox_TextChanged(object sender, EventArgs e)
        {
            TextBox text = sender as TextBox;
            Regex reg = new Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])$");
            if (reg.IsMatch(text.Text))
            {
                text.BackColor = Color.White;
            }
            else
            {
                if (text.Text != "")
                    text.BackColor = Color.Red;
            }
        }

        private void connect_btn_Click(object sender, EventArgs e)
        {
            if (connect_btn.Text.Equals("连接"))
            {
                bool connected = false;
                serialPort = new SerialPort();
                serialPort.BaudRate = 115200;
                PortsComboBox.Enabled = false;
                connect_btn.Enabled = false;
                string portNa = (string)PortsComboBox.SelectedItem;
                if (!SerialPort.GetPortNames().Contains(portNa))
                {
                    MessageBox.Show("不存在该端口\n");
                    PortsComboBox.Enabled = true;
                    connect_btn.Enabled = true;
                    return;
                }
                serialPort.PortName = portNa;
                try
                {
                    serialPort.Open();
                }
                catch
                {
                    Console.WriteLine("open fail");
                    connect_btn.Enabled = true;
                    PortsComboBox.Enabled = true;
                    MessageBox.Show("串口打开失败\n");
                    return;
                }

                for (int j = 0; j < 10; j++)
                {
                    string jsonStr = JsonConvert.SerializeObject(MesConfig.readMes);
                    writeBuffer(jsonStr);
                    read_flag = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        if (read_flag == 1)
                        {
                            try
                            {
                                MesConfig.machineMessage = JsonConvert.DeserializeObject<MesConfig.MachineMessage>(Encoding.ASCII.GetString(read_buffer));
                                if (MesConfig.machineMessage.errorCode != 0)
                                {
                                    MessageBox.Show("写入失败\n" + "错误码：" + MesConfig.machineMessage.errorCode, "错误提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                    connect_btn.Enabled = true;
                                    PortsComboBox.Enabled = true;
                                    serialPort.Close();
                                    return;
                                }
                                connected = true;
                            }
                            catch
                            {
                                Console.WriteLine("erro json");
                                continue;
                            }
                            break;
                        }
                        Thread.Sleep(100);
                    }
                    if (connected)
                    {
                        MessageBox.Show("连接成功");
                        connect_btn.Text = "断开";
                        connect_btn.Enabled = true;
                        PortsComboBox.Enabled = true;
                        EnabledControl(0, true);
                        break;
                    }
                    else if (j == 9)
                    {
                        connect_btn.Enabled = true;
                        PortsComboBox.Enabled = true;
                        serialPort.Close();
                        MessageBox.Show("串口连接失败");
                        return;
                    }
                }
            }
            else
            {
                connect_btn.Enabled = false;
                serialPort.Close();
                PortsComboBox.Enabled = true;
                connect_btn.Text = "连接";
                connect_btn.Enabled = true;
                EnabledControl(0, false);
            }
        }
        public void EnabledControl(int order, bool select)
        {

            write_btn.Enabled = select;
            read_btn.Enabled = select;
            start_rtdata_btn.Enabled = select;
            
            if (order == 1)
            {
                connect_btn.Enabled = select;
            }
        }

        private void start_rtdata_btn_Click(object sender, EventArgs e)
        {
            EnabledControl(1, false);
            stop_rtdata_btn.Enabled = true;
            manualResetEvent.Set();
        }


        private void plcTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }

        private void stop_rtdata_btn_Click(object sender, EventArgs e)
        {
            value1_textBox.Text = "";
            value2_textBox.Text = "";
            value3_textBox.Text = "";
            value4_textBox.Text = "";
            value5_textBox.Text = "";
            value6_textBox.Text = "";
            value7_textBox.Text = "";
            value8_textBox.Text = "";
            //MessageBox.Show(jsonStr);
            EnabledControl(1, true);
            stop_rtdata_btn.Enabled = false;
            manualResetEvent.Reset();
        }

        public int check_type(string type)
        {
            if (type == "4-20mA") return 0;
            if (type == "0-20mA") return 1;
            if (type == "0-10V") return 2;
            return -1;
        }
        public string check_num(int num)
        {
            if (num == 0) return "4-20mA";
            if (num == 1) return "0-20mA";
            if (num == 2) return "0-10V";
            return "";
        }
        public int check_plcType(string type)
        {
          if (type == "S7-200（金马）") return 0;
          if (type == "S7-200（其他）") return 1;
          if (type == "三菱FX") return 2;
          return -1;
        }
        public string check_plcNum(int num)
        {
          if (num == 0) return "S7-200（金马）";
          if (num == 1) return "S7-200（其他）";
          if (num == 2) return "三菱FX";
          return "";
        }
  }
}

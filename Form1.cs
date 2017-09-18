using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Data;
using System.Collections;
using Renci.SshNet;
using System.Linq;
using MROFtpDownloader.Properties;
using System.Threading;
//×××××××
//待完成：1、添加根据映射表下载制定文件的功能。
//          顺序，先从映射表里找数据，如果没有再从服务器中一个个找
namespace MROFtpDownloader
{
    public partial class Form1 : Form
    {

        private static string ftpServerIP;
        private static string ftpUser;
        private static string ftpPwd;
        private static int ftpPort;

        private static string dfuPath;
        private static string dfudate;
        private static string ftpinfoTable;
        private static string inNeedInfo;
        private static string localpath;
        private static bool isOutputFile; //是否输出ftp服务器文件列表
        private static string MRType;
        private static string Hour;
        private static int restCnt;
        private static List<ListViewItem> myCache;
        private static List<string> listleft;
        private int errNum;
        //private SystemParaReader spReader;
        public Form1()
        {
            InitializeComponent();
            //Settings.Default.ftpfile = toolStripTextBox1.Text;
            //Settings.Default.indinfo = toolStripTextBox2.Text;
            //Settings.Default.localpath = toolStripTextBox3.Text;
            //Settings.Default.datestr = dateTimePicker1.Value;
            Settings.Default.Save();
            myCache = new List<ListViewItem>();
        }
        
        void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (myCache != null)
            {
                e.Item = myCache[e.ItemIndex];
            }
            else
            {
                //A cache miss, so create a new ListViewItem and pass it back.
                int x = e.ItemIndex * e.ItemIndex;
                e.Item = new ListViewItem(x.ToString());
            }
        }
        public static void setConString(string sevIP, string port, string usr, string pwd, string dfpath)
        {
            ftpServerIP = sevIP;
            ftpUser = usr;
            ftpPwd = pwd;
            dfuPath = dfpath;
            ftpPort = int.Parse(port);
        }
        public string[] GetFileList()
        {
            string[] downloadFiles;
            StringBuilder result = new StringBuilder();
            FtpWebRequest reqFTP;
            
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + ftpServerIP + "/" + dfuPath));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUser, ftpPwd);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());

                string line = reader.ReadLine();
                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }
                result.Remove(result.ToString().LastIndexOf('\n'), 1);
                reader.Close();
                response.Close();

                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                //System.Windows.Forms.MessageBox.Show("获取文件信息失败:" + ex.Message + ftpServerIP, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowInfo(textBox3, "获取文件信息失败:" + ex.Message + ftpServerIP + "操作失败");
                downloadFiles = null;
                return downloadFiles;
            }
        }
        public List<string> GetFileListToList(string ftpAds, string ftpUsr, string ftpPwd, string ftpPath, string fc, string dfdate)
        {
            //string[] downloadFiles;
            StringBuilder result = new StringBuilder();
            List<string> ret = new List<string>();
            FtpWebRequest reqFTP;
            
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + ftpAds + "/" + ftpPath));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUsr, ftpPwd);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                reqFTP.Timeout = 1000;
                //reqFTP.ReadWriteTimeout = 160;
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                ShowInfo(textBox3, ""  + ftpAds + "：正在获取文件列表 ");
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (fc == "大唐") line = line.Replace("ENB=", "");
                    ret.Add(line);
                    line = reader.ReadLine();
                }
                reader.Close();
                response.Close();

                return ret;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("(550)") && errNum <=3 ){
                    ftpPath = dfdate+ "\\" ;
                    errNum++;
                    return GetFileListToList(ftpAds, ftpUsr, ftpPwd, ftpPath, fc, dfdate);
                }
                else
                {
                    ShowInfo(textBox3, "获取文件信息失败:" + ex.Message + ftpAds + "操作失败");
                    return null;
                }
                
            }
        }

        public string[] sftpconn(string host, int port, string username, string password, string workingdirectory)
        {
            try
            {
                StringBuilder result = new StringBuilder();
                using (var client = new SftpClient(host, port, username, password)) //创建连接对象
                {
                    client.Connect(); //连接
                    client.ChangeDirectory(workingdirectory); //切换目录

                    var listDirectory = client.ListDirectory(workingdirectory); //获取目录下所有文件

                    foreach (var fi in listDirectory) //遍历文件
                    {
                        //Console.WriteLine(" - " + fi.Name);
                        result.Append(fi.Name);
                        result.Append("\n");
                    }
                    result.Remove(result.ToString().LastIndexOf('\n'), 1);
                    return result.ToString().Split('\n');
                }
            }
            catch (Exception ex)
            {
                //System.Windows.Forms.MessageBox.Show("获取文件信息失败:" + ex.Message + host, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowInfo(textBox3, "获取文件信息失败:" + ex.Message + "操作失败");
                return null;
            }
        }

        /// <summary>
        /// 获取FTP上指定文件的大小
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns>文件大小</returns>
        public long GetFileSize(string filename)
        {
            FtpWebRequest reqFTP;
            long fileSize = 0;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + ftpServerIP + "/" + dfuPath + filename));
                reqFTP.Method = WebRequestMethods.Ftp.GetFileSize;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUser, ftpPwd);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                fileSize = response.ContentLength;

                ftpStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取文件大小时，出现异常:\n" + ex.Message, "获取文件大小失败！", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return fileSize;
        }


        /// <summary>
        /// 实现ftp下载操作
        /// </summary>
        /// <param name="filePath">保存到本地的文件名</param>
        /// <param name="fileName">远程文件名</param>
        public void Download(string filePath, string fileName)
        {
            FtpWebRequest reqFTP;
            try
            {
                //filePath = <<The full path where the file is to be created.>>,
                //fileName = <<Name of the file to be created(Need not be the name of the file on FTP server).>>
                if (!File.Exists(filePath + fileName))
                {
                    FileStream outputStream = new FileStream(filePath + "\\" + fileName, FileMode.Create);

                    reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + ftpServerIP + "/" + dfuPath + fileName));
                    reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                    reqFTP.UseBinary = true;
                    reqFTP.Credentials = new NetworkCredential(ftpUser, ftpPwd);
                    FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                    Stream ftpStream = response.GetResponseStream();
                    long cl = response.ContentLength;
                    int bufferSize = 2048;
                    int readCount;
                    byte[] buffer = new byte[bufferSize];

                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                    while (readCount > 0)
                    {
                        outputStream.Write(buffer, 0, readCount);
                        readCount = ftpStream.Read(buffer, 0, bufferSize);
                    }

                    ftpStream.Close();
                    outputStream.Close();
                    response.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 下载文件 
        /// </summary>
        /// <param name="remoteFileName">包含全路径的服务器端文件名</param>
        /// <param name="localFileName">本地保存的文件名</param>
        /// <returns></returns>
        public bool Download(string localFileName, string remoteFileName, int fc = 1)
        {
            if (!File.Exists(localFileName))
            {
                using (var client = new SftpClient(ftpServerIP, ftpPort, ftpUser, ftpPwd)) //创建连接对象
                {
                    client.Connect(); //连接
                    try
                    {
                        client.ChangeDirectory(dfuPath); //切换目录
                        FileStream fs = File.OpenWrite(localFileName);
                        client.DownloadFile(remoteFileName, fs);
                        fs.Close();
                        client.Disconnect();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        //logger.Error("[{0}]　文件下载发生错误。", remoteFileName, ex);
                        return false;
                    }
                }
            }
            return true;
        }

        public static void createDir(string strpath)
        {
            if (!Directory.Exists(strpath))
                Directory.CreateDirectory(strpath);
        }
        public DataSet excelToDS(string path)
        {
            OleDbConnection cn = new OleDbConnection("provider=Microsoft.Jet.OLEDB.4.0;extended properties=excel 8.0;data source=" + path);
            cn.Open();
            OleDbDataAdapter command = new OleDbDataAdapter("select * from [mr$] ", cn);
            System.Data.DataSet ds = new System.Data.DataSet();
            command.Fill(ds, "table1");
            cn.Close();
            return ds;
        }
        public ArrayList txtToList(string path)
        {
            ArrayList arlist = new ArrayList();

            if (File.Exists(path))
            {
                //File.Open(path, FileMode.Open);
                StreamReader sr = new StreamReader(path);
                while (sr.Peek() > -1)
                {
                    arlist.Add(sr.ReadLine());
                }
                sr.Close();
            }
            return arlist;
        }
        public static List<string> txtFileToList(string path)
        {
            List<string> arlist = new List<string>();

            if (File.Exists(path))
            {
                //File.Open(path, FileMode.Open);
                StreamReader sr = new StreamReader(path);
                while (sr.Peek() > -1)
                {
                    var s = sr.ReadLine();
                    if(s.Length>0)
                        arlist.Add(s);
                }
                sr.Close();
            }
            return arlist;
        }
        public static void writeToFile(string path, string dfdate, string ftpAds, string fc, List<string> str1)
        {
            //string path = "D\1.txt";//文件的路径，保证文件存在。
            FileStream fs = new FileStream(path, FileMode.Append);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            foreach (string s in str1)
            {
                sw.WriteLine(dfdate + "," + ftpAds + "," + fc + "," + s);
            }
            //sw.WriteLine(info);       
            sw.Close();
            fs.Close();
        }
        
        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
                listView1.Cursor = System.Windows.Forms.Cursors.Arrow;  //指定鼠标形状（更好看）
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".txt")  //判断文件类型，只接受txt文件
                {
                    //listView1.Text += file + "\r\n";
                    ShowInfoFile(listView1, file);
                    listView1.Cursor = System.Windows.Forms.Cursors.IBeam; //还原鼠标形状
                }
            }

        }
        
        

       

        private void 打开FTP服务器文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "xlsx文件(*.xls)|*.xls|所有文件(*.*)|*.*";
            openFileDialog1.Title = "请打开文件";
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                toolStripTextBox1.Text = openFileDialog1.FileName;
                ftpinfoTable = toolStripTextBox1.Text;
            }
            Settings.Default.ftpfile = toolStripTextBox1.Text;
            Settings.Default.Save();
        }


        private void 打开LTE室分文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "xlsx文件(*.xlsx)|*.xlsx|所有文件(*.*)|*.*";
            openFileDialog1.Title = "请打开文件";
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                toolStripTextBox2.Text = openFileDialog1.FileName;
                inNeedInfo = toolStripTextBox2.Text;
            }
            Settings.Default.indinfo = toolStripTextBox2.Text;
            Settings.Default.Save();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = " ";
            saveFileDialog1.Title = "请选择保存位置";
            saveFileDialog1.FileName = " ";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string locpath = saveFileDialog1.FileName.ToString();
                toolStripTextBox3.Text = locpath.Substring(0, locpath.LastIndexOf("\\") + 1);
                localpath = toolStripTextBox3.Text;
            }
            Settings.Default.localpath = toolStripTextBox3.Text;
            Settings.Default.Save();
        }

        private void 输出ftp上的文件列表ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (输出ftp上的文件列表ToolStripMenuItem.Checked)
                输出ftp上的文件列表ToolStripMenuItem.Checked = false;
            else
                输出ftp上的文件列表ToolStripMenuItem.Checked = true;
            Settings.Default.isOutput = 输出ftp上的文件列表ToolStripMenuItem.Checked;
            Settings.Default.Save();
        }
        //配置文件保存
        private void toolStripTextBox1_changed(object sender, EventArgs e)
        {
            Settings.Default.ftpfile = toolStripTextBox1.Text;
            Settings.Default.indinfo = toolStripTextBox2.Text;
            Settings.Default.localpath = toolStripTextBox3.Text;
            Settings.Default.isOutput = 输出ftp上的文件列表ToolStripMenuItem.Checked;
            Settings.Default.Save();
        }
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            Settings.Default.datestr = dateTimePicker1.Value;
            Settings.Default.Save();
        }

        private delegate void txt1Delegate(System.Windows.Forms.TextBox txtInfo, string Info);
        public void ShowInfo(System.Windows.Forms.TextBox txtInfo, string Info)
        {
            if (txtInfo.InvokeRequired)
            {
                Invoke(new txt1Delegate(ShowInfo), txtInfo , Info );
            }
            else
            {
                txtInfo.AppendText(Info);
                txtInfo.AppendText(Environment.NewLine);
                txtInfo.ScrollToCaret();
            }
            
        }
        /*
        public void ShowInfo(System.Windows.Forms.TextBox txtInfo, string Info)
        {
            txtInfo.AppendText(Info);
            txtInfo.AppendText(Environment.NewLine);
            txtInfo.ScrollToCaret();
        }
        */
        public static void ShowInfo(System.Windows.Forms.ListView listInfo, List<string> list)
        {
            myCache.Clear();
           // listInfo.Clear();
            foreach (string row in list)
            {
                if (row != null)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = row;
                    myCache.Add(lvi);
                }
            }
            listInfo.VirtualListSize = myCache.Count;
            listInfo.Invalidate();
            listInfo.EnsureVisible(0);
            //ListViewItem lvi2 = listInfo.FindItemWithText("111111");
            ////Select the item found and scroll it into view.
            //if (lvi2 != null)
            //{
            //    listInfo.SelectedIndices.Add(lvi2.Index);
            //    listInfo.EnsureVisible(lvi2.Index);
            //}
            /*listInfo.BeginUpdate();
            ListViewItem lvi = new ListViewItem();
            lvi.Text = Info;

            listInfo.Items.Add(lvi);
            listInfo.EndUpdate();
            listInfo.EnsureVisible(0);*/
        }
        
        public void ShowInfoFile(System.Windows.Forms.ListView listInfo, string Info)
        {
            myCache.Clear();
            toolStripTextBox2.Text = Info;
            List<string> Lstr = txtFileToList(Info);
            //listInfo.BeginUpdate();
            foreach (string row in Lstr)
            {
                if (row != null) {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = row;
                    myCache.Add(lvi);
                }
                
            }
            listInfo.VirtualListSize = myCache.Count;
            listInfo.Invalidate();
            //listInfo.EndUpdate();
            listInfo.EnsureVisible(0);
        }
        private delegate void lvDelegate(List<string> list);
        public void ShowlvInfo(List<string> list)
        {
            if (listView1.InvokeRequired)
            {
                Invoke(new lvDelegate(ShowlvInfo), new List<string>[] { list });
            }
            else
            {
                myCache.Clear();
                // listInfo.Clear();
                foreach (string row in list)
                {
                    if (row != null)
                    {
                        ListViewItem lvi = new ListViewItem();
                        lvi.Text = row;
                        myCache.Add(lvi);
                    }
                }
                listView1.VirtualListSize = myCache.Count;
                listView1.Invalidate();
                //listView1.EnsureVisible(0);
            }

        }
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            Thread thread = new Thread(new ThreadStart(LoadData));
            thread.IsBackground = true;
            thread.Start();

        }
        public void LoadData()
        {

            ftpinfoTable = toolStripTextBox1.Text;
            inNeedInfo = toolStripTextBox2.Text;
            localpath = toolStripTextBox3.Text;
            isOutputFile = 输出ftp上的文件列表ToolStripMenuItem.Checked;
            this.BeginInvoke(new MethodInvoker(delegate ()
            {
                MRType = comboBox1.SelectedItem.ToString();
                Hour = comboBox2.SelectedItem.ToString();
            }));
            // MRType = comboBox1.SelectedItem.ToString();
            // Hour = comboBox2.SelectedItem.ToString();
            //根据小区属性确定待下载的小区数据文件
            //ArrayList list1 = new ArrayList();
            //list1 = txtToList(inNeedInfo);

            var List2 = txtFileToList(inNeedInfo);
            restCnt = List2.Count();
            listleft = List2;

            //ShowInfo(listView1, List2);
            ShowlvInfo(List2);

            //ShowInfo(textBox1, cnt.ToString());
            //读取LTE MR服务器文件 获得服务器IP,用户名，密码和文件存储目录
            string ftpAds, ftpPort, ftpUsr, ftpPwd, ftpPath, fc;

            try
            {
                //listView1.Columns.Add("eNB", 120, HorizontalAlignment.Left); //一步添加  
                DataSet ds = excelToDS(ftpinfoTable);
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    ftpAds = ds.Tables[0].Rows[i]["服务器IP"].ToString();
                    ftpPort = ds.Tables[0].Rows[i]["FTP端口"].ToString();
                    int fport = int.Parse(ds.Tables[0].Rows[i]["FTP端口"].ToString());
                    ftpUsr = ds.Tables[0].Rows[i]["FTP账号"].ToString();
                    ftpPwd = ds.Tables[0].Rows[i]["FTP密码"].ToString();
                    ftpPath = ds.Tables[0].Rows[i]["文件存储目录"].ToString();
                    fc = ds.Tables[0].Rows[i]["厂商"].ToString();
                    //int daydiff = -1;//昨天为-1
                    string dfdate=""; // dfdate = DateTime.Today.AddDays(daydiff).ToString("yyyy-MM-dd"); //获取日期字符串
                                      //dfdate = DateTime.Today.AddDays(daydiff).ToString("yyyyMMdd"); //获取日期字符串
                    dfdate = dateTimePicker1.Text;
                    if (fc == "大唐")
                        dfdate = dfdate.Substring(0, 4) + "-" + dfdate.Substring(4, 2) + "-" + dfdate.Substring(6, 2); 
                    dfudate = dfdate;
                    ShowInfo(textBox3, "正在检索第" + (i + 1) + "个服务器:" + fc + " " + ftpAds + " 余" + restCnt + "个站未下载");
                    takeFileFromFtp(ftpAds, ftpPort, ftpUsr, ftpPwd, ftpPath, fc, List2, dfdate, isOutputFile, localpath);
                   // MessageBox.Show("ok");
                }
                ShowInfo(textBox3, "检索完成，余 " + restCnt + " 个站未下载");
                ds.Clear();
                ds.Dispose();
                this.BeginInvoke(new MethodInvoker(delegate ()
                {
                    button1.Enabled = true;
                }));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        public void takeFileFromFtp(string ftpAds, string ftpPort, string ftpUsr, string ftpPwd, string ftpPath, string fc, List<string> list2, string dfdate, bool isOutputFile, string localpath)
        {

            ftpPath += dfdate + "/";   //获取服务器上相应日期的MR小区列表

            //基站文件夹名
            List<string> str1;
            if (fc != "中兴")
            {
                str1 = GetFileListToList(ftpAds, ftpUsr, ftpPwd, ftpPath,fc,dfdate);
            }
            else
                str1 = null;
            
            if (str1 == null)
                return;
            if (isOutputFile) writeToFile(localpath + "ftp小区映射" + dfdate.Replace("-","") + ".csv", dfdate, ftpAds, fc, str1);
           
            var Listret = list2.Intersect(str1).ToList();  //求服务器的基站列表与文件基站列表的交集
            
            listleft = listleft.Except(str1).ToList();
            
            downloadFtpFile(Listret, ftpAds, ftpPort, ftpUsr, ftpPwd, ftpPath, localpath, dfdate, fc);
            restCnt = restCnt - Listret.Count();

            //ShowInfo(listView1, listleft);
            ShowlvInfo(listleft);
        }


        public void downloadFtpFile(List<string> listret, string ftpAds, string ftpPort, string ftpUsr, string ftpPwd, string ftpPath, string locpath, string dfdate, string fc)
        {
            string serverDateStr = dfdate;
            string localDateStr = dfdate.Replace("-", "");
            string serverPath = ftpPath;
            int i = 0;
            foreach (string s in listret)
            {
                string s1 = s;
                i++;
                ShowInfo(textBox2, "正在下载"+ fc + "," + ftpAds + ":第" + i +"个站:" + s1);
                if (fc == "大唐")
                {
                    s1 = "ENB=" + s1;
                    serverPath = serverDateStr + "/" + s1 + "/";
                }
                else
                {
                    serverPath = ftpPath + s1 + "/";
                }
                string loacalSitePath = locpath + localDateStr + "/" + s1 + "/";
                createDir(loacalSitePath);

                //setConString(ftpAds, ftpPort, ftpUsr, ftpPwd, ftpPath + s1 + "/");

                //List<string> str2;
                if (fc != "中兴")
                {
                    List<string> ret = new List<string>();
                    FtpWebRequest reqFTP;
                    try
                    {
                        reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + ftpAds + "/" + serverPath));
                        reqFTP.UseBinary = true;
                        reqFTP.Credentials = new NetworkCredential(ftpUsr, ftpPwd);
                        reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                        WebResponse response = reqFTP.GetResponse();
                        StreamReader reader = new StreamReader(response.GetResponseStream());

                        string line = reader.ReadLine();
                        while (line != null)
                        {   //line为MR文件
                            if (line.Contains(MRType) && line.Contains(localDateStr + Hour)) 
                            {
                                DownLoadFtpOneFile(reqFTP, ftpAds,ftpUsr,ftpPwd, loacalSitePath, serverPath, line);

                            }
                            line = reader.ReadLine();
                        }
                        //result.Remove(result.ToString().LastIndexOf('\n'), 1);
                        reader.Close();
                        response.Close();

                    }
                    catch (Exception ex)
                    {
                        //System.Windows.Forms.MessageBox.Show("获取文件信息失败:" + ex.Message + ftpServerIP, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ShowInfo(textBox3, "获取文件信息失败:" + ex.Message + ftpServerIP + "操作失败");
                    }
                }
                ShowInfo(textBox2, "下载完成" + fc + "," + ftpAds + ":第" + i + "个站:" + s1);
            }
        }
        public void DownLoadFtpOneFile(FtpWebRequest reqFTP, string ftpAds,string ftpUsr,string ftpPwd,string localPath, string serverPath, string serverfile)
        {
            try
            {
                if (!File.Exists(localPath + serverfile))
                {
                    FileStream outputStream = new FileStream(localPath + "\\" + serverfile, FileMode.Create);

                    reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + ftpAds + "/" + serverPath + serverfile));
                    reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                    reqFTP.UseBinary = true;
                    reqFTP.Credentials = new NetworkCredential(ftpUsr, ftpPwd);
                    FtpWebResponse response2 = (FtpWebResponse)reqFTP.GetResponse();
                    Stream ftpStream = response2.GetResponseStream();
                    long cl = response2.ContentLength;
                    int bufferSize = 2048;
                    int readCount;
                    byte[] buffer = new byte[bufferSize];

                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                    while (readCount > 0)
                    {
                        outputStream.Write(buffer, 0, readCount);
                        readCount = ftpStream.Read(buffer, 0, bufferSize);
                    }

                    ftpStream.Close();
                    outputStream.Close();
                    response2.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        
    }


}
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;
using Oracle.DataAccess.Client;
using System.Text;
using Lucene.Net.Analysis;
using Microsoft.Office.Interop.OneNote;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace OnenoteOCRDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        #region 全局变量
        private string __OutputFileName = string.Empty;
        private WebClient client = new WebClient();
        private Stopwatch stw = new Stopwatch();

        #endregion

        #region 系统函数
        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region 用户函数
        private bool fn数据验证()
        {
            if ((bool)this.rbtn本地图片.IsChecked)
            {
                if (!File.Exists(this.txt本地图片.Text))
                {
                    this.labMsg.Content = "本地图片不存在，请重新选择。";
                    this.txt本地图片.Focus();
                    return true;
                }
            }
            if (!Directory.Exists(this.txt输出目录.Text))
            {
                this.labMsg.Content = "输出目录不存在，请重新选择。";
                this.txt输出目录.Focus();
                return true;
            }

            return false;
        }

        private void fnStartDownload(string v_strImgPath, string v_strOutputDir, out string v_strTmpPath)
        {
            int n = v_strImgPath.LastIndexOf('/');
            string URLAddress = v_strImgPath.Substring(0, n);
            string fileName = v_strImgPath.Substring(n + 1, v_strImgPath.Length - n - 1);
            this.__OutputFileName = v_strOutputDir + "\\" + fileName.Substring(0, fileName.LastIndexOf(".")) + ".txt";

            if (!Directory.Exists(System.Configuration.ConfigurationManager.AppSettings["tmpPath"]))
            {
                Directory.CreateDirectory(System.Configuration.ConfigurationManager.AppSettings["tmpPath"]);
            }

            string Dir = System.Configuration.ConfigurationManager.AppSettings["tmpPath"];
            v_strTmpPath = Dir + "\\" + fileName;
            this.client.DownloadFile(v_strImgPath, v_strTmpPath);
        }
        
        private void fnOCR(string v_strImgPath)
        {
            //获取图片的Base64编码
            FileInfo file = new FileInfo(v_strImgPath);

            using (MemoryStream ms = new MemoryStream())
            {
                Bitmap bp = new Bitmap(v_strImgPath);

                switch (file.Extension.ToLower())
                {
                    case ".jpg":
                        bp.Save(ms, ImageFormat.Jpeg);
                        break;
                    case ".jpeg":
                        bp.Save(ms, ImageFormat.Jpeg);
                        break;
                    case ".gif":
                        bp.Save(ms, ImageFormat.Gif);
                        break;
                    case ".bmp":
                        bp.Save(ms, ImageFormat.Bmp);
                        break;
                    case ".tiff":
                        bp.Save(ms, ImageFormat.Tiff);
                        break;
                    case ".png":
                        bp.Save(ms, ImageFormat.Png);
                        break;
                    case ".emf":
                        bp.Save(ms, ImageFormat.Emf);
                        break;
                    default:
                        this.labMsg.Content = "不支持的图片格式。";
                        return;
                }

                byte[] buffer = ms.GetBuffer();
                string _Base64 = Convert.ToBase64String(buffer);

                //向Onenote2010中插入图片
                var onenoteApp = new Microsoft.Office.Interop.OneNote.Application();


                /*string sectionID; Console.WriteLine("wang");
                onenoteApp.OpenHierarchy(AppDomain.CurrentDomain.BaseDirectory + "tmpPath/" + "newfile.one", 
                    null, out sectionID, Microsoft.Office.Interop.OneNote.CreateFileType.cftSection);
                string pageID = "{A975EE72-19C3-4C80-9C0E-EDA576DAB5C6}{1}{B0}";  // 格式 {guid}{tab}{??}
                onenoteApp.CreateNewPage(sectionID, out pageID, Microsoft.Office.Interop.OneNote.NewPageStyle.npsBlankPageNoTitle);
                */

                var existingPageId = "";
                //var pageNode;
                string notebookXml;
                if (existingPageId == "")
                {
                    onenoteApp.GetHierarchy(null, Microsoft.Office.Interop.OneNote.HierarchyScope.hsPages, out notebookXml);
                    //onenoteApp.GetHierarchy(pageID, HierarchyScope.hsPages, out notebookXml);

                    var doc = XDocument.Parse(notebookXml);
                    var ns = doc.Root.Name.Namespace;
                    var sectionNode = doc.Descendants(ns + "Section").FirstOrDefault();
                    var sectionID = sectionNode.Attribute("ID").Value;
                    onenoteApp.CreateNewPage(sectionID, out existingPageId);
                    var pageNode = doc.Descendants(ns + "Page").FirstOrDefault();
                    if (pageNode != null)
                    {
                        //Image Type 只支持这些类型：auto|png|emf|jpg
                        string ImgExtension = file.Extension.ToLower().Substring(1);
                        switch (ImgExtension)
                        {
                            case "jpg":
                                ImgExtension = "jpg";
                                break;
                            case "png":
                                ImgExtension = "png";
                                break;
                            case "emf":
                                ImgExtension = "emf";
                                break;
                            default:
                                ImgExtension = "auto";
                                break;
                        }


                        var page = new XDocument(new XElement(ns + "Page", new XAttribute("ID", existingPageId),
                                         new XElement(ns + "Outline",
                                           new XElement(ns + "OEChildren",
                                             new XElement(ns + "OE",
                                               new XElement(ns + "Image",
                                                 new XAttribute("format", ImgExtension), new XAttribute("originalPageNumber", "0"),
                                                 new XElement(ns + "Position",
                                                        new XAttribute("x", "0"), new XAttribute("y", "0"), new XAttribute("z", "0")),
                                                 new XElement(ns + "Size",
                                                        new XAttribute("width", bp.Width.ToString()), new XAttribute("height", bp.Height.ToString())),
                                                    new XElement(ns + "Data", _Base64)))))));
                        //page.Root.SetAttributeValue("ID", existingPageId);
                        onenoteApp.UpdatePageContent(page.ToString(), DateTime.MinValue);

                        //线程休眠时间，单位毫秒，若图片很大，则延长休眠时间，保证Onenote OCR完毕
                        System.Threading.Thread.Sleep(Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["WaitTIme"]));

                        string pageXml;
                        onenoteApp.GetPageContent(existingPageId, out pageXml, Microsoft.Office.Interop.OneNote.PageInfo.piBinaryData);//piAll

                        //获取OCR后的内容
                        FileStream tmpXml = new FileStream(System.Configuration.ConfigurationManager.AppSettings["tmpPath"] + @"\tmp.xml", FileMode.Create, FileAccess.ReadWrite);
                        StreamWriter sw = new StreamWriter(tmpXml);
                        sw.Write(pageXml);
                        sw.Flush();
                        sw.Close();
                        tmpXml.Close();

                        FileStream tmpOnenote = new FileStream(System.Configuration.ConfigurationManager.AppSettings["tmpPath"] + @"\tmp.xml", FileMode.Open, FileAccess.ReadWrite);
                        XmlReader reader = XmlReader.Create(tmpOnenote);
                        XElement rdlc = XElement.Load(reader);

                        XmlNameTable nameTable = reader.NameTable;
                        XmlNamespaceManager mgr = new XmlNamespaceManager(nameTable);
                        mgr.AddNamespace("one", ns.ToString());

                        StringReader sr = new StringReader(pageXml);
                        XElement onenote = XElement.Load(sr);

                        var xml = from o in onenote.XPathSelectElements("//one:Image", mgr)
                                  select o.XPathSelectElement("//one:OCRText", mgr).Value;
                        this.txtOCRed.Text = (xml.First().ToString()).Replace(" ", "");

                        sr.Close();
                        reader.Close();
                        tmpOnenote.Close();
                        onenoteApp.DeleteHierarchy(existingPageId);
                        //onenoteApp.DeleteHierarchy(sectionID, DateTime.MinValue, true);  // 摧毁原始页面
                    }
                }

                /*Onenote 2010 中图片的XML格式
                   <one:Image format="" originalPageNumber="0" lastModifiedTime="" objectID="">
                        <one:Position x="" y="" z=""/>
                        <one:Size width="" height=""/>
                        <one:Data>Base64</one:Data>
                  
                        //以下标签由Onenote 2010自动生成，不要在程序中处理，目标是获取OCRText中的内容。
                        <one:OCRData lang="en-US">
                        <one:OCRText>
                            <![CDATA[   OCR后的文字   ]]>
                        </one:OCRText>
                        <one:OCRToken startPos="0" region="0" line="0" x="4.251968383789062" y="3.685039281845092" width="31.18110275268555" height="7.370078563690185"/>
                        <one:OCRToken startPos="7" region="0" line="0" x="39.40157318115234" y="3.685039281845092" width="13.32283401489258" height="8.78740119934082"/>
                        <one:OCRToken startPos="12" region="0" line="1" x="4.251968383789062" y="17.85826683044434" width="23.52755928039551" height="6.803150177001953"/>
                        <one:OCRToken startPos="18" region="0" line="1" x="32.031494140625" y="17.85826683044434" width="41.10236358642578" height="6.803150177001953"/>
                        <one:OCRToken startPos="28" region="0" line="1" x="77.66928863525391" y="17.85826683044434" width="31.46456718444824" height="6.803150177001953"/>
                        ................
                   </one:Image>      
                */


                /*ObjectID格式
                  The representation of an object to be used for identification of objects on a page. Not unique through OneNote, but unique on the page and the hierarchy. 
                   <xsd:simpleType name="ObjectID" ">
                      <xsd:restriction base="xsd:string">
                         <xsd:pattern value="\{[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\}\{[0-9]+\}\{[A-Z][0-9]+\}" />
                      </xsd:restriction>
                   </xsd:simpleType>
                */

                
            }
        }
        #endregion

        #region 系统事件
        private void btn浏览_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "请选择一个本地图片";
            ofd.Multiselect = false;
            ofd.Filter = "支持的图片格式(*.jpg,*.jpeg,*.gif,*.bmp,*.png,*.tiff,*.emf)|*.jpg;*.jpeg;*.gif;*.bmp;*.png;*.tiff;*.emf";

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.txt本地图片.Text = ofd.FileName;
                this.img图片.Source = new BitmapImage(new Uri(ofd.FileName, UriKind.RelativeOrAbsolute));

                this.labMsg.Content = "本地图片已成功加载。";
            }
        }

        private void rbtn本地图片_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                if ((bool)this.rbtn本地图片.IsChecked)
                {
                    this.txt本地图片.Text = string.Empty;
                    this.txt本地图片.IsEnabled = true;
                    this.btn浏览.IsEnabled = true;
                    this.txt本地图片.Focus();
                    this.img图片.Source = null;
                }
            }
        }

        private void btn输出浏览_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "请选择一个输出目录";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.txt输出目录.Text = fbd.SelectedPath;
            }
        }

        private void btn清空_Click(object sender, RoutedEventArgs e)
        {
            this.txtOCRed.Text = string.Empty;
            this.txtOCRed.Focus();
        }

        private void btnOCR_Click(object sender, RoutedEventArgs e)
        {
            if (this.fn数据验证())
            {
                return;
            }

            try
            {
                stw.Reset(); //停止时间间隔测量，并将运行时间重置为零。
                stw.Start();
                DirectoryInfo dir = new DirectoryInfo(this.txt输出目录.Text);

                if ((bool)this.rbtn本地图片.IsChecked)
                {
                    this.fnOCR(this.txt本地图片.Text);

                    FileInfo file = new FileInfo(this.txt本地图片.Text);
                    string name = file.Name.Substring(0, file.Name.LastIndexOf("."));
                    this.__OutputFileName = dir.FullName + @"\" + name + ".txt";
                }

                FileStream fs = new FileStream(this.__OutputFileName, FileMode.Create, FileAccess.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(this.txtOCRed.Text);
                sw.Flush();
                sw.Close();
                fs.Close();

                stw.Stop();
                this.labMsg.Content = "OCR成功。" + "程序共运行时间:" + stw.Elapsed.Seconds.ToString() + "秒" ;
            }
            catch
            {
                this.labMsg.Content = "OCR失败。";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            searchWord.Text = "人物";

            /*string connString = "User ID=oracle;Password=123456;Data Source=XE";
            OracleConnection conn = new OracleConnection(connString);
            try
            {
                conn.Open();
                FileStream fs = new FileStream("C:/Users/red_tongue/Desktop/wang.txt", FileMode.Open, FileAccess.Read);
                StreamReader read = new StreamReader(fs, System.Text.Encoding.Default);
                string str;
                int i = 0;
                while (read.Peek() != -1)
                {
                    i++;
                    str = read.ReadLine();
                    string sql = "insert into same(same_id,word) values('" + i + "','" + str + "')";
                    //string sql ="update chnword set item = '" + s[1] + "' where unicode = '" + s[0] + "'";
                    OracleCommand command = new OracleCommand(sql, conn);
                    command.ExecuteReader();
                }
                Console.WriteLine("ok");
                read.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                //ShowErrorMessage(ex.Message.ToString());
            }
            finally
            {
                conn.Close();
            }*/
           
            //对应到chnword表中去
            /*int N = 4000;
            string[] unicode_word = new string[1000]; //存储unico码对应的近义词
            for(int i = 0;i < 1000; i++)
            {
                unicode_word[i] = "" + (i + 1 + N) + "&";
            }
                string word = null;
                string[] s;
                int sum;
                int id = 0;
                FileStream fs = new FileStream("C:/Users/red_tongue/Desktop/wang.txt", FileMode.Open, FileAccess.Read);
                StreamReader read = new StreamReader(fs, System.Text.Encoding.Default);
                while (read.Peek() != -1)
                {
                    word = read.ReadLine();
                    id++;
                    s = word.Split(',');
                    for (int i = 0; i < s.Length; i++)
                    {
                        sum = 0;
                        foreach (char index in s[i])
                        {
                            sum += (index - 19968);       //得到每个近义词的unicode码
                        }
                        sum %= 5000;        //对五千取余
                        if (sum < N + 1000 && sum >= N)
                        {
                            unicode_word[sum - N] += (s[i] + "," + id + ",");
                        }
                    }
                }

            //FileStream fs = new FileStream("C:/Users/red_tongue/Desktop/wangyunxiang.txt", FileMode.Create);
            //StreamWriter sw = new StreamWriter(fs);
            StreamWriter sw = File.AppendText("C:/Users/red_tongue/Desktop/wangyunxiang.txt");
            //开始写入
            for (int i = 0; i < 1000; i++)
            {
                sw.WriteLine(unicode_word[i]);
            }
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            //fs.Close();
            Console.WriteLine("OK!");*/

            //写入word表中
            /*string connString = "User ID=oracle;Password=123456;Data Source=XE";
            OracleConnection conn = new OracleConnection(connString);
            try
            {
                conn.Open();
                FileStream fs = new FileStream("C:/Users/red_tongue/Desktop/wangyunxiang.txt", FileMode.Open, FileAccess.Read);
                StreamReader read = new StreamReader(fs, System.Text.Encoding.Default);
                string str;
                int i = 0;
                while (read.Peek() != -1)
                {
                    str = read.ReadLine();
                    string[] s = str.Split('&');
                    string sql = "insert into chnword(unicode,item) values('" + s[0] + "','" + s[1] + "')";
                    //string sql ="update chnword set item = '" + s[1] + "' where unicode = '" + s[0] + "'";
                    OracleCommand command = new OracleCommand(sql, conn);
                    command.ExecuteReader();
                }
                Console.WriteLine("ok");
                read.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                //ShowErrorMessage(ex.Message.ToString());
            }
            finally
            {
                conn.Close();
            }*/
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DirectoryInfo dir = new DirectoryInfo(System.Configuration.ConfigurationManager.AppSettings["tmpPath"]);
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
        }
        
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Remove(0, sb.Length);
            string t1 = "";
            int i = 0;
            Analyzer analyzer = new Lucene.China.ChineseAnalyzer();
            StringReader sr = new StringReader("今天天气真的很好！");
            TokenStream stream = analyzer.TokenStream(null, sr);

            long begin = System.DateTime.Now.Ticks;
            Token t = stream.Next();
            while (t != null)
            {
                t1 = t.ToString();   //显示格式： (关键词,0,2) ，需要处理
                t1 = t1.Replace("(", "");
                char[] separator = { ',' };
                t1 = t1.Split(separator)[0];

                sb.Append(i + ":" + t1 + "\r\n");
                Console.WriteLine(t1);
                t = stream.Next();
                i++;
            }
            txtSearched.Text += "ok";
            txtSearched.Text += sb.ToString();
        }

        private void btn_search_Click(object sender, RoutedEventArgs e)
        {
            /*string id = get_id(searchWord.Text);
            txtSearched.Text += ("id="+id + ";");
            string[] s = get_word(txtOCRed.Text);
            //string[] s = get_word("人，士，人物，人士，人氏，人选");
            for (int i = 0;i < s.Length;i++)
            {
                if (get_id(s[i]).Equals(id))
                {
                    txtSearched.Text += (s[i] + ";");
                }
            }*/
            searchWord.Text = "ok!"+ searchWord.Text;
        }

        public string get_id(string text)
        {
            string connString = "User ID=oracle;Password=123456;Data Source=XE";
            OracleConnection conn = new OracleConnection(connString);
            string sql = "";
            try
            {
                conn.Open();
                int sum = 0;
                foreach (char index in text)
                {
                    sum += (index - 19968);       //得到每个近义词的unicode码
                }
                sum %= 5000;        //对五千取余
                //sum = 3;
                sum++;
                Console.WriteLine(text+"sum:"+sum);
                //txtSearched.Text += "sum:" + sum + ";";
                sql = "select item from chnword where unicode = '" + sum + "'";
                //sql = "insert into wang(unicode,item) values('3','haha')";
                OracleCommand command = new OracleCommand(sql, conn);
                OracleDataReader reader = command.ExecuteReader();
                string word = null;
                string id = "w";
                while (reader.Read())
                {
                    word = reader.GetString(0); ;
                    Console.WriteLine("ok");
                    //txtSearched.Text = "id:" +word+";";
                    string[] s = word.Split(',');
                    for (int i = 0; i < s.Length; i++)
                    {
                        if (s[i].Equals(text))
                        {
                            id = s[i + 1];
                            break;
                        }
                        i++;
                    }
                }
                //txtSearched.Text += "id:" + id + ";";
                return id ;
                /*sql = "select word from same where same_id = '" + id + "'";
                command = new OracleCommand(sql, conn);
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    word = reader.GetString(0);
                    txtSearched.Text += word;
                }*/
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                //ShowErrorMessage(ex.Message.ToString());
            }
            finally
            {
                conn.Close();
            }
            return "不存在！";
        }

        public string[] get_word(string index)
        {
            string[] s =new string[100];
            StringBuilder sb = new StringBuilder();
            sb.Remove(0, sb.Length);
            string t1 = "";
            int i = 0;
            Analyzer analyzer = new Lucene.China.ChineseAnalyzer();
            StringReader sr = new StringReader(index);
            TokenStream stream = analyzer.TokenStream(null, sr);

            long begin = System.DateTime.Now.Ticks;
            Token t = stream.Next();
            while (t != null)
            {
                t1 = t.ToString();   //显示格式： (关键词,0,2) ，需要处理
                t1 = t1.Replace("(", "");
                char[] separator = { ',' };
                t1 = t1.Split(separator)[0];
                s[i] = t1;
                t = stream.Next();
                i++;
            }
            return s;
        }
        #endregion

        public class ocr_test
        {
            public void test()
            {
                MainWindow win = new MainWindow();
                win.ShowDialog();
            }
        }

    }
}

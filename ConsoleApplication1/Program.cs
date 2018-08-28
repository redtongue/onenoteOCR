using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConsoleApplication1
{
     class Program
    {
        private static Stopwatch stw = new Stopwatch();
        static void Main(string[] args)
        {
            String filename = "C:\\Users\\red_tongue\\Desktop\\test.png";
            Console.Write("输入待OCR文件：");
            //String filename = Console.ReadLine();
            string outputFilename = "C:\\Users\\red_tongue\\Desktop";
            Console.Write("输入OCR结果目录：");
            //string outputFilename = Console.ReadLine();
            stw.Reset(); //停止时间间隔测量，并将运行时间重置为零。
            stw.Start();
            DirectoryInfo dir = new DirectoryInfo(outputFilename);
            string txtOCRed = "";
            try
            {
                txtOCRed = fnOCR(filename);
                Console.WriteLine(txtOCRed);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }


            FileInfo file = new FileInfo(filename);
            string name = file.Name.Substring(0, file.Name.LastIndexOf("."));
            string __OutputFileName = dir.FullName + @"\" + name + ".txt";
            FileStream fs = new FileStream(__OutputFileName, FileMode.Create, FileAccess.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(txtOCRed);
            sw.Flush();
            sw.Close();
            fs.Close();

            Console.WriteLine("ok");
            Console.ReadLine();
        }

        private static string fnOCR(string v_strImgPath)
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
                        //this.labMsg.Content = "不支持的图片格式。";
                        return "不支持的图片格式。";
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
                        System.Threading.Thread.Sleep(Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["WaitTIme"]));
                        /*try
                        {
                            My.onenoteApp = onenoteApp;
                        My.page = page;
                        Thread thread = new Thread(new ThreadStart(My.update));
                        thread.Start();
                        thread.Join();
                        //线程休眠时间，单位毫秒，若图片很大，则延长休眠时间，保证Onenote OCR完毕
                            //System.Threading.Thread.Sleep(Int32.Parse(System.Configuration.ConfigurationManager.AppSettings["WaitTIme"]));
                            //int fileSize = Convert.ToInt32(file.Length / 1024 / 1024);
                            //System.Threading.Thread.Sleep(1000 * (fileSize > 1 ? fileSize : 1));
                        }catch(Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }*/

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
                        string txtOCRed = (xml.First().ToString()).Replace(" ", "");

                        sr.Close();
                        reader.Close();
                        tmpOnenote.Close();
                        onenoteApp.DeleteHierarchy(existingPageId);
                        return txtOCRed;
                        //onenoteApp.DeleteHierarchy(sectionID, DateTime.MinValue, true);  // 摧毁原始页面
                    }
                }
                return "null";
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
        
    }
}

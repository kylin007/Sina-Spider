using Ivony.Html;
using Ivony.Html.Parser;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.IE;
using Skay.WebBot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium.Support.UI;

namespace Sina
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread th = new Thread(Sina);
            th.Start();
        }
        public static void Sina()
        {

            IWebDriver driver = new ChromeDriver();

            Console.WriteLine("正在打开浏览器");
            driver.Url = "http://weibo.com/";

            driver.FindElement(By.Id("loginname")).SendKeys("15236155225");
            driver.FindElement(By.ClassName("password")).FindElement(By.ClassName("W_input")).SendKeys("yishi123.");
            driver.FindElements(By.ClassName("login_btn"))[0].Click();
            SqlConnection conn = new SqlConnection("Data Source=.;Initial Catalog=Sina1;Integrated Security=True");
            conn.Open();

            HttpUtility http = new HttpUtility();

            int weiboID = 0;
            int all_weiboID = 444;//--------------------------------------------------最后一条微博的ID
            
            string[] uurrll = { "http://weibo.com/2087169013/E9LDdxpos", "http://weibo.com/1992613670/E8Ya59iag", "http://weibo.com/1644114654/Ea1wOFIR6", "http://weibo.com/2028810631/E9NPPuSMF", "http://weibo.com/1642088277/E9VjpbjTn", "http://weibo.com/2803301701/E9VeWi5gD", "http://weibo.com/1784473157/E9VgHifON"};
            for(int weibou = 0; weibou<uurrll.Length; weibou++)
            {
                weiboID = all_weiboID+1;
                all_weiboID = weiboID;

                string weibourl = null;
                int k = 1;//行数
                int j = 0;
                int flag = 0;
                string sql = null;
                int floor = 1;
                int fatherWeiboID = weiboID;
                int rootWeiboID = weiboID;
                int pagemax = 4;//--------------------------
                string repostnum = null;

                //事件的抓取  获取第二层的连接
                Console.WriteLine("事件的抓取  获取第二层的连接");
                //-----------------------------------------------------//循环节  每次需 1.添加网址  3.修改weiboID
                weibourl = uurrll[weibou].ToString();//-------------------------------------------------------------------

                driver.Url = weibourl;

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("WB_info")));

                string html = driver.PageSource;
                var documenthtml = new JumonyParser().Parse(html);
                string userID;          ///用户昵称
                string content;         ///微博内容
                string postTime;        ///发送时间
                string datatime;        ///抓取时;


                userID = documenthtml.FindFirst(".WB_info .W_f14").InnerText();
                content = documenthtml.FindFirst(".WB_text").InnerText();
                postTime = documenthtml.FindFirst(".WB_from a").InnerText();
                datatime = DateTime.Now.ToLocalTime().ToString();
                content = content.Replace('\'', '-').Replace('\n', ' ');

                sql = string.Format("INSERT INTO N4_weibo_msg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", weiboID, userID, content, postTime, datatime, weibourl, floor, fatherWeiboID, rootWeiboID);
                SqlCommand com = new SqlCommand(sql, conn);
                com.ExecuteNonQuery();
                Console.WriteLine("--------------------微博信息插入完成-------");

                var commenttext = documenthtml.Find(".list_ul .list_li");
                string commentUID;      ///评论用户 ID
                string commentcontent;  ///评论内容
                string commentTime;     ///评论时间

                foreach (var item1 in commenttext)
                {
                    string txt = item1.FindFirst(".WB_text").InnerText();
                    commentUID = txt.Split('：')[0].ToString();
                    commentcontent = txt.Split('：')[1].ToString();
                    commentTime = item1.FindFirst(".WB_from").InnerText();

                    commentcontent = commentcontent.Replace('\'', '-').Replace('\n', ' ');
                    sql = string.Format("INSERT INTO N4_comment_msg VALUES ('{0}','{1}','{2}','{3}')", weiboID, commentUID, commentcontent, commentTime);
                    com = new SqlCommand(sql, conn);
                    com.ExecuteNonQuery();
                }
                for (int page = 0; page < pagemax; page++) //向下抓取页数-
                {

                    try
                    {
                        driver.FindElement(By.ClassName("W_pages")).FindElement(By.ClassName("next")).Click();

                        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成
                        wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("WB_text")));

                        Thread.Sleep(3000);

                        html = driver.PageSource;
                        var documenthtml_1 = new JumonyParser().Parse(html);

                        commenttext = documenthtml_1.Find(".list_ul .list_li");

                        foreach (var item1 in commenttext)
                        {
                            string txt = item1.FindFirst(".WB_text").InnerText();
                            commentUID = txt.Split('：')[0].ToString();
                            commentcontent = txt.Split('：')[1].ToString();
                            commentTime = item1.FindFirst(".WB_from").InnerText();

                            commentcontent = commentcontent.Replace('\'', '-').Replace('\n', ' ');
                            sql = string.Format("INSERT INTO N4_comment_msg VALUES ('{0}','{1}','{2}','{3}')", weiboID, commentUID, commentcontent, commentTime);
                            com = new SqlCommand(sql, conn);
                            com.ExecuteNonQuery();

                        }//foreach
                    }
                    catch { }

                }//页数for循环

                //------------------------------------以上为每条微博评论信息----------------------
                //------------------------------------以下为每条微博转发信息----------------------

                string repostuserID;    ///转发博主昵称
                fatherWeiboID = weiboID;      ///从那条微博转发而来的
                string repostcontent;   ///微博内容
                string repostTime;      ///转发时间

                string url1 = weibourl + "?type=repost";


                driver.Url = url1;
                Console.WriteLine("--------------------等待打开微博的repost-------");

                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("WB_text")));
                Thread.Sleep(6000);

                html = driver.PageSource;
                documenthtml = new JumonyParser().Parse(html);

                var reposttext = documenthtml.Find(".list_ul .list_li");
                j = 0; floor++;//层数增加
                flag = 0;
                foreach (var ite in reposttext)
                {

                    string txt = ite.FindFirst(".WB_text").InnerText();
                    repostuserID = txt.Split('：')[0].ToString();
                    repostcontent = txt.Split('：')[1].ToString();

                    repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
                    string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
                    repostweibo_url = "http://weibo.com" + repostweibo_url;

                    datatime = DateTime.Now.ToLocalTime().ToString();
                    repostcontent = repostcontent.Replace('\'', '-').Replace('\n', ' ');

                    var sss = ite.Find("li");
                    int i = 0;
                    foreach (var ppap in sss)
                    {
                        i++;
                        if (i == 2)
                        {
                            repostnum = ppap.FindFirst("a").InnerText();
                            break;
                        }
                    }
                    j = repostnum.Length;
                    if (j > 2)
                    {
                        weiboID++;
                        all_weiboID++;
                        sql = string.Format("INSERT INTO N4_weibo_msg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", weiboID, repostuserID, repostcontent, repostTime, datatime, repostweibo_url, floor, fatherWeiboID, rootWeiboID);
                        com = new SqlCommand(sql, conn);
                        com.ExecuteNonQuery();
                    }
                    sql = string.Format("INSERT INTO N4_report_msg VALUES ('{0}','{1}','{2}','{3}','{4}')", repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
                    com = new SqlCommand(sql, conn);
                    com.ExecuteNonQuery();
                    flag = 1;
                }
                if (flag == 0)
                {
                    var sss = documenthtml.FindFirst(".WB_feed_handle").FindFirst(".WB_handle").Find("li");
                    int i = 0;
                    foreach (var ppap in sss)
                    {
                        i++;
                        if (i == 2)
                        {
                            repostnum = ppap.FindFirst(".line").FindLast("em").InnerText();
                            break;
                        }
                    }
                    j = repostnum.Length;
                    if (j == 2)
                    {
                        flag = 1;
                    }
                }
                if (flag == 0)
                {
                    Console.WriteLine("----------为成功打开--------------");
                }
                for (int page = 1; page < pagemax; page++) //向下抓取页数
                {

                    try
                    {
                        driver.FindElement(By.ClassName("W_pages")).FindElement(By.ClassName("next")).Click();
                        
                        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成
                        wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("list_ul")));
                        Thread.Sleep(3000);

                        html = driver.PageSource;
                        documenthtml = new JumonyParser().Parse(html);

                        reposttext = documenthtml.Find(".list_ul .list_li");

                        foreach (var ite in reposttext)
                        {

                            string txt = ite.FindFirst(".WB_text").InnerText();
                            repostuserID = txt.Split('：')[0].ToString();
                            repostcontent = txt.Split('：')[1].ToString();

                            repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
                            string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
                            repostweibo_url = "http://weibo.com" + repostweibo_url;

                            datatime = DateTime.Now.ToLocalTime().ToString();
                            repostcontent = repostcontent.Replace('\'', '-').Replace('\n', ' ');

                            sql = string.Format("INSERT INTO N4_report_msg VALUES ('{0}','{1}','{2}','{3}','{4}')", repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
                            com = new SqlCommand(sql, conn);
                            com.ExecuteNonQuery();

                        }
                    }
                    catch { }

                }//页数for循环

                DataTable dt = new DataTable();

                for (int ii = 2; ii < 6; ii++)
                {
                    //
                    Console.WriteLine("第" + ii + "层的抓取  获取第" + (ii + 1) + "层连接");
                    dt = new DataTable();
                    sql = "select * from N4_weibo_msg where floor = " + floor + " and rootWeiboID = " + rootWeiboID;//-------找到要循环的url 开始下一层
                    SqlDataAdapter dapt = new SqlDataAdapter(sql, conn);
                    dapt.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        Console.WriteLine("抓取此事件完成  共抓取" + (ii - 1) + "层");
                        break;
                    }
                    for (k = 0; k < dt.Rows.Count; k++)
                    {
                        weibourl = dt.Rows[k]["weibourl"].ToString();
                        weiboID = int.Parse(dt.Rows[k]["weiboID"].ToString());
                        floor = int.Parse(dt.Rows[k]["floor"].ToString());
                        floor++;
                        fatherWeiboID = weiboID;

                        driver.Url = weibourl;
                        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成
                        wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("WB_text")));
                        Thread.Sleep(4000);

                        html = driver.PageSource;
                        documenthtml = new JumonyParser().Parse(html);

                        commenttext = documenthtml.Find(".list_ul .list_li");
                        flag = 0;
                        foreach (var item1 in commenttext)
                        {
                            string txt = item1.FindFirst(".WB_text").InnerText();
                            commentUID = txt.Split('：')[0].ToString();
                            commentcontent = txt.Split('：')[1].ToString();
                            commentTime = item1.FindFirst(".WB_from").InnerText();

                            commentcontent = commentcontent.Replace('\'', '-').Replace('\n', ' ');
                            sql = string.Format("INSERT INTO N4_comment_msg VALUES ('{0}','{1}','{2}','{3}')", weiboID, commentUID, commentcontent, commentTime);
                            com = new SqlCommand(sql, conn);
                            com.ExecuteNonQuery();
                            flag = 1;
                        }
                        if (flag == 0)
                        {
                            var sss = documenthtml.FindFirst(".WB_feed_handle").FindFirst(".WB_handle").Find("li");
                            int i = 0;
                            foreach (var ppap in sss)
                            {
                                i++;
                                if (i == 3)
                                {
                                    repostnum = ppap.FindFirst(".line").FindLast("em").InnerText();
                                    break;
                                }
                            }
                            j = repostnum.Length;
                            if (j <= 2)
                            {
                                flag = 1;
                            }
                        }
                        if (flag == 0)
                        {
                            Console.WriteLine("----------为成功打开--------------");
                        }
                        for (int page = 0; page < pagemax; page++) //向下抓取页数
                        {

                            try
                            {
                                driver.FindElement(By.ClassName("W_pages")).FindElement(By.ClassName("next")).Click();
                                
                                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成
                                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("WB_text")));
                                Thread.Sleep(2000);
                                html = driver.PageSource;
                                var documenthtml_1 = new JumonyParser().Parse(html);

                                commenttext = documenthtml_1.Find(".list_ul .list_li");

                                foreach (var item1 in commenttext)
                                {
                                    string txt = item1.FindFirst(".WB_text").InnerText();
                                    commentUID = txt.Split('：')[0].ToString();
                                    commentcontent = txt.Split('：')[1].ToString();
                                    commentTime = item1.FindFirst(".WB_from").InnerText();

                                    commentcontent = commentcontent.Replace('\'', '-').Replace('\n', ' ');
                                    sql = string.Format("INSERT INTO N4_comment_msg VALUES ('{0}','{1}','{2}','{3}')", weiboID, commentUID, commentcontent, commentTime);
                                    com = new SqlCommand(sql, conn);
                                    com.ExecuteNonQuery();

                                }
                            }
                            catch { }

                        }//页数for循环

                        //------------------------------------以上为每条微博评论信息----------------------
                        //------------------------------------以下为每条微博转发信息----------------------

                        fatherWeiboID = weiboID;      ///从那条微博转发而来的

                        url1 = weibourl + "?type=repost";

                        //driver.FindElements(By.ClassName("pos"))[1].FindElement(By.ClassName("S_line1")).Click();
                        driver.Url = url1;
                        Console.WriteLine("--------------------等待打开微博的repost-------");

                        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成
                        wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("WB_text")));
                        Thread.Sleep(4000);

                        html = driver.PageSource;
                        documenthtml = new JumonyParser().Parse(html);

                        reposttext = documenthtml.Find(".list_ul .list_li");
                        flag = 0;
                        foreach (var ite in reposttext)
                        {
                            string txt = ite.FindFirst(".WB_text").InnerText();
                            repostuserID = txt.Split('：')[0].ToString();
                            repostcontent = txt.Split('：')[1].ToString();

                            repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
                            string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
                            repostweibo_url = "http://weibo.com" + repostweibo_url;

                            datatime = DateTime.Now.ToLocalTime().ToString();
                            repostcontent = repostcontent.Replace('\'', '-').Replace('\n', ' ');

                            var sss = ite.Find("li");
                            int i = 0;
                            foreach (var ppap in sss)
                            {
                                i++;
                                if (i == 2)
                                {
                                    repostnum = ppap.FindFirst("a").InnerText();
                                    break;
                                }
                            }
                            j = repostnum.Length;
                            if (j > 2)
                            {
                                all_weiboID++;
                                sql = string.Format("INSERT INTO N4_weibo_msg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", all_weiboID, repostuserID, repostcontent, repostTime, datatime, repostweibo_url, floor, fatherWeiboID, rootWeiboID);
                                com = new SqlCommand(sql, conn);
                                com.ExecuteNonQuery();
                            }
                            sql = string.Format("INSERT INTO N4_report_msg VALUES ('{0}','{1}','{2}','{3}','{4}')", repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
                            com = new SqlCommand(sql, conn);
                            com.ExecuteNonQuery();
                            flag = 1;
                        }
                        if (flag == 0)
                        {
                            var sss = documenthtml.FindFirst(".WB_feed_handle").FindFirst(".WB_handle").Find("li");
                            int i = 0;
                            foreach (var ppap in sss)
                            {
                                i++;
                                if (i == 3)
                                {
                                    repostnum = ppap.FindFirst(".line").FindLast("em").InnerText();
                                    break;
                                }
                            }
                            j = repostnum.Length;
                            if (j <= 2)
                            {
                                flag = 1;
                            }
                        }
                        if (flag == 0)
                        {
                            Console.WriteLine("----------为成功打开--------------");
                        }
                        for (int page = 1; page < pagemax; page++) //向下抓取页数
                        {

                            try
                            {
                                driver.FindElement(By.ClassName("W_pages")).FindElement(By.ClassName("next")).Click();
                                
                                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成
                                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.ClassName("WB_text")));
                                Thread.Sleep(2000);

                                html = driver.PageSource;
                                documenthtml = new JumonyParser().Parse(html);

                                reposttext = documenthtml.Find(".list_ul .list_li");

                                foreach (var ite in reposttext)
                                {

                                    string txt = ite.FindFirst(".WB_text").InnerText();
                                    repostuserID = txt.Split('：')[0].ToString();
                                    repostcontent = txt.Split('：')[1].ToString();

                                    repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
                                    string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
                                    repostweibo_url = "http://weibo.com" + repostweibo_url;

                                    datatime = DateTime.Now.ToLocalTime().ToString();
                                    repostcontent = repostcontent.Replace('\'', '-').Replace('\n', ' ');

                                    sql = string.Format("INSERT INTO N4_report_msg VALUES ('{0}','{1}','{2}','{3}','{4}')", repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
                                    com = new SqlCommand(sql, conn);
                                    com.ExecuteNonQuery();

                                }
                            }
                            catch { }

                        }//页数for循环
                    }
                }//for循环
            
            }//连接循环
            


            

           




            //IWebDriver driver = new ChromeDriver();

            //Console.WriteLine("正在打开浏览器");
            //driver.Url = "http://weibo.com/";

            //WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成

            //driver.FindElement(By.Id("loginname")).SendKeys("15236155225");
            //driver.FindElement(By.ClassName("password")).FindElement(By.ClassName("W_input")).SendKeys("yishi123");
            //driver.FindElements(By.ClassName("login_btn"))[0].Click();
            //SqlConnection conn = new SqlConnection("Data Source=.;Initial Catalog=Sina_third;Integrated Security=True");
            //conn.Open();

            //HttpUtility http = new HttpUtility();
            //int weiboID = 14;//---------------------------------------------------

            //for (int page = 4;  page < 10; page++)
            //{
            //    driver.Url = "http://s.weibo.com/weibo/%25E8%25AF%2588%25E9%25AA%2597%25E5%259B%25A2%25E4%25BD%2593&Refer=STopic_box&page=" + page;

            //    Console.WriteLine("--------等待打开-------");
            //    Thread.Sleep(2000);

            //    string html = driver.PageSource;
            //    var documenthtml = new JumonyParser().Parse(html);

            //    var list_all = documenthtml.Find(".WB_cardwrap .clearfix .feed_action_row4 .S_line1");
            //    int list_num = list_all.Count();

            //    var time_list = documenthtml.Find(".feed_from");
            //    int time_num = time_list.Count();

            //    int i = 0, j = 0;
            //    foreach (var item in list_all)
            //    {
            //        i++;
            //        if (i % 4 == 3)
            //        {
            //            j++;
            //            try
            //            {
            //                int num = int.Parse(item.FindFirst("em").InnerText()); //找到有评论的微博
            //                int t = 0;
            //                foreach (var ite in time_list)
            //                {
            //                    t++;
            //                    if (t == j)         //找到对应的URL
            //                    {
            //                        string weibo_url = ite.FindFirst("a").Attribute("href").Value();
            //                        weibo_url = weibo_url + "?type=comment";

            //                        //打开详细信息  开始抓取
            //                        driver.Url = weibo_url;
            //                        Thread.Sleep(2000);
            //                        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成

            //                        string userID;          ///用户昵称
            //                        string content;         ///微博内容
            //                        string postTime;        ///发送时间
            //                        string datatime;        ///抓取时;
            //                        string weibourl;        ///URl

            //                        weibourl = weibo_url;
            //                        html = driver.PageSource;
            //                        var documenthtml_1 = new JumonyParser().Parse(html);
            //                        userID = documenthtml_1.FindFirst(".WB_info .W_f14").InnerText();
            //                        content = documenthtml_1.FindFirst(".WB_text").InnerText();
            //                        postTime = documenthtml_1.FindFirst(".WB_from a").InnerText();
            //                        datatime = DateTime.Now.ToLocalTime().ToString();
            //                        content = content.Replace('\'', '-');
            //                        weiboID++;

            //                        string sql = string.Format("INSERT INTO weibo_msg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}')", weiboID, userID, content, postTime, datatime, weibourl);
            //                        SqlCommand com = new SqlCommand(sql, conn);
            //                        com.ExecuteNonQuery();
            //                        Console.WriteLine("--------------------微博信息插入完成-------");

            //                        var commenttext = documenthtml_1.Find(".list_ul .list_li");

            //                        int xx = commenttext.Count();

            //                        string commentUID;      ///评论用户 ID
            //                        string commentcontent;  ///评论内容
            //                        string commentTime;     ///评论时间

            //                        foreach (var item1 in commenttext)
            //                        {
            //                            string txt = item1.FindFirst(".WB_text").InnerText();
            //                            commentUID = txt.Split('：')[0].ToString();
            //                            commentcontent = txt.Split('：')[1].ToString();
            //                            commentTime = item1.FindFirst(".WB_from").InnerText();

            //                            commentcontent = commentcontent.Replace('\'', '-');
            //                            sql = string.Format("INSERT INTO comment VALUES ('{0}','{1}','{2}','{3}')", weiboID, commentUID, commentcontent, commentTime);
            //                            com = new SqlCommand(sql, conn);
            //                            com.ExecuteNonQuery();

            //                        }//foreach

            //                        if (num > xx) //下一页
            //                        {
            //                            //.W_pages .next 
            //                            try
            //                            {
            //                                driver.FindElement(By.ClassName("W_pages")).FindElement(By.ClassName("next")).Click();

            //                                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(100));   //等待页面加载完成

            //                                html = driver.PageSource;
            //                                var documenthtml_2 = new JumonyParser().Parse(html);

            //                                commenttext = documenthtml_2.Find(".list_ul .list_li");

            //                                foreach (var item1 in commenttext)
            //                                {
            //                                    string txt = item1.FindFirst(".WB_text").InnerText();
            //                                    commentUID = txt.Split('：')[0].ToString();
            //                                    commentcontent = txt.Split('：')[1].ToString();
            //                                    commentTime = item1.FindFirst(".WB_from").InnerText();

            //                                    commentcontent = commentcontent.Replace('\'', '-');
            //                                    sql = string.Format("INSERT INTO comment VALUES ('{0}','{1}','{2}','{3}')", weiboID, commentUID, commentcontent, commentTime);
            //                                    com = new SqlCommand(sql, conn);
            //                                    com.ExecuteNonQuery();

            //                                }//foreach
            //                            }
            //                            catch { }

            //                        }//if

            //                    }//if

            //                }//foreach

            //            }//try

            //            catch
            //            { }

            //        }//if

            //    }//foreach

            //}//for循环



            //IWebDriver driver = new FirefoxDriver();
            //driver.Url = "http://weibo.com/";
            //Console.WriteLine("--------------------等待打开浏览器-------");
            ////Thread.Sleep(10000);

            //SqlConnection conn = new SqlConnection("Data Source=.;Initial Catalog=Sina1;Integrated Security=True");
            //conn.Open();

            //HttpUtility http = new HttpUtility();


            //int weiboID = 1;//-----------------------------------------------------------------------------
            //int WBID = weiboID;
            //string url = "http://weibo.com/2803301701/DEgxNwT8g";//--------------------------------------------------

            //string url1 = url + "?type=comment";
            //driver.Url = url1;
            //Console.WriteLine("--------------------等待打开首层URL的comment-------");
            //Thread.Sleep(20000);
            //string html = driver.PageSource;
            //var documenthtml = new JumonyParser().Parse(html);

            //string userID;          ///用户昵称
            //string content;         ///微博内容
            //string postTime;        ///发送时间
            //string datatime;        ///抓取时;
            //string weibourl;        ///URl
            //int floor = 1;

            ////--------------------------------------------------------------------------------------------------------------------------
            //weibourl = url;
            //userID = documenthtml.FindFirst(".WB_info a").InnerHtml();
            //content = documenthtml.FindFirst(".WB_text").InnerText();
            //postTime = documenthtml.FindFirst(".WB_from a").InnerHtml();
            //datatime = DateTime.Now.ToLocalTime().ToString();
            //content = content.Replace('\'', '-');
            //int repostnum1 = 0;
            //try
            //{
            //    repostnum1 = int.Parse(documenthtml.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //}
            //catch { }
            //string sql = string.Format("INSERT INTO weiboMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}', '{6}','{7}','{8}')", weiboID, userID, content, postTime, datatime, weibourl, repostnum1, 29, floor);
            //SqlCommand com = new SqlCommand(sql, conn);
            //com.ExecuteNonQuery();
            //Console.WriteLine("--------------------表一插入完成-------");
            //int rootWeiboID = weiboID;
            //weiboID++;
            //floor++;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //var commenttext = documenthtml.Find(".list_ul .list_li");
            //string commentUID;      ///评论用户 ID
            //string commentcontent;  ///评论内容
            //string commentTime;     ///评论时间
            //int j = 0;

            //int commentnum = 30;

            //foreach (var item in commenttext)
            //{
            //    if (j >= commentnum)
            //    {
            //        break;
            //    }
            //    j++;
            //    string txt = item.FindFirst(".WB_text").InnerText();
            //    commentUID = txt.Split('：')[0].ToString();
            //    commentcontent = txt.Split('：')[1].ToString();
            //    commentTime = item.FindFirst(".WB_from").InnerText();
            //    int weiboidx = weiboID - 1;

            //    commentcontent = commentcontent.Replace('\'', '-');
            //    sql = string.Format("INSERT INTO commentMsg VALUES ('{0}','{1}','{2}','{3}')", weiboidx, commentUID, commentcontent, commentTime);
            //    com = new SqlCommand(sql, conn);
            //    com.ExecuteNonQuery();
            //}


            /////////////////////////////////////////////////////////////////////
            //url1 = url + "?type=repost";
            //driver.Url = url1;
            //Console.WriteLine("--------------------等待打开首条URL的repost-------");
            //Thread.Sleep(5000);
            //string html1 = driver.PageSource;
            //var documenthtml1 = new JumonyParser().Parse(html1);

            //var reposttext = documenthtml1.Find(".list_ul .list_li");

            //int Y_weiboID;          ///在表一中的ID
            //string repostuserID;    ///转发博主昵称
            //int fatherWeiboID;      ///从那条微博转发而来的

            //string repostcontent;   ///微博内容
            //string repostTime;      ///转发时间
            //fatherWeiboID = rootWeiboID;

            //j = 0;

            //int repostnum = 30;          
            //foreach (var ite in reposttext)
            //{
            //    if (j >= repostnum)
            //    {
            //        break;
            //    }
            //    j++;
            //    string txt = ite.FindFirst(".WB_text").InnerText();
            //    repostuserID = txt.Split('：')[0].ToString();
            //    repostcontent = txt.Split('：')[1].ToString();
            //    repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
            //    string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
            //    repostweibo_url = "http://weibo.com" + repostweibo_url;

            //    datatime = DateTime.Now.ToLocalTime().ToString();
            //    repostcontent = repostcontent.Replace('\'', '-');
            //    sql = string.Format("INSERT INTO weiboMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", weiboID, repostuserID, repostcontent, repostTime, datatime, repostweibo_url,0, 0, floor);
            //    com = new SqlCommand(sql, conn);
            //    com.ExecuteNonQuery();
            //    Y_weiboID = weiboID;
            //    weiboID++;

            //    sql = string.Format("INSERT INTO repostMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}')", Y_weiboID, repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
            //    com = new SqlCommand(sql, conn);
            //    com.ExecuteNonQuery();

            //}



            //Console.WriteLine("--------------------首页URL插入完成-------");

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //DataTable dt = new DataTable();
            //sql = "select * from weiboMsg";
            //SqlDataAdapter dapt = new SqlDataAdapter(sql, conn);
            //dapt.Fill(dt);

            //floor++;
            //int dtcount = dt.Rows.Count;
            //Random ran = new Random();
            //int RandKey = ran.Next(11, 18);//随机数
            //for (int k = (WBID + 1); k < (WBID+11); k++)
            //{
            //    url = dt.Rows[k]["weibourl"].ToString();
            //    url1 = url + "?type=comment";
            //    driver.Url = url1;
            //    Console.WriteLine("--------------------等待打开第" + k + "条数据的comment-------");
            //    Thread.Sleep(15000);
            //    html = driver.PageSource;
            //    documenthtml = new JumonyParser().Parse(html);

            //    fatherWeiboID = int.Parse(dt.Rows[k]["weiboID"].ToString());

            //    commenttext = documenthtml.Find(".list_ul .list_li");
            //    j = 0;
            //    commentnum = 0;
            //    try
            //    {
            //        commentnum = int.Parse(documenthtml.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //    }
            //    catch { }
            //    RandKey = ran.Next(11, 18);//随机数
            //    if (commentnum > 20)
            //    {
            //        commentnum = RandKey;
            //    }
            //    foreach (var item in commenttext)
            //    {
            //        if (j >= commentnum)
            //        {
            //            break;
            //        }
            //        j++;
            //        string txt = item.FindFirst(".WB_text").InnerText();
            //        commentUID = txt.Split('：')[0].ToString();
            //        commentcontent = txt.Split('：')[1].ToString();
            //        commentTime = item.FindFirst(".WB_from").InnerText();

            //        commentcontent = commentcontent.Replace('\'', '-');
            //        sql = string.Format("INSERT INTO commentMsg VALUES ('{0}','{1}','{2}','{3}')", fatherWeiboID, commentUID, commentcontent, commentTime);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();
            //    }


            //    url1 = url + "?type=repost";
            //    driver.Url = url1;
            //    Console.WriteLine("--------------------等待打开第" + k + "条数据的repost-------");
            //    Thread.Sleep(5000);
            //    html1 = driver.PageSource;
            //    documenthtml1 = new JumonyParser().Parse(html1);

            //    reposttext = documenthtml1.Find(".list_ul .list_li");

            //    j = 0;
            //    repostnum = 0;
            //    try
            //    {
            //        repostnum = int.Parse(documenthtml1.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //    }
            //    catch { }
            //    repostnum1 = repostnum;
            //    RandKey = ran.Next(11, 18);//随机数
            //    if (repostnum > 20)
            //    {
            //        repostnum = RandKey;
            //    }
            //    sql = string.Format("UPDATE weiboMsg SET all_repost_num = " + repostnum1 + " WHERE weiboID = "+fatherWeiboID);
            //    com = new SqlCommand(sql, conn);
            //    com.ExecuteNonQuery();
            //    sql = string.Format("UPDATE weiboMsg SET repost_num = " + repostnum + " WHERE weiboID = "+fatherWeiboID);
            //    com = new SqlCommand(sql, conn);
            //    com.ExecuteNonQuery();
            //    foreach (var ite in reposttext)
            //    {
            //        if (j >= repostnum)
            //        {
            //            break;
            //        }
            //        j++;
            //        string txt = ite.FindFirst(".WB_text").InnerText();
            //        repostuserID = txt.Split('：')[0].ToString();
            //        repostcontent = txt.Split('：')[1].ToString();

            //        rootWeiboID = WBID;
            //        repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
            //        string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
            //        repostweibo_url = "http://weibo.com" + repostweibo_url;

            //        datatime = DateTime.Now.ToLocalTime().ToString();
            //        repostcontent = repostcontent.Replace('\'', '-');
            //        sql = string.Format("INSERT INTO weiboMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}', '{8}')", weiboID, repostuserID, repostcontent, repostTime, datatime, repostweibo_url,0, 0, floor);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();
            //        Y_weiboID = weiboID;
            //        weiboID++;

            //        sql = string.Format("INSERT INTO repostMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}')", Y_weiboID, repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();

            //    }


            //    //、、、、、、、、、、、、、、、、、、、、、、、.........................................................................
            //}
            //Console.WriteLine("--------------------第三层插入成功-------");


            //dt = new DataTable();
            //sql = "select * from weiboMsg";
            //dapt = new SqlDataAdapter(sql, conn);
            //dapt.Fill(dt);
            //floor++;

            //for (int k = (dtcount + 1); k < (dtcount + 3); k++)
            //{
            //    url = dt.Rows[k]["weibourl"].ToString();
            //    url1 = url + "?type=comment";
            //    driver.Url = url1;
            //    Console.WriteLine("--------------------等待打开第" + k + "条数据的comment-------");
            //    Thread.Sleep(15000);
            //    html = driver.PageSource;
            //    documenthtml = new JumonyParser().Parse(html);

            //    fatherWeiboID = int.Parse(dt.Rows[k]["weiboID"].ToString());

            //    commenttext = documenthtml.Find(".list_ul .list_li");
            //    j = 0;
            //    commentnum = 0;
            //    try
            //    {
            //        commentnum = int.Parse(documenthtml.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //    }
            //    catch { }
            //    RandKey = ran.Next(11, 18);//随机数
            //    if (commentnum > 20)
            //    {
            //        commentnum = RandKey;
            //    }
            //    foreach (var item in commenttext)
            //    {
            //        if (j >= commentnum)
            //        {
            //            break;
            //        }
            //        j++;
            //        string txt = item.FindFirst(".WB_text").InnerText();
            //        commentUID = txt.Split('：')[0].ToString();
            //        commentcontent = txt.Split('：')[1].ToString();
            //        commentTime = item.FindFirst(".WB_from").InnerText();

            //        commentcontent = commentcontent.Replace('\'', '-');
            //        sql = string.Format("INSERT INTO commentMsg VALUES ('{0}','{1}','{2}','{3}')", fatherWeiboID, commentUID, commentcontent, commentTime);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();
            //    }


            //    url1 = url + "?type=repost";
            //    driver.Url = url1;
            //    Console.WriteLine("--------------------等待打开第" + k + "条数据的repost-------");
            //    Thread.Sleep(5000);
            //    html1 = driver.PageSource;
            //    documenthtml1 = new JumonyParser().Parse(html1);

            //    reposttext = documenthtml1.Find(".list_ul .list_li");

            //    j = 0;
            //    repostnum = 0;
            //    try
            //    {
            //        repostnum = int.Parse(documenthtml1.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //    }
            //    catch { }
            //    repostnum1 = repostnum;
            //    RandKey = ran.Next(11, 18);//随机数
            //    if (repostnum > 20)
            //    {
            //        repostnum = RandKey;
            //    }
            //    sql = string.Format("UPDATE weiboMsg SET all_repost_num = " + repostnum1 + " WHERE weiboID = " + fatherWeiboID);
            //    com = new SqlCommand(sql, conn);
            //    com.ExecuteNonQuery();
            //    sql = string.Format("UPDATE weiboMsg SET repost_num = " + repostnum + " WHERE weiboID = " + fatherWeiboID);
            //    com = new SqlCommand(sql, conn);
            //    com.ExecuteNonQuery();
            //    foreach (var ite in reposttext)
            //    {
            //        if (j >= repostnum)
            //        {
            //            break;
            //        }
            //        j++;
            //        string txt = ite.FindFirst(".WB_text").InnerText();
            //        repostuserID = txt.Split('：')[0].ToString();
            //        repostcontent = txt.Split('：')[1].ToString();

            //        rootWeiboID = WBID;
            //        repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
            //        string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
            //        repostweibo_url = "http://weibo.com" + repostweibo_url;

            //        datatime = DateTime.Now.ToLocalTime().ToString();
            //        repostcontent = repostcontent.Replace('\'', '-');
            //        sql = string.Format("INSERT INTO weiboMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}', '{8}')", weiboID, repostuserID, repostcontent, repostTime, datatime, repostweibo_url,0,0, floor);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();
            //        Y_weiboID = weiboID;
            //        weiboID++;

            //        sql = string.Format("INSERT INTO repostMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}')", Y_weiboID, repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();

            //    }


            //    //、、、、、、、、、、、、、、、、、、、、、、、.........................................................................
            //}
            //Console.WriteLine("--------------------第四层插入成功-------");




            //conn.Close();
            //Console.WriteLine("____________________抓取完成_______________________");

































            //---------------------------------------------------------------------------------------------------------------------
            //    IWebDriver driver = new FirefoxDriver();
            //    driver.Url = "http://weibo.com/";
            //    Console.WriteLine("--------------------等待打开浏览器-------");
            //    //Thread.Sleep(10000);

            //    SqlConnection conn = new SqlConnection("Data Source=.;Initial Catalog=Sina_weibo;Integrated Security=True");
            //    conn.Open();

            //    HttpUtility http = new HttpUtility();


            //    int weiboID = 501;//-----------------------------------------------------------------------------
            //    int WBID = weiboID;
            //    string url = "http://weibo.com/2803301701/DEgxNwT8g";//--------------------------------------------------

            //    string url1 = url + "?type=comment";
            //    driver.Url = url1;
            //    Console.WriteLine("--------------------等待打开首层URL的comment-------");
            //    Thread.Sleep(20000);
            //    string html = driver.PageSource;
            //    var documenthtml = new JumonyParser().Parse(html);

            //    string userID;          ///用户昵称
            //    string content;         ///微博内容
            //    string postTime;        ///发送时间
            //    string datatime;        ///抓取时;
            //    string weibourl;        ///URl
            //    int floor = 1;

            //    //--------------------------------------------------------------------------------------------------------------------------
            //    weibourl = url;
            //    userID = documenthtml.FindFirst(".WB_info a").InnerHtml();
            //    content = documenthtml.FindFirst(".WB_text").InnerText();
            //    postTime = documenthtml.FindFirst(".WB_from a").InnerHtml();
            //    datatime = DateTime.Now.ToLocalTime().ToString();
            //    content = content.Replace('\'', '-');
            //    string sql = string.Format("INSERT INTO weiboMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}', '{6}')", weiboID, userID, content, postTime, datatime, weibourl, floor);
            //    SqlCommand com = new SqlCommand(sql, conn);
            //    com.ExecuteNonQuery();
            //    Console.WriteLine("--------------------表一插入完成-------");
            //    int rootWeiboID = weiboID;
            //    weiboID++;
            //    floor++;

            //    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //    var commenttext = documenthtml.Find(".list_ul .list_li");
            //    string commentUID;      ///评论用户 ID
            //    string commentcontent;  ///评论内容
            //    string commentTime;     ///评论时间
            //    int j = 0;

            //    int commentnum = 0;
            //    try
            //    {
            //        commentnum = int.Parse(documenthtml.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //    }
            //    catch { }

            //    if (commentnum > 10)
            //    {
            //        commentnum = 10;
            //    }
            //    foreach (var item in commenttext)
            //    {
            //        if (j >= commentnum)
            //        {
            //            break;
            //        }
            //        j++;
            //        string txt = item.FindFirst(".WB_text").InnerText();
            //        commentUID = txt.Split('：')[0].ToString();
            //        commentcontent = txt.Split('：')[1].ToString();
            //        commentTime = item.FindFirst(".WB_from").InnerText();
            //        int weiboidx = weiboID - 1;

            //        commentcontent = commentcontent.Replace('\'', '-');
            //        sql = string.Format("INSERT INTO commentMsg VALUES ('{0}','{1}','{2}','{3}')", weiboidx, commentUID, commentcontent, commentTime);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();
            //    }


            /////////////////////////////////////////////////////////////////////
            //    url1 = url + "?type=repost";
            //    driver.Url = url1;
            //    Console.WriteLine("--------------------等待打开首条URL的repost-------");
            //    Thread.Sleep(5000);
            //    string html1 = driver.PageSource;
            //    var documenthtml1 = new JumonyParser().Parse(html1);

            //    var reposttext = documenthtml1.Find(".list_ul .list_li");

            //    int Y_weiboID;          ///在表一中的ID
            //    string repostuserID;    ///转发博主昵称
            //    int fatherWeiboID;      ///从那条微博转发而来的

            //    string repostcontent;   ///微博内容
            //    string repostTime;      ///转发时间
            //    fatherWeiboID = rootWeiboID;

            //    j = 0;

            //    int repostnum = 0;
            //    try
            //    {
            //        repostnum = int.Parse(documenthtml1.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //    }
            //    catch { }
            //    if (repostnum > 10)
            //    {
            //        repostnum = 10;
            //    }
            //    foreach (var ite in reposttext)
            //    {
            //        if (j >= repostnum)
            //        {
            //            break;
            //        }
            //        j++;
            //        string txt = ite.FindFirst(".WB_text").InnerText();
            //        repostuserID = txt.Split('：')[0].ToString();
            //        repostcontent = txt.Split('：')[1].ToString();
            //        repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
            //        string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
            //        repostweibo_url = "http://weibo.com" + repostweibo_url;

            //        datatime = DateTime.Now.ToLocalTime().ToString();
            //        repostcontent = repostcontent.Replace('\'', '-');
            //        sql = string.Format("INSERT INTO weiboMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}', '{6}')", weiboID, repostuserID, repostcontent, repostTime, datatime, repostweibo_url, floor);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();
            //        Y_weiboID = weiboID;
            //        weiboID++;

            //        sql = string.Format("INSERT INTO repostMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}')", Y_weiboID, repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
            //        com = new SqlCommand(sql, conn);
            //        com.ExecuteNonQuery();

            //    }



            //    Console.WriteLine("--------------------首页URL插入完成-------");

            //    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //    DataTable dt = new DataTable();
            //    sql = "select * from weiboMsg";
            //    SqlDataAdapter dapt = new SqlDataAdapter(sql, conn);
            //    dapt.Fill(dt);

            //    floor ++;
            //    int dtcount = dt.Rows.Count;
            //    for (int k = (WBID + 1); k < dt.Rows.Count; k++)
            //    {
            //        url = dt.Rows[k]["weibourl"].ToString();
            //        url1 = url + "?type=comment";
            //        driver.Url = url1;
            //        Console.WriteLine("--------------------等待打开第" + k + "条数据的comment-------");
            //        Thread.Sleep(15000);
            //        html = driver.PageSource;
            //        documenthtml = new JumonyParser().Parse(html);

            //        fatherWeiboID = int.Parse(dt.Rows[k]["weiboID"].ToString());

            //        commenttext = documenthtml.Find(".list_ul .list_li");
            //        j = 0;
            //        commentnum = 0;
            //        try
            //        {
            //            commentnum = int.Parse(documenthtml.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //        }
            //        catch { }

            //        if (commentnum > 10)
            //        {
            //            commentnum = 10;
            //        }
            //        foreach (var item in commenttext)
            //        {
            //            if (j >= commentnum)
            //            {
            //                break;
            //            }
            //            j++;
            //            string txt = item.FindFirst(".WB_text").InnerText();
            //            commentUID = txt.Split('：')[0].ToString();
            //            commentcontent = txt.Split('：')[1].ToString();
            //            commentTime = item.FindFirst(".WB_from").InnerText();

            //            commentcontent = commentcontent.Replace('\'', '-');
            //            sql = string.Format("INSERT INTO commentMsg VALUES ('{0}','{1}','{2}','{3}')", fatherWeiboID, commentUID, commentcontent, commentTime);
            //            com = new SqlCommand(sql, conn);
            //            com.ExecuteNonQuery();
            //        }


            //        url1 = url + "?type=repost";
            //        driver.Url = url1;
            //        Console.WriteLine("--------------------等待打开第" + k + "条数据的repost-------");
            //        Thread.Sleep(5000);
            //        html1 = driver.PageSource;
            //        documenthtml1 = new JumonyParser().Parse(html1);

            //        reposttext = documenthtml1.Find(".list_ul .list_li");

            //        j = 0;
            //        repostnum = 0;
            //        try
            //        {
            //            repostnum = int.Parse(documenthtml1.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //        }
            //        catch { }
            //        if (repostnum > 5)
            //        {
            //            repostnum = 5;
            //        }
            //        foreach (var ite in reposttext)
            //        {
            //            if (j >= repostnum)
            //            {
            //                break;
            //            }
            //            j++;
            //            string txt = ite.FindFirst(".WB_text").InnerText();
            //            repostuserID = txt.Split('：')[0].ToString();
            //            repostcontent = txt.Split('：')[1].ToString();

            //            rootWeiboID = WBID;
            //            repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
            //            string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
            //            repostweibo_url = "http://weibo.com" + repostweibo_url;

            //            datatime = DateTime.Now.ToLocalTime().ToString();
            //            repostcontent = repostcontent.Replace('\'', '-');
            //            sql = string.Format("INSERT INTO weiboMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}', '{6}')", weiboID, repostuserID, repostcontent, repostTime, datatime, repostweibo_url, floor);
            //            com = new SqlCommand(sql, conn);
            //            com.ExecuteNonQuery();
            //            Y_weiboID = weiboID;
            //            weiboID++;

            //            sql = string.Format("INSERT INTO repostMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}')", Y_weiboID, repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
            //            com = new SqlCommand(sql, conn);
            //            com.ExecuteNonQuery();

            //        }


            //        //、、、、、、、、、、、、、、、、、、、、、、、.........................................................................
            //    }
            //    Console.WriteLine("--------------------第三层插入成功-------");


            //    dt = new DataTable();
            //    sql = "select * from weiboMsg";
            //    dapt = new SqlDataAdapter(sql, conn);
            //    dapt.Fill(dt);
            //    floor++;

            //    for (int k = (dtcount + 1); k < dt.Rows.Count; k++)
            //    {
            //        url = dt.Rows[k]["weibourl"].ToString();
            //        url1 = url + "?type=comment";
            //        driver.Url = url1;
            //        Console.WriteLine("--------------------等待打开第" + k + "条数据的comment-------");
            //        Thread.Sleep(15000);
            //        html = driver.PageSource;
            //        documenthtml = new JumonyParser().Parse(html);

            //        fatherWeiboID = int.Parse(dt.Rows[k]["weiboID"].ToString());

            //        commenttext = documenthtml.Find(".list_ul .list_li");
            //        j = 0;
            //        commentnum = 0;
            //        try
            //        {
            //            commentnum = int.Parse(documenthtml.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //        }
            //        catch { }

            //        if (commentnum > 10)
            //        {
            //            commentnum = 10;
            //        }
            //        foreach (var item in commenttext)
            //        {
            //            if (j >= commentnum)
            //            {
            //                break;
            //            }
            //            j++;
            //            string txt = item.FindFirst(".WB_text").InnerText();
            //            commentUID = txt.Split('：')[0].ToString();
            //            commentcontent = txt.Split('：')[1].ToString();
            //            commentTime = item.FindFirst(".WB_from").InnerText();

            //            commentcontent = commentcontent.Replace('\'', '-');
            //            sql = string.Format("INSERT INTO commentMsg VALUES ('{0}','{1}','{2}','{3}')", fatherWeiboID, commentUID, commentcontent, commentTime);
            //            com = new SqlCommand(sql, conn);
            //            com.ExecuteNonQuery();
            //        }


            //        url1 = url + "?type=repost";
            //        driver.Url = url1;
            //        Console.WriteLine("--------------------等待打开第" + k + "条数据的repost-------");
            //        Thread.Sleep(5000);
            //        html1 = driver.PageSource;
            //        documenthtml1 = new JumonyParser().Parse(html1);

            //        reposttext = documenthtml1.Find(".list_ul .list_li");

            //        j = 0;
            //        repostnum = 0;
            //        try
            //        {
            //            repostnum = int.Parse(documenthtml1.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //        }
            //        catch { }
            //        if (repostnum > 5)
            //        {
            //            repostnum = 5;
            //        }
            //        foreach (var ite in reposttext)
            //        {
            //            if (j >= repostnum)
            //            {
            //                break;
            //            }
            //            j++;
            //            string txt = ite.FindFirst(".WB_text").InnerText();
            //            repostuserID = txt.Split('：')[0].ToString();
            //            repostcontent = txt.Split('：')[1].ToString();

            //            rootWeiboID = WBID;
            //            repostTime = ite.FindFirst(".WB_from a").Attribute("title").Value();
            //            string repostweibo_url = ite.FindFirst(".WB_from a").Attribute("href").Value();
            //            repostweibo_url = "http://weibo.com" + repostweibo_url;

            //            datatime = DateTime.Now.ToLocalTime().ToString();
            //            repostcontent = repostcontent.Replace('\'', '-');
            //            sql = string.Format("INSERT INTO weiboMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}', '{6}')", weiboID, repostuserID, repostcontent, repostTime, datatime, repostweibo_url, floor);
            //            com = new SqlCommand(sql, conn);
            //            com.ExecuteNonQuery();
            //            Y_weiboID = weiboID;
            //            weiboID++;

            //            sql = string.Format("INSERT INTO repostMsg VALUES ('{0}','{1}','{2}','{3}','{4}','{5}')", Y_weiboID, repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime);
            //            com = new SqlCommand(sql, conn);
            //            com.ExecuteNonQuery();

            //        }


            //        //、、、、、、、、、、、、、、、、、、、、、、、.........................................................................
            //    }
            //    Console.WriteLine("--------------------第四层插入成功-------");




            //    conn.Close();
            //    Console.WriteLine("____________________抓取完成_______________________");










            //DataTable dt = new DataTable();
            //SqlConnection conn = new SqlConnection("Data Source=.;Initial Catalog=Sina_weibo;Integrated Security=True");
            //string sql = "select * from URL";
            //SqlDataAdapter dapt = new SqlDataAdapter(sql, conn);
            //dapt.Fill(dt);
            //conn.Open();
            //HttpUtility http = new HttpUtility();
            //for (int i = 1; i < dt.Rows.Count; i++)
            //{
            //    int flag = 0;

            //    string weiboID;         ///微博ID
            //    string userID;          ///用户昵称
            //    string content;         ///微博内容
            //    string postTime;        ///发送时间
            //                            ///
            //    string commentUID;      ///评论用户 ID
            //    string commentcontent;  ///评论内容
            //    string commentTime;     ///评论时间

            //    string repostweiboID;   ///转发微博ID
            //    string repostuserID;    ///转发博主ID
            //    string fatherWeiboID;   ///从那条微博转发而来的
            //    string rootWeiboID;     ///原创微博ID
            //    string repostcontent;   ///微博内容
            //    string repostTime;      ///转发时间

            //    //Thread.Sleep(10000);
            //    Console.WriteLine("--------------------等待响应1-------");
            //    string Sina_url = dt.Rows[i]["URL"].ToString();
            //    string Sina_url1 = Sina_url + "?type=comment";
            //    driver.Url = Sina_url1;
            //    Thread.Sleep(20000);
            //    string html = driver.PageSource;
            //    var documenthtml = new JumonyParser().Parse(html);
            //    //Thread.Sleep(10000);
            //    Console.WriteLine("--------------------等待响应2-------");
            //    string Sina_url2 = Sina_url + "?type=repost";
            //    driver.Url = Sina_url2;
            //    Thread.Sleep(20000);
            //    string  html1 = driver.PageSource;
            //    var documenthtml1 = new JumonyParser().Parse(html1);


            //    weiboID = Sina_url.Split('/')[4].ToString();
            //    userID = documenthtml.FindFirst(".WB_info a").InnerHtml();
            //    content = documenthtml.FindFirst(".WB_text").InnerText();
            //    postTime = documenthtml.FindFirst(".WB_from a").InnerHtml();

            //    //var commentnum1 = documenthtml.FindFirst(".WB_row_line").FindFirst(".curr");
            //    //string commmm = commentnum1.FindFirst(".S_line1").FindLast("em").InnerHtml();
            //    int commentnum = int.Parse(documenthtml.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //    if(commentnum<10)
            //    {
            //        flag = 1;
            //    }
            //    else
            //    {
            //        commentnum = 10;
            //    }
            //    int repostnum = int.Parse(documenthtml1.FindFirst(".WB_row_line").FindFirst(".curr").FindFirst(".S_line1").FindLast("em").InnerHtml());
            //    if (repostnum < 10)
            //    {
            //        flag = 1;
            //    }
            //    else
            //    {
            //        repostnum = 10;
            //    }
            //    int num = 10;
            //    if (flag == 1)
            //    {
            //        if(commentnum>repostnum)
            //        {
            //            num = repostnum;
            //        }
            //        else
            //        {
            //            num = commentnum;
            //        }
            //    }
            //    var commenttext = documenthtml.Find(".list_ul .list_li");
            //    //int x = commenttext.Count();
            //    int j = 0;
            //    string commentUID1 = "#";      ///评论用户 ID
            //    string commentcontent1 = "#";  ///评论内容
            //    string commentTime1 = "#";     ///评论时间
            //    foreach (var item in commenttext)
            //    {
            //        if(j >= num)
            //        {
            //            break;
            //        }
            //        j++;
            //        string txt  = item.FindFirst(".WB_text").InnerText();
            //        string commentUID2 = txt.Split('：')[0].ToString();
            //        commentUID1 += commentUID2 + "#";
            //        string commentcontent2 = txt.Split('：')[1].ToString();
            //        commentcontent1 += commentcontent2 + "#";
            //        string commentTime2 = item.FindFirst(".WB_from").InnerText();
            //        commentTime1 += commentTime2 + "#";
            //    }

            //    var reposttext = documenthtml1.Find(".list_ul .list_li");
            //    //int x = commenttext.Count();
            //    j = 0;
            //    string repostweiboID1 = "#";   ///转发微博ID
            //    string repostuserID1 = "#";    ///转发博主ID
            //    string fatherWeiboID1 = "#";   ///从那条微博转发而来的
            //    string repostcontent1 = "#";   ///微博内容
            //    string repostTime1 = "#";      ///转发时间
            //    foreach (var ite in reposttext)
            //    {
            //        if (j >= num)
            //        {
            //            break;
            //        }
            //        j++;
            //        string txt = ite.FindFirst(".WB_text").InnerText();
            //        string repostuserID2 = txt.Split('：')[0].ToString();
            //        repostuserID1 += repostuserID2 + "#";
            //        string repostcontent2 = txt.Split('：')[1].ToString();
            //        repostcontent1 += repostcontent2 + "#";
            //        if(repostcontent2.IndexOf("//")>-1)
            //        {
            //            string fatherWeiboID2 = repostcontent2.Split('@')[1].Split(':')[0].ToString();
            //            fatherWeiboID1 += fatherWeiboID2 + "#";
            //        }
            //        else
            //        {
            //            fatherWeiboID1 += userID + "#";
            //        }
            //        string repostTime2 = ite.FindFirst(".WB_from a").Attribute("title").Value();
            //        repostTime1 += repostTime2 + "#";
            //        string repostweiboID2 = ite.FindFirst(".WB_from a").Attribute("href").Value().Split('/')[2];
            //        repostweiboID1 += repostweiboID2 + "#";

            //    }


            //    string datat = DateTime.Now.ToLocalTime().ToString();
            //    for(j = 1; j<=num; j++)
            //    {
            //        commentUID = commentUID1.Split('#')[j].ToString();
            //        commentcontent = commentcontent1.Split('#')[j].ToString();
            //        commentTime = commentTime1.Split('#')[j].ToString();
            //        repostweiboID = repostweiboID1.Split('#')[j].ToString();
            //        repostuserID = repostuserID1.Split('#')[j].ToString();
            //        fatherWeiboID = fatherWeiboID1.Split('#')[j].ToString();
            //        repostcontent = repostcontent1.Split('#')[j].ToString();
            //        repostTime = repostTime1.Split('#')[j].ToString();
            //        rootWeiboID = weiboID;
            //        Console.WriteLine("用户昵称: " + userID + "         评论用户 ID: " + commentUID + "         转发用户ID: " + repostuserID);
            //        content = content.Replace('\'','-');
            //        string sql2 = string.Format("INSERT INTO Sina VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}')", weiboID, userID, content, postTime, commentUID, commentcontent, commentTime, repostweiboID, repostuserID, fatherWeiboID, rootWeiboID, repostcontent, repostTime, datat);
            //        SqlCommand com = new SqlCommand(sql2, conn);
            //        com.ExecuteNonQuery();
            //    }

            //}

            //driver.FindElement(By.Id("startShopping")).Click();
        }

    }
}

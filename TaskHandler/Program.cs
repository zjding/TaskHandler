using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskHandler
{
    class Program
    {
        static string connectionString;

        static void Main(string[] args)
        {

            string taskName = args[0];
            //string taskName = "152250181138-20160928150214";
            List<string> UrlNumbers = taskName.Split('-')[0].Split('~').ToList();

            SetConnectionString();
            SqlConnection cn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cn;
            cn.Open();
            string sqlString;

            ChromeDriver driver = new ChromeDriver();

            decimal price = 0;
            string options = string.Empty;
            int optionsNumber;

            string eBayUrlNumbers = string.Empty;

            foreach (string urlNumber in UrlNumbers)
            {
                eBayUrlNumbers += "'" + urlNumber + "',";

                sqlString = @"  SELECT  eBayListingPrice, CostcoOptions 
                                FROM    eBay_CurrentListings
                                WHERE   eBayItemNumber = '" + urlNumber + "'" +
                            @"  AND     PendingChange = 1 " +
                            @"  AND     DeleteDT is NULL";
                cmd.CommandText = sqlString;
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    price = Convert.ToDecimal(reader["eBayListingPrice"]);
                    options = Convert.ToString(reader["CostcoOptions"]);
                }
                reader.Close();

                string priceString = String.Format("{0:C}", price);

                int nOptions_db = 0;

                foreach (string l1 in options.Split('|'))
                {
                    nOptions_db += l1.Split(';').Length;
                }

                driver.Navigate().GoToUrl("http://www.ebay.com/itm/" + urlNumber);

                int nOptions = 0;

                if (hasElement(driver, By.Id("msku-sel-1")))
                {
                    IWebElement eSel1 = driver.FindElement(By.Id("msku-sel-1"));
                    var eOpt1s = eSel1.FindElements(By.TagName("option"));

                    int nOpt1s = eOpt1s.Count;

                    if (hasElement(driver, By.Id("msku-sel-2")))
                    {
                        for (int j = 1; j < nOpt1s; j++) 
                        {
                            driver.FindElement(By.Id("msku-sel-1")).FindElements(By.TagName("option"))[j].Click();

                            IWebElement eSel2 = driver.FindElementById("msku-sel-2");
                            var eOpt2s = eSel2.FindElements(By.TagName("option"));

                            nOptions += (eOpt2s.Count - 1);
                        }
                    }
                    else
                    {
                        nOptions = eOpt1s.Count - 1;
                    }
                }

                IWebElement ePrice = driver.FindElementById("prcIsum");

                string ePriceText = ePrice.Text.Replace("US", "").Trim();


                IWebElement eQty = driver.FindElementById("qtySubTxt");

                if (ePriceText == priceString && nOptions == nOptions_db)
                {
                    sqlString = @"UPDATE Tasks SET Completed = 1 WHERE TaskName = '" + taskName + "'";
                    cmd.CommandText = sqlString;
                    cmd.ExecuteNonQuery();

                    
                }
            }

            eBayUrlNumbers = eBayUrlNumbers.Substring(0, eBayUrlNumbers.Length - 1);

            sqlString = @" UPDATE eBay_CurrentListings SET PendingChange = '' WHERE eBayItemNumber in (" + eBayUrlNumbers + ") AND DeleteDT is null";
            cmd.CommandText = sqlString;
            cmd.ExecuteNonQuery();

            cn.Close();
            
            driver.Dispose();
        }

        private static void SetConnectionString()
        {
            string azureConnectionString = "Server=tcp:zjding.database.windows.net,1433;Initial Catalog=Costco;Persist Security Info=False;User ID=zjding;Password=G4indigo;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            SqlConnection cn = new SqlConnection(azureConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cn;

            cn.Open();
            string sqlString = "SELECT ConnectionString FROM DatabaseToUse WHERE bUse = 1 and ApplicationName = 'Crawler'";
            cmd.CommandText = sqlString;
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    connectionString = (reader.GetString(0)).ToString();
                }
            }
            reader.Close();
            cn.Close();
        }

        private static bool hasElement(IWebElement webElement, By by)
        {
            try
            {
                webElement.FindElement(by);
                return true;
            }
            catch (NoSuchElementException e)
            {
                return false;
            }
        }

        private static bool hasElement(IWebDriver webDriver, By by)
        {
            try
            {
                webDriver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException e)
            {
                return false;
            }
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace EXMOFB
{
    public partial class Form4 : Form
    {
        string balSlave;
        string balMaster;
        string pair;
        string master;
        string slave;
        readonly int order_book = 8;
        readonly int limit = 2;
        string min_quantity;//минимум купить-продать монет
        string min_amount;//минимум тратим на 1 ордер
        string stask;
        string stbid;
        double avgSell;
        string typeLastord;
        string priceLastbuy;
        string priceLastsell;
        double orderspred = 0;
        double avgprice;
        double stacanspred;
        int price_precision;//кол-во знаков после запятой у цены
        string qBuy;
        string qSell;

        public Form4()
        {
            InitializeComponent();
        }
        void Form4_Load(object sender, EventArgs e)
        {
            AllPair();
        }
        void AllPair()//все пары для торгов
        {
            string rez = new API().ApiQuery("ticker", new Dictionary<string, string> { });

            if (rez.Contains("false"))
            {
                string[] temp = rez.Split('"');
                MessageBox.Show(temp[5], "Error");
            }
            else
            {
                dynamic bc = JsonConvert.DeserializeObject(rez.ToString());

                foreach (var item in bc)
                {
                    comboBox1.Items.Add(item.Name);
                }
            }
        }
        void GetPricePairSetting()//лимиты пары
        {
            try
            {
                pair = comboBox1.Text;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://api.exmo.com/v1/pair_settings/?pair={pair}");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader sr = new StreamReader(stream);
                string data = sr.ReadToEnd();
                response.Close();
                dynamic d = JsonConvert.DeserializeObject(data);

                min_quantity = (d[pair].min_quantity);
                min_amount = (d[pair].min_amount).ToString();
                price_precision = d[pair].price_precision;
            }
            catch
            {
                MessageBox.Show("Проверьте Настройки");
            }
        }
        void User_info()//получаем баланс по паре
        {
            master = comboBox1.Text.Substring(comboBox1.Text.IndexOf('_') + 1);//обрезка до символа
            slave = comboBox1.Text.Substring(0, comboBox1.Text.IndexOf('_'));//обрезка после символа;
            string rez = new API().ApiQuery("user_info", new Dictionary<string, string> { });
            if (rez.Contains("false"))
            {
                return;
            }
            else
            {
                dynamic bc = JsonConvert.DeserializeObject(rez.ToString());
                dataGridView5.Rows.Clear();
                balMaster = (bc["balances"][master]).ToString();
                balSlave = (bc["balances"][slave]).ToString();
                dataGridView5.Rows.Add(master, balMaster);
                dataGridView5.Rows.Add(slave, balSlave);
                dataGridView5.Refresh();
                User_trades();
            }
        }
        void GetPriceOrderbook()//получаем ордербук
        {
            try
            {
                pair = comboBox1.Text;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://api.exmo.com/v1/order_book/?pair={pair}");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader sr = new StreamReader(stream);
                string data = sr.ReadToEnd();
                response.Close();
                dynamic d = JsonConvert.DeserializeObject(data);

                string[] ask = new string[order_book];
                string[] vol_ask = new string[order_book];
                string[] vol_cur_ask = new string[order_book];
                string[] bid = new string[order_book];
                string[] vol_bid = new string[order_book];
                string[] vol_cur_bid = new string[order_book];

                dataGridView1.Rows.Clear();
                dataGridView2.Rows.Clear();
                for (int i = 0; i < order_book; i++)
                {
                    ask[i] = (d[pair].ask[i][0]).ToString();
                    vol_ask[i] = (d[pair].ask[i][1]).ToString();
                    vol_cur_ask[i] = (d[pair].ask[i][2]).ToString();
                    dataGridView1.Rows.Add(ask[i], vol_ask[i], vol_cur_ask[i]);

                    bid[i] = (d[pair].bid[i][0]).ToString();
                    vol_bid[i] = (d[pair].bid[i][1]).ToString();
                    vol_cur_bid[i] = (d[pair].bid[i][2]).ToString();
                    dataGridView2.Rows.Add(bid[i], vol_bid[i], vol_cur_bid[i]);
                    stask = ask[0].ToString();
                    stbid = bid[0].ToString();
                }
                dataGridView1.Refresh();
                dataGridView2.Refresh();
                avgprice = ((double.Parse(dataGridView1.Rows[0].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) + double.Parse(dataGridView2.Rows[0].Cells[0].Value.ToString(), CultureInfo.InvariantCulture)) / 2);
                User_open_orders();
                ControlSpred();
            }
            catch
            {

            }
        }
        void User_trades()//получаем историю торгов
        {
            pair = comboBox1.Text;
            string rez = new API().ApiQuery("user_trades", new Dictionary<string, string> { { "pair", pair.ToString() }, { "limit", limit.ToString() }, { "offset", "0" }, });

            if (rez.Contains("false"))
            {
                return;
            }
            else
            {
                string[] typeOrder = new string[limit];
                dynamic bc = JsonConvert.DeserializeObject(rez.ToString());
                int x = bc[pair].Count;

                if (bc != null)
                {
                    for (int i = 0; i < x; i++)
                    {
                        typeOrder[i] = (bc[pair][i]["type"]).ToString();
                        typeLastord = typeOrder[0].ToString();
                    }
                }
            }
        }
        void User_open_orders()//открытые ордера
        {
            int rows3 = dataGridView3.Rows.Count;
            int rows4 = dataGridView4.Rows.Count;
            int row = rows3 - 1;
            int ro = rows3 - 2;
            pair = comboBox1.Text;
            string rez = new API().ApiQuery("user_open_orders", new Dictionary<string, string> { });

            if (rez.Contains("false"))
            {
                return;
            }
            else
            {
                dataGridView3.Rows.Clear();
                dataGridView4.Rows.Clear();

                if (rez.Contains(pair))
                {
                    dynamic bc = JsonConvert.DeserializeObject(rez.ToString());
                    int x = bc[pair].Count;
                    string[] price_order = new string[x];
                    string[] quantity_order = new string[x];
                    string[] amount_order = new string[x];
                    string[] type_order = new string[x];
                    string[] order_id_order = new string[x];

                    if (rez.Contains("order_id") == true)
                    {
                        for (int i = 0; i < x; i++)
                        {
                            price_order[i] = (bc[pair][i]["price"]).ToString();
                            quantity_order[i] = (bc[pair][i]["quantity"]).ToString();
                            amount_order[i] = (bc[pair][i]["amount"]).ToString();
                            type_order[i] = (bc[pair][i]["type"]).ToString();
                            order_id_order[i] = (bc[pair][i]["order_id"]).ToString();

                            if (type_order[i].Contains("sell") == true)
                            {
                                dataGridView3.Rows.Add(price_order[i], quantity_order[i], amount_order[i], order_id_order[i]);
                                if (rows4 != 0 && rows3 > 2 && price_order[row] != null && price_order[ro] != null)
                                {
                                    avgSell = (double.Parse(price_order[row].ToString(), CultureInfo.InvariantCulture) + double.Parse(price_order[ro].ToString(), CultureInfo.InvariantCulture))/2;
                                }
                            }
                            if (type_order[i].Contains("buy") == true)
                            {                                                
                                dataGridView4.Rows.Add(price_order[i], quantity_order[i], amount_order[i], order_id_order[i]);
                            }
                        }
                    }
                }
                else
                {
                    dataGridView3.Rows.Clear();
                    dataGridView4.Rows.Clear();
                }
                dataGridView3.Refresh();
                dataGridView4.Refresh();
            }
        }
        double ControlSpred()
        {
            int rows1 = dataGridView1.Rows.Count;
            int rows2 = dataGridView2.Rows.Count;
            int rows3 = dataGridView3.Rows.Count;
            int rows4 = dataGridView4.Rows.Count;
            int s = rows3 - 1;
            int b = rows4 - 1;

            if (rows1 != 0 && rows2 != 0)
            {
                stacanspred = Math.Round(((double.Parse(dataGridView1.Rows[0].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) / double.Parse(dataGridView2.Rows[0].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) - 1) * 100), 2);
                label28.Text = stacanspred.ToString();
            }

            if (rows4 == 0 && rows3 == 0)
            {
                orderspred = stacanspred;
                label24.Text = orderspred.ToString();
            }
            if (rows4 != 0 && rows3 == 0)
            {
                orderspred = Math.Round(((double.Parse(dataGridView1.Rows[0].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) / double.Parse(dataGridView4.Rows[b].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) - 1) * 100), 2);
                label24.Text = orderspred.ToString();
                ResetSpred();
            }
            if (rows4 == 0 && rows3 != 0)
            {
                orderspred = Math.Round(((double.Parse(dataGridView3.Rows[s].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) / double.Parse(dataGridView2.Rows[0].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) - 1) * 100), 2); ;
                label24.Text = orderspred.ToString();
                ResetSpred();
            }

            if (rows4 != 0 && rows3 != 0)
            {
                orderspred = Math.Round(((double.Parse(dataGridView3.Rows[s].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) / double.Parse(dataGridView4.Rows[b].Cells[0].Value.ToString(), CultureInfo.InvariantCulture) - 1) * 100), 2);
                label24.Text = orderspred.ToString();
                ResetSpred();
            }

            return orderspred;
        }
        void ResetSpred()
        {
            int rows3 = dataGridView3.Rows.Count;
            int rows4 = dataGridView4.Rows.Count;

            try
            {
                if (textBox7.Text.ToString() != "" && orderspred > int.Parse(textBox7.Text.ToString(), CultureInfo.InvariantCulture))
                {
                    timer3.Stop();
                    for (int i = 0; i < rows4; i++)
                    {
                        if (rows4 > 0)
                        {
                            Reset_buy(dataGridView4.Rows[0].Cells[3].Value.ToString());
                            dataGridView4.Rows.Clear();
                            User_open_orders();
                            dataGridView4.Refresh();
                        }
                    }
                    for (int i = 0; i < rows3; i++)
                    {
                        if (rows3 > 0)
                        {
                            Reset_sell(dataGridView3.Rows[0].Cells[3].Value.ToString());
                            dataGridView3.Rows.Clear();
                            User_open_orders();
                            dataGridView3.Refresh();
                        }
                        orderspred = stacanspred;
                        label24.Text = orderspred.ToString();
                    }
                    timer3.Start();
                }
            }
            catch
            {

            }
        }
        void QuantityBuy(double priceBuy)
        {
            qBuy = textBox1.Text;
            Buy_Order_create(Convert.ToString(Math.Round(priceBuy, price_precision), CultureInfo.InvariantCulture), qBuy);
        }
        void Buy_Order_create(string e, string qBuy )//установить ордер на покупку
        {
            string rez = new API().ApiQuery("order_create", new Dictionary<string, string> { { "pair", comboBox1.Text.ToString() },
                { "quantity", qBuy }, { "price", e.ToString() }, { "type", "buy" } });
            User_info();
        }
        void QuantitySell(double priceSell)
        {
            qSell = textBox4.Text;
            Sell_Order_create(Convert.ToString(Math.Round(priceSell, price_precision), CultureInfo.InvariantCulture), qSell);
        }
        void Sell_Order_create(string priceLastsel, string qSell)//установить ордер на продажу
        {
            string rez = new API().ApiQuery("order_create", new Dictionary<string, string> { { "pair", comboBox1.Text.ToString() },
                { "quantity", qSell }, { "price", priceLastsel }, { "type", "sell" } });
            User_info();
        }
        void Reset_buy(string e )//сброс покупки
        {
            int rows4 = dataGridView4.Rows.Count;

            if (rows4 != 0)
            {
                string rez = new API().ApiQuery("order_cancel", new Dictionary<string, string> { { "order_id", e } });
                User_info();
            }
        }
        void Reset_sell(string e)//сброс продажи
        {
            int rows3 = dataGridView3.Rows.Count;

            if(rows3 != 0)
            {
                string rez = new API().ApiQuery("order_cancel", new Dictionary<string, string> { { "order_id", e } });
                User_info();
            }
        }
        void AlgoritmBuy()//покупки
        {
            int rows4 = dataGridView4.Rows.Count;
            int b = rows4 - 1;

            if (button5.Enabled == false && double.Parse(balMaster, CultureInfo.InvariantCulture) > double.Parse(min_amount, CultureInfo.InvariantCulture))
            {
                if (rows4 == 0)
                {
                    var otstupBuy = double.Parse(textBox2.Text, CultureInfo.InvariantCulture);
                    var priceBuy = avgprice;
                    priceBuy = priceBuy - (priceBuy / 100) * otstupBuy;
                    QuantityBuy(priceBuy);
                }
                if(rows4 == 1)
                {
                    int count = int.Parse(textBox6.Text.ToString(), CultureInfo.InvariantCulture)-1;
                    var otstupBuy = double.Parse(textBox9.Text, CultureInfo.InvariantCulture);
                    var priceBuy = double.Parse(dataGridView4.Rows[b].Cells[0].Value.ToString(), CultureInfo.InvariantCulture);

                    for (int i = 0; i < count; i++)
                    {
                        priceBuy = priceBuy - (priceBuy / 100) * otstupBuy;
                        QuantityBuy(priceBuy);
                    }
                }
                orderControbuy();
            }
        }
        void AlgoritmSell()//продажи
        {
            int rows3 = dataGridView3.Rows.Count;

            if (button6.Enabled == false && double.Parse(balSlave, CultureInfo.InvariantCulture) > double.Parse(min_quantity, CultureInfo.InvariantCulture))
            {
                if (rows3 == 0)
                {
                    var otstupSell = double.Parse(textBox3.Text, CultureInfo.InvariantCulture);
                    var priceSell = avgprice;
                    priceSell = priceSell + (priceSell / 100) * otstupSell;
                    QuantitySell(priceSell);
                }
                if (rows3 == 1)
                {
                    int count = int.Parse(textBox5.Text.ToString(), CultureInfo.InvariantCulture) - 1;
                    var otstupSell = double.Parse(textBox8.Text, CultureInfo.InvariantCulture);
                    var priceSell = double.Parse(dataGridView3.Rows[0].Cells[0].Value.ToString(), CultureInfo.InvariantCulture); ;

                    if (int.Parse(textBox5.Text.ToString(), CultureInfo.InvariantCulture) != 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            priceSell = priceSell + (priceSell / 100) * otstupSell;
                            QuantitySell(priceSell);
                        }
                    }
                }
                orderControllsell();
            }
        }
        void orderControllsell()
        {
            int rows3 = dataGridView3.Rows.Count;
            int h = rows3 - 1;
            try
            {
                if (rows3 > int.Parse(textBox5.Text.ToString(), CultureInfo.InvariantCulture))
                {
                    Reset_sell(dataGridView3.Rows[h].Cells[3].Value.ToString());
                    User_info();
                }
            }
            catch { }
        }
        void orderControbuy()
        {
            int rows4 = dataGridView4.Rows.Count;
            int h = rows4 - 1;
            try
            {
                if (rows4 > int.Parse(textBox6.Text.ToString(), CultureInfo.InvariantCulture))
                {
                    Reset_buy(dataGridView4.Rows[h].Cells[3].Value.ToString());
                    User_info();
                }
            }
            catch { }
        }
        //void AutoAvgSell()
        //{
        //    if(checkBox2.Checked == true)
        //    {
        //        int rows3 = dataGridView3.Rows.Count;
        //        int g = int.Parse(textBox5.Text.ToString(), CultureInfo.InvariantCulture);

        //       if (rows3 > g)
        //       {
        //            double amountCoi = double.Parse(textBox4.Text.ToString())*2;
        //            string amountCoin = (Convert.ToString(Math.Round(amountCoi, 8), CultureInfo.InvariantCulture));
        //            string avgS = (Convert.ToString(Math.Round(avgSell, 8), CultureInfo.InvariantCulture));

        //            timer3.Stop();
        //            Thread.Sleep(1000);
        //           for(int i = 0; i < 2; i++)
        //           {
        //               Reset_sell(dataGridView3.Rows[i].Cells[3].Value.ToString());
        //           }
        //            Thread.Sleep(1000);
        //            string rez = new API().ApiQuery("order_create", new Dictionary<string, string> { { "pair", comboBox1.Text.ToString() }, { "quantity", amountCoin.ToString() },
        //           { "price", avgS.ToString() }, { "type", "sell" } });
              
        //            User_info();
        //            Thread.Sleep(1000);
        //            timer3.Start();
        //       }
        //    }

        //}
        void button1_Click(object sender, EventArgs e)//Стоп торговля
        {
            comboBox1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            textBox5.Enabled = true;
            textBox4.Enabled = true;
            textBox3.Enabled = true;
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            textBox6.Enabled = true;
            textBox7.Enabled = true;
            textBox8.Enabled = true;
            textBox9.Enabled = true;

            timer3.Stop();
            typeLastord = null;
        }
        void button2_Click(object sender, EventArgs e)//Проверка подключения
        {
            if (comboBox1.Text == "")
            {
                MessageBox.Show("Выберите пару торгов");
            }
            else
            {
                GetPricePairSetting();
                GetPriceOrderbook();
                User_info();
                User_open_orders();
                ControlSpred();
                button2.Enabled = false;
                typeLastord = null;
            }
        }
        void button3_Click(object sender, EventArgs e)//сброс покупки
        {
            int rows4 = dataGridView4.Rows.Count;
            int b = rows4 - 1;

            if (rows4 > 0)
            {
                Reset_buy(dataGridView4.Rows[b].Cells[3].Value.ToString());
                dataGridView4.Rows.Clear();
                User_open_orders();
                dataGridView4.Refresh();
            }
            if (typeLastord != null)
            {
                typeLastord = null;
            }
        }
        void button4_Click(object sender, EventArgs e)//сброс продажи    
        {
            int rows3 = dataGridView3.Rows.Count;
            int s = rows3 - 1;

            if (rows3 > 0)
            {
                Reset_sell(dataGridView3.Rows[s].Cells[3].Value.ToString());
                dataGridView3.Rows.Clear();
                User_open_orders();
                dataGridView3.Refresh();
            }
            if (typeLastord != null)
            {
                typeLastord = null;
            }
        }
        void button5_Click(object sender, EventArgs e)//Старт покупок
        {
            if (button2.Enabled == true || textBox1.Text == "" || textBox2.Text == "" || textBox6.Text == "" || textBox7.Text == "")
            {
                MessageBox.Show("Заполните настройки торгов. Нажмите проверить");
            }
            else
            {
                button5.Enabled = false;
                comboBox1.Enabled = false;
                button2.Enabled = false;
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox6.Enabled = false;
                textBox7.Enabled = false;
                textBox9.Enabled = false;

                if (timer3.Enabled == false)
                {
                    timer3.Start();
                }
            }
        }
        void button6_Click(object sender, EventArgs e)//Старт продаж
        {
            if (button2.Enabled == true || textBox5.Text == "" || textBox3.Text == ""  ||  textBox4.Text == "" || textBox7.Text == "")
            {
                MessageBox.Show("Заполните настройки торгов. Нажмите проверить");
            }
            else
            {
                button6.Enabled = false;
                comboBox1.Enabled = false;
                button2.Enabled = false;
                textBox5.Enabled = false;
                textBox4.Enabled = false;
                textBox3.Enabled = false;
                textBox7.Enabled = false;
                textBox8.Enabled = false;

                if (timer3.Enabled == false)
                {
                    timer3.Start();
                }
            }
        }
        void timer3_Tick(object sender, EventArgs e)
        {
            GetPriceOrderbook();
            ControlSpred();
            AlgoritmBuy();
            AlgoritmSell();
            //AutoAvgSell();
        }
    }
}


   


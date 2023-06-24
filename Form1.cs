using Enfiyat.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Enfiyat
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Akakce akakce = new Akakce();

        private void Form1_LoadAsync(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            label1.Visible = true;
            var products = await akakce.Search(textBox1.Text);
            if (dataGridView1.Rows.Count > 0)
            {
                dataGridView1.Rows.Clear();
            }
            if (products.Item1 == null) { }
            else
            {
                foreach (ProductModel product in products.Item1)
                {
                    var item = products.Item2.Children<JObject>()
                        .FirstOrDefault(o => o["prCode"] != null && o["prCode"].ToString() == product.prCode)["qvPrices"];
                    string price = "";
                    string seller = "";
                    foreach (var prices in item)
                    {
                        price += String.Format("{0:N2}", Convert.ToDecimal(prices["price"].ToString()).ToString("C2")) + $"{Environment.NewLine}";
                        seller += prices["vdName"] + $"{Environment.NewLine}";
                    }
                    Bitmap Image = new DtImage().Convert(product.ImageUrl);
                    dataGridView1.Rows.Add(Image, product.Name, price, seller, product.ProductUrl);
                    //Console.WriteLine(product.ImageUrl);
                }
            }
            label1.Visible = false;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell.GetType() == typeof(DataGridViewLinkCell))
            {
                Process.Start((string)dataGridView1.CurrentCell.Value);
            }
        }

        private void exitBtn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}

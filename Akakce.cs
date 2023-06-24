using Enfiyat.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using System.Text;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Policy;

namespace Enfiyat
{
    public class Akakce
    {
        private HttpClientHandler handler;
        private CookieContainer cookies;

        public HttpClient Client { get; set; }
        public HtmlDocument Doc { get; set; }

        public void AkakceCrawler()
        {
            handler = new HttpClientHandler()
            {
                CookieContainer = (cookies != null) ? cookies : cookies = new CookieContainer(),
                UseCookies = true
            };
            Client = new HttpClient(handler);
            Client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.102 Safari/537.36");
            Doc = new HtmlDocument();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }


        public async Task<List<ProductDetailModel>> GetDetail(string url)
        {
            AkakceCrawler();
            var response = await Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string html = "";
                var stream = await response.Content?.ReadAsStreamAsync();
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("windows-1254")))
                    html = reader.ReadToEnd();
                Doc.LoadHtml(html);
                List<ProductDetailModel> productDetails = new List<ProductDetailModel>();
                var sellers = Doc.GetElementbyId("APL")?.SelectNodes(".//li");
                foreach (var seller in sellers)
                {
                    ProductDetailModel productDetail = new ProductDetailModel();
                    var ProductName = seller.SelectSingleNode(".//b");
                    if (ProductName == null)
                    {
                        continue;
                    }
                    productDetail.Name = WebUtility.HtmlDecode(ProductName.InnerText);
                    productDetail.Price = WebUtility.HtmlDecode(seller.SelectSingleNode(".//span[@class='pt_v8']")?.InnerText);
                    productDetail.Seller = WebUtility.HtmlDecode(seller.SelectSingleNode(".//span[@class='v_v8']")?.InnerText?.Replace("Satıcı:", ""));
                    productDetail.Seller = (productDetail.Seller.Split('/').Count() > 0) ? productDetail.Seller.Split('/')[0] : productDetail.Seller;
                    productDetail.Url = WebUtility.HtmlDecode(seller.SelectSingleNode(".//a[@class='iC']").GetAttributeValue("href", ""));
                    productDetail.Url = WebUtility.HtmlDecode("http://www.akakce.com/c" + productDetail.Url.Substring(productDetail.Url.IndexOf("/?c")));
                    productDetail.ImageUrl = WebUtility.HtmlDecode("http:" + seller.SelectSingleNode(".//img").GetAttributeValue("style", "").Replace("background-image:url(", "").Replace(")", ""));

                    productDetails.Add(productDetail);
                }
                return productDetails;
            }
            return new List<ProductDetailModel>();
        }

        public async Task<Tuple<List<ProductModel>, JArray>> Search(string query)
        {
            AkakceCrawler();
            List<ProductModel> crawledProducts = new List<ProductModel>();
            var response = await Client.GetAsync($"http://www.akakce.com/arama/?q={WebUtility.UrlEncode(query)}");
            if (response.IsSuccessStatusCode)
            {
                string html = "";
                string ProductCodes = "";
                var stream = await response.Content?.ReadAsStreamAsync();
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("utf-8")))
                    html = reader.ReadToEnd();
                Doc.LoadHtml(html);
                var products = Doc.GetElementbyId("CPL")?.SelectNodes(".//li").Where(x => !string.IsNullOrEmpty(x.InnerText)).ToList();
                if (products == null)
                {
                    products = Doc.GetElementbyId("APL")?.SelectNodes(".//li").Where(x => !string.IsNullOrEmpty(x.InnerText)).ToList();
                }
                foreach (var product in products)
                {
                    try
                    {
                        ProductModel crawledProduct = new ProductModel();
                        var link = product.SelectNodes(".//a");
                        if (link == null)
                        {
                            link = product.SelectNodes(".//span[@class='w_v8']");
                        }
                        var productName = product.SelectSingleNode(".//h3[@class='pn_v8']");
                        if (productName == null)
                        {
                            continue;
                        }
                        crawledProduct.Name = WebUtility.HtmlDecode(productName.InnerText);
                        ProductCodes += product?.Attributes.Where(x => x.Name == "data-pr").ToList()[0].Value + ",";
                        crawledProduct.prCode = product?.Attributes.Where(x => x.Name == "data-pr").ToList()[0].Value;
                        //crawledProduct.MinPriceWithShipment = WebUtility.HtmlDecode(product.SelectSingleNode("//[@class='l']").InnerText);
                        crawledProduct.ProductUrl = WebUtility.HtmlDecode("http://www.akakce.com" + link[0].GetAttributeValue("href", ""));
                        var img = product.SelectNodes(".//img")[0];
                        string imageUrl = img.GetAttributeValue("src", "");
                        if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.Contains("data:image"))
                            crawledProduct.ImageUrl = WebUtility.HtmlDecode("http:" + imageUrl);
                        else
                        {
                            imageUrl = img.GetAttributeValue("data-src", "");
                            crawledProduct.ImageUrl = WebUtility.HtmlDecode("http:" + imageUrl);
                        }
                        crawledProducts.Add(crawledProduct);
                    }
                    catch { }
                }
                var RestResponse = await Client.GetAsync($"https://api6.akakce.com/quickview/getall?prCodes={ProductCodes}");
                if (Convert.ToInt32(RestResponse.StatusCode) != 200)
                {
                    return new Tuple<List<ProductModel>, JArray>(null, null);
                }
                var RestStream = await RestResponse.Content?.ReadAsStringAsync();
                JObject json = (JObject)JsonConvert.DeserializeObject(RestStream.ToString());
                //ProductModel crawledProduct = new ProductModel();
                //foreach (JToken qvDetails in json["result"]["products"])
                //{
                //    ApiData = qvDetails;
                //    //foreach (JToken item in qvDetails["qvPrices"])
                //    //{
                //    //crawledProduct.Price = (float)item["price"];
                //    //crawledProduct.ShipPrice = (float)item["shipPrice"];
                //    //crawledProduct.Store = (string)item["vdName"];
                //    //apiCrawledProduct.Add(crawledProduct);
                //    //}
                //}
                return new Tuple<List<ProductModel>, JArray>(crawledProducts, (JArray)json["result"]["products"]);
            }
            return new Tuple<List<ProductModel>, JArray>(null, null);
        }
    }
}

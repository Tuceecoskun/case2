using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ReceiptParser
{

    public class ResponseItem
    {
        public string locale { get; set; }
        public string description { get; set; }
        public BoundingPoly boundingPoly { get; set; }
    }

    public class BoundingPoly
    {
        public List<Vertex> vertices { get; set; }
    }

    public class Vertex
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "response.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("response.json dosyası aynı klasörde bulunamadı.");
                return;
            }

            string jsonContent = File.ReadAllText(filePath);

            try
            {
                List<ResponseItem> responses = JsonConvert.DeserializeObject<List<ResponseItem>>(jsonContent);

                // İlk öğeyi (tüm metni içeren kısım) çıktıyı bozduğu için filtreleyerek diğer öğeleri sonucu almak için kullandım
                var filteredResponses = responses
                    .Where((r, index) =>
                        index != 0 && // ilk indeks genelde tüm metni içerir
                        !string.IsNullOrWhiteSpace(r.description) &&
                        r.boundingPoly?.vertices != null &&
                        r.boundingPoly.vertices.Count > 0)
                    .ToList();

                // y koordinatına göre gruplama yaparak aynı satırdaki textleri buldum
                var lineGroups = new Dictionary<int, List<ResponseItem>>();
                int tolerance = 10;

                foreach (var item in filteredResponses)
                {
                    if (item.boundingPoly?.vertices != null && item.boundingPoly.vertices.Count > 0)
                    {
                        int y = item.boundingPoly.vertices[0].y;
                        bool added = false;

                        foreach (var key in lineGroups.Keys.ToList())
                        {
                            if (Math.Abs(key - y) <= tolerance)
                            {
                                lineGroups[key].Add(item);
                                added = true;
                                break;
                            }
                        }

                        if (!added)
                        {
                            lineGroups[y] = new List<ResponseItem> { item };
                        }
                    }
                }

                
                var sortedLines = lineGroups.OrderBy(g => g.Key);

                int lineNumber = 1;
                foreach (var line in sortedLines)
                {
                    // Her satırı kendi içinde x koordinatına göre sıraladım (kelime sıraları doğru olması için)
                    var sortedWords = line.Value.OrderBy(w => w.boundingPoly.vertices[0].x);
                    string lineText = string.Join(" ", sortedWords.Select(w => w.description));

                    Console.WriteLine($"{lineNumber.ToString().PadRight(4)} {lineText}");
                    lineNumber++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON okuma veya işleme hatası: " + ex.Message);
            }
        }
    }
}

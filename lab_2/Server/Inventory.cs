
using Newtonsoft.Json;

namespace lab_2.Server
{
    public class Inventory
    {
        private static readonly string StockFilePath = Directory.Exists("lab_2") ? Path.Combine("lab_2", "stock.json") : "stock.json";
        private readonly object _lock = new();
        private int orderId = 0;
        private Dictionary<string, int> _stock;

        public Inventory()
        {
            if (File.Exists(StockFilePath))
            {
                string serializedStock = File.ReadAllText(StockFilePath);
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, int>>(serializedStock)
                    ?? throw new InvalidDataException("Stock file does not contain a valid inventory.");
                _stock = new Dictionary<string, int>(loaded, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _stock = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Coffee"] = Random.Shared.Next(10, 51),
                    ["Tea"] = Random.Shared.Next(10, 51),
                    ["Croissant"] = Random.Shared.Next(10, 51)
                };
                SaveStockToFile();
            }
        }
        public Dictionary<string, int> GetStock()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(_stock, StringComparer.OrdinalIgnoreCase);
            }
        }

        public bool TryDeduct(Dictionary<string, int> order)
        {
            lock (_lock)
            {
                foreach (var kvp in order)
                {
                    string orderItem = kvp.Key;
                    int orderAmount = kvp.Value;
                    if (orderAmount > 0)
                    {
                        if (_stock.ContainsKey(orderItem))
                        {

                            if (_stock[orderItem] >= orderAmount)
                            {
                                continue;
                            }

                        }

                    }
                    return false;
                }
                foreach (var kvp in order)
                {

                    string orderItem = kvp.Key;
                    int orderAmount = kvp.Value;

                    _stock[orderItem] = _stock[orderItem] - orderAmount;

                }
                SaveStockToFile();
                return true;

            }
        }
        public bool TryAdd(Dictionary<string, int> delivery)
        {
            lock (_lock)
            {
                foreach (var kvp in delivery)
                {
                    string deliveryItem = kvp.Key;
                    int deliveryAmount = kvp.Value;
                    if (deliveryAmount > 0)
                    {
                        if (_stock.ContainsKey(deliveryItem))
                        {
                            continue;
                        }

                    }
                    return false;
                }
                foreach (var kvp in delivery)
                {

                    string deliveryItem = kvp.Key;
                    int deliveryAmount = kvp.Value;

                    _stock[deliveryItem] = _stock[deliveryItem] + deliveryAmount;

                }
                SaveStockToFile();
                return true;

            }
        }

        private void SaveStockToFile()
        {
            File.WriteAllText(StockFilePath, JsonConvert.SerializeObject(_stock, Formatting.Indented));
        }
        public int PrepareOrder(Dictionary<string, int> order)
        {
            Dictionary<string, int> itemsPreparationTime = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Coffee"] = 4,
                ["Tea"] = 3,
                ["Croissant"] = 2
            };
            int totalTime = 0;
            foreach (var kvp in order)
            {
                string orderItem = kvp.Key;
                int orderAmount = kvp.Value;
                if (itemsPreparationTime.TryGetValue(orderItem, out int timePerItem))
                {
                    totalTime += timePerItem * orderAmount;
                }
                else
                {
                    totalTime += 2 * orderAmount;
                }
            }

            return totalTime;
        }

        public int GetId()
        {
            lock (_lock)
            {
                orderId += 1;
                return orderId;
            }
        }
    }
}
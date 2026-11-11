

namespace lab_2.Server
{
    public class Inventory
    {
        private readonly object _lock = new();
        private int orderId = 0;
        private Dictionary<string, int> _stock = new()
        {
            ["Coffee"] = Random.Shared.Next(10, 51),
            ["Tea"] = Random.Shared.Next(10, 51),
            ["Croissant"] = Random.Shared.Next(10, 51)
        };
        public Dictionary<string, int> GetStock()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(_stock);
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
                return true;

            }
        }
        public int PrepareOrder(Dictionary<string, int> order)
        {
            Dictionary<string, int> itemsPreparationTime = new()
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
                totalTime += itemsPreparationTime[orderItem] * orderAmount;
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
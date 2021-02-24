using System;

namespace HL21
{
    public class Client
    {
        private System.Net.Http.HttpClient m_http = new System.Net.Http.HttpClient();
        private License m_license = null;

        public Client(string schema, string host, int port)
        {
            m_http.BaseAddress = new Uri(schema + "://" + host + ":" + port.ToString());
        }

        ~Client()
        {
            m_http = null;
        }

        public async System.Threading.Tasks.Task<Block> post_explore(int posX, int posY, int sizeX, int sizeY)
        {
            var request_text_stream = new System.IO.MemoryStream();
            var request_json_stream = new System.Text.Json.Utf8JsonWriter(request_text_stream);

            request_json_stream.WriteStartObject();
            request_json_stream.WriteNumber("posX", posX);
            request_json_stream.WriteNumber("posY", posY);
            request_json_stream.WriteNumber("sizeX", sizeX);
            request_json_stream.WriteNumber("sizeY", sizeY);
            request_json_stream.WriteEndObject();

            request_json_stream.Flush();

            var request = new System.Net.Http.ByteArrayContent(request_text_stream.ToArray());
            request.Headers.Add("Content-Type", "application/json");

            var response = await m_http.PostAsync("/explore", request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return new Block()
            {
                posX = json.RootElement.GetProperty("area").GetProperty("posX").GetInt32(),
                posY = json.RootElement.GetProperty("area").GetProperty("posY").GetInt32(),
                sizeX = json.RootElement.GetProperty("area").GetProperty("sizeX").GetInt32(),
                sizeY = json.RootElement.GetProperty("area").GetProperty("sizeY").GetInt32(),
                amount = json.RootElement.GetProperty("amount").GetInt32()
            };
        }

        public async System.Threading.Tasks.Task<License> post_license(int coin = -1)
        {
            var request_text_stream = new System.IO.MemoryStream();
            var request_json_stream = new System.Text.Json.Utf8JsonWriter(request_text_stream);

            request_json_stream.WriteStartArray();
            if (coin != -1)
                request_json_stream.WriteNumberValue(coin);
            request_json_stream.WriteEndArray();

            request_json_stream.Flush();

            var request = new System.Net.Http.ByteArrayContent(request_text_stream.ToArray());
            request.Headers.Add("Content-Type", "application/json");

            var response = await m_http.PostAsync("/licenses", request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return new License()
            {
                id = json.RootElement.GetProperty("id").GetInt32(),
                digAllowed = json.RootElement.GetProperty("digAllowed").GetInt32(),
                digUsed = json.RootElement.GetProperty("digUsed").GetInt32()
            };
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.List<string>> post_dig(int licenseID, int posX, int posY, int depth)
        {
            var request_text_stream = new System.IO.MemoryStream();
            var request_json_stream = new System.Text.Json.Utf8JsonWriter(request_text_stream);

            request_json_stream.WriteStartObject();
            request_json_stream.WriteNumber("licenseID", licenseID);
            request_json_stream.WriteNumber("posX", posX);
            request_json_stream.WriteNumber("posY", posY);
            request_json_stream.WriteNumber("depth", depth);
            request_json_stream.WriteEndObject();

            request_json_stream.Flush();

            var request = new System.Net.Http.ByteArrayContent(request_text_stream.ToArray());
            request.Headers.Add("Content-Type", "application/json");

            var response = await m_http.PostAsync("/dig", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new System.Collections.Generic.List<string>();
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var kostyl = new System.Collections.Generic.List<string>();
                kostyl.Add("i_need_license!!!");
                return kostyl;
            }
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var length = json.RootElement.GetArrayLength();
            var treasures = new System.Collections.Generic.List<string>(length);

            for (int i = 0; i < length; ++i)
                treasures.Add(json.RootElement[i].GetString());

            return treasures;
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.List<int>> post_cash(string treasure)
        {
            var request_text_stream = new System.IO.MemoryStream();
            var request_json_stream = new System.Text.Json.Utf8JsonWriter(request_text_stream);

            request_json_stream.WriteStringValue(treasure);

            request_json_stream.Flush();

            var request = new System.Net.Http.ByteArrayContent(request_text_stream.ToArray());
            request.Headers.Add("Content-Type", "application/json");

            var response = await m_http.PostAsync("/cash", request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var length = json.RootElement.GetArrayLength();
            var money = new System.Collections.Generic.List<int>(length);

            for (int i = 0; i < length; ++i)
                money.Add(json.RootElement[i].GetInt32());

            return money;
        }

        public async System.Threading.Tasks.Task explore_blocks(int posX, System.Collections.Generic.List<Block> blocks)
        {
            for (int block = 0; block < 10; ++block)
            {
                Block result = null;
                while (result is null)
                    result = await post_explore(posX, block * 350, 35, 350);
                blocks.Add(result);
            }
        }

        public async System.Threading.Tasks.Task dig_blocks(System.Collections.Generic.List<Block> blocks, System.Collections.Generic.List<int> coins)
        {
            while (0 < blocks.Count)
            {
                var block = blocks[0];
                blocks.RemoveAt(0);

                for (int x = block.posX; x < block.posX + block.sizeX; ++x)
                    for (int y = block.posY; y < block.posY + block.sizeY; ++y)
                    {
                        Block result = null;
                        while (result is null)
                            result = await post_explore(x, y, 1, 1);
                        for (int h = 0; h < 10 && 0 < result.amount; ++h)
                        {
                            while (m_license is null || m_license.digAllowed <= m_license.digUsed)
                            {
                                int coin = -1;
                                if (0 < coins.Count)
                                {
                                    coin = coins[0];
                                    coins.RemoveAt(0);
                                }
                                m_license = await post_license(coin);
                            }
                            System.Collections.Generic.List<string> treasures = null;
                            while (treasures is null)
                                treasures = await post_dig(m_license.id, x, y, h + 1);
                            ++m_license.digUsed;
                            if (treasures.Count == 1 && treasures[0] == "i_need_license!!!")
                            {
                                m_license = null;
                                --h;
                                continue;
                            }
                            foreach (var treasure in treasures)
                            {
                                System.Collections.Generic.List<int> money = null;
                                while (money is null)
                                    money = await post_cash(treasure);
                                coins.AddRange(money);
                            }
                        }
                    }
            }
        }
    }

    public class Block : IComparable
    {
        public int posX;
        public int posY;
        public int sizeX;
        public int sizeY;
        public int amount;

        public int CompareTo(object obj)
        {
            return (obj as Block).amount.CompareTo(amount);
        }
    }

    public class License
    {
        public int id;
        public int digAllowed;
        public int digUsed;
    }

    class Program
    {
        static void Main(string[] args)
        {
            string host = System.Environment.GetEnvironmentVariable("ADDRESS") ?? "127.0.0.1";
            int port = int.Parse(System.Environment.GetEnvironmentVariable("Port") ?? "8000");
            string schema = System.Environment.GetEnvironmentVariable("Schema") ?? "http";

            var max_clients = 100;
            var max_dig_clients = 10;

            var clients = new System.Collections.Generic.List<Client>(max_clients);
            for (int i = 0; i < max_clients; ++i)
                clients.Add(new Client(schema, host, port));

            var blocks = new System.Collections.Generic.List<Block>(1000);

            // Explore
            // 100x100 blocks, 35x35 size
            // Each client tests 100 vertical blocks and 2 horizontal
            var explore_tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>(100);
            for (int i = 0; i < 100; ++i)
                explore_tasks.Add(clients[i % max_clients].explore_blocks(i * 35, blocks));
            System.Threading.Tasks.Task.WhenAll(explore_tasks).Wait();
            blocks.Sort();

            var coins = new System.Collections.Generic.List<int>(1000000);

            // Digs
            // 10 connections on 10 free licenses
            var dig_tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>(10);
            for (int i = 0; i < max_clients; ++i)
                if (i < max_dig_clients)
                    dig_tasks.Add(clients[i].dig_blocks(blocks, coins));
                else
                    clients[i] = null;
            System.Threading.Tasks.Task.WhenAll(dig_tasks).Wait();
        }
    }
}

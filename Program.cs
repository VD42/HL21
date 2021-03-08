#define _DEBUG

using System;

namespace HL21
{
    public class NowDateTime
    {
        public static string Prefix()
        {
            return System.DateTime.Now.ToString("[dd-MM-yyyy HH:mm:ss.fffffff] ");
        }
    }

    public class Client
    {
        private System.Net.Http.HttpClient m_http = new System.Net.Http.HttpClient();
        private Stats m_stats;

        public Client(string schema, string host, int port, Stats stats)
        {
            m_stats = stats;
            m_http.BaseAddress = new Uri(schema + "://" + host + ":" + port.ToString());
        }

        ~Client()
        {
        }

        public Block post_explore(int posX, int posY, int sizeX, int sizeY)
        {
            var start_time = DateTime.Now;
            try
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

                request_text_stream.Position = 0;
                var content = new System.Net.Http.StreamContent(request_text_stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = m_http.PostAsync("/explore", content).Result;

                if (response.StatusCode != System.Net.HttpStatusCode.OK || sizeX * sizeY != 1)
                    m_stats.answer("/explore (" + (sizeX * sizeY).ToString() + ")", response.StatusCode, DateTime.Now - start_time);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var json = System.Text.Json.JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
                var block = new Block()
                {
                    posX = json.RootElement.GetProperty("area").GetProperty("posX").GetInt32(),
                    posY = json.RootElement.GetProperty("area").GetProperty("posY").GetInt32(),
                    sizeX = json.RootElement.GetProperty("area").GetProperty("sizeX").GetInt32(),
                    sizeY = json.RootElement.GetProperty("area").GetProperty("sizeY").GetInt32(),
                    amount = json.RootElement.GetProperty("amount").GetInt32()
                };

                if (sizeX * sizeY == 1)
                    m_stats.answer("/explore (" + (sizeX * sizeY).ToString() + ", " + block.amount.ToString() + ")", response.StatusCode, DateTime.Now - start_time);

                return block;
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/explore (" + (sizeX * sizeY).ToString() + ")", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/explore (" + (sizeX * sizeY).ToString() + ")", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        public License post_license(System.Collections.Generic.List<int> coins)
        {
            var start_time = DateTime.Now;
            try
            {
                var request_text_stream = new System.IO.MemoryStream();
                var request_json_stream = new System.Text.Json.Utf8JsonWriter(request_text_stream);

                request_json_stream.WriteStartArray();
                foreach (var coin in coins)
                    request_json_stream.WriteNumberValue(coin);
                request_json_stream.WriteEndArray();

                request_json_stream.Flush();

                request_text_stream.Position = 0;
                var content = new System.Net.Http.StreamContent(request_text_stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var task = m_http.PostAsync("/licenses", content);
                task.Wait(200);

                if (coins.Count == 0 && !task.IsCompleted)
                {
                    m_stats.answer("/licenses (" + coins.Count.ToString() + ")", System.Net.HttpStatusCode.Processing, DateTime.Now - start_time);
                    return null;
                }

                var response = task.Result;

                m_stats.answer("/licenses (" + coins.Count.ToString() + ")", response.StatusCode, DateTime.Now - start_time);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var json = System.Text.Json.JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
                return new License()
                {
                    id = json.RootElement.GetProperty("id").GetInt32(),
                    digAllowed = json.RootElement.GetProperty("digAllowed").GetInt32(),
                    digUsed = json.RootElement.GetProperty("digUsed").GetInt32()
                };
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/licenses (" + coins.Count.ToString() + ")", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/licenses (" + coins.Count.ToString() + ")", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        public void fast_post_license(int coin = -1)
        {
            var start_time = DateTime.Now;
            try
            {
                var request_text_stream = new System.IO.MemoryStream();
                var request_json_stream = new System.Text.Json.Utf8JsonWriter(request_text_stream);

                request_json_stream.WriteStartArray();
                if (coin != -1)
                    request_json_stream.WriteNumberValue(coin);
                request_json_stream.WriteEndArray();

                request_json_stream.Flush();

                request_text_stream.Position = 0;
                var content = new System.Net.Http.StreamContent(request_text_stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                m_http.PostAsync("/licenses", content).Wait(1);

                m_stats.answer("/licenses " + (coin == -1 ? "(free, fast)" : "(paid, fast)"), System.Net.HttpStatusCode.Accepted, DateTime.Now - start_time);
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/licenses " + (coin == -1 ? "(free, fast)" : "(paid, fast)"), System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
            }
            catch (Exception ex)
            {
                m_stats.answer("/licenses " + (coin == -1 ? "(free, fast)" : "(paid, fast)"), System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
            }
        }

        public System.Collections.Generic.List<string> post_dig(int licenseID, int posX, int posY, int depth)
        {
            var start_time = DateTime.Now;
            try
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

                request_text_stream.Position = 0;
                var content = new System.Net.Http.StreamContent(request_text_stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = m_http.PostAsync("/dig", content).Result;

                m_stats.answer("/dig", response.StatusCode, DateTime.Now - start_time);

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

                var json = System.Text.Json.JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);

                var length = json.RootElement.GetArrayLength();
                var treasures = new System.Collections.Generic.List<string>(length);

                for (int i = 0; i < length; ++i)
                    treasures.Add(json.RootElement[i].GetString());

                return treasures;
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/dig", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/dig", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        public System.Collections.Generic.List<int> post_cash(string treasure)
        {
            var start_time = DateTime.Now;
            try
            {
                var request_text_stream = new System.IO.MemoryStream();
                var request_json_stream = new System.Text.Json.Utf8JsonWriter(request_text_stream);

                request_json_stream.WriteStringValue(treasure);

                request_json_stream.Flush();

                request_text_stream.Position = 0;
                var content = new System.Net.Http.StreamContent(request_text_stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = m_http.PostAsync("/cash", content).Result;

                m_stats.answer("/cash", response.StatusCode, DateTime.Now - start_time);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var json = System.Text.Json.JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);

                var length = json.RootElement.GetArrayLength();
                var money = new System.Collections.Generic.List<int>(length);

                for (int i = 0; i < length; ++i)
                    money.Add(json.RootElement[i].GetInt32());

                return money;
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/cash", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/cash", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        public System.Collections.Generic.List<int> fast_post_cash(string treasure)
        {
            var start_time = DateTime.Now;
            try
            {
                var request_text_stream = new System.IO.MemoryStream();
                var request_json_stream = new System.Text.Json.Utf8JsonWriter(request_text_stream);

                request_json_stream.WriteStringValue(treasure);

                request_json_stream.Flush();

                request_text_stream.Position = 0;
                var content = new System.Net.Http.StreamContent(request_text_stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var task = m_http.PostAsync("/cash", content);
                task.Wait(50);

                if (!task.IsCompleted)
                {
                    m_stats.answer("/cash (fast)", System.Net.HttpStatusCode.Processing, DateTime.Now - start_time);
                    return new System.Collections.Generic.List<int>();
                }

                var response = task.Result;

                m_stats.answer("/cash (fast)", task.Result.StatusCode, DateTime.Now - start_time);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var json = System.Text.Json.JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);

                var length = json.RootElement.GetArrayLength();
                var money = new System.Collections.Generic.List<int>(length);

                for (int i = 0; i < length; ++i)
                    money.Add(json.RootElement[i].GetInt32());

                return money;
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/cash (fast)", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/cash (fast)", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        public System.Collections.Generic.List<int> get_balance()
        {
            var start_time = DateTime.Now;
            try
            {
                var response = m_http.GetAsync("/balance").Result;

                m_stats.answer("/balance", response.StatusCode, DateTime.Now - start_time);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var json = System.Text.Json.JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);

                var length = json.RootElement.GetProperty("wallet").GetArrayLength();
                var money = new System.Collections.Generic.List<int>(length);

                for (int i = 0; i < length; ++i)
                    money.Add(json.RootElement.GetProperty("wallet")[i].GetInt32());

                return money;
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/balance", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/balance", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        public System.Collections.Generic.List<License> get_licenses()
        {
            var start_time = DateTime.Now;
            try
            {
                var response = m_http.GetAsync("/licenses").Result;

                m_stats.answer("/licenses", response.StatusCode, DateTime.Now - start_time);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;

                var json = System.Text.Json.JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);

                var length = json.RootElement.GetArrayLength();
                var licenses = new System.Collections.Generic.List<License>(length);

                for (int i = 0; i < length; ++i)
                {
                    licenses.Add(new License() {
                        id = json.RootElement[i].GetProperty("id").GetInt32(),
                        digAllowed = json.RootElement[i].GetProperty("digAllowed").GetInt32(),
                        digUsed = json.RootElement[i].GetProperty("digUsed").GetInt32()
                    });
                }

                return licenses;
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/licenses", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/licenses", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        public string get_health_check()
        {
            var start_time = DateTime.Now;
            try
            {
                var response = m_http.GetAsync("/health-check").Result;

                m_stats.answer("/health-check", response.StatusCode, DateTime.Now - start_time);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return "";

                return response.Content.ReadAsStringAsync().Result;
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/health-check", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/health-check", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        public void work(
            int index, int count,
            System.Threading.Mutex big_blocks_mutex, System.Collections.Generic.List<Block> big_blocks,
            System.Threading.Mutex blocks_mutex, System.Collections.Generic.List<Block> blocks,
            LicenseManager lm,
            System.Threading.Mutex treasures_mutex, System.Collections.Generic.List<Treasure> treasures
        )
        {
            int current_big_block_x = 0;
            int current_big_block_y = index;

            bool i_ve_500 = false;
            //bool i_ve_40000 = false;

            while (true)
            {
                if (i_ve_500 && 30 <= index)
                    break;

                // Treasures

                bool found_treasure = false;
                {
                    Treasure treasure = null;
                    lock (treasures_mutex)
                    {
                        if (0 < treasures.Count)
                        {
                            treasure = treasures[treasures.Count - 1];
                            treasures.RemoveAt(treasures.Count - 1);
                        }
                    }
                    if (treasure != null)
                    {
                        if (i_ve_500 && treasure.depth < 2)
                            continue;
                        System.Collections.Generic.List<int> money;
                        if (!i_ve_500)
                            money = post_cash(treasure.id);
                        else
                            money = fast_post_cash(treasure.id);
                        if (money is null)
                        {
                            lock (treasures_mutex)
                            {
                                treasures.Add(treasure);
                                treasures.Sort();
                            }
                        }
                        else
                        {
                            int max_m = -1;
                            foreach (var m in money)
                                if (max_m < m)
                                    max_m = m;
                            if (!i_ve_500 && 500 < max_m)
                                i_ve_500 = true;
                            lock (Program.coin_mutex)
                            {
                                if (max_m == -1)
                                    max_m = Program.max_coin_id + 3 * (treasure.depth + 1);
                                if (Program.max_coin_id < max_m)
                                    Program.max_coin_id = max_m;
                            }
                        }
                        found_treasure = true;
                    }
                }
                if (found_treasure)
                    continue;

                // Licenses

                //if (lm.update_licenses(this))
                //    continue;

                // Digs

                bool found_block = false;
                {
                    Block block = null;
                    lock (blocks_mutex)
                    {
                        if (0 < blocks.Count)
                        {
                            block = blocks[blocks.Count - 1];
                            blocks.RemoveAt(blocks.Count - 1);
                        }
                    }
                    if (block != null)
                    {
                        var max_h = 10;
                        for (int h = block.last_h; h < max_h && 0 < block.amount; ++h)
                        {
                            block.last_h = h;
                            var license_id = lm.get_license(this);
                            if (license_id == null)
                            {
                                lock (blocks_mutex)
                                {
                                    blocks.Add(block);
                                    blocks.Sort();
                                }
                                break;
                            }
                            System.Collections.Generic.List<string> surprise = null;
                            while (surprise is null)
                                surprise = post_dig(license_id.Value, block.posX, block.posY, h + 1);
                            lm.use_license(license_id.Value);
                            if (surprise.Count == 1 && surprise[0] == "i_need_license!!!")
                            {
                                lock (blocks_mutex)
                                {
                                    blocks.Add(block);
                                    blocks.Sort();
                                }
                                break;
                            }
                            lock (treasures_mutex)
                            {
                                foreach (var treasure in surprise)
                                    treasures.Add(new Treasure() { id = treasure, depth = h });
                                treasures.Sort();
                            }
                            block.amount -= treasures.Count;
                            if (h + 1 < max_h && 0 < block.amount)
                            {
                                block.last_h = h + 1;
                                lock (blocks_mutex)
                                {
                                    blocks.Add(block);
                                    blocks.Sort();
                                }
                                break;
                            }
                        }
                        found_block = true;
                    }
                }
                if (found_block)
                    continue;

                // Small blocks

                bool found_big_block = false;
                {
                    Block big_block = null;
                    lock (big_blocks_mutex)
                    {
                        if (0 < big_blocks.Count)
                        {
                            big_block = big_blocks[big_blocks.Count - 1];
                            big_blocks.RemoveAt(big_blocks.Count - 1);
                        }
                    }
                    if (big_block != null)
                    {
                        int left_size = big_block.sizeX / 2;
                        int right_size = big_block.sizeX - left_size;

                        Block left_block = null;
                        while (left_block is null)
                            left_block = post_explore(big_block.posX, big_block.posY, left_size, 1);

                        var right_block = new Block() { posX = left_block.posX + left_block.sizeX, posY = big_block.posY, sizeX = right_size, sizeY = 1, amount = big_block.amount - left_block.amount };

                        if (0 < left_block.amount)
                        {
                            if (left_block.sizeX == 1)
                            {
                                lock (blocks_mutex)
                                {
                                    blocks.Add(left_block);
                                    blocks.Sort();
                                }
                            }
                            else
                            {
                                lock (big_blocks_mutex)
                                {
                                    big_blocks.Add(left_block);
                                    big_blocks.Sort();
                                }
                            }
                        }

                        if (0 < right_block.amount)
                        {
                            if (right_block.sizeX == 1)
                            {
                                lock (blocks_mutex)
                                {
                                    blocks.Add(right_block);
                                    blocks.Sort();
                                }
                            }
                            else
                            {
                                lock (big_blocks_mutex)
                                {
                                    big_blocks.Add(right_block);
                                    big_blocks.Sort();
                                }
                            }
                        }

                        found_big_block = true;
                    }
                }
                if (found_big_block)
                    continue;

                // Big blocks

                if (current_big_block_x < 3500 && current_big_block_y < 3500)
                {
                    var block_size = 14;
                    Block block = null;
                    while (block is null)
                        block = post_explore(current_big_block_x, current_big_block_y, block_size, 1);
                    if (0 < block.amount)
                    {
                        lock (big_blocks_mutex)
                        {
                            big_blocks.Add(block);
                            big_blocks.Sort();
                        }
                    }
                    current_big_block_y += count;
                    if (3500 <= current_big_block_y)
                    {
                        current_big_block_x += block_size;
                        current_big_block_y = index;
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
        public int last_h = 0;

        public int CompareTo(object obj)
        {
            var amount_compare = amount.CompareTo((obj as Block).amount);
            if (amount_compare == 0)
            {
                var last_h_compare = last_h.CompareTo((obj as Block).last_h);
                if (last_h_compare == 0)
                    return ((obj as Block).sizeX * (obj as Block).sizeY).CompareTo(sizeX * sizeY);
                return last_h_compare;
            }
            return amount_compare;
        }
    }

    public class License : IComparable
    {
        public int id;
        public int digAllowed;
        public int digUsed;

        public int digUsing;

        public int CompareTo(object obj)
        {
            return -(digAllowed - digUsing).CompareTo((obj as License).digAllowed - (obj as License).digUsing);
        }
    }

    public class Treasure : IComparable
    {
        public string id;
        public int depth;

        public int CompareTo(object obj)
        {
            return depth.CompareTo((obj as Treasure).depth);
        }
    }

    public class AnswerInfo
    {
        public int count = 0;
        public TimeSpan time = new TimeSpan();
        public TimeSpan min_time = TimeSpan.MaxValue;
        public TimeSpan max_time = TimeSpan.MinValue;
    }

    public class Stats
    {
        private int m_total = 0;
        private System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<System.Net.HttpStatusCode, AnswerInfo>> m_answers;
        public bool m_verbose = false;

        private System.Threading.Mutex m_big_blocks_mutex;
        private System.Threading.Mutex m_blocks_mutex;
        private System.Threading.Mutex m_treasures_mutex;

        private System.Collections.Generic.List<Block> m_big_blocks;
        private System.Collections.Generic.List<Block> m_blocks;
        private System.Collections.Generic.List<Treasure> m_treasures;

        private LicenseManager m_lm;

        public Stats(
            System.Threading.Mutex big_blocks_mutex, System.Collections.Generic.List<Block> big_blocks,
            System.Threading.Mutex blocks_mutex, System.Collections.Generic.List<Block> blocks,
            LicenseManager lm,
            System.Threading.Mutex treasures_mutex, System.Collections.Generic.List<Treasure> treasures
        )
        {
#if _DEBUG
            m_big_blocks_mutex = big_blocks_mutex;
            m_blocks_mutex = blocks_mutex;
            m_treasures_mutex = treasures_mutex;
            m_big_blocks = big_blocks;
            m_blocks = blocks;
            m_treasures = treasures;
            m_lm = lm;

            Console.Error.WriteLine(NowDateTime.Prefix() + "Start");
            m_answers = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<System.Net.HttpStatusCode, AnswerInfo>>();
#endif
        }

        public void answer(string method, System.Net.HttpStatusCode status, TimeSpan ts)
        {
#if _DEBUG
            ++m_total;

            m_answers.TryAdd(method, new System.Collections.Concurrent.ConcurrentDictionary<System.Net.HttpStatusCode, AnswerInfo>());
            m_answers[method].TryAdd(status, new AnswerInfo());
            ++m_answers[method][status].count;
            m_answers[method][status].time += ts;
            if (ts < m_answers[method][status].min_time)
                m_answers[method][status].min_time = ts;
            if (m_answers[method][status].max_time < ts)
                m_answers[method][status].max_time = ts;

            if (m_verbose)
                Console.Error.WriteLine(NowDateTime.Prefix() + method + " " + status.ToString());
#endif
        }

        public void print()
        {
#if _DEBUG
            int big_blocks = 0;
            lock (m_big_blocks_mutex)
            {
                big_blocks = m_big_blocks.Count;
            }
            int blocks = 0;
            lock (m_blocks_mutex)
            {
                blocks = m_blocks.Count;
            }
            int treausures = 0;
            lock (m_treasures_mutex)
            {
                treausures = m_treasures.Count;
            }
            int coins = 0;
            lock (Program.coin_mutex)
            {
                coins = Program.max_coin_id - Program.current_coin_id;
            }

            Console.Error.WriteLine(NowDateTime.Prefix() + "Stats:");
            var total_time = new TimeSpan();
            foreach (var answer in m_answers)
            {
                Console.Error.WriteLine("    " + answer.Key);
                foreach (var status in answer.Value)
                {
                    Console.Error.WriteLine("        " + status.Key.ToString() + ": " + status.Value.count.ToString() + " (" + status.Value.min_time.TotalMilliseconds.ToString() + " ... " + (status.Value.time.TotalMilliseconds / (0 < status.Value.count ? status.Value.count : 1)).ToString() + " ... " + status.Value.max_time.TotalMilliseconds.ToString() + ")");
                    total_time += status.Value.time;
                }
            }
            Console.Error.WriteLine("    TOTAL: " + m_total.ToString() + " (" + total_time.TotalMilliseconds.ToString() + ")");
            Console.Error.WriteLine("    COINS: " + coins.ToString());
            Console.Error.WriteLine("    TREASURES: " + treausures.ToString());
            Console.Error.WriteLine("    BLOCKS: " + blocks.ToString());
            Console.Error.WriteLine("    BIG BLOCKS: " + big_blocks.ToString());
#endif
        }

        public void stats(Client client)
        {
#if _DEBUG
            while (true)
            {
                System.Threading.Thread.Sleep(10000);
                print();
                //Console.Error.WriteLine("Server status: ");
                //Console.Error.WriteLine(client.get_health_check());
            }
#endif
        }
    }

    public class LicenseManager
    {
        private System.Collections.Generic.List<License> m_licenses = new System.Collections.Generic.List<License>();
        private System.Threading.Mutex m_mutex = new System.Threading.Mutex();

        public LicenseManager()
        {
        }

        public int? get_license(Client client)
        {
            lock (m_mutex)
            {
                m_licenses.Sort();
                for (int i = m_licenses.Count - 1; 0 <= i; --i)
                    if (m_licenses[i] != null && m_licenses[i].digUsing < m_licenses[i].digAllowed)
                    {
                        ++m_licenses[i].digUsing;
                        return m_licenses[i].id;
                    }
            }
            if (update_licenses(client))
            {
                lock (m_mutex)
                {
                    m_licenses.Sort();
                    for (int i = m_licenses.Count - 1; 0 <= i; --i)
                        if (m_licenses[i] != null && m_licenses[i].digUsing < m_licenses[i].digAllowed)
                        {
                            ++m_licenses[i].digUsing;
                            return m_licenses[i].id;
                        }
                }
            }
            return null;
        }

        public void use_license(int id)
        {
            lock (m_mutex)
            {
                for (int i = 0; i < m_licenses.Count; ++i)
                    if (m_licenses[i] != null && m_licenses[i].id == id)
                    {
                        if (m_licenses[i].digAllowed <= (++m_licenses[i].digUsed))
                            m_licenses.RemoveAt(i);
                        break;
                    }
            }
        }

        public bool update_licenses(Client client)
        {
            bool working = false;

            lock (m_mutex)
            {
                working = (m_licenses.Count < 10);
                if (working)
                    m_licenses.Add(null);
            }

            if (!working)
                return false;

            License license = null;
            while (license is null)
            {
                var coins = new System.Collections.Generic.List<int>();
                lock (Program.coin_mutex)
                {
                    if (Program.current_coin_id < Program.max_coin_id)
                        coins.Add(++Program.current_coin_id);
                }
                license = client.post_license(coins);
            }

            lock (m_mutex)
            {
                for (int i = 0; i < m_licenses.Count; ++i)
                    if (m_licenses[i] is null)
                    {
                        m_licenses.RemoveAt(i);
                        break;
                    }
                m_licenses.Add(license);
            }

            return true;
        }
    }

    public class Program
    {
        public static System.Threading.Mutex coin_mutex = new System.Threading.Mutex();
        public static int current_coin_id = -1;
        public static int max_coin_id = -1;

        public static void Main(string[] args)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 65536;

            string host = System.Environment.GetEnvironmentVariable("ADDRESS") ?? "127.0.0.1";
            int port = int.Parse(System.Environment.GetEnvironmentVariable("Port") ?? "8000");
            string schema = System.Environment.GetEnvironmentVariable("Schema") ?? "http";

            var big_blocks_mutex = new System.Threading.Mutex();
            var blocks_mutex = new System.Threading.Mutex();
            var treasures_mutex = new System.Threading.Mutex();

            var big_blocks = new System.Collections.Generic.List<Block>();
            var blocks = new System.Collections.Generic.List<Block>();
            var treasures = new System.Collections.Generic.List<Treasure>();

            var lm = new LicenseManager();

            var stats = new Stats(
                big_blocks_mutex, big_blocks,
                blocks_mutex, blocks,
                lm,
                treasures_mutex, treasures
            );

            var max_threads = 60;

            var threads = new System.Collections.Generic.List<System.Threading.Thread>(max_threads);

            for (int i = 0; i < max_threads; ++i)
            {
                var index = i;
                var client = new Client(schema, host, port, stats);
                threads.Add(new System.Threading.Thread(() =>
                {
                    client.work(
                        index, max_threads,
                        big_blocks_mutex, big_blocks,
                        blocks_mutex, blocks,
                        lm,
                        treasures_mutex, treasures
                    );
                }));
                threads[threads.Count - 1].Start();
            }

            {
                var client = new Client(schema, host, port, stats);
                stats.stats(client);
            }
        }
    }
}

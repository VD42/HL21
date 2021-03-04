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

        public License post_license(int coin = -1)
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
                var response = m_http.PostAsync("/licenses", content).Result;

                m_stats.answer("/licenses " + (coin == -1 ? "(free)" : "(paid)"), response.StatusCode, DateTime.Now - start_time);

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
                m_stats.answer("/licenses " + (coin == -1 ? "(free)" : "(paid)"), System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
                return null;
            }
            catch (Exception ex)
            {
                m_stats.answer("/licenses " + (coin == -1 ? "(free)" : "(paid)"), System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
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

        public void fast_post_cash(string treasure)
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
                System.Net.HttpStatusCode code = System.Net.HttpStatusCode.Accepted;
                while (true)
                {
                    var task = m_http.PostAsync("/cash", content);
                    task.Wait(5);
                    if (!task.IsCompleted || (code = task.Result.StatusCode) == System.Net.HttpStatusCode.OK)
                        break;
                }

                m_stats.answer("/cash (fast)", code, DateTime.Now - start_time);
            }
            catch (AggregateException ae)
            {
                m_stats.answer("/cash (fast)", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                ae.Handle((ex) => {
                    //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
                    return true;
                });
            }
            catch (Exception ex)
            {
                m_stats.answer("/cash (fast)", System.Net.HttpStatusCode.NoContent, DateTime.Now - start_time);
                //Console.Error.WriteLine(NowDateTime.Prefix() + ex.GetType().Name + ": " + ex.Message);
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

        public void explore_big_blocks(System.Threading.Mutex big_blocks_mutex, System.Collections.Generic.List<Block> big_blocks, int i, int count)
        {
            for (int x = i * 10; x < 3500; x += 10 * count)
            {
                for (int y = 0; y < 3500; y += 10)
                {
                    Block block = null;
                    while (block is null)
                        block = post_explore(x, y, 10, 10);
                    if (0 < block.amount)
                    {
                        lock (big_blocks_mutex)
                        {
                            big_blocks.Add(block);
                            big_blocks.Sort();
                        }
                    }
                }
            }
        }

        public void explore_blocks(System.Threading.Mutex big_blocks_mutex, System.Collections.Generic.List<Block> big_blocks, System.Threading.Mutex blocks_mutex, System.Collections.Generic.List<Block> blocks)
        {
            while (true)
            {
                Block block = null;
                lock (big_blocks_mutex)
                {
                    if (0 < big_blocks.Count)
                    {
                        block = big_blocks[big_blocks.Count - 1];
                        big_blocks.RemoveAt(big_blocks.Count - 1);
                    }
                }
                if (block == null)
                {
                    System.Threading.Thread.Sleep(1);
                    continue;
                }

                for (int by = block.posY; by < block.posY + 10 && 0 < block.amount; ++by)
                {
                    Block line = null;
                    if (by == block.posY + 10 - 1)
                    {
                        line = new Block() { posX = block.posX, posY = by, sizeX = 10, sizeY = 1, amount = block.amount };
                    }
                    else
                    {
                        while (line is null)
                            line = post_explore(block.posX, by, 10, 1);
                    }
                    if (0 < line.amount)
                    {
                        lock (blocks_mutex)
                        {
                            blocks.Add(line);
                            blocks.Sort();
                        }
                        block.amount -= line.amount;
                    }
                }
            }

            /*for (int y = 0; y < 3500; ++y)
            {
                for (int x = 0; x < 3500; x += 10)
                {
                    Block line = null;
                    while (line is null)
                        line = post_explore(x, y, 10, 1);
                    if (0 < line.amount)
                    {
                        lock (blocks_mutex)
                        {
                            blocks.Add(line);
                            blocks.Sort();
                        }
                    }
                }
            }*/
        }

        public void dig_blocks(System.Threading.Mutex blocks_mutex, System.Collections.Generic.List<Block> blocks, LicenseManager lm, System.Threading.Mutex treasures_mutex, System.Collections.Generic.List<Treasure> treasures)
        {
            while (true)
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
                if (block == null)
                {
                    System.Threading.Thread.Sleep(1);
                    continue;
                }

                /*for (int y = block.posY; y < block.posY + 10 && 0 < block.amount; ++y)
                {
                    Block line = null;
                    while (line is null)
                        line = post_explore(block.posX, y, 10, 1);*/

                    for (int x = block.posX; x < block.posX + block.sizeX && 0 < block.amount; ++x)
                    {
                        Block result = null;
                        if (x == block.posX + block.sizeX - 1)
                        {
                            result = new Block() { posX = x, posY = block.posY, sizeX = 1, sizeY = 1, amount = block.amount };
                        }
                        else
                        {
                            while (result is null)
                                result = post_explore(x, block.posY, 1, 1);
                        }

                        for (int h = 0; h < 10 && 0 < result.amount; ++h)
                        {
                            int? license_id = null;
                            while (license_id is null)
                                license_id = lm.get_license();
                            System.Collections.Generic.List<string> surprise = null;
                            while (surprise is null)
                                surprise = post_dig(license_id.Value, x, block.posY, h + 1);
                            lm.use_license(license_id.Value);
                            if (surprise.Count == 1 && surprise[0] == "i_need_license!!!")
                            {
                                --h;
                                continue;
                            }
                            lock (treasures_mutex)
                            {
                                foreach (var treasure in surprise)
                                    treasures.Add(new Treasure() { id = treasure, depth = h });
                                treasures.Sort();
                            }
                            result.amount -= treasures.Count;
                            //line.amount -= treasures.Count;
                            block.amount -= treasures.Count;
                        }
                    }
                //}
            }
        }

        public void cash_treasures(System.Threading.Mutex treasures_mutex, System.Collections.Generic.List<Treasure> treasures, System.Collections.Concurrent.ConcurrentBag<int> coins)
        {
            while (true)
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
                if (treasure == null)
                {
                    System.Threading.Thread.Sleep(1);
                    continue;
                }

                System.Collections.Generic.List<int> money = null;
                while (money is null)
                    money = post_cash(treasure.id);
                foreach (var m in money)
                    coins.Add(m);
            }
        }

        public void manage_licenses(LicenseManager lm)
        {
            while (true)
            {
                lm.update_licenses(this);
            }
        }

        public void work(
            int index, int count,
            System.Threading.Mutex big_blocks_mutex, System.Collections.Generic.List<Block> big_blocks,
            System.Threading.Mutex blocks_mutex, System.Collections.Generic.List<Block> blocks,
            LicenseManager lm,
            System.Threading.Mutex treasures_mutex, System.Collections.Generic.List<Treasure> treasures,
            System.Collections.Concurrent.ConcurrentBag<int> coins
        )
        {
            int current_big_block_x = 0;
            int current_big_block_y = index;

            while (true)
            {
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
                        System.Collections.Generic.List<int> money = null;
                        while (money is null)
                            money = post_cash(treasure.id);
                        foreach (var m in money)
                            coins.Add(m);
                        found_treasure = true;
                    }
                }
                if (found_treasure)
                    continue;

                // Licenses

                if (lm.update_licenses(this))
                    continue;

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
                        for (int h = block.last_h; h < 10 && 0 < block.amount; ++h)
                        {
                            block.last_h = h;
                            var license_id = lm.get_license();
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
                        }
                        found_block = true;
                    }
                }
                if (found_block)
                    continue;

                if (index < 25)
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
                        for (int x = big_block.posX; x < big_block.posX + big_block.sizeX && 0 < big_block.amount; ++x)
                        {
                            Block result = null;
                            if (x == big_block.posX + big_block.sizeX - 1)
                            {
                                result = new Block() { posX = x, posY = big_block.posY, sizeX = 1, sizeY = 1, amount = big_block.amount };
                            }
                            else
                            {
                                while (result is null)
                                    result = post_explore(x, big_block.posY, 1, 1);
                            }
                            if (0 < result.amount)
                            {
                                lock (blocks_mutex)
                                {
                                    blocks.Add(result);
                                    blocks.Sort();
                                }
                                big_block.amount -= result.amount;
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
                    Block block = null;
                    while (block is null)
                        block = post_explore(current_big_block_x, current_big_block_y, 14, 1);
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
                        current_big_block_x += 14;
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
                return last_h.CompareTo((obj as Block).last_h);
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
            return (digAllowed - digUsing).CompareTo((obj as License).digAllowed - (obj as License).digUsing);
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
    }

    public class Stats
    {
        private int m_total = 0;
        private System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<System.Net.HttpStatusCode, AnswerInfo>> m_answers;
        public bool m_verbose = false;

        public Stats()
        {
#if _DEBUG
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

            if (m_verbose)
                Console.Error.WriteLine(NowDateTime.Prefix() + method + " " + status.ToString());
#endif
        }

        public void print()
        {
#if _DEBUG
            Console.Error.WriteLine(NowDateTime.Prefix() + "Stats:");
            var total_time = new TimeSpan();
            foreach (var answer in m_answers)
            {
                Console.Error.WriteLine("    " + answer.Key);
                foreach (var status in answer.Value)
                {
                    Console.Error.WriteLine("        " + status.Key.ToString() + ": " + status.Value.count.ToString() + " (" + (status.Value.time.TotalMilliseconds / (0 < status.Value.count ? status.Value.count : 1)).ToString() + ")");
                    total_time += status.Value.time;
                }
            }
            Console.Error.WriteLine("    TOTAL: " + m_total.ToString() + " (" + total_time.TotalMilliseconds.ToString() + ")");
#endif
        }

        public void stats()
        {
#if _DEBUG
            while (true)
            {
                System.Threading.Thread.Sleep(10000);
                print();
            }
#endif
        }
    }

    public class LicenseManager
    {
        private System.Collections.Generic.List<License> m_licenses = new System.Collections.Generic.List<License>();
        private System.Threading.Mutex m_mutex = new System.Threading.Mutex();
        private System.Collections.Concurrent.ConcurrentBag<int> m_coins;

        public LicenseManager(System.Collections.Concurrent.ConcurrentBag<int> coins)
        {
            m_coins = coins;
        }

        public int? get_license()
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
                return null;
            }
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
                working = (m_licenses.Count < Math.Max(Math.Min(m_coins.Count, 10), 2));
                if (working)
                    m_licenses.Add(null);
            }

            if (!working)
            {
                //System.Threading.Thread.Sleep(1);
                return false;
            }

            License license = null;
            while (license is null)
            {
                int coin;
                if (!m_coins.TryTake(out coin))
                    coin = -1;
                license = client.post_license(coin);
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

    class Program
    {
        static void Main(string[] args)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 65536;

            var stats = new Stats();

            string host = System.Environment.GetEnvironmentVariable("ADDRESS") ?? "127.0.0.1";
            int port = int.Parse(System.Environment.GetEnvironmentVariable("Port") ?? "8000");
            string schema = System.Environment.GetEnvironmentVariable("Schema") ?? "http";

            var big_blocks_mutex = new System.Threading.Mutex();
            var blocks_mutex = new System.Threading.Mutex();
            var treasures_mutex = new System.Threading.Mutex();

            var big_blocks = new System.Collections.Generic.List<Block>();
            var blocks = new System.Collections.Generic.List<Block>();
            var treasures = new System.Collections.Generic.List<Treasure>();
            var coins = new System.Collections.Concurrent.ConcurrentBag<int>();

            var lm = new LicenseManager(coins);

            var max_threads = 50;

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
                        treasures_mutex, treasures,
                        coins
                    );
                }));
                threads[threads.Count - 1].Start();
            }

            /*for (int i = 0; i < 6; ++i)
            {
                var client = new Client(schema, host, port, stats);
                threads.Add(new System.Threading.Thread(() =>
                {
                    client.manage_licenses(lm);
                }));
                threads[threads.Count - 1].Start();
            }

            for (int i = 0; i < 1; ++i)
            {
                var index = i;
                var client = new Client(schema, host, port, stats);
                threads.Add(new System.Threading.Thread(() =>
                {
                    client.explore_big_blocks(big_blocks_mutex, big_blocks, index, 1);
                }));
                threads[threads.Count - 1].Start();
            }

            for (int i = 0; i < 1; ++i)
            {
                var client = new Client(schema, host, port, stats);
                threads.Add(new System.Threading.Thread(() =>
                {
                    client.explore_blocks(big_blocks_mutex, big_blocks, blocks_mutex, blocks);
                }));
                threads[threads.Count - 1].Start();
            }

            for (int i = 0; i < 40; ++i)
            {
                var client = new Client(schema, host, port, stats);
                threads.Add(new System.Threading.Thread(() =>
                {
                    client.cash_treasures(treasures_mutex, treasures, coins);
                }));
                threads[threads.Count - 1].Priority = System.Threading.ThreadPriority.Highest;
                threads[threads.Count - 1].Start();
            }

            for (int i = 0; i < 10; ++i)
            {
                var client = new Client(schema, host, port, stats);
                threads.Add(new System.Threading.Thread(() =>
                {
                    client.dig_blocks(blocks_mutex, blocks, lm, treasures_mutex, treasures);
                }));
                threads[threads.Count - 1].Start();
            }*/

            stats.stats();
        }
    }
}

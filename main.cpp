#include <cstdio>
#include <curl/curl.h>
#include <cstdlib>
#include <string>
#include <algorithm>
#include <chrono>
#include <atomic>
#include "date.h"
#include <mutex>
#include "rapidjson/writer.h"
#include "rapidjson/document.h"
#include <vector>
#include <thread>
#include <iostream>
#include <array>
#include <future>

namespace global
{
	std::mutex coin_mutex;
	int current_coin_id = -1;
	int max_coin_id = -1;
    int cashes = 0;
}

struct CBlock final
{
    int posX;
    int posY;
    int sizeX;
    int sizeY;
    int amount;
    int last_h = 0;

    bool operator < (CBlock const& other) const
    {
        if (other.sizeX * other.sizeY == sizeX * sizeY)
        {
            if (amount == other.amount)
                return (last_h < other.last_h);
            return (amount < other.amount);
        }
        return (other.sizeX * other.sizeY < sizeX * sizeY);
    }

    std::string key()
    {
        return std::to_string(posX) + "x" + std::to_string(posY);
    }
};

struct CLicense final
{
	int id;
	int digAllowed;
	int digUsed;

	int digUsing = 0;

	bool operator < (CLicense const& other) const
	{
        if (digAllowed - digUsing == other.digAllowed - other.digUsing)
            return !(digAllowed < other.digAllowed);
		return !(digAllowed - digUsing < other.digAllowed - other.digUsing);
	}
};

class CClient;

class CLicenseManager final
{
public:
    std::optional<int> get_license(CClient const& client);
    void use_license(int id);
    bool update_licenses(CClient const& client);

private:
	std::vector<std::optional<CLicense>> m_licenses;
	std::mutex m_mutex;
    std::atomic_int m_count = 0;
};

struct CAnswerInfo final
{
    int count = 0;
    std::chrono::steady_clock::duration time = std::chrono::steady_clock::duration::zero();
    std::chrono::steady_clock::duration min_time = std::chrono::steady_clock::duration::max();
    std::chrono::steady_clock::duration max_time = std::chrono::steady_clock::duration::min();
    int costs = 0;
};

class CStats final
{
public:
    static std::chrono::steady_clock::time_point now()
    {
        return std::chrono::steady_clock::now();
    }

    static std::chrono::system_clock::time_point sysnow()
    {
        return std::chrono::system_clock::now();
    }

    static std::string prefix()
    {
        return "[" + date::format("%d-%m-%Y %H:%M:%S", sysnow()) + "] ";
    }

    CStats(
        std::mutex & big_blocks_mutex, std::vector<CBlock> & big_blocks,
        std::mutex & blocks_mutex, std::vector<CBlock> & blocks,
        CLicenseManager & lm
    ) :
        m_big_blocks_mutex(big_blocks_mutex), m_blocks_mutex(blocks_mutex),
        m_big_blocks(big_blocks), m_blocks(blocks), m_lm(lm)
    {
        std::cout << prefix() << "Start" << std::endl;
    }

    std::chrono::steady_clock::time_point start(int64_t cost, bool blocked, CClient const& client)
    {
        /*if (blocked)
        {
            while (true)
            {
                {
                    auto lock = std::unique_lock{ m_mutex };
                    if (cost <= m_free_costs)
                    {
                        m_free_costs -= cost;
                        break;
                    }
                }
                //if (!m_lm.update_licenses(client))
                    std::this_thread::sleep_for(std::chrono::microseconds(1));
            }
        }
        else
        {*/
            auto lock = std::unique_lock{ m_mutex };
            m_free_costs -= cost;
        //}
        return CStats::now();
    }

    void answer(int64_t cost, std::string method, long code, std::chrono::steady_clock::duration duration)
    {
        ++m_total;
        if (code != 0 && !(method == "/cash" && code == 503))
        {
            m_sum_costs += cost;
        }
        else
        {
            auto lock = std::unique_lock{ m_mutex };
            m_free_costs += cost;
            if (std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::seconds(1)).count() < m_free_costs)
                m_free_costs = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::seconds(1)).count();
        }

        auto lock = std::unique_lock{ m_mutex };

        ++m_answers[method][code].count;
        m_answers[method][code].time += duration;
        if (duration < m_answers[method][code].min_time)
            m_answers[method][code].min_time = duration;
        if (m_answers[method][code].max_time < duration)
            m_answers[method][code].max_time = duration;
        if (code != 0 && !(method == "/cash" && code == 503))
            m_answers[method][code].costs += cost;
    }

    void print()
    {
        int big_blocks = 0;
        {
            auto lock = std::unique_lock{ m_big_blocks_mutex };
            big_blocks = m_big_blocks.size();
        }
        int blocks = 0;
        {
            auto lock = std::unique_lock{ m_blocks_mutex };
            blocks = m_blocks.size();
        }
        int max_coins = 0;
        int used_coins = 0;
        int coins = 0;
        int cashes = 0;
        {
            auto lock = std::unique_lock{ global::coin_mutex };
            max_coins = global::max_coin_id;
            used_coins = global::current_coin_id;
            coins = global::max_coin_id - global::current_coin_id;
            cashes = global::cashes;
        }

        std::cout << prefix() << "Stats:" << std::endl;
        auto total_time = std::chrono::steady_clock::duration::zero();
        auto license_wait = std::chrono::steady_clock::duration::zero();
        {
            auto lock = std::unique_lock{ m_mutex };
            for (auto const& answer : m_answers)
            {
				std::cout << "    " << answer.first << std::endl;
                for (auto const& status : answer.second)
                {
					std::cout << "        " << status.first << ": " << status.second.count << " (" << std::chrono::duration_cast<std::chrono::milliseconds>(status.second.min_time).count() << " ... " << (std::chrono::duration_cast<std::chrono::milliseconds>(status.second.time).count() / (0 < status.second.count ? status.second.count : 1)) << " ... " << std::chrono::duration_cast<std::chrono::milliseconds>(status.second.max_time).count() << ") / " << status.second.costs << " (" << (status.second.costs / (double)std::max(static_cast<int>(m_sum_costs), 1)) << ")" << std::endl;
					total_time += status.second.time;
                }
            }
            license_wait = m_license_wait;
        }
        std::cout << "    TOTAL: " << m_total << " (" << std::chrono::duration_cast<std::chrono::milliseconds>(total_time).count() << ")" << std::endl;
        std::cout << "    COSTS: " << free_costs() << " (" << m_sum_costs << ")" << std::endl;
        std::cout << "    COINS: " << coins << " (" << max_coins << " - " << used_coins << ")" << std::endl;
        std::cout << "    BLOCKS: " << blocks << std::endl;
        std::cout << "    BIG BLOCKS: " << big_blocks << std::endl;
        std::cout << "    CASHES: " << cashes << std::endl;
        std::cout << "    LICENSE WAIT: " << std::chrono::duration_cast<std::chrono::milliseconds>(license_wait).count() << std::endl;
    }

    void stats(CClient const& client)
    {
        auto prev = CStats::now();
        while (true)
        {
            const auto now = CStats::now();

            if (m_free_costs_last_update < now)
            {
                auto lock = std::unique_lock{ m_mutex };
                m_free_costs += std::chrono::duration_cast<std::chrono::microseconds>(now - m_free_costs_last_update).count();
                if (std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::seconds(1)).count() < m_free_costs)
                    m_free_costs = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::seconds(1)).count();
                m_free_costs_last_update = now;
            }

            if (prev + std::chrono::seconds(10) <= now)
            {
                print();
                prev = CStats::now();
            }

            std::this_thread::sleep_for(std::chrono::microseconds(1));
        }
    }

    void license_wait(std::chrono::steady_clock::duration duration)
    {
        auto lock = std::unique_lock{ m_mutex };
        m_license_wait += duration;
    }

    int free_costs()
    {
        auto lock = std::unique_lock{ m_mutex };
        return m_free_costs;
    }

private:
    std::atomic_int m_total = 0;
    std::mutex m_mutex;

    std::unordered_map<std::string, std::unordered_map<long, CAnswerInfo>> m_answers;

    std::mutex & m_big_blocks_mutex;
    std::mutex & m_blocks_mutex;

    std::vector<CBlock> & m_big_blocks;
    std::vector<CBlock> & m_blocks;

    CLicenseManager & m_lm;

    std::atomic_int64_t m_sum_costs = 0;
    int64_t m_free_costs = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::seconds(1)).count();
    std::chrono::steady_clock::time_point m_free_costs_last_update = CStats::now();

    std::chrono::steady_clock::duration m_license_wait = std::chrono::steady_clock::duration::zero();
};

class CClient final
{
public:
    CClient(CURLSH * curl_share, std::string schema, std::string host, std::string port, CStats & stats)
        : m_curl_share(curl_share), m_base(schema + "://" + host + ":" + port), m_stats(stats)
    {
    }

    CClient(CClient const& other)
        : m_curl_share(other.m_curl_share), m_base(other.m_base), m_stats(other.m_stats)
    {
    }

    std::optional<CBlock> post_explore(int posX, int posY, int sizeX, int sizeY) const
    {
        const int64_t explore_cost = [&] () {
            const auto blocks = sizeX * sizeY;
            if (blocks < 4) return 500;
            if (blocks < 8) return 1000;
            if (blocks < 16) return 1500;
            if (blocks < 32) return 2000;
            if (blocks < 64) return 2500;
            if (blocks < 128) return 3000;
            if (blocks < 256) return 3500;
            if (blocks < 512) return 4000;
            if (blocks < 1024) return 4500;
            return 5000;
        }();
        const auto start_time = m_stats.start(explore_cost, true, *this);

        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);

        writer.StartObject();
        writer.Key("posX"); writer.Int(posX);
        writer.Key("posY"); writer.Int(posY);
        writer.Key("sizeX"); writer.Int(sizeX);
        writer.Key("sizeY"); writer.Int(sizeY);
        writer.EndObject();

        std::unique_ptr<curl_slist, decltype(curl_slist_free_all)*> list{ nullptr, curl_slist_free_all };
        list.reset(curl_slist_append(list.release(), "Content-Type:application/json"));
        list.reset(curl_slist_append(list.release(), "Expect:"));

        std::unique_ptr<CURL, decltype(curl_easy_cleanup)*> curl{ curl_easy_init(), curl_easy_cleanup };
        curl_easy_setopt(curl.get(), CURLOPT_SHARE, m_curl_share);

        const auto url = m_base + "/explore";

        curl_easy_setopt(curl.get(), CURLOPT_URL, url.c_str());

        curl_easy_setopt(curl.get(), CURLOPT_HTTPHEADER, list.get());

        curl_easy_setopt(curl.get(), CURLOPT_MAXCONNECTS, 65535);
        curl_easy_setopt(curl.get(), CURLOPT_POST, 1L);
        curl_easy_setopt(curl.get(), CURLOPT_POSTFIELDS, buffer.GetString());
        curl_easy_setopt(curl.get(), CURLOPT_POSTFIELDSIZE, buffer.GetSize());

        std::string out_buffer;

        curl_easy_setopt(curl.get(), CURLOPT_WRITEDATA, static_cast<void*>(&out_buffer));
        curl_easy_setopt(curl.get(), CURLOPT_WRITEFUNCTION, static_cast<curl_write_callback>([] (char * buffer, size_t size, size_t nitems, void * outstream) -> size_t {
            static_cast<std::string*>(outstream)->append(buffer, buffer + (size * nitems));
            return size * nitems;
        }));

        const auto curl_code = curl_easy_perform(curl.get());

        if (curl_code != CURLE_OK)
        {
            m_stats.answer(explore_cost, "/explore (" + std::to_string(sizeX * sizeY) + ")", 0, CStats::now() - start_time);
            return std::nullopt;
        }

        long code = 0;
        curl_easy_getinfo(curl.get(), CURLINFO_HTTP_CODE, &code);

        m_stats.answer(explore_cost, "/explore (" + std::to_string(sizeX * sizeY) + ")", code, CStats::now() - start_time);

        if (code != 200)
            return std::nullopt;

        rapidjson::Document document;
        document.Parse(out_buffer.c_str(), out_buffer.length());

        return CBlock {
            document["area"]["posX"].GetInt(),
            document["area"]["posY"].GetInt(),
            document["area"]["sizeX"].GetInt(),
            document["area"]["sizeY"].GetInt(),
            document["amount"].GetInt()
        };
    }

    std::optional<CLicense> post_license(std::vector<int> coins) const
    {
        constexpr int64_t license_cost = 0;
        const auto start_time = m_stats.start(license_cost, false, *this);

        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);

        writer.StartArray();
        for (const auto coin : coins)
            writer.Int(coin);
        writer.EndArray();

        std::unique_ptr<curl_slist, decltype(curl_slist_free_all)*> list{ nullptr, curl_slist_free_all };
        list.reset(curl_slist_append(list.release(), "Content-Type:application/json"));
        list.reset(curl_slist_append(list.release(), "Expect:"));

        std::unique_ptr<CURL, decltype(curl_easy_cleanup)*> curl{ curl_easy_init(), curl_easy_cleanup };
        curl_easy_setopt(curl.get(), CURLOPT_SHARE, m_curl_share);

        const auto url = m_base + "/licenses";

        curl_easy_setopt(curl.get(), CURLOPT_URL, url.c_str());

        curl_easy_setopt(curl.get(), CURLOPT_HTTPHEADER, list.get());

        curl_easy_setopt(curl.get(), CURLOPT_MAXCONNECTS, 65535);
        curl_easy_setopt(curl.get(), CURLOPT_POST, 1L);
        curl_easy_setopt(curl.get(), CURLOPT_POSTFIELDS, buffer.GetString());
        curl_easy_setopt(curl.get(), CURLOPT_POSTFIELDSIZE, buffer.GetSize());

        std::string out_buffer;

        curl_easy_setopt(curl.get(), CURLOPT_WRITEDATA, static_cast<void*>(&out_buffer));
        curl_easy_setopt(curl.get(), CURLOPT_WRITEFUNCTION, static_cast<curl_write_callback>([] (char * buffer, size_t size, size_t nitems, void * outstream) -> size_t {
            static_cast<std::string*>(outstream)->append(buffer, buffer + (size * nitems));
            return size * nitems;
        }));

        if (coins.size() == 0)
            curl_easy_setopt(curl.get(), CURLOPT_TIMEOUT_MS, 150L);

        const auto curl_code = curl_easy_perform(curl.get());

        if (coins.size() == 0 && curl_code == CURLE_OPERATION_TIMEDOUT)
        {
            m_stats.answer(license_cost, "/licenses (" + std::to_string(coins.size()) + ")", 201, CStats::now() - start_time);
            return std::nullopt;
        }

        if (curl_code != CURLE_OK)
        {
            m_stats.answer(license_cost, "/licenses (" + std::to_string(coins.size()) + ")", 0, CStats::now() - start_time);
            return std::nullopt;
        }

        long code = 0;
        curl_easy_getinfo(curl.get(), CURLINFO_HTTP_CODE, &code);

        m_stats.answer(license_cost, "/licenses (" + std::to_string(coins.size()) + ")", code, CStats::now() - start_time);

        if (code != 200)
            return std::nullopt;

        rapidjson::Document document;
        document.Parse(out_buffer.c_str(), out_buffer.length());

        return CLicense {
            document["id"].GetInt(),
            document["digAllowed"].GetInt(),
            document["digUsed"].GetInt()
        };
    }

    std::optional<std::vector<std::string>> post_dig(int licenseID, int posX, int posY, int depth) const
    {
        const int64_t dig_cost = [&] () {
            return 1000 + 100 * (depth - 1);
        }();
        const auto start_time = m_stats.start(dig_cost, true, *this);

        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);

        writer.StartObject();
        writer.Key("licenseID"); writer.Int(licenseID);
        writer.Key("posX"); writer.Int(posX);
        writer.Key("posY"); writer.Int(posY);
        writer.Key("depth"); writer.Int(depth);
        writer.EndObject();

        std::unique_ptr<curl_slist, decltype(curl_slist_free_all)*> list{ nullptr, curl_slist_free_all };
        list.reset(curl_slist_append(list.release(), "Content-Type:application/json"));
        list.reset(curl_slist_append(list.release(), "Expect:"));

        std::unique_ptr<CURL, decltype(curl_easy_cleanup)*> curl{ curl_easy_init(), curl_easy_cleanup };
        curl_easy_setopt(curl.get(), CURLOPT_SHARE, m_curl_share);

        const auto url = m_base + "/dig";

        curl_easy_setopt(curl.get(), CURLOPT_URL, url.c_str());

        curl_easy_setopt(curl.get(), CURLOPT_HTTPHEADER, list.get());

        curl_easy_setopt(curl.get(), CURLOPT_MAXCONNECTS, 65535);
        curl_easy_setopt(curl.get(), CURLOPT_POST, 1L);
        curl_easy_setopt(curl.get(), CURLOPT_POSTFIELDS, buffer.GetString());
        curl_easy_setopt(curl.get(), CURLOPT_POSTFIELDSIZE, buffer.GetSize());

        std::string out_buffer;

        curl_easy_setopt(curl.get(), CURLOPT_WRITEDATA, static_cast<void*>(&out_buffer));
        curl_easy_setopt(curl.get(), CURLOPT_WRITEFUNCTION, static_cast<curl_write_callback>([] (char * buffer, size_t size, size_t nitems, void * outstream) -> size_t {
            static_cast<std::string*>(outstream)->append(buffer, buffer + (size * nitems));
            return size * nitems;
        }));

        const auto curl_code = curl_easy_perform(curl.get());

        if (curl_code != CURLE_OK)
        {
            m_stats.answer(dig_cost, "/dig", 0, CStats::now() - start_time);
            return std::nullopt;
        }

        long code = 0;
        curl_easy_getinfo(curl.get(), CURLINFO_HTTP_CODE, &code);

        m_stats.answer(dig_cost, "/dig", code, CStats::now() - start_time);

        if (code == 404)
            return std::vector<std::string>{};

        if (code == 403)
            return std::vector<std::string>{ "i_need_license!!!" };

        if (code != 200)
            return std::nullopt;

        rapidjson::Document document;
        document.Parse(out_buffer.c_str(), out_buffer.length());

        auto length = document.Size();
        auto treasures = std::vector<std::string>{};
        treasures.reserve(length);

        for (int i = 0; i < length; ++i)
            treasures.push_back(std::string{ document[i].GetString(), document[i].GetStringLength() });

        return treasures;
    }

    std::optional<std::vector<int>> post_cash(std::string treasure) const
    {
        constexpr int64_t cash_cost = 10000;
        const auto start_time = m_stats.start(cash_cost, false, *this);

        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);

        writer.String(treasure.c_str(), treasure.length());

        std::unique_ptr<curl_slist, decltype(curl_slist_free_all)*> list{ nullptr, curl_slist_free_all };
        list.reset(curl_slist_append(list.release(), "Content-Type:application/json"));
        list.reset(curl_slist_append(list.release(), "Expect:"));

        std::unique_ptr<CURL, decltype(curl_easy_cleanup)*> curl{ curl_easy_init(), curl_easy_cleanup };
        curl_easy_setopt(curl.get(), CURLOPT_SHARE, m_curl_share);

        const auto url = m_base + "/cash";

        curl_easy_setopt(curl.get(), CURLOPT_URL, url.c_str());

        curl_easy_setopt(curl.get(), CURLOPT_HTTPHEADER, list.get());

        curl_easy_setopt(curl.get(), CURLOPT_MAXCONNECTS, 65535);
        curl_easy_setopt(curl.get(), CURLOPT_POST, 1L);
        curl_easy_setopt(curl.get(), CURLOPT_POSTFIELDS, buffer.GetString());
        curl_easy_setopt(curl.get(), CURLOPT_POSTFIELDSIZE, buffer.GetSize());

        std::string out_buffer;

        curl_easy_setopt(curl.get(), CURLOPT_WRITEDATA, static_cast<void*>(&out_buffer));
        curl_easy_setopt(curl.get(), CURLOPT_WRITEFUNCTION, static_cast<curl_write_callback>([] (char * buffer, size_t size, size_t nitems, void * outstream) -> size_t {
            static_cast<std::string*>(outstream)->append(buffer, buffer + (size * nitems));
            return size * nitems;
        }));

        const auto curl_code = curl_easy_perform(curl.get());

        if (curl_code != CURLE_OK)
        {
            m_stats.answer(cash_cost, "/cash", 0, CStats::now() - start_time);
            return std::nullopt;
        }

        long code = 0;
        curl_easy_getinfo(curl.get(), CURLINFO_HTTP_CODE, &code);

        m_stats.answer(cash_cost, "/cash", code, CStats::now() - start_time);

        if (code != 200)
            return std::nullopt;

        rapidjson::Document document;
        document.Parse(out_buffer.c_str(), out_buffer.length());

        auto length = document.Size();
        auto money = std::vector<int>{};
        money.reserve(length);

        for (int i = 0; i < length; ++i)
            money.push_back(document[i].GetInt());

        return money;
    }

    void work(
        int index, int count,
        std::mutex & big_blocks_mutex, std::vector<CBlock> & big_blocks,
        std::mutex & blocks_mutex, std::vector<CBlock> & blocks,
        CLicenseManager & lm
    ) const
    {
        int current_big_block_x = 0;
        int current_big_block_y = index;

        std::vector<std::future<void>> futures;
        futures.reserve(1000);

        while (true)
        {
            for (size_t i = 0; i < futures.size(); ++i)
                if (futures[i].wait_for(std::chrono::milliseconds(0)) == std::future_status::ready)
                {
                    futures.erase(futures.begin() + i);
                    --i;
                }

            /*if (2000 < m_stats.in_process_costs())
            {
                if (!lm.update_licenses(*this))
                    std::this_thread::sleep_for(std::chrono::microseconds(100));
                continue;
            }*/

            /*if (m_stats.free_costs() <= 0)
            {
                if (!lm.update_licenses(*this))
                    std::this_thread::sleep_for(std::chrono::microseconds(50));
                continue;
            }*/

            // Digs

            bool found_block = false;
            {
                std::optional<CBlock> block;
                {
                    auto lock = std::unique_lock{ blocks_mutex };
                    if (0 < blocks.size())
                    {
                        block = blocks.back();
                        blocks.pop_back();
                    }
                }
                if (block.has_value())
                {
                    bool no_license = false;
                    auto max_h = 10;
                    for (int h = block->last_h; h < max_h && 0 < block->amount; ++h)
                    {
                        block->last_h = h;
                        const auto start_license_wait = CStats::now();
                        const auto license_id = lm.get_license(*this);
                        m_stats.license_wait(CStats::now() - start_license_wait);
                        if (!license_id.has_value())
                        {
                            no_license = true;
                            {
                                auto lock = std::unique_lock{ blocks_mutex };
                                blocks.push_back(block.value());
                                std::sort(blocks.begin(), blocks.end());
                            }
                            break;
                        }
                        std::optional<std::vector<std::string>> surprise;
                        while (!surprise.has_value())
                            surprise = post_dig(license_id.value(), block->posX, block->posY, h + 1);
                        lm.use_license(license_id.value());
                        if (surprise->size() == 1 && surprise->front() == "i_need_license!!!")
                        {
                            auto lock = std::unique_lock{ blocks_mutex };
                            blocks.push_back(block.value());
                            std::sort(blocks.begin(), blocks.end());
                            break;
                        }

                        if (0 < surprise->size())
                        {
                            if ([&] () {
                                return (2 <= h);
                                /*
                                int cashes = 0;
                                {
                                    auto lock = std::unique_lock{ global::coin_mutex };
                                    cashes = global::cashes;
                                }
                                if (cashes <= 0)
                                    return true;
                                if (cashes <= 1)
                                    return (1 <= h);
                                return (2 <= h);
                                */
                            }())
                            {
                                for (auto const& treasure : surprise.value())
                                {
                                    futures.push_back(std::async(std::launch::async, [m_client = *this, treasure] () mutable {
                                        {
                                            auto lock = std::unique_lock{ global::coin_mutex };
                                            ++global::cashes;
                                        }
                                        std::optional<std::vector<int>> money;
                                        while (!money.has_value())
                                            money = m_client.post_cash(treasure);
                                        int max_m = -1;
                                        for (auto m : money.value())
                                            if (max_m < m)
                                                max_m = m;
                                        {
                                            auto lock = std::unique_lock{ global::coin_mutex };
                                            if (global::max_coin_id < max_m)
                                                global::max_coin_id = max_m;
                                            --global::cashes;
                                        }
                                    }));
                                }
                            }
                            block->amount -= surprise->size();
                        }
                        if (h + 1 < max_h && 0 < block->amount)
                        {
                            block->last_h = h + 1;
                            {
                                auto lock = std::unique_lock{ blocks_mutex };
                                blocks.push_back(block.value());
                                std::sort(blocks.begin(), blocks.end());
                            }
                            break;
                        }
                    }
                    if (!no_license)
                        found_block = true;
                }
            }
            if (found_block)
                continue;

            // Small blocks

            bool found_big_block = false;
            {
                std::optional<CBlock> big_block;
                {
                    auto lock = std::unique_lock{ big_blocks_mutex };
                    if (0 < big_blocks.size())
                    {
                        big_block = big_blocks.back();
                        big_blocks.pop_back();
                    }
                }
                if (big_block.has_value())
                {
                    int left_size = big_block->sizeX / 2;
                    if (31 <= left_size)
                        left_size = 31;
                    else if (3 <= left_size)
                        left_size = 3;
                    int right_size = big_block->sizeX - left_size;

                    std::optional<CBlock> left_block;
                    while (!left_block.has_value())
                        left_block = post_explore(big_block->posX, big_block->posY, left_size, 1);

                    const auto right_block = CBlock{ left_block->posX + left_block->sizeX, big_block->posY, right_size, 1, big_block->amount - left_block->amount };

                    if (0 < left_block->amount)
                    {
                        if (left_block->sizeX == 1)
                        {
                            auto lock = std::unique_lock{ blocks_mutex };
                            blocks.push_back(left_block.value());
                            std::sort(blocks.begin(), blocks.end());
                        }
                        else
                        {
                            auto lock = std::unique_lock{ big_blocks_mutex };
                            big_blocks.push_back(left_block.value());
                            std::sort(big_blocks.begin(), big_blocks.end());
                        }
                    }

                    if (0 < right_block.amount)
                    {
                        if (right_block.sizeX == 1)
                        {
                            auto lock = std::unique_lock{ blocks_mutex };
                            blocks.push_back(right_block);
                            std::sort(blocks.begin(), blocks.end());
                        }
                        else
                        {
                            auto lock = std::unique_lock{ big_blocks_mutex };
                            big_blocks.push_back(right_block);
                            std::sort(big_blocks.begin(), big_blocks.end());
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
                constexpr auto block_size = 3500;
                std::optional<CBlock> block;
                while (!block.has_value())
                    block = post_explore(current_big_block_x, current_big_block_y, block_size, 1);
                if (0 < block->amount)
                {
                    auto lock = std::unique_lock{ big_blocks_mutex };
                    big_blocks.push_back(block.value());
                    std::sort(big_blocks.begin(), big_blocks.end());
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

private:
    CURLSH * const m_curl_share;
    const std::string m_base;
    CStats & m_stats;
};

std::optional<int> CLicenseManager::get_license(CClient const& client)
{
    {
        auto lock = std::unique_lock{ m_mutex };
        if (0 < m_licenses.size())
        {
            std::sort(m_licenses.begin(), m_licenses.end());
            for (int i = m_licenses.size() - 1; 0 <= i; --i)
                if (m_licenses[i].has_value() && m_licenses[i]->digUsing < m_licenses[i]->digAllowed)
                {
                    ++m_licenses[i]->digUsing;
                    return m_licenses[i]->id;
                }
        }
    }
    return std::nullopt;
}

void CLicenseManager::use_license(int id)
{
    auto lock = std::unique_lock{ m_mutex };
    for (size_t i = 0; i < m_licenses.size(); ++i)
        if (m_licenses[i].has_value() && m_licenses[i]->id == id)
        {
            if (m_licenses[i]->digAllowed <= (++m_licenses[i]->digUsed))
                m_licenses.erase(m_licenses.begin() + i);
            break;
        }
}

bool CLicenseManager::update_licenses(CClient const& client)
{
    /*bool working = false;

    {
        auto lock = std::unique_lock{ m_mutex };
        working = (m_licenses.size() < 10);
        if (working)
            m_licenses.push_back(std::nullopt);
    }

    if (!working)
        return false;*/

    const auto use_free = true;
	//const auto use_free = (m_count++ % 10 < 4);

    std::optional<CLicense> license;
    while (!license.has_value())
    {
        std::vector<int> coins;
        if (!use_free)
        {
            auto lock = std::unique_lock{ global::coin_mutex };
            if (global::current_coin_id < global::max_coin_id)
                coins.push_back(++global::current_coin_id);
        }
        license = client.post_license(std::move(coins));
        //if (!license.has_value())
        //    use_free = false;

        if (!license.has_value())
            return true;
    }

    {
        auto lock = std::unique_lock{ m_mutex };
        /*for (int i = 0; i < m_licenses.size(); ++i)
            if (!m_licenses[i].has_value())
            {
                m_licenses.erase(m_licenses.begin() + i);
                break;
            }*/
        m_licenses.push_back(license);
    }

    return true;
}

int main()
{
    curl_global_init(CURL_GLOBAL_ALL);

    std::array<std::mutex, curl_lock_data::CURL_LOCK_DATA_LAST> share_mutexes;

    auto * curl_share = curl_share_init();
    curl_share_setopt(curl_share, CURLSHOPT_USERDATA, static_cast<void*>(&share_mutexes));
    curl_share_setopt(curl_share, CURLSHOPT_LOCKFUNC, static_cast<curl_lock_function>([] (CURL * handle, curl_lock_data data, curl_lock_access locktype, void * userptr) {
        auto * mutexes = static_cast<decltype(share_mutexes)*>(userptr);
        mutexes->at(data).lock();
    }));
    curl_share_setopt(curl_share, CURLSHOPT_UNLOCKFUNC, static_cast<curl_unlock_function>([] (CURL * handle, curl_lock_data data, void * userptr) {
        auto * mutexes = static_cast<decltype(share_mutexes)*>(userptr);
        mutexes->at(data).unlock();
    }));
    curl_share_setopt(curl_share, CURLSHOPT_SHARE, curl_lock_data::CURL_LOCK_DATA_CONNECT);
    curl_share_setopt(curl_share, CURLSHOPT_SHARE, curl_lock_data::CURL_LOCK_DATA_COOKIE);
    curl_share_setopt(curl_share, CURLSHOPT_SHARE, curl_lock_data::CURL_LOCK_DATA_DNS);
    curl_share_setopt(curl_share, CURLSHOPT_SHARE, curl_lock_data::CURL_LOCK_DATA_PSL);
    curl_share_setopt(curl_share, CURLSHOPT_SHARE, curl_lock_data::CURL_LOCK_DATA_SSL_SESSION);

    auto host = std::getenv("ADDRESS") ? std::string{ std::getenv("ADDRESS") } : std::string{ "127.0.0.1" };
    auto port = std::getenv("Port") ? std::string{ std::getenv("Port") } : std::string{ "8000" };
    auto schema = std::getenv("Schema") ? std::string{ std::getenv("Schema") } : std::string{ "http" };
    
    std::mutex big_blocks_mutex;
    std::mutex blocks_mutex;

    std::vector<CBlock> big_blocks;
    std::vector<CBlock> blocks;

    CLicenseManager lm;

    auto stats = CStats{
        big_blocks_mutex, big_blocks,
        blocks_mutex, blocks,
        lm
    };

    constexpr auto max_threads = 20;
    constexpr auto license_threads = 10;

    std::vector<std::thread> threads;
    threads.reserve(max_threads + license_threads);

    for (int i = 0; i < license_threads; ++i)
    {
        threads.push_back(std::thread([&] () {
            auto client = CClient{ curl_share, schema, host, port, stats };
            while (true)
                if (!lm.update_licenses(client))
                    std::this_thread::sleep_for(std::chrono::microseconds(1));
        }));
    }

    for (int i = 0; i < max_threads; ++i)
    {
		threads.push_back(std::thread([&, m_index = i] () {
			auto client = CClient{ curl_share, schema, host, port, stats };
			client.work(
                m_index, max_threads,
				big_blocks_mutex, big_blocks,
				blocks_mutex, blocks,
				lm
			);
		}));
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }

    {
		auto client = CClient{ curl_share, schema, host, port, stats };
        stats.stats(client);
    }

    return 0;
}
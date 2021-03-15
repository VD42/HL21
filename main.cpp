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

#define _STATS

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

namespace hardcode
{
	void AddPredefinedBlocks(std::mutex & blocks_mutex, std::vector<CBlock> & blocks)
	{
	}

	bool IsPredefinedBlock(CBlock const& block)
	{
		return false;
	}
}

struct CLicense final
{
	int id;
	int digAllowed;
	int digUsed;

	int digUsing = 0;

	bool operator < (CLicense const& other) const
	{
		return !(digAllowed - digUsing < other.digAllowed - other.digUsing);
	}
};

struct CTreasure final
{
	std::string id;
	int depth;

	bool operator < (CTreasure const& other) const
	{
		return depth < other.depth;
	}
};

class CClient;

class CLicenseManager final
{
public:
    std::optional<int> get_license(CClient & client);
    void use_license(int id);
    bool update_licenses(CClient & client);

private:
	std::vector<std::optional<CLicense>> m_licenses;
	std::mutex m_mutex;
};

struct CAnswerInfo final
{
    int count = 0;
    std::chrono::high_resolution_clock::duration time = std::chrono::high_resolution_clock::duration::zero();
    std::chrono::high_resolution_clock::duration min_time = std::chrono::high_resolution_clock::duration::max();
    std::chrono::high_resolution_clock::duration max_time = std::chrono::high_resolution_clock::duration::min();
};

class CStats final
{
public:
    static std::chrono::high_resolution_clock::time_point now()
    {
        return std::chrono::high_resolution_clock::now();
    }

    static std::string prefix()
    {
        return "[" + date::format("%d-%m-%Y %H:%M:%S", date::floor<std::chrono::microseconds>(now())) + "] ";
    }

    CStats(
        std::mutex & big_blocks_mutex, std::vector<CBlock> & big_blocks,
        std::mutex & blocks_mutex, std::vector<CBlock> & blocks,
        CLicenseManager & lm,
        std::mutex & treasures_mutex, std::vector<CTreasure> & treasures
    ) :
        m_big_blocks_mutex(big_blocks_mutex), m_blocks_mutex(blocks_mutex), m_treasures_mutex(treasures_mutex),
        m_big_blocks(big_blocks), m_blocks(blocks), m_treasures(treasures), m_lm(lm)
    {
#ifdef _STATS
        std::cout << prefix() << "Start" << std::endl;
#endif
    }

    void answer(std::string method, long code, std::chrono::high_resolution_clock::duration duration)
    {
#ifdef _STATS
        ++m_total;

        auto lock = std::unique_lock(m_mutex);

        ++m_answers[method][code].count;
        m_answers[method][code].time += duration;
        if (duration < m_answers[method][code].min_time)
            m_answers[method][code].min_time = duration;
        if (m_answers[method][code].max_time < duration)
            m_answers[method][code].max_time = duration;
#endif
    }

    void print()
    {
#ifdef _STATS
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
        int treausures = 0;
        {
            auto lock = std::unique_lock{ m_treasures_mutex };
            treausures = m_treasures.size();
        }
        int coins = 0;
        int cashes = 0;
        {
            auto lock = std::unique_lock{ global::coin_mutex };
            coins = global::max_coin_id - global::current_coin_id;
            cashes = global::cashes;
        }

        std::cout << prefix() << "Stats:" << std::endl;
        auto total_time = std::chrono::high_resolution_clock::duration::zero();
        {
            auto lock = std::unique_lock{ m_mutex };
            for (auto const& answer : m_answers)
            {
				std::cout << "    " << answer.first << std::endl;
                for (auto const& status : answer.second)
                {
					std::cout << "        " << status.first << ": " << status.second.count << " (" << std::chrono::duration_cast<std::chrono::milliseconds>(status.second.min_time).count() << " ... " << (std::chrono::duration_cast<std::chrono::milliseconds>(status.second.time).count() / (0 < status.second.count ? status.second.count : 1)) << " ... " << std::chrono::duration_cast<std::chrono::milliseconds>(status.second.max_time).count() << ")" << std::endl;
					total_time += status.second.time;
                }
            }
        }
        std::cout << "    TOTAL: " << m_total << " (" << std::chrono::duration_cast<std::chrono::milliseconds>(total_time).count() << ")" << std::endl;
        std::cout << "    COINS: " << coins << std::endl;
        std::cout << "    TREASURES: " << treausures << std::endl;
        std::cout << "    BLOCKS: " << blocks << std::endl;
        std::cout << "    BIG BLOCKS: " << big_blocks << std::endl;
        std::cout << "    CASHES: " << cashes << std::endl;
#endif
    }

    void stats(CClient & client)
    {

        while (true)
        {
			std::this_thread::sleep_for(std::chrono::seconds(10));
#ifdef _STATS
            print();
            //Console.Error.WriteLine("Server status: ");
            //Console.Error.WriteLine(client.get_health_check());
#endif
        }
    }

private:
    std::atomic_int m_total = 0;
    std::mutex m_mutex;

    std::unordered_map<std::string, std::unordered_map<long, CAnswerInfo>> m_answers;

    std::mutex & m_big_blocks_mutex;
    std::mutex & m_blocks_mutex;
    std::mutex & m_treasures_mutex;

    std::vector<CBlock> & m_big_blocks;
    std::vector<CBlock> & m_blocks;
    std::vector<CTreasure> & m_treasures;

    CLicenseManager & m_lm;
};

class CClient final
{
public:
    CClient(std::string schema, std::string host, std::string port, CStats & stats)
        : m_stats(stats)
    {
        m_base = schema + "://" + host + ":" + port;
        m_curl = curl_easy_init();
    }

    CClient(CClient const& other)
        : m_stats(other.m_stats)
    {
        m_base = other.m_base;
        m_curl = curl_easy_init();
    }

    ~CClient()
    {
        curl_easy_cleanup(m_curl);
    }

    std::optional<CBlock> post_explore(int posX, int posY, int sizeX, int sizeY)
    {
        const auto start_time = CStats::now();

        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);

        writer.StartObject();
        writer.Key("posX"); writer.Int(posX);
        writer.Key("posY"); writer.Int(posY);
        writer.Key("sizeX"); writer.Int(sizeX);
        writer.Key("sizeY"); writer.Int(sizeY);
        writer.EndObject();

        curl_easy_reset(m_curl);

        const auto url = m_base + "/explore";

        curl_easy_setopt(m_curl, CURLOPT_URL, url.c_str());

        curl_slist * list = nullptr;
        list = curl_slist_append(list, "Content-Type:application/json");

        curl_easy_setopt(m_curl, CURLOPT_HTTPHEADER, list);

        curl_easy_setopt(m_curl, CURLOPT_POST, 1L);
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDS, buffer.GetString());
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDSIZE, buffer.GetSize());

        std::string out_buffer;

        curl_easy_setopt(m_curl, CURLOPT_WRITEDATA, static_cast<void*>(&out_buffer));
        curl_easy_setopt(m_curl, CURLOPT_WRITEFUNCTION, static_cast<curl_write_callback>([] (char * buffer, size_t size, size_t nitems, void * outstream) -> size_t {
            static_cast<std::string*>(outstream)->append(buffer, buffer + (size * nitems));
            return size * nitems;
        }));

        const auto curl_code = curl_easy_perform(m_curl);

        curl_slist_free_all(list);

        if (curl_code != CURLE_OK)
        {
            m_stats.answer("/explore (" + std::to_string(sizeX * sizeY) + ")", 0, CStats::now() - start_time);
            return std::nullopt;
        }

        long code = 0;
        curl_easy_getinfo(m_curl, CURLINFO_HTTP_CODE, &code);

        if (code != 200 || sizeX * sizeY != 1)
            m_stats.answer("/explore (" + std::to_string(sizeX * sizeY) + ")", code, CStats::now() - start_time);

        if (code != 200)
            return std::nullopt;

        rapidjson::Document document;
        document.Parse(out_buffer.c_str(), out_buffer.length());

        auto block = CBlock {
            document["area"]["posX"].GetInt(),
            document["area"]["posY"].GetInt(),
            document["area"]["sizeX"].GetInt(),
            document["area"]["sizeY"].GetInt(),
            document["amount"].GetInt()
        };

        if (sizeX * sizeY == 1)
            m_stats.answer("/explore (" + std::to_string(sizeX * sizeY) + ", " + std::to_string(block.amount) + ")", code, CStats::now() - start_time);

        return block;
    }

    std::optional<CLicense> post_license(std::vector<int> coins)
    {
        const auto start_time = CStats::now();

        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);

        writer.StartArray();
        for (const auto coin : coins)
            writer.Int(coin);
        writer.EndArray();

        curl_easy_reset(m_curl);

        const auto url = m_base + "/licenses";

        curl_easy_setopt(m_curl, CURLOPT_URL, url.c_str());

        curl_slist * list = nullptr;
        list = curl_slist_append(list, "Content-Type:application/json");

        curl_easy_setopt(m_curl, CURLOPT_HTTPHEADER, list);

        curl_easy_setopt(m_curl, CURLOPT_POST, 1L);
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDS, buffer.GetString());
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDSIZE, buffer.GetSize());

        std::string out_buffer;

        curl_easy_setopt(m_curl, CURLOPT_WRITEDATA, static_cast<void*>(&out_buffer));
        curl_easy_setopt(m_curl, CURLOPT_WRITEFUNCTION, static_cast<curl_write_callback>([] (char * buffer, size_t size, size_t nitems, void * outstream) -> size_t {
            static_cast<std::string*>(outstream)->append(buffer, buffer + (size * nitems));
            return size * nitems;
        }));

        curl_easy_setopt(m_curl, CURLOPT_TIMEOUT_MS, 200L);

        const auto curl_code = curl_easy_perform(m_curl);

        curl_slist_free_all(list);

        if (coins.size() == 0 && curl_code == CURLE_OPERATION_TIMEDOUT)
        {
            m_stats.answer("/licenses (" + std::to_string(coins.size()) + ")", 201, CStats::now() - start_time);
            return std::nullopt;
        }

        if (curl_code != CURLE_OK)
        {
            m_stats.answer("/licenses (" + std::to_string(coins.size()) + ")", 0, CStats::now() - start_time);
            return std::nullopt;
        }

        long code = 0;
        curl_easy_getinfo(m_curl, CURLINFO_HTTP_CODE, &code);

        m_stats.answer("/licenses (" + std::to_string(coins.size()) + ")", code, CStats::now() - start_time);

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

    std::optional<std::vector<std::string>> post_dig(int licenseID, int posX, int posY, int depth)
    {
        const auto start_time = CStats::now();

        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);

        writer.StartObject();
        writer.Key("licenseID"); writer.Int(licenseID);
        writer.Key("posX"); writer.Int(posX);
        writer.Key("posY"); writer.Int(posY);
        writer.Key("depth"); writer.Int(depth);
        writer.EndObject();

        curl_easy_reset(m_curl);

        const auto url = m_base + "/dig";

        curl_easy_setopt(m_curl, CURLOPT_URL, url.c_str());

        curl_slist * list = nullptr;
        list = curl_slist_append(list, "Content-Type:application/json");

        curl_easy_setopt(m_curl, CURLOPT_HTTPHEADER, list);

        curl_easy_setopt(m_curl, CURLOPT_POST, 1L);
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDS, buffer.GetString());
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDSIZE, buffer.GetSize());

        std::string out_buffer;

        curl_easy_setopt(m_curl, CURLOPT_WRITEDATA, static_cast<void*>(&out_buffer));
        curl_easy_setopt(m_curl, CURLOPT_WRITEFUNCTION, static_cast<curl_write_callback>([] (char * buffer, size_t size, size_t nitems, void * outstream) -> size_t {
            static_cast<std::string*>(outstream)->append(buffer, buffer + (size * nitems));
            return size * nitems;
        }));

        const auto curl_code = curl_easy_perform(m_curl);

        curl_slist_free_all(list);

        if (curl_code != CURLE_OK)
        {
            m_stats.answer("/dig", 0, CStats::now() - start_time);
            return std::nullopt;
        }

        long code = 0;
        curl_easy_getinfo(m_curl, CURLINFO_HTTP_CODE, &code);

        m_stats.answer("/dig", code, CStats::now() - start_time);

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

        for (int i = 0; i < length; ++i)
            treasures.push_back(std::string{ document[i].GetString(), document[i].GetStringLength() });

        return treasures;
    }

    std::optional<std::vector<int>> post_cash(std::string treasure)
    {
        const auto start_time = CStats::now();

        rapidjson::StringBuffer buffer;
        rapidjson::Writer<rapidjson::StringBuffer> writer(buffer);

        writer.String(treasure.c_str(), treasure.length());

        curl_easy_reset(m_curl);

        const auto url = m_base + "/cash";

        curl_easy_setopt(m_curl, CURLOPT_URL, url.c_str());

        curl_slist * list = nullptr;
        list = curl_slist_append(list, "Content-Type:application/json");

        curl_easy_setopt(m_curl, CURLOPT_HTTPHEADER, list);

        curl_easy_setopt(m_curl, CURLOPT_POST, 1L);
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDS, buffer.GetString());
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDSIZE, buffer.GetSize());

        std::string out_buffer;

        curl_easy_setopt(m_curl, CURLOPT_WRITEDATA, static_cast<void*>(&out_buffer));
        curl_easy_setopt(m_curl, CURLOPT_WRITEFUNCTION, static_cast<curl_write_callback>([] (char * buffer, size_t size, size_t nitems, void * outstream) -> size_t {
            static_cast<std::string*>(outstream)->append(buffer, buffer + (size * nitems));
            return size * nitems;
        }));

        const auto curl_code = curl_easy_perform(m_curl);

        curl_slist_free_all(list);

        if (curl_code != CURLE_OK)
        {
            m_stats.answer("/cash", 0, CStats::now() - start_time);
            return std::nullopt;
        }

        long code = 0;
        curl_easy_getinfo(m_curl, CURLINFO_HTTP_CODE, &code);

        m_stats.answer("/cash", code, CStats::now() - start_time);

        if (code != 200)
            return std::nullopt;

        rapidjson::Document document;
        document.Parse(out_buffer.c_str(), out_buffer.length());

        auto length = document.Size();
        auto money = std::vector<int>{};

        for (int i = 0; i < length; ++i)
            money.push_back(document[i].GetInt());

        return money;
    }

    void work(
        int index, int count,
        std::mutex & big_blocks_mutex, std::vector<CBlock> & big_blocks,
        std::mutex & blocks_mutex, std::vector<CBlock> & blocks,
        CLicenseManager & lm,
        std::mutex & treasures_mutex, std::vector<CTreasure> & treasures
    )
    {
        int current_big_block_x = 0;
        int current_big_block_y = index;

        bool i_ve_enough = false;
        int enough_money = 500;
        int min_exchange_level = 2;

        while (true)
        {
            if (!i_ve_enough)
            {
                auto lock = std::unique_lock{ global::coin_mutex };
                if (enough_money <= global::max_coin_id)
                    i_ve_enough = true;
            }

            // Treasures

            bool found_treasure = false;
            {
                std::optional<CTreasure> treasure;
                {
                    auto lock = std::unique_lock{ treasures_mutex };
                    if (0 < treasures.size())
                    {
                        treasure = treasures.back();
                        treasures.pop_back();
                    }
                }
                if (treasure.has_value())
                {
                    if (i_ve_enough && treasure->depth < min_exchange_level)
                        continue;
					std::thread([m_client = *this, m_treasure = treasure.value()] () mutable {
                        {
                            auto lock = std::unique_lock{ global::coin_mutex };
                            ++global::cashes;
                        }
                        std::optional<std::vector<int>> money;
                        while (!money.has_value())
                            money = m_client.post_cash(m_treasure.id);
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
                    }).detach();
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
                        const auto license_id = lm.get_license(*this);
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

						//if (h == 0 && !hardcode::IsPredefinedBlock(block) && block.amount == 2)
						//	std::cout << "AddPredefinedBlock(blocks, new Block() { posX = " << block.posX << ", posY = " << block.posY << ", sizeX = 1, sizeY = 1, amount = " << block.amount + " });" << std::endl;

                        if (0 < surprise->size())
                        {
                            {
                                auto lock = std::unique_lock{ treasures_mutex };
                                for (auto const& treasure : surprise.value())
                                    treasures.push_back(CTreasure{ treasure, h });
                                std::sort(treasures.begin(), treasures.end());
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
                    int right_size = big_block->sizeX - left_size;

                    std::optional<CBlock> left_block;
                    while (!left_block.has_value())
                        left_block = post_explore(big_block->posX, big_block->posY, left_size, 1);

                    const auto right_block = CBlock{ left_block->posX + left_block->sizeX, big_block->posY, right_size, 1, big_block->amount - left_block->amount };

                    if (0 < left_block->amount)
                    {
                        if (left_block->sizeX == 1)
                        {
                            if (!hardcode::IsPredefinedBlock(left_block.value()))
                            {
                                auto lock = std::unique_lock{ blocks_mutex };
                                blocks.push_back(left_block.value());
                                std::sort(blocks.begin(), blocks.end());
                            }
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
                            if (!hardcode::IsPredefinedBlock(right_block))
                            {
                                auto lock = std::unique_lock{ blocks_mutex };
                                blocks.push_back(right_block);
                                std::sort(blocks.begin(), blocks.end());
                            }
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
                auto block_size = 14;
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
    CURL * m_curl = nullptr;
    std::string m_base;
    CStats & m_stats;
};

std::optional<int> CLicenseManager::get_license(CClient & client)
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
    if (update_licenses(client))
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

bool CLicenseManager::update_licenses(CClient& client)
{
    bool working = false;

    {
        auto lock = std::unique_lock{ m_mutex };
        working = (m_licenses.size() < 10);
        if (working)
            m_licenses.push_back(std::nullopt);
    }

    if (!working)
        return false;

    std::optional<CLicense> license;
    while (!license.has_value())
    {
        std::vector<int> coins;
        {
            auto lock = std::unique_lock{ global::coin_mutex };
            if (global::current_coin_id < global::max_coin_id)
                coins.push_back(++global::current_coin_id);
        }
        license = client.post_license(std::move(coins));
    }

    {
        auto lock = std::unique_lock{ m_mutex };
        for (int i = 0; i < m_licenses.size(); ++i)
            if (!m_licenses[i].has_value())
            {
                m_licenses.erase(m_licenses.begin() + i);
                break;
            }
        m_licenses.push_back(license);
    }

    return true;
}

int main()
{
    curl_global_init(CURL_GLOBAL_ALL);

    auto host = std::getenv("ADDRESS") ? std::string{ std::getenv("ADDRESS") } : std::string{ "127.0.0.1" };
    auto port = std::getenv("Port") ? std::string{ std::getenv("Port") } : std::string{ "8000" };
    auto schema = std::getenv("Schema") ? std::string{ std::getenv("Schema") } : std::string{ "http" };
    
    std::mutex big_blocks_mutex;
    std::mutex blocks_mutex;
    std::mutex treasures_mutex;

    std::vector<CBlock> big_blocks;
    std::vector<CBlock> blocks;
    std::vector<CTreasure> treasures;

    CLicenseManager lm;

    auto stats = CStats{
        big_blocks_mutex, big_blocks,
        blocks_mutex, blocks,
        lm,
        treasures_mutex, treasures
    };

    hardcode::AddPredefinedBlocks(blocks_mutex, blocks);

    auto max_threads = 42;

    auto threads = std::vector<std::thread>{ max_threads };

    for (int i = 0; i < max_threads; ++i)
    {
		threads.push_back(std::thread([&, m_index = i] () {
			auto client = CClient{ schema, host, port, stats };
			client.work(
                m_index, max_threads,
				big_blocks_mutex, big_blocks,
				blocks_mutex, blocks,
				lm,
				treasures_mutex, treasures
			);
		}));
    }

    {
		auto client = CClient{ schema, host, port, stats };
        stats.stats(client);
    }

    return 0;
}
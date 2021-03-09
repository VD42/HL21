using System;
using System.Collections.Generic;
using System.Text;

namespace HL21
{
    public class Hardcode
    {
        public static System.Collections.Concurrent.ConcurrentDictionary<string, Block> predefined_blocks = new System.Collections.Concurrent.ConcurrentDictionary<string, Block>();

        public static void AddPredefinedBlock(Block block)
        {
            predefined_blocks.TryAdd(block.GetKey(), block);
        }

        public static bool IsPredefinedBlock(Block block)
        {
            return predefined_blocks.ContainsKey(block.GetKey());
        }

        public static void AddPredefinedBlocks(System.Threading.Mutex blocks_mutex, System.Collections.Generic.List<Block> blocks)
        {
            AddPredefinedBlock(new Block() { posX = 73, posY = 3446, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 295, posY = 1440, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 327, posY = 3321, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 431, posY = 383, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 710, posY = 2009, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 737, posY = 721, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 738, posY = 132, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 827, posY = 446, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 830, posY = 18, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 917, posY = 1626, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 964, posY = 1167, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1068, posY = 12, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1278, posY = 3447, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1395, posY = 3021, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1449, posY = 3007, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1587, posY = 2773, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1604, posY = 2724, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1608, posY = 2287, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1618, posY = 972, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1661, posY = 2042, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1696, posY = 1026, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1716, posY = 2102, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1719, posY = 629, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1765, posY = 2304, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1767, posY = 3077, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1819, posY = 1163, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 1911, posY = 1564, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 2134, posY = 3076, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 2150, posY = 1582, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 2231, posY = 2849, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 2357, posY = 564, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 2755, posY = 2843, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 2772, posY = 922, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 2995, posY = 3182, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 3005, posY = 3253, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 3088, posY = 861, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 3096, posY = 678, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 3253, posY = 3491, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 3389, posY = 967, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 3407, posY = 2061, sizeX = 1, sizeY = 1, amount = 3 });
            AddPredefinedBlock(new Block() { posX = 3456, posY = 1267, sizeX = 1, sizeY = 1, amount = 3 });

            lock (blocks_mutex)
            {
                foreach (var block in predefined_blocks)
                    blocks.Add(block.Value);
            }
        }
    }
}

using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 大小境界前缀和投影（境界稿 §6）：flatIndex ↔ (大境界 major, 小境界 sub)。
    /// 大境界 = UnifiedTierOf 同 UT 极大连续段；SubLevelCount[major] = 第 major 段长度。
    /// offsets[m] = Σ_{i&lt;m} SubLevelCount[i]（前缀和）。纯整数确定，禁浮点，不进运行期。
    /// INV-REALM-1：Decode∘Encode == id（含佛 plateau 形 1,1,…,2）。
    /// </summary>
    public static class RealmProjection
    {
        /// <summary>(大境界, 小境界) → flatIndex：offsets[major] + sub（前缀和闭式）。</summary>
        public static int Encode(int major, int sub, IReadOnlyList<int> subLevelCount)
        {
            int offset = 0;
            for (int i = 0; i < major; i++)
                offset += subLevelCount[i];
            return offset + sub;
        }

        /// <summary>
        /// flatIndex → (大境界, 小境界)：最大 m 使 offsets[m] ≤ flatIndex；sub = flatIndex - offsets[m]。
        /// </summary>
        public static (int major, int sub) Decode(int flatIndex, IReadOnlyList<int> subLevelCount)
        {
            int offset = 0;
            for (int m = 0; m < subLevelCount.Count; m++)
            {
                int next = offset + subLevelCount[m];
                if (flatIndex < next)
                    return (m, flatIndex - offset);
                offset = next;
            }
            // flatIndex 越界（≥ Σ SubLevelCount）：归末大境界末小境界（防御，调用方应保证 fi 合法）。
            int last = subLevelCount.Count - 1;
            return (last, flatIndex - (offset - subLevelCount[last]));
        }
    }
}

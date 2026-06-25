using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// INV-DERIVED 注册门（CR-2026-06-25 R-2）。CI-blocking gate：Fail = build break。
    ///
    /// 背景：<see cref="DerivedRegistry.Resolve"/> 对未注册 key 静默返回 0（恒 0 伤害且无报错）。
    /// 已实证 footgun：YinguoFazePath 设计稿写 <c>derived:lawBreadth</c>，因空注册恒返 0，
    /// 开发者被迫手动改用 sumArtPower 绕开（见该文件注释）。下个写 <c>derived:新key</c>
    /// 而忘记注册的人会再次静默拿 0。本门把"静默 0"提前到构建期红掉。
    /// </summary>
    public class DerivedRegistryGateTests
    {
        /// <summary>
        /// 21 路战力公式引用的每个 <c>derived:&lt;key&gt;</c> 必须在 DerivedRegistry 已注册。
        /// 未注册 = Resolve 恒回 0 = 该 term 静默失效。
        /// </summary>
        [Fact]
        public void InvDerived_EveryReferencedKey_IsRegistered()
        {
            var reg = new PathRegistry(new CodePathSource());
            var unregistered = new List<string>();

            foreach (var path in reg.All)
            {
                foreach (var term in path.Power.Terms)
                {
                    if (!term.Src.StartsWith("derived:", System.StringComparison.Ordinal))
                        continue;
                    var key = term.Src.Substring("derived:".Length);
                    if (!DerivedRegistry.IsRegistered(key))
                        unregistered.Add($"{path.PathId}: derived:{key} (Src={term.Src})");
                }
            }

            Assert.True(
                unregistered.Count == 0,
                "以下 path 公式引用了未注册的 derived key（DerivedRegistry.Resolve 将静默回 0 → 恒 0 伤害）：\n  "
                    + string.Join("\n  ", unregistered)
                    + "\n修复：在 DerivedProviders.RegisterAll 注册对应 provider，或改用引擎已结算项（如 sumArtPower）。");
        }

        /// <summary>
        /// IsRegistered 真值校验：已注册 key 返 true、不存在 key 返 false（守住门本身的探针不退化）。
        /// </summary>
        [Fact]
        public void IsRegistered_KnownVsUnknown_Correct()
        {
            // 访问 DerivedRegistry 任一成员即触发其静态构造 → DerivedProviders.RegisterAll 注册全部 provider。
            Assert.True(DerivedRegistry.IsRegistered("rosterWeighted"));
            Assert.True(DerivedRegistry.IsRegistered("wenGong"));
            Assert.False(DerivedRegistry.IsRegistered("lawBreadth"));            // 实证 footgun：设计稿曾写、从未注册
            Assert.False(DerivedRegistry.IsRegistered("__definitely_not_a_key"));
        }
    }
}

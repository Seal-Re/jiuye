using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

/// <summary>
/// IL 浮点扫描器：读 PE 元数据，遍历指定命名空间下类型的方法体，线性扫 IL 字节流，
/// 命中浮点 opcode（ldc.r4/r8、conv.r4/r8、conv.r.un）即记录 "Namespace.Type::Method"。
/// 全程不加载/执行被扫程序集，纯静态读取。CorePurity 的 IL 级扩展（spec §0 C5 / S0-b）。
/// </summary>
public static class ILFloatScanner
{
    // 浮点单字节 opcode（ECMA-335 §III）。
    private const byte LdcR4 = 0x22;   // ldc.r4
    private const byte LdcR8 = 0x23;   // ldc.r8
    private const byte ConvR4 = 0x6B;  // conv.r4
    private const byte ConvR8 = 0x6C;  // conv.r8
    private const byte ConvRUn = 0x76; // conv.r.un

    /// <summary>扫 asmPath 程序集中 nsPrefix 命名空间（含子命名空间）的所有方法体；返回 offender 列表。</summary>
    public static List<string> ScanNamespace(string asmPath, string nsPrefix)
    {
        var offenders = new List<string>();
        using var stream = File.OpenRead(asmPath);
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();

        foreach (var typeHandle in md.TypeDefinitions)
        {
            var type = md.GetTypeDefinition(typeHandle);
            string ns = md.GetString(type.Namespace);
            if (!NamespaceMatches(ns, nsPrefix)) continue;
            string typeName = md.GetString(type.Name);

            foreach (var methodHandle in type.GetMethods())
            {
                var method = md.GetMethodDefinition(methodHandle);
                int rva = method.RelativeVirtualAddress;
                if (rva == 0) continue; // 抽象/extern：无 IL 体
                var body = pe.GetMethodBody(rva);
                var il = body.GetILBytes();
                if (il == null) continue;
                if (BodyHasFloat(il))
                    offenders.Add(ns + "." + typeName + "::" + md.GetString(method.Name));
            }
        }
        return offenders;
    }

    /// <summary>
    /// 返回 ScanNamespace 在 asmPath/nsPrefix 下**实际遍历**到的类型全名集（"Namespace.Type"）。
    /// 与 <see cref="ScanNamespace"/> 共享同一 <see cref="NamespaceMatches"/> 谓词与 md.TypeDefinitions 遍历，
    /// 故如实反映扫描覆盖面：覆盖守卫据此断言模块结算 + 唯一档 handler 在内，scanner 一旦窄化即红。
    /// </summary>
    public static HashSet<string> ScannedTypeFullNames(string asmPath, string nsPrefix)
    {
        var names = new HashSet<string>();
        using var stream = File.OpenRead(asmPath);
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();

        foreach (var typeHandle in md.TypeDefinitions)
        {
            var type = md.GetTypeDefinition(typeHandle);
            string ns = md.GetString(type.Namespace);
            if (!NamespaceMatches(ns, nsPrefix)) continue;
            names.Add(ns + "." + md.GetString(type.Name));
        }
        return names;
    }

    private static bool NamespaceMatches(string ns, string prefix)
        => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal);

    /// <summary>线性遍历 IL 字节流，按 opcode 操作数长度对齐推进；命中浮点 opcode 即返回 true。</summary>
    private static bool BodyHasFloat(byte[] il)
    {
        int i = 0;
        while (i < il.Length)
        {
            byte op = il[i++];
            if (op == 0xFE)
            {
                // 双字节前缀 opcode：本扫描关注的浮点 opcode 均为单字节，
                // 0xFE 族操作数除 ldftn/ldvirtftn(4)/unaligned(1) 外多为 token(4)/无操作数。
                if (i >= il.Length) break;
                byte op2 = il[i++];
                i += TwoByteOperandLen(op2);
                continue;
            }

            if (op == LdcR4 || op == LdcR8 || op == ConvR4 || op == ConvR8 || op == ConvRUn)
                return true;

            i += OneByteOperandLen(op, il, i);
        }
        return false;
    }

    // 单字节 opcode 的内联操作数长度（ECMA-335 §III.1.2 操作数编码）。
    private static int OneByteOperandLen(byte op, byte[] il, int operandStart)
    {
        switch (op)
        {
            // ShortInlineVar / ShortInlineI / ShortInlineBrTarget（1 字节）
            case 0x0E: // ldarg.s
            case 0x0F: // ldarga.s
            case 0x10: // starg.s
            case 0x11: // ldloc.s
            case 0x12: // ldloca.s
            case 0x13: // stloc.s
            case 0x1F: // ldc.i4.s
            case 0x2B: case 0x2C: case 0x2D: case 0x2E: case 0x2F: // br.s..brfalse.s/brtrue.s 起
            case 0x30: case 0x31: case 0x32: case 0x33: case 0x34: case 0x35: case 0x36: case 0x37: // ..blt.un.s
            case 0xDE: // leave.s
                return 1;

            // InlineI（4 字节）
            case 0x20: // ldc.i4
                return 4;

            // InlineI8（8 字节）
            case 0x21: // ldc.i8
                return 8;

            // ldc.r4 / ldc.r8 在调用处已拦截，但保持对齐：r4=4, r8=8
            case LdcR4: return 4;
            case LdcR8: return 8;

            // InlineMethod / InlineType / InlineField / InlineString / InlineTok / InlineSig（4 字节 token）
            case 0x27: // jmp
            case 0x28: // call
            case 0x29: // calli
            case 0x6F: // callvirt
            case 0x70: // cpobj
            case 0x71: // ldobj
            case 0x72: // ldstr
            case 0x73: // newobj
            case 0x74: // castclass
            case 0x75: // isinst
            case 0x79: // unbox
            case 0x7B: // ldfld
            case 0x7C: // ldflda
            case 0x7D: // stfld
            case 0x7E: // ldsfld
            case 0x7F: // ldsflda
            case 0x80: // stsfld
            case 0x81: // stobj
            case 0x8C: // box
            case 0x8D: // newarr
            case 0x8F: // ldelema
            case 0xA3: // ldelem
            case 0xA4: // stelem
            case 0xA5: // unbox.any
            case 0xC2: // refanyval
            case 0xC6: // mkrefany
            case 0xD0: // ldtoken
                return 4;

            // InlineBrTarget（4 字节）：br(0x38)..blt.un(0x44)
            case 0x38: case 0x39: case 0x3A: case 0x3B: case 0x3C: case 0x3D: case 0x3E: case 0x3F:
            case 0x40: case 0x41: case 0x42: case 0x43: case 0x44:
                return 4;

            // InlineSwitch：4 字节 count N + N*4 字节跳转表
            case 0x45: // switch
                if (operandStart + 4 > il.Length) return il.Length - operandStart; // 截断保护
                uint n = (uint)(il[operandStart] | (il[operandStart + 1] << 8)
                                | (il[operandStart + 2] << 16) | (il[operandStart + 3] << 24));
                return 4 + (int)(n * 4);

            // ShortInlineVar（1 字节）补：ldarg.s 等已列；其余单字节 opcode 均 InlineNone。
            default:
                return 0;
        }
    }

    // 0xFE 前缀双字节 opcode 操作数长度。
    private static int TwoByteOperandLen(byte op2)
    {
        switch (op2)
        {
            case 0x06: // ldftn
            case 0x07: // ldvirtftn
                return 4; // InlineMethod token
            case 0x09: // ldarg
            case 0x0A: // ldarga
            case 0x0B: // starg
            case 0x0C: // ldloc
            case 0x0D: // ldloca
            case 0x0E: // stloc
                return 2; // InlineVar
            case 0x12: // unaligned.
                return 1; // ShortInlineI
            case 0x15: // initobj
            case 0x16: // constrained.
            case 0x1C: // sizeof
                return 4; // InlineType/Tok token
            default:
                return 0; // ceq/cgt/clt/ldnull 等无操作数
        }
    }
}

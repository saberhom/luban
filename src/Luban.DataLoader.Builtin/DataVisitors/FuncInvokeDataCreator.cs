using System.Numerics;
using Luban.DataLoader.Builtin.FuncInvoke;
using Luban.DataLoader.Builtin.Utils;
using Luban.Datas;
using Luban.Defs;
using Luban.Types;
using Luban.TypeVisitors;
using Luban.Utils;

namespace Luban.DataLoader.Builtin.DataVisitors;

class FuncInvokeDataCreator : ITypeFuncVisitor<FuncInvokeData, DefAssembly, DType>
{
    public static FuncInvokeDataCreator Ins { get; } = new();

    public DType Accept(TBool type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Bool type not supported in FuncInvoke format");
    }

    public DType Accept(TByte type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Byte type not supported in FuncInvoke format");
    }

    public DType Accept(TShort type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Short type not supported in FuncInvoke format");
    }

    public DType Accept(TInt type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Int type not supported in FuncInvoke format");
    }

    public DType Accept(TLong type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Long type not supported in FuncInvoke format");
    }

    public DType Accept(TFloat type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Float type not supported in FuncInvoke format");
    }

    public DType Accept(TDouble type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Double type not supported in FuncInvoke format");
    }

    public DType Accept(TEnum type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Enum type not supported in FuncInvoke format");
    }

    public DType Accept(TString type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("String type not supported in FuncInvoke format");
    }

    public DType Accept(TDateTime type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("DateTime type not supported in FuncInvoke format");
    }

    public DType Accept(TBean type, FuncInvokeData x, DefAssembly ass)
    {
        var bean = type.DefBean;

        DefBean implBean;
        if (bean.IsAbstractType)
        {
            // 从FuncInvokeData中获取类型名
            string subType = x.TypeName;
            if (string.IsNullOrEmpty(subType))
            {
                throw new Exception($"结构:{bean.FullName} 是多态类型，funcInvoke格式必须指定类型名");
            }
            implBean = DataUtil.GetImplTypeByNameOrAlias(bean, subType);
        }
        else
        {
            implBean = bean;
        }

        var fields = new List<DType>();
        foreach (DefField f in implBean.HierarchyFields)
        {
            if (x.Parameters.TryGetValue(f.Name, out string paramValue))
            {
                try
                {
                    // 根据字段类型解析参数值
                    var parsedValue = ParseValueByType(f.CType, paramValue);
                    // 直接创建对应的DType对象，而不是使用Apply方法
                    fields.Add(CreateDType(f.CType, parsedValue));
                }
                catch (DataCreateException dce)
                {
                    dce.Push(implBean, f);
                    throw;
                }
                catch (Exception e)
                {
                    var dce = new DataCreateException(e, "");
                    dce.Push(bean, f);
                    throw dce;
                }
            }
            else if (f.CType.IsNullable)
            {
                fields.Add(null);
            }
            else
            {
                throw new Exception($"Missing parameter '{f.Name}' for type '{x.TypeName}' in funcInvoke format");
            }
        }
        return new DBean(type, implBean, fields);
    }

    public DType Accept(TArray type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Array type not supported in FuncInvoke format");
    }

    public DType Accept(TList type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("List type not supported in FuncInvoke format");
    }

    public DType Accept(TSet type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Set type not supported in FuncInvoke format");
    }

    public DType Accept(TMap type, FuncInvokeData x, DefAssembly ass)
    {
        throw new NotSupportedException("Map type not supported in FuncInvoke format");
    }

    // 根据类型解析字符串值
    private object ParseValueByType(TType type, string value)
    {
        switch (type)
        {
            case TBool:
                return LoadDataUtil.ParseExcelBool(value);
            case TByte:
                if (LoadDataUtil.TryParseExcelByteFromNumberOrConstAlias(value, out byte byteValue))
                    return byteValue;
                throw new Exception($"{value} 不是 byte 类型值");
            case TShort:
                if (LoadDataUtil.TryParseExcelShortFromNumberOrConstAlias(value, out short shortValue))
                    return shortValue;
                throw new Exception($"{value} 不是 short 类型值");
            case TInt:
                if (LoadDataUtil.TryParseExcelIntFromNumberOrConstAlias(value, out int intValue))
                    return intValue;
                throw new Exception($"{value} 不是 int 类型值");
            case TLong:
                if (LoadDataUtil.TryParseExcelLongFromNumberOrConstAlias(value, out long longValue))
                    return longValue;
                throw new Exception($"{value} 不是 long 类型值");
            case TFloat:
                if (LoadDataUtil.TryParseExcelFloatFromNumberOrConstAlias(value, out float floatValue))
                    return floatValue;
                throw new Exception($"{value} 不是 float 类型值");
            case TDouble:
                if (LoadDataUtil.TryParseExcelDoubleFromNumberOrConstAlias(value, out double doubleValue))
                    return doubleValue;
                throw new Exception($"{value} 不是 double 类型值");
            case TString:
                return DataUtil.RemoveStringQuote(value);
            case TDateTime:
                return DataUtil.CreateDateTime(value);
            case TEnum:
                return value;
            default:
                throw new NotSupportedException($"Type {type.GetType().Name} not supported in FuncInvoke format");
        }
    }

    // 根据类型和值创建DType对象
    private DType CreateDType(TType type, object value)
    {
        switch (type)
        {
            case TBool t:
                return DBool.ValueOf((bool)value);
            case TByte t:
                return DByte.ValueOf((byte)value);
            case TShort t:
                return DShort.ValueOf((short)value);
            case TInt t:
                return DInt.ValueOf((int)value);
            case TLong t:
                return DLong.ValueOf((long)value);
            case TFloat t:
                return DFloat.ValueOf((float)value);
            case TDouble t:
                return DDouble.ValueOf((double)value);
            case TString t:
                return DString.ValueOf(t, (string)value);
            case TDateTime t:
                return (DDateTime)value;
            case TEnum t:
                return new DEnum(t, (string)value);
            default:
                throw new NotSupportedException($"Type {type.GetType().Name} not supported in FuncInvoke format");
        }
    }
}

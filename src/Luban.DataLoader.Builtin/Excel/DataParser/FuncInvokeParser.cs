using Luban.DataLoader.Builtin.DataVisitors;
using Luban.DataLoader.Builtin.FuncInvoke;
using Luban.Datas;
using Luban.Defs;
using Luban.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luban.DataLoader.Builtin.Excel.DataParser;

public class FuncInvokeParser : DataParserBase
{
    public override DType ParseAny(TType type, List<Cell> cells, TitleRow title)
    {
        var cell = cells[title.SelfTitle.FromIndex];
        string cellValue = cell?.ToString()?.Trim();
        
        if (string.IsNullOrEmpty(cellValue))
        {
            if (type.IsNullable)
                return null;
            throw new Exception($"Empty cell value for type {type}");
        }

        // 解析funcInvoke格式：Limit_BuildingLevel(type=31,level=1)
        var funcInvokeData = ParseFuncInvokeString(cellValue);
        if (funcInvokeData == null)
        {
            throw new Exception($"Invalid funcInvoke format: {cellValue}");
        }

        return type.Apply(FuncInvokeDataCreator.Ins, funcInvokeData, type.DefBean.Assembly);
    }

    public override DBean ParseBean(TBean type, List<Cell> cells, TitleRow title)
    {
        var cell = cells[title.SelfTitle.FromIndex];
        string cellValue = cell?.ToString()?.Trim();
        
        if (string.IsNullOrEmpty(cellValue))
        {
            if (type.IsNullable)
                return null;
            throw new Exception($"Empty cell value for type {type}");
        }

        // 解析funcInvoke格式
        var funcInvokeData = ParseFuncInvokeString(cellValue);
        if (funcInvokeData == null)
        {
            throw new Exception($"Invalid funcInvoke format: {cellValue}");
        }

        return (DBean)type.Apply(FuncInvokeDataCreator.Ins, funcInvokeData, type.DefBean.Assembly);
    }

    public override List<DType> ParseCollectionElements(TType collectionType, List<Cell> cells, TitleRow title)
    {
        var cell = cells[title.SelfTitle.FromIndex];
        string cellValue = cell?.ToString()?.Trim();
        
        if (string.IsNullOrEmpty(cellValue))
        {
            return new List<DType>();
        }

        // 解析多个funcInvoke格式，用分号分隔
        var elements = new List<DType>();
        var funcInvokeStrings = cellValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var funcInvokeStr in funcInvokeStrings)
        {
            var trimmedStr = funcInvokeStr.Trim();
            if (string.IsNullOrEmpty(trimmedStr))
                continue;

            var funcInvokeData = ParseFuncInvokeString(trimmedStr);
            if (funcInvokeData == null)
            {
                throw new Exception($"Invalid funcInvoke format: {trimmedStr}");
            }

            elements.Add(collectionType.ElementType.Apply(FuncInvokeDataCreator.Ins, funcInvokeData, collectionType.ElementType.DefBean.Assembly));
        }

        return elements;
    }

    public override DMap ParseMap(TMap type, List<Cell> cells, TitleRow title)
    {
        throw new NotSupportedException("Map type not supported in FuncInvokeParser");
    }

    public override KeyValuePair<DType, DType> ParseMapEntry(TMap type, List<Cell> cells, TitleRow title)
    {
        throw new NotSupportedException("Map entry not supported in FuncInvokeParser");
    }

    // 解析funcInvoke格式字符串
    private FuncInvokeData ParseFuncInvokeString(string input)
    {
        input = input.Trim();
        
        // 查找左括号的位置
        int leftParenIndex = input.IndexOf('(');
        int rightParenIndex = input.LastIndexOf(')');
        
        if (leftParenIndex == -1 || rightParenIndex == -1 || rightParenIndex <= leftParenIndex)
        {
            return null;
        }

        // 提取类型名
        string typeName = input.Substring(0, leftParenIndex).Trim();
        
        // 提取参数部分
        string paramsStr = input.Substring(leftParenIndex + 1, rightParenIndex - leftParenIndex - 1).Trim();
        
        var parameters = new Dictionary<string, string>();
        
        if (!string.IsNullOrEmpty(paramsStr))
        {
            // 解析参数：type=31,level=1
            var paramPairs = paramsStr.Split(',');
            foreach (var pair in paramPairs)
            {
                var trimmedPair = pair.Trim();
                if (string.IsNullOrEmpty(trimmedPair))
                    continue;
                    
                int equalIndex = trimmedPair.IndexOf('=');
                if (equalIndex == -1)
                {
                    throw new Exception($"Invalid parameter format in '{trimmedPair}'");
                }
                
                string paramName = trimmedPair.Substring(0, equalIndex).Trim();
                string paramValue = trimmedPair.Substring(equalIndex + 1).Trim();
                
                if (string.IsNullOrEmpty(paramName))
                {
                    throw new Exception($"Empty parameter name in '{trimmedPair}'");
                }
                
                parameters[paramName] = paramValue;
            }
        }
        
        return new FuncInvokeData(typeName, parameters);
    }
}

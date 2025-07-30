using System.Text;
using Luban.DataLoader.Builtin.DataVisitors;
using Luban.Datas;
using Luban.Defs;
using Luban.Types;
using Luban.Utils;

namespace Luban.DataLoader.Builtin.FuncInvoke;

[DataLoader("funcinvoke")]
public class FuncInvokeDataSource : DataLoaderBase
{
    private List<string> _lines = new List<string>();

    public override void Load(string rawUrl, string sheetName, Stream stream)
    {
        RawUrl = rawUrl;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string content = reader.ReadToEnd();
        
        // 按行分割内容
        _lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(line => line.Trim())
                       .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("//"))
                       .ToList();
    }

    public override List<Record> ReadMulti(TBean type)
    {
        var records = new List<Record>();
        
        foreach (string line in _lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                continue;
                
            var record = ReadRecord(line, type);
            if (record != null)
            {
                records.Add(record);
            }
        }
        
        return records;
    }

    public override Record ReadOne(TBean type)
    {
        if (_lines.Count == 0)
            return null;
            
        return ReadRecord(_lines[0], type);
    }

    private Record ReadRecord(string line, TBean type)
    {
        // 解析funcInvoke格式：Limit_BuildingLevel(type=31,level=1)
        var funcInvokeData = ParseFuncInvokeLine(line);
        if (funcInvokeData == null)
            return null;
            
        var data = (DBean)type.Apply(FuncInvokeDataCreator.Ins, funcInvokeData, type.DefBean.Assembly);
        return new Record(data, RawUrl, null);
    }

    private FuncInvokeData ParseFuncInvokeLine(string line)
    {
        line = line.Trim();
        
        // 查找左括号的位置
        int leftParenIndex = line.IndexOf('(');
        int rightParenIndex = line.LastIndexOf(')');
        
        if (leftParenIndex == -1 || rightParenIndex == -1 || rightParenIndex <= leftParenIndex)
        {
            return null;
        }

        // 提取类型名
        string typeName = line.Substring(0, leftParenIndex).Trim();
        
        // 提取参数部分
        string paramsStr = line.Substring(leftParenIndex + 1, rightParenIndex - leftParenIndex - 1).Trim();
        
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

// 用于表示funcInvoke格式数据的类
public class FuncInvokeData
{
    public string TypeName { get; }
    public Dictionary<string, string> Parameters { get; }

    public FuncInvokeData(string typeName, Dictionary<string, string> parameters)
    {
        TypeName = typeName;
        Parameters = parameters;
    }
}

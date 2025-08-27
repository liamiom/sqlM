namespace sqlM.ResultClassTypes;

public struct Column
{
    public string DataType;
    public string NullFlag;
    public string ColumnName;
    public int Index;
    public string DefaultValue;
    public string FullDataType => $"{DataType}{NullFlag}";
    public bool IsKey;
    public bool IsIdentity;
}

namespace sqlM.ResultClassTypes;

internal struct Column
{
    public string DataType;
    public readonly string NullFlag => AllowNull ? "?" : "";
    public required bool AllowNull;
    public string ColumnName;
    public int Index;
    public string DefaultValue;
    public bool IsIdentity;
    public readonly string FullDataType => $"{DataType}{NullFlag}";
    public readonly string Required => !AllowNull && !IsIdentity && string.IsNullOrWhiteSpace(DefaultValue) ? "required " : "";
}
